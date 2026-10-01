namespace RibbonSpace.Model;

/// <summary>Kind of a <see cref="RibbonIcon"/>.</summary>
public enum RibbonIconKind
{
    /// <summary>A glyph of an icon font (Segoe Fluent Icons / Uno Fluent Symbols code points by default).</summary>
    Glyph,
    /// <summary>A vector path in SVG / XAML path mini-language.</summary>
    Path,
    /// <summary>A bitmap or SVG image addressed by URI (ms-appx:///, https://, ...).</summary>
    Image,
    /// <summary>Plain text or emoji rendered as the icon.</summary>
    Text,
}

/// <summary>
/// UI-agnostic icon description. The Uno layer converts it to <c>FontIcon</c>, <c>PathIcon</c>, <c>Image</c> or <c>TextBlock</c>.
/// </summary>
/// <param name="Kind">Icon kind.</param>
/// <param name="Value">Glyph, path data, URI or text depending on <paramref name="Kind"/>.</param>
/// <param name="FontFamily">Optional font family for glyph icons.</param>
/// <param name="Foreground">Optional fixed color (#RRGGBB / #AARRGGBB); <c>null</c> uses the theme icon brush.</param>
/// <param name="ViewBoxSize">Design grid size of path icons (e.g. 16, 20, 24).</param>
public sealed record RibbonIcon(RibbonIconKind Kind, string Value, string? FontFamily = null, string? Foreground = null, double ViewBoxSize = 24)
{
    /// <summary>Creates a font glyph icon.</summary>
    public static RibbonIcon Glyph(string glyph, string? fontFamily = null, string? foreground = null) => new(RibbonIconKind.Glyph, glyph, fontFamily, foreground);

    /// <summary>Creates a vector path icon.</summary>
    public static RibbonIcon Path(string data, double viewBoxSize = 24, string? foreground = null) => new(RibbonIconKind.Path, data, null, foreground, viewBoxSize);

    /// <summary>
    /// Creates a line-art icon: the path is stroked (not filled) with <paramref name="thickness"/> design units and
    /// round caps, e.g. CAD command icons.
    /// </summary>
    public static RibbonIcon Stroke(string data, double thickness = 1.5, double viewBoxSize = 24, string? foreground = null)
        => new(RibbonIconKind.Path, new RibbonIconLayer(data, thickness).ToString(), null, foreground, viewBoxSize);

    /// <summary>
    /// Creates a multi-layer vector icon (e.g. blue geometry with a yellow highlight). Layers are drawn in order;
    /// layers without a color follow the theme icon brush.
    /// </summary>
    public static RibbonIcon Layers(double viewBoxSize, params RibbonIconLayer[] layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        return new(RibbonIconKind.Path, string.Join("|", layers.Select(l => l.ToString())), null, null, viewBoxSize);
    }

    /// <summary>Creates an image icon.</summary>
    public static RibbonIcon Image(string uri) => new(RibbonIconKind.Image, uri);

    /// <summary>Creates a text / emoji icon.</summary>
    public static RibbonIcon Text(string text, string? foreground = null) => new(RibbonIconKind.Text, text, null, foreground);

    /// <summary>Returns a copy with a fixed foreground color.</summary>
    public RibbonIcon WithForeground(string? color) => this with { Foreground = color };

    /// <summary>Implicitly creates a glyph icon from a string.</summary>
    public static implicit operator RibbonIcon(string glyph) => Glyph(glyph);
}

/// <summary>
/// One layer of a vector icon: path data that is filled (<see cref="StrokeThickness"/> = 0) or stroked, with an
/// optional fixed color. Serialized as <c>[stroke=1.5;color=#E8C66E;opacity=0.6]M4,4 L20,20</c>; several layers are
/// separated by <c>|</c>. The same text works directly in XAML (square brackets, because a value starting with
/// <c>{</c> is a markup extension in XAML): <c>Icon="[viewbox=32;stroke=1.5]M4,28 L28,4|[color=#E8C66E]M2,26 h4 v4 h-4 Z"</c>.
/// <c>viewbox</c> (first layer) sets the design grid of string icons (24 by default); curly-brace headers are accepted too.
/// </summary>
/// <param name="Data">Path mini-language data.</param>
/// <param name="StrokeThickness">Stroke thickness in design units; 0 fills the path.</param>
/// <param name="Color">Fixed color (#RRGGBB / #AARRGGBB); <c>null</c> follows the icon foreground.</param>
/// <param name="Opacity">Layer opacity (0..1).</param>
public sealed record RibbonIconLayer(string Data, double StrokeThickness = 0, string? Color = null, double Opacity = 1)
{
    // Get-only properties (no init accessors): see RibbonSearchEntry for the WinUI XAML compiler issue.

    /// <summary>Path mini-language data.</summary>
    public string Data { get; } = Data;

    /// <summary>Stroke thickness in design units; 0 fills the path.</summary>
    public double StrokeThickness { get; } = StrokeThickness;

    /// <summary>Fixed color; <c>null</c> follows the icon foreground.</summary>
    public string? Color { get; } = Color;

    /// <summary>Layer opacity (0..1).</summary>
    public double Opacity { get; } = Opacity;

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string>(3);
        if (StrokeThickness > 0)
        {
            parts.Add("stroke=" + StrokeThickness.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(Color))
        {
            parts.Add("color=" + Color);
        }

        if (Opacity < 1)
        {
            parts.Add("opacity=" + Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return parts.Count == 0 ? Data : "[" + string.Join(";", parts) + "]" + Data;
    }

    /// <summary>Reads the <c>viewbox=</c> setting of the first layer header (design grid of string icons).</summary>
    public static double? ParseViewBox(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.TrimStart();
        var close = text.StartsWith('[') ? ']' : text.StartsWith('{') ? '}' : '\0';
        var end = close == '\0' ? -1 : text.IndexOf(close, StringComparison.Ordinal);
        if (end <= 0)
        {
            return null;
        }

        foreach (var setting in text[1..end].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = setting.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2 && pair[0].Equals("viewbox", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(pair[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size) && size > 0)
            {
                return size;
            }
        }

        return null;
    }

    /// <summary>Parses layered icon text (layers separated by <c>|</c>, optional <c>{key=value;...}</c> headers).</summary>
    public static IReadOnlyList<RibbonIconLayer> Parse(string? value)
    {
        var layers = new List<RibbonIconLayer>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return layers;
        }

        foreach (var raw in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var data = raw;
            double stroke = 0, opacity = 1;
            string? color = null;
            var close = data.StartsWith('[') ? ']' : '}';
            if ((data.StartsWith('[') || data.StartsWith('{')) && data.IndexOf(close, StringComparison.Ordinal) is var end and > 0)
            {
                foreach (var setting in data[1..end].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var pair = setting.Split('=', 2, StringSplitOptions.TrimEntries);
                    var key = pair[0].ToLowerInvariant();
                    var text = pair.Length > 1 ? pair[1] : string.Empty;
                    switch (key)
                    {
                        case "stroke":
                            stroke = double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 1.5;
                            break;
                        case "color" or "fill":
                            color = text.Length > 0 ? text : null;
                            break;
                        case "opacity":
                            opacity = double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var o) ? Math.Clamp(o, 0, 1) : 1;
                            break;
                    }
                }

                data = data[(end + 1)..].Trim();
            }

            if (data.Length > 0)
            {
                layers.Add(new RibbonIconLayer(data, stroke, color, opacity));
            }
        }

        return layers;
    }
}

