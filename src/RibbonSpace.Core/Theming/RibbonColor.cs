using System.Globalization;

namespace RibbonSpace.Theming;

/// <summary>UI-agnostic ARGB color with Office-compatible tint / shade helpers.</summary>
public readonly record struct RibbonColor(byte A, byte R, byte G, byte B)
{
    /// <summary>Fully transparent color.</summary>
    public static RibbonColor Transparent => new(0, 0, 0, 0);

    /// <summary>Opaque white.</summary>
    public static RibbonColor White => new(255, 255, 255, 255);

    /// <summary>Opaque black.</summary>
    public static RibbonColor Black => new(255, 0, 0, 0);

    /// <summary>Creates an opaque color.</summary>
    public static RibbonColor FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>Parses #RGB, #RRGGBB or #AARRGGBB (the leading # is optional).</summary>
    public static RibbonColor Parse(string value)
    {
        if (!TryParse(value, out var color))
        {
            throw new FormatException($"'{value}' is not a valid color. Use #RRGGBB or #AARRGGBB.");
        }

        return color;
    }

    /// <summary>Tries to parse #RGB, #RRGGBB or #AARRGGBB.</summary>
    public static bool TryParse(string? value, out RibbonColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim().TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        }

        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }

        if (hex.Length != 8 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            return false;
        }

        color = new RibbonColor((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
        return true;
    }

    /// <summary>Relative luminance (0 = black, 1 = white) per WCAG.</summary>
    public double Luminance
    {
        get
        {
            static double Channel(byte c)
            {
                var v = c / 255d;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Channel(R)) + (0.7152 * Channel(G)) + (0.0722 * Channel(B));
        }
    }

    /// <summary>HSL lightness (0..1).</summary>
    public double Lightness => ToHsl().L;

    /// <summary>True when black text is more readable than white text on this color.</summary>
    public bool PrefersDarkForeground => ContrastRatio(this, Black) >= ContrastRatio(this, White);

    /// <summary>WCAG contrast ratio between two colors (1..21).</summary>
    public static double ContrastRatio(RibbonColor a, RibbonColor b)
    {
        var l1 = a.Luminance;
        var l2 = b.Luminance;
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    /// <summary>Returns the color with a new alpha.</summary>
    public RibbonColor WithAlpha(byte alpha) => this with { A = alpha };

    /// <summary>Moves the HSL lightness towards white by <paramref name="amount"/> (0..1), like Office "Lighter 40%".</summary>
    public RibbonColor Lighten(double amount)
    {
        var (h, s, l) = ToHsl();
        return FromHsl(h, s, l + ((1 - l) * Math.Clamp(amount, 0, 1)), A);
    }

    /// <summary>Moves the HSL lightness towards black by <paramref name="amount"/> (0..1), like Office "Darker 25%".</summary>
    public RibbonColor Darken(double amount)
    {
        var (h, s, l) = ToHsl();
        return FromHsl(h, s, l * (1 - Math.Clamp(amount, 0, 1)), A);
    }

    /// <summary>Linear blend with another color (0 = this, 1 = other).</summary>
    public RibbonColor Blend(RibbonColor other, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte Mix(byte x, byte y) => (byte)Math.Round(x + ((y - x) * amount));
        return new RibbonColor(Mix(A, other.A), Mix(R, other.R), Mix(G, other.G), Mix(B, other.B));
    }

    /// <summary>Converts to hue (0..360), saturation and lightness (0..1).</summary>
    public (double H, double S, double L) ToHsl()
    {
        var r = R / 255d;
        var g = G / 255d;
        var b = B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        if (Math.Abs(max - min) < 1e-9)
        {
            return (0, 0, l);
        }

        var d = max - min;
        var s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        double h;
        if (max == r)
        {
            h = ((g - b) / d) + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            h = ((b - r) / d) + 2;
        }
        else
        {
            h = ((r - g) / d) + 4;
        }

        return (h * 60, s, l);
    }

    /// <summary>Creates a color from HSL components.</summary>
    public static RibbonColor FromHsl(double h, double s, double l, byte alpha = 255)
    {
        h = ((h % 360) + 360) % 360 / 360d;
        s = Math.Clamp(s, 0, 1);
        l = Math.Clamp(l, 0, 1);
        if (s <= 0)
        {
            var v = (byte)Math.Round(l * 255);
            return new RibbonColor(alpha, v, v, v);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
        var p = (2 * l) - q;

        static double Hue(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1d / 6) return p + ((q - p) * 6 * t);
            if (t < 1d / 2) return q;
            if (t < 2d / 3) return p + ((q - p) * ((2d / 3) - t) * 6);
            return p;
        }

        return new RibbonColor(
            alpha,
            (byte)Math.Round(Hue(p, q, h + (1d / 3)) * 255),
            (byte)Math.Round(Hue(p, q, h) * 255),
            (byte)Math.Round(Hue(p, q, h - (1d / 3)) * 255));
    }

    /// <summary>Formats as #RRGGBB (opaque) or #AARRGGBB.</summary>
    public string ToHex(bool includeAlpha = false)
        => includeAlpha || A != 255
            ? $"#{A:X2}{R:X2}{G:X2}{B:X2}"
            : $"#{R:X2}{G:X2}{B:X2}";

    /// <inheritdoc />
    public override string ToString() => ToHex();
}
