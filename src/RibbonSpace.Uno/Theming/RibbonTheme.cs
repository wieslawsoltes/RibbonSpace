using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Theming;
using Windows.UI;

namespace RibbonSpace.Controls;

/// <summary>Runtime theming API for RibbonSpace (palette, accent, chrome style, light / dark).</summary>
public static class RibbonTheme
{
    private static RibbonThemeResources? _resources;

    /// <summary>Raised after the palette or chrome style changed.</summary>
    public static event EventHandler? Changed;

    /// <summary>Theme resources merged into the application.</summary>
    public static RibbonThemeResources Resources => EnsureResources();

    /// <summary>Current palette.</summary>
    public static RibbonThemePalette Palette => Resources.Palette;

    /// <summary>Current chrome style.</summary>
    public static RibbonChromeStyle ChromeStyle => Resources.ChromeStyle;

    /// <summary>
    /// Makes sure <see cref="RibbonThemeResources"/> is merged into <c>Application.Current.Resources</c>
    /// (reuses an instance merged in App.xaml). Called automatically by RibbonSpace controls.
    /// </summary>
    public static RibbonThemeResources EnsureResources()
    {
        if (_resources is not null)
        {
            return _resources;
        }

        var app = Application.Current;
        if (app is not null)
        {
            _resources = FindMerged(app.Resources);
            if (_resources is null)
            {
                _resources = new RibbonThemeResources();
                app.Resources.MergedDictionaries.Add(_resources);
            }
        }

        return _resources ??= new RibbonThemeResources();
    }

    private static RibbonThemeResources? FindMerged(ResourceDictionary dictionary)
    {
        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (merged is RibbonThemeResources resources)
            {
                return resources;
            }

            if (FindMerged(merged) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    /// <summary>Applies a palette (e.g. <see cref="RibbonThemePalette.Excel"/>) keeping the chrome style.</summary>
    public static void ApplyPalette(RibbonThemePalette palette) => Apply(palette, ChromeStyle);

    /// <summary>Applies a custom accent color.</summary>
    public static void ApplyAccent(Color accent) => ApplyPalette(RibbonThemePalette.FromAccent(new RibbonColor(accent.A, accent.R, accent.G, accent.B)));

    /// <summary>Switches between accent-colored ("Colorful") and neutral chrome.</summary>
    public static void ApplyChromeStyle(RibbonChromeStyle style) => Apply(Palette, style);

    /// <summary>Applies palette and chrome style.</summary>
    public static void Apply(RibbonThemePalette palette, RibbonChromeStyle style)
    {
        Resources.Apply(palette, style);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Overrides a single brush for a theme ("Light", "Dark", "HighContrast").</summary>
    public static void SetBrushColor(string key, Color color, string theme = "Light") => Resources.SetColor(key, color, theme);

    /// <summary>Sets Light / Dark / Default on an element subtree (typically the window content).</summary>
    public static void SetTheme(FrameworkElement root, ElementTheme theme)
    {
        ArgumentNullException.ThrowIfNull(root);
        root.RequestedTheme = theme;
    }

    /// <summary>
    /// Resolves a RibbonSpace brush for the theme of <paramref name="scope"/> (its <c>ActualTheme</c>, or High Contrast
    /// when enabled). Unlike <c>Application.Current.Resources[key]</c> this honours <see cref="SetTheme"/> /
    /// <c>RequestedTheme</c> on a subtree or popup. Unknown keys fall back to the application resources.
    /// </summary>
    public static Brush? GetBrush(FrameworkElement? scope, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var themeName = IsHighContrast() ? "HighContrast" : (scope?.ActualTheme ?? ApplicationTheme()) == ElementTheme.Dark ? "Dark" : "Light";
        if (Resources.GetBrush(key, themeName) is { } brush)
        {
            return brush;
        }

        return Application.Current?.Resources.TryGetValue(key, out var value) == true ? value as Brush : null;
    }

    /// <summary>
    /// Sets <paramref name="property"/> of <paramref name="target"/> to a themed brush and keeps it in sync when the
    /// element's theme changes (code-built visuals such as popups, overflow menus and dialogs).
    /// </summary>
    public static void SetThemeBrush(FrameworkElement target, DependencyProperty property, string key)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        target.SetValue(property, GetBrush(target, key));
        var registrations = ThemeBrushRegistrations.GetOrCreateValue(target);
        registrations[property] = key;
        if (registrations.Count == 1)
        {
            target.ActualThemeChanged += OnThemedElementThemeChanged;
        }
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<FrameworkElement, Dictionary<DependencyProperty, string>> ThemeBrushRegistrations = new();

    private static void OnThemedElementThemeChanged(FrameworkElement sender, object args)
    {
        if (ThemeBrushRegistrations.TryGetValue(sender, out var registrations))
        {
            foreach (var (property, key) in registrations)
            {
                sender.SetValue(property, GetBrush(sender, key));
            }
        }
    }

    private static ElementTheme ApplicationTheme()
        => Application.Current?.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

    private static bool IsHighContrast()
    {
        try
        {
            return new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Converts a Core color.</summary>
    public static Color ToColor(this RibbonColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    /// <summary>Converts to a Core color.</summary>
    public static RibbonColor ToRibbonColor(this Color color) => new(color.A, color.R, color.G, color.B);
}
