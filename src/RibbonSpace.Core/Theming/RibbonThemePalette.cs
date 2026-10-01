namespace RibbonSpace.Theming;

/// <summary>Visual style of the title bar / window chrome, mirroring the Office themes.</summary>
public enum RibbonChromeStyle
{
    /// <summary>Accent-colored title bar (Office "Colorful").</summary>
    Colorful,
    /// <summary>Neutral title bar matching the ribbon (Office "White" / modern Office look).</summary>
    Neutral,
}

/// <summary>
/// Accent palette used to theme a ribbon. Presets mirror the Office application brand colors.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="Accent">Primary accent (selected tab text, focus, checked state, backstage pane).</param>
/// <param name="AccentDark">Accent used in dark theme (lighter for contrast).</param>
public sealed record RibbonThemePalette(string Name, RibbonColor Accent, RibbonColor AccentDark)
{
    /// <summary>Word blue.</summary>
    public static RibbonThemePalette Word { get; } = new("Word", RibbonColor.Parse("#185ABD"), RibbonColor.Parse("#6CA0F5"));

    /// <summary>Excel green.</summary>
    public static RibbonThemePalette Excel { get; } = new("Excel", RibbonColor.Parse("#107C41"), RibbonColor.Parse("#5CC689"));

    /// <summary>PowerPoint orange-red.</summary>
    public static RibbonThemePalette PowerPoint { get; } = new("PowerPoint", RibbonColor.Parse("#C43E1C"), RibbonColor.Parse("#F08B6C"));

    /// <summary>Outlook blue.</summary>
    public static RibbonThemePalette Outlook { get; } = new("Outlook", RibbonColor.Parse("#0F6CBD"), RibbonColor.Parse("#62ABF5"));

    /// <summary>OneNote purple.</summary>
    public static RibbonThemePalette OneNote { get; } = new("OneNote", RibbonColor.Parse("#7719AA"), RibbonColor.Parse("#C38DE8"));

    /// <summary>Access dark red.</summary>
    public static RibbonThemePalette Access { get; } = new("Access", RibbonColor.Parse("#A4373A"), RibbonColor.Parse("#EB8487"));

    /// <summary>Visio indigo.</summary>
    public static RibbonThemePalette Visio { get; } = new("Visio", RibbonColor.Parse("#3955A3"), RibbonColor.Parse("#8DA6EE"));

    /// <summary>Project green.</summary>
    public static RibbonThemePalette Project { get; } = new("Project", RibbonColor.Parse("#31752F"), RibbonColor.Parse("#7DC47A"));

    /// <summary>Publisher teal.</summary>
    public static RibbonThemePalette Publisher { get; } = new("Publisher", RibbonColor.Parse("#077568"), RibbonColor.Parse("#4FC4B6"));

    /// <summary>Teams violet.</summary>
    public static RibbonThemePalette Teams { get; } = new("Teams", RibbonColor.Parse("#5B5FC7"), RibbonColor.Parse("#9EA2FF"));

    /// <summary>Neutral graphite for professional tools (CAD, IDEs).</summary>
    public static RibbonThemePalette Graphite { get; } = new("Graphite", RibbonColor.Parse("#3B4758"), RibbonColor.Parse("#9DB4D3"));

    /// <summary>All built-in presets.</summary>
    public static IReadOnlyList<RibbonThemePalette> Presets { get; } =
        [Word, Excel, PowerPoint, Outlook, OneNote, Access, Visio, Project, Publisher, Teams, Graphite];

    /// <summary>Creates a palette from a single accent, deriving the dark-theme variant automatically.</summary>
    public static RibbonThemePalette FromAccent(RibbonColor accent, string name = "Custom")
        => new(name, accent, accent.Lightness < 0.55 ? accent.Lighten(0.45) : accent);

    /// <summary>Returns the accent for the requested theme.</summary>
    public RibbonColor GetAccent(bool dark) => dark ? AccentDark : Accent;

    /// <summary>Very light accent tint used for hover / checked backgrounds in light theme.</summary>
    public RibbonColor GetAccentSubtle(bool dark) => dark ? AccentDark.WithAlpha(0x33) : Accent.Lighten(0.86);

    /// <summary>Stronger accent tint used for pressed / checked-hover backgrounds in light theme.</summary>
    public RibbonColor GetAccentSubtleStrong(bool dark) => dark ? AccentDark.WithAlpha(0x55) : Accent.Lighten(0.74);

    /// <summary>Foreground to draw on top of the accent color.</summary>
    public RibbonColor GetOnAccent(bool dark) => GetAccent(dark).PrefersDarkForeground ? RibbonColor.Black : RibbonColor.White;
}
