using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Controls;
using RibbonSpace.Demo.Pages;
using RibbonSpace.Theming;

namespace RibbonSpace.Demo;

public sealed partial class ShellPage : Page
{
    private readonly Dictionary<string, Func<FrameworkElement>> _factories;
    private readonly Dictionary<string, FrameworkElement> _pages = [];

    public ShellPage()
    {
        InitializeComponent();
        _factories = new()
        {
            ["word"] = () => new WordPage(),
            ["excel"] = () => new ExcelPage(),
            ["powerpoint"] = () => new PowerPointPage(),
            ["tools"] = () => new ToolsPage(),
            ["cad"] = () => new CadPage(),
            ["settings"] = () => new SettingsPage(),
        };
        CadRail.Icon = Cad.CadIcon.Get(nameof(Cad.CadIcons.AppLogo));
        foreach (var toggle in Rail.Items.OfType<RibbonToggleButton>())
        {
            toggle.Checked += (s, _) => Show((string)((FrameworkElement)s!).Tag);
        }

        Loaded += (_, _) => Show("word");

        // Office pages are recreated after a surface style change so that their controls pick up the new shapes.
        DemoSettings.SurfaceStyleChanged += (_, _) =>
        {
            foreach (var stale in _pages.Where(p => p.Key is not ("settings" or "cad") && p.Key != CurrentPage).ToList())
            {
                PageHost.Children.Remove(stale.Value);
                _pages.Remove(stale.Key);
            }
        };
    }

    private static Ribbon? FindRibbon(DependencyObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is Ribbon ribbon)
        {
            return ribbon;
        }

        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindRibbon(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private RibbonChromeStyle _officeChrome = RibbonChromeStyle.Neutral;

    public string CurrentPage { get; private set; } = "word";

    private void OnCadThemeChanged(FrameworkElement sender, object args)
    {
        if (CurrentPage == "cad")
        {
            Rail.RequestedTheme = sender.ActualTheme;
        }
    }

    public FrameworkElement? CurrentContent => _pages.GetValueOrDefault(CurrentPage);

    public void Show(string key)
    {
        // Pages are collapsed, not unloaded: close the hidden page's ribbon popups and hide its floating panels.
        if (CurrentPage != key && FindRibbon(_pages.GetValueOrDefault(CurrentPage)) is { } previousRibbon)
        {
            previousRibbon.SuspendPopups();
        }

        CurrentPage = key;

        // The CAD page always uses the CAD surface style, the other pages the style chosen in Settings. Shape
        // resources (corners, margins) are read when controls are templated, so the style is applied before a page
        // is created; brushes update live.
        var style = key == "cad" ? RibbonThemeStyle.Cad : DemoSettings.SurfaceStyle;
        if (key == "cad" && RibbonTheme.Style != RibbonThemeStyle.Cad)
        {
            _officeChrome = RibbonTheme.ChromeStyle;
        }

        if (RibbonTheme.Style != style || key == "cad")
        {
            RibbonTheme.Apply(key == "cad" ? RibbonThemePalette.Cad : RibbonTheme.Palette, key == "cad" ? RibbonChromeStyle.Neutral : _officeChrome, style);
        }

        if (!_pages.TryGetValue(key, out var page))
        {
            page = _factories[key]();
            _pages[key] = page;
            PageHost.Children.Add(page);
        }

        foreach (var child in PageHost.Children)
        {
            child.Visibility = ReferenceEquals(child, page) ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var toggle in Rail.Items.OfType<RibbonToggleButton>())
        {
            toggle.IsChecked = (string)toggle.Tag == key;
        }

        // The rail follows the CAD page's own light / dark theme while it is shown.
        FindRibbon(page)?.ResumePopups();
        if (page is CadPage cad)
        {
            Rail.RequestedTheme = cad.ActualTheme;
            cad.ActualThemeChanged -= OnCadThemeChanged;
            cad.ActualThemeChanged += OnCadThemeChanged;
            return;
        }

        Rail.RequestedTheme = ElementTheme.Default;
        RibbonTheme.ApplyPalette(key switch
        {
            "excel" => DemoSettings.PaletteOverride ?? RibbonThemePalette.Excel,
            "powerpoint" => DemoSettings.PaletteOverride ?? RibbonThemePalette.PowerPoint,
            "tools" => DemoSettings.PaletteOverride ?? RibbonThemePalette.Graphite,
            _ => DemoSettings.PaletteOverride ?? RibbonThemePalette.Word,
        });
    }
}

/// <summary>Settings shared by demo pages.</summary>
public static class DemoSettings
{
    public static RibbonThemePalette? PaletteOverride { get; set; }

    public static Ribbon? WordRibbon { get; set; }

    /// <summary>Surface style of the Office pages (the CAD page always uses <see cref="RibbonThemeStyle.Cad"/>).</summary>
    public static RibbonThemeStyle SurfaceStyle { get; private set; } = RibbonThemeStyle.Office;

    public static event EventHandler? SurfaceStyleChanged;

    public static void SetSurfaceStyle(RibbonThemeStyle style)
    {
        if (SurfaceStyle == style)
        {
            return;
        }

        SurfaceStyle = style;
        RibbonTheme.ApplyStyle(style);
        SurfaceStyleChanged?.Invoke(null, EventArgs.Empty);
    }

    public static event EventHandler? Changed;

    public static RibbonDensity Density { get; private set; }

    public static RibbonDisplayMode? DisplayMode { get; private set; }

    public static void Apply(RibbonDensity density, RibbonDisplayMode? displayMode)
    {
        Density = density;
        DisplayMode = displayMode;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static List<string> Log { get; } = [];

    public static event EventHandler<string>? Logged;

    public static void Write(string message)
    {
        Log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        if (Log.Count > 200)
        {
            Log.RemoveAt(Log.Count - 1);
        }

        Logged?.Invoke(null, message);
    }
}
