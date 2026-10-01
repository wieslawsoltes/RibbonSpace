using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using RibbonSpace.Controls;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace RibbonSpace.Demo;

/// <summary>
/// Screenshot automation used for documentation and visual regression checks:
/// RIBBONSPACE_CAPTURE=out.png RIBBONSPACE_PAGE=word|excel|powerpoint|tools|settings RIBBONSPACE_WIDTH=1400
/// RIBBONSPACE_ACTION=comma-separated steps (see docs/testing.md), RIBBONSPACE_THEME=Light|Dark.
/// </summary>
internal static class Capture
{
    public static async Task TryRunAsync(Window window, ShellPage shell)
    {
        var path = Environment.GetEnvironmentVariable("RIBBONSPACE_CAPTURE");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        await Task.Delay(600);
        if (int.TryParse(Environment.GetEnvironmentVariable("RIBBONSPACE_WIDTH"), out var width))
        {
            var height = int.TryParse(Environment.GetEnvironmentVariable("RIBBONSPACE_HEIGHT"), out var h) ? h : 820;
            var scale = window.Content?.XamlRoot?.RasterizationScale ?? 1;
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = (int)(width * scale), Height = (int)(height * scale) });
        }

        if (Environment.GetEnvironmentVariable("RIBBONSPACE_THEME") == "Dark")
        {
            shell.RequestedTheme = ElementTheme.Dark;
        }

        await Task.Delay(400);
        if (Environment.GetEnvironmentVariable("RIBBONSPACE_PAGE") is { Length: > 0 } page)
        {
            shell.Show(page);
        }

        await Task.Delay(1400);
        var ribbon = FindRibbon(shell.CurrentContent);
        try
        {
            await RunActionAsync(Environment.GetEnvironmentVariable("RIBBONSPACE_ACTION"), ribbon, shell);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[capture] action failed: " + ex);
        }

        await Task.Delay(900);
        var root = (UIElement)window.Content!;
        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(root);
        var pixels = (await rtb.GetPixelsAsync()).ToArray();
        var pixelScale = rtb.PixelWidth / Math.Max(1, root.ActualSize.X);
        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot))
        {
            if (popup.Child is not FrameworkElement child || child.ActualWidth < 1)
            {
                continue;
            }

            try
            {
                var popupBitmap = new RenderTargetBitmap();
                await popupBitmap.RenderAsync(child);
                var popupPixels = (await popupBitmap.GetPixelsAsync()).ToArray();
                var origin = child.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
                Blend(pixels, rtb.PixelWidth, rtb.PixelHeight, popupPixels, popupBitmap.PixelWidth, popupBitmap.PixelHeight, (int)Math.Round(origin.X * pixelScale), (int)Math.Round(origin.Y * pixelScale));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[capture] popup render failed: " + ex.Message);
            }
        }

        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)rtb.PixelWidth, (uint)rtb.PixelHeight, 96, 96, pixels);
        await encoder.FlushAsync();
        stream.Seek(0);
        var bytes = new byte[stream.Size];
        await stream.AsStreamForRead().ReadExactlyAsync(bytes);
        await File.WriteAllBytesAsync(path, bytes);
        Environment.Exit(0);
    }

    private static async Task RunActionAsync(string? action, Ribbon? ribbon, ShellPage shell)
    {
        if (ribbon is null || string.IsNullOrEmpty(action))
        {
            return;
        }

        foreach (var step in action.Split(','))
        {
            switch (step.Trim())
            {
                case "keytips":
                    ribbon.ShowKeyTips();
                    break;
                case "keytips-home":
                    ribbon.ShowKeyTips();
                    await Task.Delay(300);
                    ribbon.SelectTab("home");
                    ribbon.HideKeyTips();
                    ribbon.ShowKeyTips();
                    await Task.Delay(200);
                    SimulateKeyTip(ribbon, 'H');
                    break;
                case "backstage":
                    ribbon.IsBackstageOpen = true;
                    break;
                case "simplified":
                    ribbon.DisplayMode = RibbonDisplayMode.Simplified;
                    break;
                case "classic":
                    ribbon.DisplayMode = RibbonDisplayMode.Classic;
                    break;
                case "overflow":
                    await Task.Delay(400);
                    ribbon.SelectedTab?.ShowOverflow();
                    break;
                case "minimized":
                    ribbon.VisibilityMode = RibbonVisibilityMode.TabsOnly;
                    break;
                case "minimized-popup":
                    ribbon.VisibilityMode = RibbonVisibilityMode.TabsOnly;
                    await Task.Delay(300);
                    ribbon.OpenMinimizedPopup();
                    break;
                case "fullscreen":
                    ribbon.VisibilityMode = RibbonVisibilityMode.FullScreen;
                    break;
                case "contextual":
                    ribbon.SetActiveContextualGroups("table", "chart");
                    break;
                case "insert":
                    ribbon.SelectTab("insert");
                    break;
                case "design":
                    ribbon.SelectTab("design");
                    break;
                case "layout":
                    ribbon.SelectTab("layout");
                    break;
                case "view":
                    ribbon.SelectTab("view");
                    break;
                case "gallery":
                    await Task.Delay(300);
                    (ribbon.FindItem("stylesGallery") as RibbonGallery ?? ribbon.GetAllItems().OfType<RibbonGallery>().FirstOrDefault())?.OpenDropDown();
                    break;
                case "colorpicker":
                    (ribbon.FindItem("fontColor") as RibbonColorPicker)?.OpenDropDown();
                    break;
                case "paste":
                    (ribbon.FindItem("paste") as RibbonSplitButton)?.OpenDropDown();
                    break;
                case "qat-below":
                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                    break;
                case "search":
                    FindDescendant<RibbonSearchBox>(shell.CurrentContent)?.FocusSearch();
                    await Task.Delay(200);
                    if (FindDescendant<RibbonSearchBox>(shell.CurrentContent) is { } box && FindDescendant<TextBox>(box) is { } text)
                    {
                        text.Text = "font col";
                    }

                    break;
                case "palette":
                    RibbonCommandPalette.Show(ribbon);
                    break;
                case "customize":
                    ribbon.ShowCustomizeDialog(RibbonCustomizePage.Ribbon);
                    break;
                case "customize-qat":
                    ribbon.ShowCustomizeDialog(RibbonCustomizePage.QuickAccessToolbar);
                    break;
                case "add-qat":
                    ribbon.AddToQuickAccess("bold");
                    ribbon.AddToQuickAccess("paste");
                    break;
                case "collapsed-group":
                    await Task.Delay(300);
                    ribbon.SelectedTab?.Groups.FirstOrDefault(g => g.State == RibbonGroupState.Collapsed)?.OpenPopup();
                    break;
                case "touch":
                    ribbon.Density = RibbonDensity.Touch;
                    break;
                case "compact":
                    ribbon.Density = RibbonDensity.Compact;
                    break;
                case "colorful":
                    RibbonTheme.ApplyChromeStyle(RibbonSpace.Theming.RibbonChromeStyle.Colorful);
                    break;
                case "dark":
                    shell.RequestedTheme = ElementTheme.Dark;
                    break;
            }

            await Task.Delay(500);
        }
    }

    private static void Blend(byte[] target, int tw, int th, byte[] source, int sw, int sh, int ox, int oy)
    {
        for (var y = 0; y < sh; y++)
        {
            var ty = y + oy;
            if (ty < 0 || ty >= th)
            {
                continue;
            }

            for (var x = 0; x < sw; x++)
            {
                var tx = x + ox;
                if (tx < 0 || tx >= tw)
                {
                    continue;
                }

                var si = ((y * sw) + x) * 4;
                var ti = ((ty * tw) + tx) * 4;
                var a = source[si + 3] / 255.0;
                if (a <= 0)
                {
                    continue;
                }

                for (var c = 0; c < 3; c++)
                {
                    target[ti + c] = (byte)Math.Round(source[si + c] + (target[ti + c] * (1 - a)));
                }

                target[ti + 3] = 255;
            }
        }
    }

    private static void SimulateKeyTip(Ribbon ribbon, char key)
    {
        var method = typeof(Ribbon).GetMethod("ActivateKeyTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var header = ribbon.TabHeaders.FirstOrDefault(h => h.Tab.EffectiveIdPublic() == "home");
        if (method is not null && header is not null)
        {
            method.Invoke(ribbon, [header]);
        }
    }

    private static string? EffectiveIdPublic(this RibbonTab tab) => tab.Id ?? tab.Header;

    private static Ribbon? FindRibbon(DependencyObject? root) => FindDescendant<Ribbon>(root);

    private static T? FindDescendant<T>(DependencyObject? root)
        where T : class
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            if (FindDescendant<T>(VisualTreeHelper.GetChild(root, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
