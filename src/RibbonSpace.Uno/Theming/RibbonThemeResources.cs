using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Theming;
using Windows.UI;

namespace RibbonSpace.Controls;

/// <summary>
/// Theme resources (Light, Dark, HighContrast) used by every RibbonSpace control. Merged automatically into
/// <c>Application.Current.Resources</c> by <see cref="RibbonTheme.EnsureResources"/>; you may also merge it
/// explicitly in App.xaml (<c>&lt;rs:RibbonThemeResources /&gt;</c>). Brush instances are mutated in place when the
/// palette changes, so accent changes apply live without re-templating.
/// </summary>
public sealed partial class RibbonThemeResources : ResourceDictionary
{
    /// <summary>All brush keys defined by RibbonSpace.</summary>
    public static IReadOnlyList<string> BrushKeys { get; } =
    [
        "RibbonChromeBackgroundBrush", "RibbonChromeForegroundBrush", "RibbonCommandBarBackgroundBrush", "RibbonCommandBarBorderBrush",
        "RibbonForegroundBrush", "RibbonSecondaryForegroundBrush", "RibbonDisabledForegroundBrush", "RibbonIconBrush",
        "RibbonAccentBrush", "RibbonAccentForegroundBrush", "RibbonAccentSubtleBrush", "RibbonAccentSubtleStrongBrush", "RibbonAccentTextBrush",
        "RibbonItemHoverBrush", "RibbonItemPressedBrush", "RibbonItemCheckedBrush", "RibbonItemCheckedHoverBrush", "RibbonItemCheckedBorderBrush",
        "RibbonItemBorderHoverBrush", "RibbonSeparatorBrush", "RibbonTabForegroundBrush", "RibbonTabHoverBrush", "RibbonTabSelectedForegroundBrush",
        "RibbonTabIndicatorBrush", "RibbonPopupBackgroundBrush", "RibbonPopupBorderBrush", "RibbonInputBackgroundBrush", "RibbonInputBorderBrush",
        "RibbonInputHoverBorderBrush", "RibbonInputFocusBorderBrush", "RibbonKeyTipBackgroundBrush", "RibbonKeyTipForegroundBrush", "RibbonKeyTipBorderBrush",
        "RibbonTitleBarBackgroundBrush", "RibbonTitleBarForegroundBrush", "RibbonTitleBarHoverBrush", "RibbonTitleBarIconBrush", "RibbonBackstagePaneBackgroundBrush",
        "RibbonBackstagePaneForegroundBrush", "RibbonBackstagePaneHoverBrush", "RibbonBackstagePaneSelectedBrush", "RibbonBackstageContentBackgroundBrush",
        "RibbonGalleryItemBorderBrush", "RibbonGalleryItemSelectedBorderBrush", "RibbonScreenTipBackgroundBrush", "RibbonScreenTipBorderBrush",
        "RibbonFocusBrush", "RibbonWindowBackgroundBrush", "RibbonStatusBarBackgroundBrush", "RibbonStatusBarForegroundBrush", "RibbonToolBarBackgroundBrush",
        "RibbonSearchBackgroundBrush", "RibbonSearchBorderBrush", "RibbonSwatchBorderBrush", "RibbonShadowBrush", "RibbonScrollButtonBackgroundBrush",
    ];

    private readonly Dictionary<string, SolidColorBrush> _light = [];
    private readonly Dictionary<string, SolidColorBrush> _dark = [];
    private readonly Dictionary<string, SolidColorBrush> _highContrast = [];

