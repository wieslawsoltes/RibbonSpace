using System.Globalization;
using System.Text;

namespace RibbonSpace.Demo.Cad;

/// <summary>
/// Original artwork for the CAD showcase in RibbonSpace's layered path syntax: extended tooltip illustrations
/// (<c>*Help</c>, 160-unit grid), block thumbnails (<c>Block*</c>), hatch pattern swatches (<c>Hatch*</c>) and
/// visual style previews (<c>Style*</c>) on the 32-unit grid. Themed layers follow the icon brush; orange marks the
/// result of the operation, blue the grips, as in the command icons.
/// </summary>
public static class CadArt
{
    private const string Orange = "#E8A33D";
    private const string Blue = "#3DA9F5";
    private const string Red = "#E05A5A";
    private const string Green = "#5CB85C";

    /// <summary>Design grid of an artwork.</summary>
    public static double SizeOf(string name) => name.EndsWith("Help", StringComparison.Ordinal) ? 160 : 32;

    #region Extended tooltip illustrations (160 grid)

    public static string LineHelp { get; } = Help(
        Layer("stroke=2.6", "M28,124 L74,48 L118,104"),
        Layer($"stroke=2.2;color={Orange}", Dashed(118, 104, 138, 56, 7, 5)),
        Grips(3.5, (28, 124), (74, 48), (118, 104)),
        Cursor(138, 56));

    public static string CircleHelp { get; } = Help(
        Layer("stroke=2.6", Circle(76, 84, 46)),
        Layer($"stroke=2.2;color={Orange}", "M76,84 L118,60"),
        Grips(3.5, (76, 84), (122, 84), (30, 84), (76, 38), (76, 130)),
        Cursor(118, 60));

    public static string TrimHelp { get; } = Help(
        Layer($"stroke=2.2;color={Blue}", Dashed(56, 30, 56, 130, 8, 5) + " " + Dashed(108, 30, 108, 130, 8, 5)),
        Layer("stroke=2.6", "M20,80 L56,80 M108,80 L142,80"),
        Layer($"stroke=2.2;color={Orange};opacity=0.75", Dashed(56, 80, 108, 80, 6, 5)),
        Layer($"stroke=2.4;color={Red}", "M74,62 L90,78 M90,62 L74,78"),
        Layer("stroke=1.6", "M76,74 L88,74 L88,86 L76,86 Z"));

    public static string FilletHelp { get; } = Help(
        Layer("stroke=1.6;opacity=0.3", "M42,80 L42,40 L82,40"),
        Layer("stroke=2.6", "M42,138 L42,80 M82,40 L140,40"),
        Layer($"stroke=2.8;color={Orange}", "M42,80 A40,40 0 0 1 82,40"),
        Layer($"stroke=1.4;color={Blue};opacity=0.8", Dashed(82, 80, 53.7, 51.7, 5, 4)),
        Grips(3.5, (42, 80), (82, 40)),
        Layer($"color={Blue}", Circle(82, 80, 2.5)));

    public static string HatchHelp { get; } = Help(
        Layer($"stroke=1.5;color={Orange}", HatchLines(32, 40, 128, 124, 12)),
        Layer("stroke=2.6", "M32,40 L128,40 L128,124 L32,124 Z"),
        Cursor(80, 82));

    public static string ArrayHelp { get; } = Help(
        Layer($"color={Orange};opacity=0.5", "M26,96 L48,96 L48,118 L26,118 Z"),
        Layer($"stroke=2.4;color={Orange}", "M26,96 L48,96 L48,118 L26,118 Z"),
        Layer("stroke=2.2", RectGrid(26, 30, 22, 22, 4, 3, 38, 33, skipFirst: true)),
        Layer($"color={Blue}", "M126,103 L136,107 L126,111 Z M33,26 L37,16 L41,26 Z"),
        Grips(3.5, (37, 107)));

