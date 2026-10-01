namespace RibbonSpace.Controls;

/// <summary>Layout metrics of a <see cref="RibbonDensity"/>. All values are device independent pixels.</summary>
public sealed record RibbonMetrics
{
    /// <summary>Height of the tab row.</summary>
    public double TabHeight { get; init; } = 30;

    /// <summary>Height of the item area of a classic group.</summary>
    public double GroupContentHeight { get; init; } = 70;

    /// <summary>Height of the group caption row.</summary>
    public double GroupCaptionHeight { get; init; } = 17;

    /// <summary>Height of a large item.</summary>
    public double LargeItemHeight { get; init; } = 68;

    /// <summary>Minimum width of a large item.</summary>
    public double LargeItemMinWidth { get; init; } = 44;

    /// <summary>Height of a medium / small row item.</summary>
    public double RowHeight { get; init; } = 22;

    /// <summary>Large icon size.</summary>
    public double LargeIconSize { get; init; } = 32;

    /// <summary>Small icon size.</summary>
    public double SmallIconSize { get; init; } = 16;

    /// <summary>Height of the simplified command line.</summary>
    public double SimplifiedHeight { get; init; } = 40;

    /// <summary>Height of an item in the simplified line / toolbars.</summary>
    public double SimplifiedItemHeight { get; init; } = 30;

    /// <summary>Base font size.</summary>
    public double FontSize { get; init; } = 12;

    /// <summary>Caption font size.</summary>
    public double CaptionFontSize { get; init; } = 11;

    /// <summary>Horizontal padding of row items.</summary>
    public double ItemPadding { get; init; } = 5;

    /// <summary>Default (mouse) metrics.</summary>
    public static RibbonMetrics Comfortable { get; } = new();

    /// <summary>Compact metrics.</summary>
    public static RibbonMetrics Compact { get; } = new()
    {
        // Two-line large labels need icon + 2 × ~15 px lines: smaller text and icon than Comfortable, same row count.
        TabHeight = 26, GroupContentHeight = 64, GroupCaptionHeight = 15, LargeItemHeight = 62, LargeItemMinWidth = 40,
        RowHeight = 20, LargeIconSize = 26, SmallIconSize = 16, SimplifiedHeight = 34, SimplifiedItemHeight = 26, FontSize = 11, CaptionFontSize = 10, ItemPadding = 4,
    };

    /// <summary>Touch metrics.</summary>
    public static RibbonMetrics Touch { get; } = new()
    {
        TabHeight = 38, GroupContentHeight = 94, GroupCaptionHeight = 19, LargeItemHeight = 92, LargeItemMinWidth = 56,
        RowHeight = 30, LargeIconSize = 32, SmallIconSize = 20, SimplifiedHeight = 50, SimplifiedItemHeight = 40, FontSize = 13, CaptionFontSize = 12, ItemPadding = 8,
    };

    /// <summary>Metrics for a density.</summary>
    public static RibbonMetrics For(RibbonDensity density) => density switch
    {
        RibbonDensity.Compact => Compact,
        RibbonDensity.Touch => Touch,
        _ => Comfortable,
    };
}
