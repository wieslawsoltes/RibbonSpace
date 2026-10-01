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
            ["settings"] = () => new SettingsPage(),
        };
        foreach (var toggle in Rail.Items.OfType<RibbonToggleButton>())
        {
            toggle.Checked += (s, _) => Show((string)((FrameworkElement)s!).Tag);
        }

        Loaded += (_, _) => Show("word");
    }

    public string CurrentPage { get; private set; } = "word";

    public FrameworkElement? CurrentContent => _pages.GetValueOrDefault(CurrentPage);

    public void Show(string key)
    {
        CurrentPage = key;
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
