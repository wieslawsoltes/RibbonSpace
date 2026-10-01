using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Controls;
using RibbonSpace.Localization;
using RibbonSpace.Theming;

namespace RibbonSpace.Demo.Pages;

/// <summary>Runtime theming, density, layout, localization and state persistence.</summary>
public sealed partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        Palette.Items.Add("Per application (default)");
        foreach (var preset in RibbonThemePalette.Presets)
        {
            Palette.Items.Add(preset.Name);
        }

        Palette.SelectedIndex = 0;
        Theme.SelectionChanged += (_, _) =>
        {
            if (XamlRoot?.Content is FrameworkElement root)
            {
                root.RequestedTheme = Theme.SelectedIndex switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
            }
        };
        Palette.SelectionChanged += (_, _) =>
        {
            DemoSettings.PaletteOverride = Palette.SelectedIndex <= 0 ? null : RibbonThemePalette.Presets[Palette.SelectedIndex - 1];
            RibbonTheme.ApplyPalette(DemoSettings.PaletteOverride ?? RibbonThemePalette.Word);
        };
        Chrome.SelectionChanged += (_, _) => RibbonTheme.ApplyChromeStyle(Chrome.SelectedIndex == 1 ? RibbonChromeStyle.Colorful : RibbonChromeStyle.Neutral);
        Density.SelectionChanged += (_, _) => ApplyLayout();
        Layout.SelectionChanged += (_, _) => ApplyLayout();
        Language.SelectionChanged += (_, _) => RibbonStrings.Current = RibbonStrings.ForCulture(new CultureInfo((string)Language.SelectedItem));
        ShowState.Click += (_, _) => StateJson.Text = DemoSettings.WordRibbon?.SaveStateToJson() ?? "Open the Word page first.";
        ResetCustomization.Click += (_, _) => DemoSettings.WordRibbon?.ResetCustomization();
        Customize.Click += (_, _) => DemoSettings.WordRibbon?.ShowCustomizeDialog(RibbonCustomizePage.Ribbon);
        Palette2.Click += (_, _) =>
        {
            if (DemoSettings.WordRibbon is { } ribbon)
            {
                RibbonCommandPalette.Show(ribbon);
            }
        };
        DemoSettings.Logged += (_, _) => LogText.Text = string.Join(Environment.NewLine, DemoSettings.Log.Take(40));
        Loaded += (_, _) => LogText.Text = string.Join(Environment.NewLine, DemoSettings.Log.Take(40));
    }

    private void ApplyLayout() => DemoSettings.Apply(
        (RibbonDensity)Math.Max(0, Density.SelectedIndex),
        Layout.SelectedIndex switch { 1 => RibbonDisplayMode.Classic, 2 => RibbonDisplayMode.Simplified, _ => null });
}