    public static string OffsetHelp { get; } = Help(
        Layer("stroke=2.6", "M24,120 L24,60 A24,24 0 0 1 48,36 L136,36"),
        Layer($"stroke=2.6;color={Orange}", "M46,120 L46,60 A2,2 0 0 1 48,58 L136,58"),
        Layer($"stroke=1.4;color={Blue}", "M100,36 L100,58 M96.5,41 L100,36 L103.5,41 M96.5,53 L100,58 L103.5,53"),
        Cursor(118, 84));

    public static string PolylineHelp { get; } = Help(
        Layer("stroke=2.6", "M24,120 L56,52 L94,98 A26,26 0 0 1 120,72"),
        Layer($"stroke=2.2;color={Orange}", Dashed(120, 72, 138, 44, 7, 5)),
        Grips(3.5, (24, 120), (56, 52), (94, 98), (120, 72)),
        Cursor(138, 44));

    public static string MoveHelp { get; } = Help(
        Layer("stroke=2.2;opacity=0.35", "M24,76 L64,76 L64,116 L24,116 Z"),
        Layer($"stroke=2.6;color={Orange}", "M88,36 L128,36 L128,76 L88,76 Z"),
        Layer($"stroke=1.4;color={Blue}", Dashed(44, 96, 108, 56, 6, 4)),
        Grips(3.5, (44, 96)),
        Cursor(108, 56));

    #endregion

    #region Blocks (32 grid)

    public static string BlockDoor { get; } =
        "[viewbox=32;stroke=1.8]M6,27 L6,7" +
        "|[stroke=1.4;color=#E8A33D]M6,7 A20,20 0 0 1 26,27" +
        "|[stroke=1.8]M3,27 H9 M23,27 H29";

    public static string BlockWindow { get; } =
        "[viewbox=32;stroke=1.8]M3,11 H29 M3,21 H29 M3,11 V21 M29,11 V21" +
        "|[stroke=1.3;color=#3DA9F5]M3,14.5 H29 M3,17.5 H29";

    public static string BlockChair { get; } =
        "[viewbox=32;stroke=1.8]M9,11 H23 V26 A2,2 0 0 1 21,28 H11 A2,2 0 0 1 9,26 Z" +
        "|[stroke=1.8]M7,5 H25 V9 H7 Z" +
        "|[color=#E8A33D;opacity=0.45]M11,13 H21 V26 H11 Z";

    public static string BlockTable { get; } =
        "[viewbox=32;stroke=1.8]M8,16 A8,8 0 1 1 24,16 A8,8 0 1 1 8,16 Z" +
        "|[stroke=1.5;color=#E8A33D]M13,2.5 H19 V5.5 H13 Z M13,26.5 H19 V29.5 H13 Z M2.5,13 H5.5 V19 H2.5 Z M26.5,13 H29.5 V19 H26.5 Z";

    public static string BlockBed { get; } =
        "[viewbox=32;stroke=1.8]M6,4 H26 V28 H6 Z" +
        "|[stroke=1.5;color=#E8A33D]M8.5,6.5 H14.5 V11 H8.5 Z M17.5,6.5 H23.5 V11 H17.5 Z" +
        "|[stroke=1.4]M6,14 H26 M6,14 L26,20";

    public static string BlockToilet { get; } =
        "[viewbox=32;stroke=1.8]M8,4 H24 V10 H8 Z" +
        "|[stroke=1.8]M10,10 V18 A6,9 0 0 0 22,18 V10" +
        "|[stroke=1.4;color=#3DA9F5]M12.5,17 A3.5,5 0 0 0 19.5,17";

    public static string BlockBolt { get; } =
        "[viewbox=32;stroke=1.8]M10,4 H22 L28,16 L22,28 H10 L4,16 Z" +
        "|[stroke=1.6;color=#E8A33D]M10.5,16 A5.5,5.5 0 1 1 21.5,16 A5.5,5.5 0 1 1 10.5,16 Z" +
        "|[stroke=1.2;opacity=0.6]M16,2 V30 M2,16 H30";

    public static string BlockNut { get; } =
        "[viewbox=32;stroke=1.8]M3,9 H29 V23 H3 Z" +
        "|[stroke=1.4]M3,13 H29 M3,19 H29" +
        "|[stroke=1.4;color=#E8A33D]M10,9 V23 M22,9 V23";