    /// <summary>Creates the resources with the Word palette and neutral chrome.</summary>
    public RibbonThemeResources()
    {
        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();
        var highContrast = new ResourceDictionary();
        foreach (var key in BrushKeys)
        {
            light[key] = _light[key] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            dark[key] = _dark[key] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            highContrast[key] = _highContrast[key] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        foreach (var dictionary in new[] { light, dark, highContrast })
        {
            dictionary["RibbonControlCornerRadius"] = new CornerRadius(4);
            dictionary["RibbonCommandBarCornerRadius"] = new CornerRadius(8);
            dictionary["RibbonPopupCornerRadius"] = new CornerRadius(6);
            dictionary["RibbonFontFamily"] = FontFamily.XamlAutoFontFamily;
        }

        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///RibbonSpace.Uno/Themes/Shared.xaml") });
        ThemeDictionaries["Light"] = light;
        ThemeDictionaries["Default"] = light;
        ThemeDictionaries["Dark"] = dark;
        ThemeDictionaries["HighContrast"] = highContrast;
        Apply(RibbonThemePalette.Word, RibbonChromeStyle.Neutral);
    }

    /// <summary>Current palette.</summary>
    public RibbonThemePalette Palette { get; private set; } = RibbonThemePalette.Word;

    /// <summary>Current chrome style.</summary>
    public RibbonChromeStyle ChromeStyle { get; private set; }

    /// <summary>Returns the brush instance of a key for a theme ("Light", "Dark", "HighContrast").</summary>
    public SolidColorBrush? GetBrush(string key, string theme = "Light")
    {
        var map = theme switch { "Dark" => _dark, "HighContrast" => _highContrast, _ => _light };
        return map.GetValueOrDefault(key);
    }

    /// <summary>Overrides one brush color for one theme (in place).</summary>
    public void SetColor(string key, Color color, string theme = "Light")
    {
        if (GetBrush(key, theme) is { } brush)
        {
            brush.Color = color;
        }
    }

    /// <summary>Applies a palette and chrome style (in place; all controls update immediately).</summary>
    public void Apply(RibbonThemePalette palette, RibbonChromeStyle chromeStyle)
    {
        Palette = palette ?? throw new ArgumentNullException(nameof(palette));
        ChromeStyle = chromeStyle;
        ApplyLight(palette, chromeStyle);
        ApplyDark(palette, chromeStyle);
        ApplyHighContrast();
    }

    private static Color C(RibbonColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    private static Color C(string hex) => C(RibbonColor.Parse(hex));

    private void Set(Dictionary<string, SolidColorBrush> map, string key, Color color) => map[key].Color = color;

    private void ApplyLight(RibbonThemePalette palette, RibbonChromeStyle chrome)
    {
        var m = _light;
        var accent = palette.GetAccent(false);
        var colorful = chrome == RibbonChromeStyle.Colorful;
        Set(m, "RibbonWindowBackgroundBrush", C("#F5F5F5"));
        Set(m, "RibbonChromeBackgroundBrush", colorful ? C(accent) : C("#F5F5F5"));
        Set(m, "RibbonChromeForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C("#242424"));
        Set(m, "RibbonCommandBarBackgroundBrush", C("#FFFFFF"));
        Set(m, "RibbonCommandBarBorderBrush", C("#E0E0E0"));
        Set(m, "RibbonForegroundBrush", C("#242424"));
        Set(m, "RibbonSecondaryForegroundBrush", C("#616161"));
        Set(m, "RibbonDisabledForegroundBrush", C("#BDBDBD"));
        Set(m, "RibbonIconBrush", C("#424242"));
        Set(m, "RibbonAccentBrush", C(accent));
        Set(m, "RibbonAccentForegroundBrush", C(palette.GetOnAccent(false)));
        Set(m, "RibbonAccentSubtleBrush", C(palette.GetAccentSubtle(false)));
        Set(m, "RibbonAccentSubtleStrongBrush", C(palette.GetAccentSubtleStrong(false)));
        Set(m, "RibbonAccentTextBrush", C(accent.Darken(0.1)));
        Set(m, "RibbonItemHoverBrush", C("#F0F0F0"));
        Set(m, "RibbonItemPressedBrush", C("#E0E0E0"));
        Set(m, "RibbonItemCheckedBrush", C(palette.GetAccentSubtle(false)));
        Set(m, "RibbonItemCheckedHoverBrush", C(palette.GetAccentSubtleStrong(false)));
        Set(m, "RibbonItemCheckedBorderBrush", C(accent.Lighten(0.55)));
        Set(m, "RibbonItemBorderHoverBrush", C("#00000000"));
        Set(m, "RibbonSeparatorBrush", C("#E0E0E0"));
        Set(m, "RibbonTabForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C("#424242"));
        Set(m, "RibbonTabHoverBrush", colorful ? C(accent.Lighten(0.15)) : C("#E8E8E8"));
        Set(m, "RibbonTabSelectedForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C(accent));
        Set(m, "RibbonTabIndicatorBrush", colorful ? C(palette.GetOnAccent(false)) : C(accent));
        Set(m, "RibbonPopupBackgroundBrush", C("#FFFFFF"));
        Set(m, "RibbonPopupBorderBrush", C("#D1D1D1"));
        Set(m, "RibbonInputBackgroundBrush", C("#FFFFFF"));
        Set(m, "RibbonInputBorderBrush", C("#D1D1D1"));
        Set(m, "RibbonInputHoverBorderBrush", C("#A6A6A6"));
        Set(m, "RibbonInputFocusBorderBrush", C(accent));
        Set(m, "RibbonKeyTipBackgroundBrush", C("#3B3B3B"));
        Set(m, "RibbonKeyTipForegroundBrush", C("#FFFFFF"));
        Set(m, "RibbonKeyTipBorderBrush", C("#1F1F1F"));
        Set(m, "RibbonTitleBarBackgroundBrush", colorful ? C(accent) : C("#F5F5F5"));
        Set(m, "RibbonTitleBarForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C("#242424"));
        Set(m, "RibbonTitleBarHoverBrush", colorful ? C(accent.Lighten(0.15)) : C("#E8E8E8"));
        Set(m, "RibbonTitleBarIconBrush", colorful ? C(palette.GetOnAccent(false)) : C(accent));
        Set(m, "RibbonBackstagePaneBackgroundBrush", C(accent));
        Set(m, "RibbonBackstagePaneForegroundBrush", C(palette.GetOnAccent(false)));
        Set(m, "RibbonBackstagePaneHoverBrush", C(accent.Lighten(0.15)));
        Set(m, "RibbonBackstagePaneSelectedBrush", C(accent.Darken(0.25)));
        Set(m, "RibbonBackstageContentBackgroundBrush", C("#FFFFFF"));
        Set(m, "RibbonGalleryItemBorderBrush", C("#E0E0E0"));
        Set(m, "RibbonGalleryItemSelectedBorderBrush", C(accent));
        Set(m, "RibbonScreenTipBackgroundBrush", C("#FFFFFF"));
        Set(m, "RibbonScreenTipBorderBrush", C("#C7C7C7"));
        Set(m, "RibbonFocusBrush", C("#000000"));
        Set(m, "RibbonStatusBarBackgroundBrush", C("#F5F5F5"));
        Set(m, "RibbonStatusBarForegroundBrush", C("#424242"));
        Set(m, "RibbonToolBarBackgroundBrush", C("#FAFAFA"));
        Set(m, "RibbonSearchBackgroundBrush", colorful ? C(accent.Lighten(0.2)) : C("#FFFFFF"));
        Set(m, "RibbonSearchBorderBrush", colorful ? C(accent.Lighten(0.3)) : C("#D1D1D1"));
        Set(m, "RibbonSwatchBorderBrush", C("#33000000"));
        Set(m, "RibbonShadowBrush", C("#1A000000"));
        Set(m, "RibbonScrollButtonBackgroundBrush", C("#F2FFFFFF"));
    }

    private void ApplyDark(RibbonThemePalette palette, RibbonChromeStyle chrome)
    {
        var m = _dark;
        var accent = palette.GetAccent(true);
        var colorful = chrome == RibbonChromeStyle.Colorful;
        var baseAccent = palette.GetAccent(false);
        Set(m, "RibbonWindowBackgroundBrush", C("#1F1F1F"));
        Set(m, "RibbonChromeBackgroundBrush", colorful ? C(baseAccent.Darken(0.35)) : C("#1F1F1F"));
        Set(m, "RibbonChromeForegroundBrush", C("#FFFFFF"));
        Set(m, "RibbonCommandBarBackgroundBrush", C("#292929"));
        Set(m, "RibbonCommandBarBorderBrush", C("#3D3D3D"));
        Set(m, "RibbonForegroundBrush", C("#FFFFFF"));
        Set(m, "RibbonSecondaryForegroundBrush", C("#C7C7C7"));
        Set(m, "RibbonDisabledForegroundBrush", C("#6E6E6E"));
        Set(m, "RibbonIconBrush", C("#E0E0E0"));
        Set(m, "RibbonAccentBrush", C(accent));
        Set(m, "RibbonAccentForegroundBrush", C(palette.GetOnAccent(true)));
        Set(m, "RibbonAccentSubtleBrush", C(palette.GetAccentSubtle(true)));
        Set(m, "RibbonAccentSubtleStrongBrush", C(palette.GetAccentSubtleStrong(true)));
        Set(m, "RibbonAccentTextBrush", C(accent));
        Set(m, "RibbonItemHoverBrush", C("#383838"));
        Set(m, "RibbonItemPressedBrush", C("#454545"));
        Set(m, "RibbonItemCheckedBrush", C(palette.GetAccentSubtle(true)));
        Set(m, "RibbonItemCheckedHoverBrush", C(palette.GetAccentSubtleStrong(true)));
        Set(m, "RibbonItemCheckedBorderBrush", C(accent.WithAlpha(0x88)));
        Set(m, "RibbonItemBorderHoverBrush", C("#00000000"));
        Set(m, "RibbonSeparatorBrush", C("#454545"));
        Set(m, "RibbonTabForegroundBrush", C("#D6D6D6"));
        Set(m, "RibbonTabHoverBrush", C("#2E2E2E"));
        Set(m, "RibbonTabSelectedForegroundBrush", C(accent));
        Set(m, "RibbonTabIndicatorBrush", C(accent));
        Set(m, "RibbonPopupBackgroundBrush", C("#2B2B2B"));
        Set(m, "RibbonPopupBorderBrush", C("#474747"));
        Set(m, "RibbonInputBackgroundBrush", C("#1F1F1F"));
        Set(m, "RibbonInputBorderBrush", C("#5C5C5C"));
        Set(m, "RibbonInputHoverBorderBrush", C("#8A8A8A"));
        Set(m, "RibbonInputFocusBorderBrush", C(accent));
        Set(m, "RibbonKeyTipBackgroundBrush", C("#F0F0F0"));
        Set(m, "RibbonKeyTipForegroundBrush", C("#1F1F1F"));
        Set(m, "RibbonKeyTipBorderBrush", C("#FFFFFF"));
        Set(m, "RibbonTitleBarBackgroundBrush", colorful ? C(baseAccent.Darken(0.35)) : C("#1F1F1F"));
        Set(m, "RibbonTitleBarForegroundBrush", C("#FFFFFF"));
        Set(m, "RibbonTitleBarHoverBrush", C("#383838"));
        Set(m, "RibbonTitleBarIconBrush", colorful ? C("#FFFFFF") : C(accent));
        Set(m, "RibbonBackstagePaneBackgroundBrush", C("#141414"));
        Set(m, "RibbonBackstagePaneForegroundBrush", C("#FFFFFF"));
        Set(m, "RibbonBackstagePaneHoverBrush", C("#2E2E2E"));
        Set(m, "RibbonBackstagePaneSelectedBrush", C(accent.WithAlpha(0x55)));
        Set(m, "RibbonBackstageContentBackgroundBrush", C("#1F1F1F"));
        Set(m, "RibbonGalleryItemBorderBrush", C("#454545"));
        Set(m, "RibbonGalleryItemSelectedBorderBrush", C(accent));
        Set(m, "RibbonScreenTipBackgroundBrush", C("#2B2B2B"));
        Set(m, "RibbonScreenTipBorderBrush", C("#5C5C5C"));
        Set(m, "RibbonFocusBrush", C("#FFFFFF"));
        Set(m, "RibbonStatusBarBackgroundBrush", C("#1F1F1F"));
        Set(m, "RibbonStatusBarForegroundBrush", C("#D6D6D6"));
        Set(m, "RibbonToolBarBackgroundBrush", C("#262626"));
        Set(m, "RibbonSearchBackgroundBrush", C("#2E2E2E"));
        Set(m, "RibbonSearchBorderBrush", C("#474747"));
        Set(m, "RibbonSwatchBorderBrush", C("#44FFFFFF"));
        Set(m, "RibbonShadowBrush", C("#66000000"));
        Set(m, "RibbonScrollButtonBackgroundBrush", C("#F2292929"));
    }

    /// <summary>
    /// High Contrast colours follow the system theme (window, text, highlight, gray text, hot-light...) when the
    /// platform exposes them, with a black / white / cyan fallback. Hover, pressed and checked states use solid fills
    /// mixed from the window and highlight colours (still readable with the window text colour) plus highlight borders.
    /// </summary>
    private void ApplyHighContrast()
    {
        var m = _highContrast;
        var bg = SystemColor("SystemColorWindowColor", Windows.UI.ViewManagement.UIElementType.Window, C("#000000"));
        var fg = SystemColor("SystemColorWindowTextColor", Windows.UI.ViewManagement.UIElementType.WindowText, C("#FFFFFF"));
        var hl = SystemColor("SystemColorHighlightColor", Windows.UI.ViewManagement.UIElementType.Highlight, C("#1AEBFF"));
        var hlText = SystemColor("SystemColorHighlightTextColor", Windows.UI.ViewManagement.UIElementType.HighlightText, C("#000000"));
        var buttonFace = SystemColor("SystemColorButtonFaceColor", Windows.UI.ViewManagement.UIElementType.ButtonFace, bg);
        var disabled = SystemColor("SystemColorGrayTextColor", Windows.UI.ViewManagement.UIElementType.GrayText, C("#3FF23F"));
        var hotlight = SystemColor("SystemColorHotlightColor", Windows.UI.ViewManagement.UIElementType.Hotlight, C("#FFFF00"));
        foreach (var key in BrushKeys)
        {
            Set(m, key, key.Contains("Background", StringComparison.Ordinal) ? bg : fg);
        }

        foreach (var key in new[] { "RibbonAccentBrush", "RibbonTabIndicatorBrush", "RibbonInputFocusBorderBrush", "RibbonGalleryItemSelectedBorderBrush", "RibbonItemCheckedBorderBrush", "RibbonTabSelectedForegroundBrush", "RibbonItemBorderHoverBrush", "RibbonFocusBrush" })
        {
            Set(m, key, hl);
        }

        // Interaction fills are opaque and strong enough to see, but keep contrast with the (unchanged) text colour.
        var hover = Mix(bg, hl, 0.3);
        var checkedFill = Mix(bg, hl, 0.4);
        var pressed = Mix(bg, hl, 0.5);
        Set(m, "RibbonCommandBarBackgroundBrush", buttonFace);
        Set(m, "RibbonAccentForegroundBrush", hlText);
        Set(m, "RibbonAccentTextBrush", hotlight);
        Set(m, "RibbonAccentSubtleBrush", checkedFill);
        Set(m, "RibbonAccentSubtleStrongBrush", pressed);
        Set(m, "RibbonItemHoverBrush", hover);
        Set(m, "RibbonItemPressedBrush", pressed);
        Set(m, "RibbonItemCheckedBrush", checkedFill);
        Set(m, "RibbonItemCheckedHoverBrush", pressed);
        Set(m, "RibbonTabHoverBrush", hover);
        Set(m, "RibbonTitleBarHoverBrush", hover);
        Set(m, "RibbonBackstagePaneHoverBrush", hover);
        Set(m, "RibbonBackstagePaneSelectedBrush", pressed);
        Set(m, "RibbonDisabledForegroundBrush", disabled);
        Set(m, "RibbonKeyTipBackgroundBrush", hl);
        Set(m, "RibbonKeyTipForegroundBrush", hlText);
        Set(m, "RibbonKeyTipBorderBrush", fg);
        Set(m, "RibbonShadowBrush", C("#00000000"));
        Set(m, "RibbonSwatchBorderBrush", fg);
        Set(m, "RibbonScrollButtonBackgroundBrush", bg);
        EnsureHighContrastTracking();
    }

    private static Color Mix(Color a, Color b, double amount)
    {
        byte Lerp(byte x, byte y) => (byte)Math.Round(x + ((y - x) * amount));
        return Color.FromArgb(0xFF, Lerp(a.R, b.R), Lerp(a.G, b.G), Lerp(a.B, b.B));
    }

    private static bool IsHighContrastActive()
    {
        try
        {
            return new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast;
        }
        catch (Exception ex) when (ex is NotImplementedException or NotSupportedException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }

    private static Color SystemColor(string resourceKey, Windows.UI.ViewManagement.UIElementType type, Color fallback)
    {
        // 1. System colours of the active High Contrast theme.
        if (IsHighContrastActive())
        {
            try
            {
                var color = new Windows.UI.ViewManagement.UISettings().UIElementColor(type);
                if (color.A != 0)
                {
                    return color;
                }
            }
            catch (Exception ex) when (ex is NotImplementedException or NotSupportedException or InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Not available on this platform.
            }
        }

        // 2. SystemColor* resources (WinUI defines them for the HighContrast theme).
        try
        {
            if (Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true && value is Color resource && resource.A != 0)
            {
                return resource;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            // Resources not ready yet.
        }

        return fallback;
    }

    private Windows.UI.ViewManagement.AccessibilitySettings? _accessibility;

    private void EnsureHighContrastTracking()
    {
        if (_accessibility is not null)
        {
            return;
        }

        try
        {
            var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _accessibility = new Windows.UI.ViewManagement.AccessibilitySettings();
            _accessibility.HighContrastChanged += (_, _) =>
            {
                // Brushes are mutated in place, so every control updates; marshal to the UI thread.
                if (queue is null || queue.HasThreadAccess)
                {
                    ApplyHighContrast();
                }
                else
                {
                    queue.TryEnqueue(ApplyHighContrast);
                }
            };
        }
        catch (Exception ex) when (ex is NotImplementedException or NotSupportedException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            _accessibility = null;
        }
    }
}
