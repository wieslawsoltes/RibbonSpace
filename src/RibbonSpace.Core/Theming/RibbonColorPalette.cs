namespace RibbonSpace.Theming;

/// <summary>A named color swatch.</summary>
/// <param name="Color">The color.</param>
/// <param name="Name">Accessible name, e.g. "Blue, Accent 1, Lighter 40%".</param>
public sealed record RibbonColorSwatch(RibbonColor Color, string Name);

/// <summary>Office-compatible color palettes (theme colors with generated shades, standard colors).</summary>
public static class RibbonColorPalette
{
    /// <summary>The default Office theme base colors (Background 1 .. Accent 6).</summary>
    public static IReadOnlyList<RibbonColorSwatch> OfficeThemeColors { get; } =
    [
        new(RibbonColor.Parse("#FFFFFF"), "White, Background 1"),
        new(RibbonColor.Parse("#000000"), "Black, Text 1"),
        new(RibbonColor.Parse("#E7E6E6"), "Gray, Background 2"),
        new(RibbonColor.Parse("#44546A"), "Blue-Gray, Text 2"),
        new(RibbonColor.Parse("#4472C4"), "Blue, Accent 1"),
        new(RibbonColor.Parse("#ED7D31"), "Orange, Accent 2"),
        new(RibbonColor.Parse("#A5A5A5"), "Gray, Accent 3"),
        new(RibbonColor.Parse("#FFC000"), "Gold, Accent 4"),
        new(RibbonColor.Parse("#5B9BD5"), "Blue, Accent 5"),
        new(RibbonColor.Parse("#70AD47"), "Green, Accent 6"),
    ];

    /// <summary>The ten Office standard colors.</summary>
    public static IReadOnlyList<RibbonColorSwatch> StandardColors { get; } =
    [
        new(RibbonColor.Parse("#C00000"), "Dark Red"),
        new(RibbonColor.Parse("#FF0000"), "Red"),
        new(RibbonColor.Parse("#FFC000"), "Orange"),
        new(RibbonColor.Parse("#FFFF00"), "Yellow"),
        new(RibbonColor.Parse("#92D050"), "Light Green"),
        new(RibbonColor.Parse("#00B050"), "Green"),
        new(RibbonColor.Parse("#00B0F0"), "Light Blue"),
        new(RibbonColor.Parse("#0070C0"), "Blue"),
        new(RibbonColor.Parse("#002060"), "Dark Blue"),
        new(RibbonColor.Parse("#7030A0"), "Purple"),
    ];

    /// <summary>Text highlight colors (Word "Text Highlight Color").</summary>
    public static IReadOnlyList<RibbonColorSwatch> HighlightColors { get; } =
    [
        new(RibbonColor.Parse("#FFFF00"), "Yellow"),
        new(RibbonColor.Parse("#00FF00"), "Bright Green"),
        new(RibbonColor.Parse("#00FFFF"), "Turquoise"),
        new(RibbonColor.Parse("#FF00FF"), "Pink"),
        new(RibbonColor.Parse("#0000FF"), "Blue"),
        new(RibbonColor.Parse("#FF0000"), "Red"),
        new(RibbonColor.Parse("#000080"), "Dark Blue"),
        new(RibbonColor.Parse("#008080"), "Teal"),
        new(RibbonColor.Parse("#008000"), "Green"),
        new(RibbonColor.Parse("#800080"), "Violet"),
        new(RibbonColor.Parse("#800000"), "Dark Red"),
        new(RibbonColor.Parse("#808000"), "Dark Yellow"),
        new(RibbonColor.Parse("#808080"), "Gray 50%"),
        new(RibbonColor.Parse("#C0C0C0"), "Gray 25%"),
        new(RibbonColor.Parse("#000000"), "Black"),
    ];

    /// <summary>
    /// Generates the five Office shade variants of a theme color (the rows below the base color in the Office palette).
    /// </summary>
    public static IReadOnlyList<RibbonColorSwatch> GenerateShades(RibbonColorSwatch swatch)
    {
        var color = swatch.Color;
        var l = color.Lightness;
        (bool lighter, double amount)[] steps = l switch
        {
            >= 0.99 => [(false, .05), (false, .15), (false, .25), (false, .35), (false, .50)],
            <= 0.01 => [(true, .50), (true, .35), (true, .25), (true, .15), (true, .05)],
            < 0.20 => [(true, .90), (true, .75), (true, .50), (true, .25), (true, .10)],
            > 0.80 => [(false, .10), (false, .25), (false, .50), (false, .75), (false, .90)],
            _ => [(true, .80), (true, .60), (true, .40), (false, .25), (false, .50)],
        };

        return steps
            .Select(s => new RibbonColorSwatch(
                s.lighter ? color.Lighten(s.amount) : color.Darken(s.amount),
                $"{swatch.Name}, {(s.lighter ? "Lighter" : "Darker")} {s.amount * 100:0}%"))
            .ToArray();
    }

    /// <summary>
    /// Builds the Office theme grid: row 0 = base colors, rows 1..5 = generated shades. Returns rows of columns.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<RibbonColorSwatch>> BuildThemeGrid(IReadOnlyList<RibbonColorSwatch>? themeColors = null)
    {
        themeColors ??= OfficeThemeColors;
        var shades = themeColors.Select(GenerateShades).ToArray();
        var rows = new List<IReadOnlyList<RibbonColorSwatch>> { themeColors.ToArray() };
        for (var row = 0; row < 5; row++)
        {
            rows.Add(shades.Select(column => column[row]).ToArray());
        }

        return rows;
    }
}