    public static string BlockNorthArrow { get; } =
        "[viewbox=32;stroke=1.8]M16,3 L24,28 L16,22 L8,28 Z" +
        "|[color=#E8A33D]M16,3 L16,22 L8,28 Z";

    public static string BlockTitle { get; } =
        "[viewbox=32;stroke=1.8]M3,5 H29 V27 H3 Z" +
        "|[stroke=1.3]M16,20 H29 M16,20 V27 M16,23.5 H29 M22,20 V27" +
        "|[color=#E8A33D;opacity=0.7]M18,8 H27 V10 H18 Z";

    public static string BlockTree { get; } =
        "[viewbox=32;stroke=1.5]M16,3.5 C22,3.5 27,7.5 27.5,12 C29,15.5 27.5,22 22.5,25 C19.5,28.5 12.5,28.5 9.5,25 C4.5,22 3,15.5 4.5,12 C5,7.5 10,3.5 16,3.5 Z" +
        "|[stroke=1.3;color=#5CB85C]M16,16 L16,6 M16,16 L25,14 M16,16 L21,25 M16,16 L9,24 M16,16 L7,12" +
        "|[color=#5CB85C]M14.5,16 A1.5,1.5 0 1 1 17.5,16 A1.5,1.5 0 1 1 14.5,16 Z";

    public static string BlockSection { get; } =
        "[viewbox=32;stroke=1.8]M4,16 A12,12 0 1 1 28,16 A12,12 0 1 1 4,16 Z" +
        "|[stroke=1.6]M4,16 H28" +
        "|[color=#E8A33D]M16,4 L22,10 L10,10 Z";

    #endregion

    #region Hatch patterns (32 grid)

    public static string HatchSolid { get; } = "[viewbox=32;color=#E8A33D;opacity=0.9]M3,3 H29 V29 H3 Z|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchAnsi31 { get; } = Layer("stroke=1.2;color=" + Orange, HatchLines(3, 3, 29, 29, 4)) + "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchAnsi37 { get; } = Layer("stroke=1.1;color=" + Orange, HatchLines(3, 3, 29, 29, 5) + " " + HatchLines(3, 3, 29, 29, 5, mirror: true)) + "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchBrick { get; } =
        "[viewbox=32;stroke=1.1;color=#E8A33D]M3,9.5 H29 M3,16 H29 M3,22.5 H29 M10,3 V9.5 M22,3 V9.5 M16,9.5 V16 M4,9.5 V16 M28,9.5 V16 M10,16 V22.5 M22,16 V22.5 M16,22.5 V29 M4,22.5 V29 M28,22.5 V29" +
        "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchConcrete { get; } =
        "[viewbox=32;color=#E8A33D]M7,7 A1.1,1.1 0 1 1 9.2,7 A1.1,1.1 0 1 1 7,7 Z M17,10 A0.8,0.8 0 1 1 18.6,10 A0.8,0.8 0 1 1 17,10 Z M24,6 A1,1 0 1 1 26,6 A1,1 0 1 1 24,6 Z M11,17 A0.9,0.9 0 1 1 12.8,17 A0.9,0.9 0 1 1 11,17 Z M21,19 A1.2,1.2 0 1 1 23.4,19 A1.2,1.2 0 1 1 21,19 Z M6,25 A0.8,0.8 0 1 1 7.6,25 A0.8,0.8 0 1 1 6,25 Z M15,25 A1.1,1.1 0 1 1 17.2,25 A1.1,1.1 0 1 1 15,25 Z M25,26 A0.8,0.8 0 1 1 26.6,26 A0.8,0.8 0 1 1 25,26 Z" +
        "|[stroke=1;color=#E8A33D]M5,13 L8,11 L9,14 Z M19,14 L23,13 L21,16 Z M9,21 L12,22 L9.5,24 Z M24,22 L27,21 L26,24 Z" +
        "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchHoney { get; } =
        "[viewbox=32;stroke=1.1;color=#E8A33D]M3,9 L7,6 L12,9 L12,15 L7,18 L3,15 M12,9 L17,6 L22,9 L22,15 L17,18 L12,15 M22,9 L27,6 L29,7.5 M22,15 L27,18 L29,16.5 M7,18 L7,24 L12,27 L17,24 L17,18 M17,24 L22,27 L27,24 L27,18 M3,27 L7,24 M7,6 V3 M17,6 V3 M27,6 V3 M12,27 V29 M22,27 V29" +
        "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchGravel { get; } =
        "[viewbox=32;stroke=1.1;color=#E8A33D]M5,6 L10,5 L11,9 L6,10 Z M15,4 L20,6 L18,10 L14,8 Z M23,8 L27,6 L28,11 L24,12 Z M6,15 L11,13 L12,18 L7,19 Z M16,14 L21,15 L20,20 L15,18 Z M24,17 L28,18 L26,22 L23,21 Z M5,24 L9,22 L11,27 L6,27 Z M14,23 L19,24 L18,28 L13,27 Z M22,25 L27,24 L27,28 L23,28 Z" +
        "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchNet { get; } = Layer("stroke=1.1;color=" + Orange, "M3,8.2 H29 M3,13.4 H29 M3,18.6 H29 M3,23.8 H29 M8.2,3 V29 M13.4,3 V29 M18.6,3 V29 M23.8,3 V29") + "|[stroke=1.2]M3,3 H29 V29 H3 Z";

