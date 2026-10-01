using Microsoft.UI.Xaml.Media;
using RibbonSpace.Model;
using Windows.UI;

namespace RibbonSpace.Demo.Cad;

/// <summary>A drawing layer shown in the layer drop-down (on / off, freeze, lock, colour, name).</summary>
public sealed class CadLayer(string name, Color color, bool isOn = true, bool isFrozen = false, bool isLocked = false)
{
    public string Name { get; } = name;

    public Color Color { get; } = color;

    public SolidColorBrush ColorBrush { get; } = new(color);

    public bool IsOn { get; } = isOn;

    public bool IsFrozen { get; } = isFrozen;

    public bool IsLocked { get; } = isLocked;

    public RibbonIcon? OnIcon => CadIcon.Get(IsOn ? nameof(CadIcons.LayerOn) : nameof(CadIcons.LayerOff));

    public RibbonIcon? FreezeIcon => CadIcon.Get(IsFrozen ? nameof(CadIcons.LayerFreeze) : nameof(CadIcons.LayerThaw));

    public RibbonIcon? LockIcon => CadIcon.Get(IsLocked ? nameof(CadIcons.LayerLock) : nameof(CadIcons.LayerUnlock));

    public override string ToString() => Name;

    /// <summary>Layers of the sample floor plan.</summary>
    public static IReadOnlyList<CadLayer> Samples { get; } =
    [
        new("0", Color.FromArgb(255, 0xF2, 0xF2, 0xF2)),
        new("A-WALL", Color.FromArgb(255, 0xF2, 0xF2, 0xF2)),
        new("A-DOOR", Color.FromArgb(255, 0xFF, 0xD5, 0x4F)),
        new("A-GLAZ", Color.FromArgb(255, 0x4D, 0xD0, 0xE1)),
        new("A-FURN", Color.FromArgb(255, 0x9C, 0xCC, 0x65), isLocked: true),
        new("A-ANNO-DIMS", Color.FromArgb(255, 0xEF, 0x53, 0x50)),
        new("A-ANNO-TEXT", Color.FromArgb(255, 0xF2, 0xF2, 0xF2)),
        new("A-HATCH", Color.FromArgb(255, 0x8C, 0x9A, 0xAB), isFrozen: true),
        new("A-AREA", Color.FromArgb(255, 0xB3, 0x88, 0xFF), isOn: false),
        new("CENTER", Color.FromArgb(255, 0xB3, 0x88, 0xFF)),
        new("DEFPOINTS", Color.FromArgb(255, 0xF2, 0xF2, 0xF2), isOn: false, isLocked: true),
    ];
}

/// <summary>An entry of the Properties panel colour drop-down (ByLayer, ByBlock, index colours, Select Colors...).</summary>
public sealed class CadColorOption(string name, Color? color, bool isSpecial = false)
{
    public string Name { get; } = name;

    public SolidColorBrush Swatch { get; } = new(color ?? Color.FromArgb(0, 0, 0, 0));

    /// <summary>Special entries (ByLayer / ByBlock) draw a split swatch.</summary>
    public double SpecialOpacity { get; } = isSpecial ? 1 : 0;

    public override string ToString() => Name;

    public static IReadOnlyList<CadColorOption> Samples { get; } =
    [
        new("ByLayer", Color.FromArgb(255, 0xF2, 0xF2, 0xF2), true),
        new("ByBlock", Color.FromArgb(255, 0xF2, 0xF2, 0xF2), true),
        new("Red", Color.FromArgb(255, 0xFF, 0x00, 0x00)),
        new("Yellow", Color.FromArgb(255, 0xFF, 0xFF, 0x00)),
        new("Green", Color.FromArgb(255, 0x00, 0xFF, 0x00)),
        new("Cyan", Color.FromArgb(255, 0x00, 0xFF, 0xFF)),
        new("Blue", Color.FromArgb(255, 0x00, 0x00, 0xFF)),
        new("Magenta", Color.FromArgb(255, 0xFF, 0x00, 0xFF)),
        new("White", Color.FromArgb(255, 0xFF, 0xFF, 0xFF)),
        new("Select Colors...", null),
    ];
}

/// <summary>A linetype with a dash preview.</summary>
public sealed class CadLinetype(string name, string description, DoubleCollection? dashes)
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public DoubleCollection? Dashes { get; } = dashes;

    public override string ToString() => Name;

    private static DoubleCollection D(params double[] values)
    {
        var c = new DoubleCollection();
        foreach (var v in values)
        {
            c.Add(v);
        }

        return c;
    }

    public static IReadOnlyList<CadLinetype> Samples { get; } =
    [
        new("ByLayer", "", null),
        new("ByBlock", "", null),
        new("Continuous", "Solid line", null),
        new("CENTER", "Center ____ _ ____ _", D(8, 2, 2, 2)),
        new("DASHED", "Dashed __ __ __ __", D(4, 2)),
        new("HIDDEN", "Hidden _ _ _ _ _ _", D(2, 2)),
        new("PHANTOM", "Phantom ____ _ _ ____", D(9, 2, 2, 2, 2, 2)),
        new("DOT", "Dot . . . . . . .", D(0.5, 2)),
    ];
}

/// <summary>A lineweight with a thickness preview.</summary>
public sealed class CadLineweight(string name, double thickness)
{
    public string Name { get; } = name;

    public double Thickness { get; } = thickness;

    public override string ToString() => Name;

    public static IReadOnlyList<CadLineweight> Samples { get; } =
    [
        new("ByLayer", 1),
        new("ByBlock", 1),
        new("Default", 1),
        new("0.00 mm", 1),
        new("0.13 mm", 1),
        new("0.25 mm", 1.5),
        new("0.35 mm", 2),
        new("0.50 mm", 3),
        new("0.70 mm", 4),
        new("1.00 mm", 5),
    ];
}
