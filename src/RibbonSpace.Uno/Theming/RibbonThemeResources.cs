using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Theming;
using Windows.UI;

namespace RibbonSpace.Controls;

/// <summary>
/// Theme resources (Light, Dark, HighContrast) used by every RibbonSpace control. Merged automatically into
/// <c>Application.Current.Resources</c> by <see cref="RibbonTheme.EnsureResources"/>; you may also merge it
/// explicitly in App.xaml (<c>&lt;RibbonThemeResources /&gt;</c>). Brush instances are mutated in place when the
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
        "RibbonGroupCaptionBackgroundBrush", "RibbonGroupCaptionForegroundBrush", "RibbonTabSelectedBackgroundBrush", "RibbonFloatingPanelBarBrush",
    ];

    /// <summary>
    /// Shape resources (corner radii, margins, thickness) that differ between <see cref="RibbonThemeStyle"/> values.
    /// They are read when a control is templated, so a style change applies to controls created afterwards (brushes
    /// update live).
    /// </summary>
    public static IReadOnlyList<string> ShapeKeys { get; } =
    [
        "RibbonControlCornerRadius", "RibbonCommandBarCornerRadius", "RibbonPopupCornerRadius", "RibbonTabCornerRadius", "RibbonGroupCaptionCornerRadius",
        "RibbonCommandBarMargin", "RibbonCommandBarBorderThickness", "RibbonTabMargin", "RibbonTabRowPadding", "RibbonGroupCaptionMargin",
    ];

    private readonly Dictionary<string, SolidColorBrush> _light = [];
    private readonly Dictionary<string, SolidColorBrush> _dark = [];
    private readonly Dictionary<string, SolidColorBrush> _highContrast = [];
    private readonly ResourceDictionary[] _themeDictionaries;

    /// <summary>Creates the resources with the Word palette and neutral chrome.</summary>
    public RibbonThemeResources()
    {
        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();
        var highContrast = new ResourceDictionary();

        // "Default" (the fallback theme) shares the Light brush instances. It needs a dictionary of its own: WinUI
        // rejects one ResourceDictionary under two ThemeDictionaries keys.
        var fallback = new ResourceDictionary();
        foreach (var key in BrushKeys)
        {
            light[key] = fallback[key] = _light[key] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            dark[key] = _dark[key] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            highContrast[key] = _highContrast[key] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        _themeDictionaries = [light, fallback, dark, highContrast];
        foreach (var dictionary in _themeDictionaries)
        {
            dictionary["RibbonFontFamily"] = FontFamily.XamlAutoFontFamily;
        }

        ApplyShapes(RibbonThemeStyle.Office);

        // Resource URIs carry the assembly name: RibbonSpace.Uno, or RibbonSpace.WinUI for the Windows App SDK build.
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"ms-appx:///{typeof(RibbonThemeResources).Assembly.GetName().Name}/Themes/Shared.xaml") });
        ThemeDictionaries["Light"] = light;
        ThemeDictionaries["Default"] = fallback;
        ThemeDictionaries["Dark"] = dark;
        ThemeDictionaries["HighContrast"] = highContrast;
        Apply(RibbonThemePalette.Word, RibbonChromeStyle.Neutral);
    }

    /// <summary>Current palette.</summary>
    public RibbonThemePalette Palette { get; private set; } = RibbonThemePalette.Word;

    /// <summary>Current chrome style.</summary>
    public RibbonChromeStyle ChromeStyle { get; private set; }

    /// <summary>Current surface style (Office or CAD).</summary>
    public RibbonThemeStyle Style { get; private set; }

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

    /// <summary>Applies a palette and chrome style keeping the current surface style (in place; all controls update immediately).</summary>
    public void Apply(RibbonThemePalette palette, RibbonChromeStyle chromeStyle) => Apply(palette, chromeStyle, Style);

    /// <summary>Applies a palette, chrome style and surface style. Brushes update in place.</summary>
    public void Apply(RibbonThemePalette palette, RibbonChromeStyle chromeStyle, RibbonThemeStyle style)
    {
        Palette = palette ?? throw new ArgumentNullException(nameof(palette));
        ChromeStyle = chromeStyle;
        if (Style != style)
        {
            Style = style;
            ApplyShapes(style);
        }

        ApplyLight(palette, chromeStyle);
        ApplyDark(palette, chromeStyle);
        if (style == RibbonThemeStyle.Cad)
        {
            ApplyCadLight(palette, chromeStyle);
            ApplyCadDark(palette, chromeStyle);
        }

        ApplyHighContrast();
    }

    private void ApplyShapes(RibbonThemeStyle style)
    {
        var cad = style == RibbonThemeStyle.Cad;
        foreach (var dictionary in _themeDictionaries)
        {
            dictionary["RibbonControlCornerRadius"] = new CornerRadius(cad ? 2 : 4);
            dictionary["RibbonCommandBarCornerRadius"] = new CornerRadius(cad ? 0 : 8);
            dictionary["RibbonPopupCornerRadius"] = new CornerRadius(cad ? 2 : 6);
            dictionary["RibbonTabCornerRadius"] = cad ? new CornerRadius(2, 2, 0, 0) : new CornerRadius(4);
            dictionary["RibbonGroupCaptionCornerRadius"] = new CornerRadius(cad ? 1 : 0);
            dictionary["RibbonCommandBarMargin"] = cad ? new Thickness(0) : new Thickness(6, 1, 6, 6);
            dictionary["RibbonCommandBarBorderThickness"] = cad ? new Thickness(0, 0, 0, 1) : new Thickness(1);
            dictionary["RibbonTabMargin"] = cad ? new Thickness(0, 3, 1, 0) : new Thickness(1, 3, 1, 1);
            dictionary["RibbonTabRowPadding"] = cad ? new Thickness(4, 0, 4, 0) : new Thickness(6, 0, 6, 0);
            dictionary["RibbonGroupCaptionMargin"] = cad ? new Thickness(1, 1, 1, 1) : new Thickness(0);
        }
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
        Set(m, "RibbonGroupCaptionBackgroundBrush", C("#00FFFFFF"));
        Set(m, "RibbonGroupCaptionForegroundBrush", C("#616161"));
        Set(m, "RibbonTabSelectedBackgroundBrush", C("#00FFFFFF"));
        Set(m, "RibbonFloatingPanelBarBrush", C("#F0F0F0"));
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
        Set(m, "RibbonGroupCaptionBackgroundBrush", C("#00000000"));
        Set(m, "RibbonGroupCaptionForegroundBrush", C("#C7C7C7"));
        Set(m, "RibbonTabSelectedBackgroundBrush", C("#00000000"));
        Set(m, "RibbonFloatingPanelBarBrush", C("#333333"));
    }

    // CAD style: blue-grey surfaces in the spirit of AutoCAD-class applications. Only surface and neutral colours are
    // overridden; accent-derived brushes (checked, focus, selection) keep following the palette.
    private void ApplyCadDark(RibbonThemePalette palette, RibbonChromeStyle chrome)
    {
        var m = _dark;
        var accent = palette.GetAccent(true);
        var colorful = chrome == RibbonChromeStyle.Colorful;
        Set(m, "RibbonWindowBackgroundBrush", C("#2B313B"));
        Set(m, "RibbonChromeBackgroundBrush", colorful ? C(palette.GetAccent(false).Darken(0.35)) : C("#2B313B"));
        Set(m, "RibbonChromeForegroundBrush", C("#E1E6EC"));
        Set(m, "RibbonTitleBarBackgroundBrush", colorful ? C(palette.GetAccent(false).Darken(0.35)) : C("#252A33"));
        Set(m, "RibbonTitleBarForegroundBrush", C("#E1E6EC"));
        Set(m, "RibbonTitleBarHoverBrush", C("#3B4453"));
        Set(m, "RibbonCommandBarBackgroundBrush", C("#3B4453"));
        Set(m, "RibbonCommandBarBorderBrush", C("#252A33"));
        Set(m, "RibbonForegroundBrush", C("#E1E6EC"));
        Set(m, "RibbonSecondaryForegroundBrush", C("#AEB8C4"));
        Set(m, "RibbonDisabledForegroundBrush", C("#6C7787"));
        Set(m, "RibbonIconBrush", C("#D8DEE6"));
        Set(m, "RibbonItemHoverBrush", C("#4A5568"));
        Set(m, "RibbonItemPressedBrush", C("#56637A"));
        Set(m, "RibbonItemBorderHoverBrush", C("#5F6E86"));
        Set(m, "RibbonSeparatorBrush", C("#2F3641"));
        Set(m, "RibbonTabForegroundBrush", C("#C9D1DB"));
        Set(m, "RibbonTabHoverBrush", C("#343C48"));
        Set(m, "RibbonTabSelectedForegroundBrush", C("#FFFFFF"));
        Set(m, "RibbonTabSelectedBackgroundBrush", C("#3B4453"));
        Set(m, "RibbonTabIndicatorBrush", C("#00000000"));
        Set(m, "RibbonPopupBackgroundBrush", C("#3B4453"));
        Set(m, "RibbonPopupBorderBrush", C("#252A33"));
        Set(m, "RibbonInputBackgroundBrush", C("#2B313B"));
        Set(m, "RibbonInputBorderBrush", C("#56637A"));
        Set(m, "RibbonInputHoverBorderBrush", C("#7A889E"));
        Set(m, "RibbonInputFocusBorderBrush", C(accent));
        Set(m, "RibbonKeyTipBackgroundBrush", C("#E8EDF2"));
        Set(m, "RibbonKeyTipForegroundBrush", C("#1B2027"));
        Set(m, "RibbonKeyTipBorderBrush", C("#FFFFFF"));
        Set(m, "RibbonBackstagePaneBackgroundBrush", C("#252A33"));
        Set(m, "RibbonBackstagePaneHoverBrush", C("#343C48"));
        Set(m, "RibbonBackstageContentBackgroundBrush", C("#2B313B"));
        Set(m, "RibbonGalleryItemBorderBrush", C("#4A5568"));
        Set(m, "RibbonScreenTipBackgroundBrush", C("#3B4453"));
        Set(m, "RibbonScreenTipBorderBrush", C("#252A33"));
        Set(m, "RibbonStatusBarBackgroundBrush", C("#2B313B"));
        Set(m, "RibbonStatusBarForegroundBrush", C("#C9D1DB"));
        Set(m, "RibbonToolBarBackgroundBrush", C("#3B4453"));
        Set(m, "RibbonSearchBackgroundBrush", C("#2B313B"));
        Set(m, "RibbonSearchBorderBrush", C("#4A5568"));
        Set(m, "RibbonScrollButtonBackgroundBrush", C("#F23B4453"));
        Set(m, "RibbonGroupCaptionBackgroundBrush", C("#323A47"));
        Set(m, "RibbonGroupCaptionForegroundBrush", C("#B8C2CE"));
        Set(m, "RibbonFloatingPanelBarBrush", C("#2F3641"));
    }

    private void ApplyCadLight(RibbonThemePalette palette, RibbonChromeStyle chrome)
    {
        var m = _light;
        var accent = palette.GetAccent(false);
        var colorful = chrome == RibbonChromeStyle.Colorful;
        Set(m, "RibbonWindowBackgroundBrush", C("#D5D8DD"));
        Set(m, "RibbonChromeBackgroundBrush", colorful ? C(accent) : C("#DADDE2"));
        Set(m, "RibbonChromeForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C("#1F242B"));
        Set(m, "RibbonTitleBarBackgroundBrush", colorful ? C(accent) : C("#CDD1D7"));
        Set(m, "RibbonTitleBarForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C("#1F242B"));
        Set(m, "RibbonTitleBarHoverBrush", colorful ? C(accent.Lighten(0.15)) : C("#BFC5CD"));
        Set(m, "RibbonCommandBarBackgroundBrush", C("#F0F1F3"));
        Set(m, "RibbonCommandBarBorderBrush", C("#B7BEC8"));
        Set(m, "RibbonForegroundBrush", C("#1F242B"));
        Set(m, "RibbonSecondaryForegroundBrush", C("#4E5866"));
        Set(m, "RibbonDisabledForegroundBrush", C("#9AA3AF"));
        Set(m, "RibbonIconBrush", C("#2F3742"));
        Set(m, "RibbonItemHoverBrush", C("#DDE3EA"));
        Set(m, "RibbonItemPressedBrush", C("#CBD3DD"));
        Set(m, "RibbonItemBorderHoverBrush", C("#A9B4C2"));
        Set(m, "RibbonSeparatorBrush", C("#C7CCD3"));
        Set(m, "RibbonTabForegroundBrush", colorful ? C(palette.GetOnAccent(false)) : C("#2F3742"));
        Set(m, "RibbonTabHoverBrush", colorful ? C(accent.Lighten(0.15)) : C("#E3E6EA"));
        Set(m, "RibbonTabSelectedForegroundBrush", C("#000000"));
        Set(m, "RibbonTabSelectedBackgroundBrush", C("#F0F1F3"));
        Set(m, "RibbonTabIndicatorBrush", C("#00000000"));
        Set(m, "RibbonPopupBackgroundBrush", C("#F7F8F9"));
        Set(m, "RibbonPopupBorderBrush", C("#A9B4C2"));
        Set(m, "RibbonInputBorderBrush", C("#B7BEC8"));
        Set(m, "RibbonInputHoverBorderBrush", C("#8C97A5"));
        Set(m, "RibbonInputFocusBorderBrush", C(accent));
        Set(m, "RibbonBackstageContentBackgroundBrush", C("#F0F1F3"));
        Set(m, "RibbonGalleryItemBorderBrush", C("#C7CCD3"));
        Set(m, "RibbonScreenTipBackgroundBrush", C("#F7F8F9"));
        Set(m, "RibbonScreenTipBorderBrush", C("#A9B4C2"));
        Set(m, "RibbonStatusBarBackgroundBrush", C("#D5D8DD"));
        Set(m, "RibbonStatusBarForegroundBrush", C("#2F3742"));
        Set(m, "RibbonToolBarBackgroundBrush", C("#F0F1F3"));
        Set(m, "RibbonSearchBackgroundBrush", colorful ? C(accent.Lighten(0.2)) : C("#FFFFFF"));
        Set(m, "RibbonSearchBorderBrush", colorful ? C(accent.Lighten(0.3)) : C("#B7BEC8"));
        Set(m, "RibbonScrollButtonBackgroundBrush", C("#F2F0F1F3"));
        Set(m, "RibbonGroupCaptionBackgroundBrush", C("#DCE0E5"));
        Set(m, "RibbonGroupCaptionForegroundBrush", C("#3F4855"));
        Set(m, "RibbonFloatingPanelBarBrush", C("#DCE0E5"));
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