    public static string HatchGradient { get; } = CadIcons.Gradient;

    #endregion

    #region Visual styles (32 grid)

    public static string Style2DWireframe { get; } = "[viewbox=32;stroke=1.6]M5,9 L19,9 L19,23 L5,23 Z M13,5 L27,5 L27,19 L13,19 Z M5,9 L13,5 M19,9 L27,5 M19,23 L27,19 M5,23 L13,19";

    public static string StyleHidden { get; } = "[viewbox=32;stroke=1.6]M5,11 L19,11 L19,27 L5,27 Z M5,11 L13,5 L27,5 L19,11 M19,27 L27,21 L27,5";

    public static string StyleShaded { get; } =
        "[viewbox=32;color=#8FA7C4]M5,11 L19,11 L19,27 L5,27 Z|[color=#6E87A6]M19,11 L27,5 L27,21 L19,27 Z|[color=#B5C7DC]M5,11 L13,5 L27,5 L19,11 Z" +
        "|[stroke=1.4]M5,11 L19,11 L19,27 L5,27 Z M5,11 L13,5 L27,5 L19,11 M19,27 L27,21 L27,5";

    public static string StyleRealistic { get; } =
        "[viewbox=32;color=#C9935A]M5,11 L19,11 L19,27 L5,27 Z|[color=#A0703F]M19,11 L27,5 L27,21 L19,27 Z|[color=#E2B57F]M5,11 L13,5 L27,5 L19,11 Z" +
        "|[stroke=0.9;color=#7A5230;opacity=0.6]M5,15 H19 M5,19 H19 M5,23 H19";

    public static string StyleConceptual { get; } =
        "[viewbox=32;color=#E8A33D;opacity=0.85]M5,11 L19,11 L19,27 L5,27 Z|[color=#3DA9F5;opacity=0.8]M19,11 L27,5 L27,21 L19,27 Z|[color=#F2D27A]M5,11 L13,5 L27,5 L19,11 Z" +
        "|[stroke=1.4]M5,11 L19,11 L19,27 L5,27 Z M5,11 L13,5 L27,5 L19,11 M19,27 L27,21 L27,5";

    public static string StyleXRay { get; } =
        "[viewbox=32;color=#8FA7C4;opacity=0.35]M5,11 L19,11 L19,27 L5,27 Z M19,11 L27,5 L27,21 L19,27 Z" +
        "|[stroke=1.4]M5,11 L19,11 L19,27 L5,27 Z M5,11 L13,5 L27,5 L19,11 M19,27 L27,21 L27,5|[stroke=1.2;opacity=0.5]M5,27 L13,21 L27,21 M13,21 V5";

    #endregion

    #region Builders

