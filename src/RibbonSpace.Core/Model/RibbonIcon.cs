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

    /// <summary>Creates an image icon.</summary>
    public static RibbonIcon Image(string uri) => new(RibbonIconKind.Image, uri);

    /// <summary>Creates a text / emoji icon.</summary>
    public static RibbonIcon Text(string text, string? foreground = null) => new(RibbonIconKind.Text, text, null, foreground);

    /// <summary>Returns a copy with a fixed foreground color.</summary>
    public RibbonIcon WithForeground(string? color) => this with { Foreground = color };

    /// <summary>Implicitly creates a glyph icon from a string.</summary>
    public static implicit operator RibbonIcon(string glyph) => Glyph(glyph);
}