    // Tooltip illustrations: themed geometry (the ScreenTip image presenters use the icon brush) on a faint frame and
    // grid, with fixed accent colours; the first header declares the 160-unit design grid.
    private static string Help(params string[] layers)
        => Join([Layer("viewbox=160;stroke=1.2;opacity=0.25", "M6,6 L154,6 L154,154 L6,154 Z"), Layer("stroke=0.8;opacity=0.12", "M6,38 H154 M6,70 H154 M6,102 H154 M6,134 H154 M38,6 V154 M70,6 V154 M102,6 V154 M134,6 V154"), .. layers]);

    private static string Join(params string[] layers) => string.Join("|", layers.Where(l => l.Length > 0));

    private static string Layer(string header, string data) => "[" + header + "]" + data;

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Circle(double cx, double cy, double r)
        => $"M{F(cx - r)},{F(cy)} A{F(r)},{F(r)} 0 1 1 {F(cx + r)},{F(cy)} A{F(r)},{F(r)} 0 1 1 {F(cx - r)},{F(cy)} Z";

    private static string Grips(double half, params (double X, double Y)[] points)
    {
        var sb = new StringBuilder();
        foreach (var (x, y) in points)
        {
            sb.Append($"M{F(x - half)},{F(y - half)} L{F(x + half)},{F(y - half)} L{F(x + half)},{F(y + half)} L{F(x - half)},{F(y + half)} Z ");
        }

        return Layer($"color={Blue}", sb.ToString().Trim());
    }

    private static string Cursor(double x, double y)
        => Layer("stroke=1.4", $"M{F(x - 16)},{F(y)} L{F(x - 4)},{F(y)} M{F(x + 4)},{F(y)} L{F(x + 16)},{F(y)} M{F(x)},{F(y - 16)} L{F(x)},{F(y - 4)} M{F(x)},{F(y + 4)} L{F(x)},{F(y + 16)} M{F(x - 3)},{F(y - 3)} L{F(x + 3)},{F(y - 3)} L{F(x + 3)},{F(y + 3)} L{F(x - 3)},{F(y + 3)} Z");

    private static string Dashed(double x1, double y1, double x2, double y2, double dash, double gap)
    {
        var length = Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        var (ux, uy) = ((x2 - x1) / length, (y2 - y1) / length);
        var sb = new StringBuilder();
        for (double d = 0; d < length; d += dash + gap)
        {
            var e = Math.Min(length, d + dash);
            sb.Append($"M{F(x1 + (ux * d))},{F(y1 + (uy * d))} L{F(x1 + (ux * e))},{F(y1 + (uy * e))} ");
        }

        return sb.ToString().Trim();
    }

    // 45° hatch lines clipped to a rectangle.
    private static string HatchLines(double left, double top, double right, double bottom, double spacing, bool mirror = false)
    {
        var sb = new StringBuilder();
        var width = right - left;
        var height = bottom - top;
        for (var c = spacing; c < width + height; c += spacing)
        {
            // Line x + y = c in local coordinates (or x - y for the mirrored set).
            var x1 = Math.Max(0, c - height);
            var y1 = c - x1;
            var x2 = Math.Min(width, c);
            var y2 = c - x2;
            if (mirror)
            {
                sb.Append($"M{F(right - x1)},{F(top + y1)} L{F(right - x2)},{F(top + y2)} ");
            }
            else
            {
                sb.Append($"M{F(left + x1)},{F(top + y1)} L{F(left + x2)},{F(top + y2)} ");
            }
        }

        return sb.ToString().Trim();
    }

    private static string RectGrid(double left, double top, double w, double h, int columns, int rows, double dx, double dy, bool skipFirst)
    {
        var sb = new StringBuilder();
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                // Row 0 is the bottom row (where the source object sits).
                var x = left + (c * dx);
                var y = top + ((rows - 1 - r) * dy);
                if (skipFirst && r == 0 && c == 0)
                {
                    continue;
                }

                sb.Append($"M{F(x)},{F(y)} L{F(x + w)},{F(y)} L{F(x + w)},{F(y + h)} L{F(x)},{F(y + h)} Z ");
            }
        }

        return sb.ToString().Trim();
    }

    #endregion
}
