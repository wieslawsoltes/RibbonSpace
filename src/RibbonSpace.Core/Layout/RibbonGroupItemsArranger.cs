namespace RibbonSpace.Layout;

/// <summary>Input of <see cref="RibbonGroupItemsArranger"/>.</summary>
/// <param name="Width">Desired width.</param>
/// <param name="Height">Desired height.</param>
/// <param name="IsFullHeight">Large items occupy a full column.</param>
/// <param name="StartsNewColumn">Forces a column break before the item.</param>
public readonly record struct RibbonArrangeItem(double Width, double Height, bool IsFullHeight, bool StartsNewColumn = false);

/// <summary>Placement produced by <see cref="RibbonGroupItemsArranger"/>.</summary>
/// <param name="X">Left.</param>
/// <param name="Y">Top.</param>
/// <param name="Width">Width.</param>
/// <param name="Height">Height.</param>
/// <param name="Column">Column index.</param>
/// <param name="Row">Row index inside the column.</param>
public readonly record struct RibbonArrangedItem(double X, double Y, double Width, double Height, int Column, int Row);

/// <summary>Result of <see cref="RibbonGroupItemsArranger.Arrange"/>.</summary>
/// <param name="Items">Placements, one per input item.</param>
/// <param name="Width">Total width.</param>
/// <param name="Height">Total height.</param>
public sealed record RibbonArrangeResult(IReadOnlyList<RibbonArrangedItem> Items, double Width, double Height);

/// <summary>
/// Classic Office group layout: full-height items take their own column; row items stack top-down in columns of
/// <c>rows</c> slots, each slot being <c>contentHeight / rows</c> tall. Columns are as wide as their widest item.
/// </summary>
public static class RibbonGroupItemsArranger
{
    /// <summary>Arranges items into columns.</summary>
    /// <param name="items">Items in order.</param>
    /// <param name="contentHeight">Height of the item area.</param>
    /// <param name="rows">Rows per column (2 or 3).</param>
    /// <param name="columnSpacing">Horizontal spacing between columns.</param>
    /// <param name="distributeRows">When true, a column with fewer items than rows spreads them vertically; otherwise they stack from the top.</param>
    public static RibbonArrangeResult Arrange(IReadOnlyList<RibbonArrangeItem> items, double contentHeight, int rows = 3, double columnSpacing = 2, bool distributeRows = false)
    {
        rows = Math.Clamp(rows, 1, 3);
        var slotHeight = contentHeight / rows;
        var result = new RibbonArrangedItem[items.Count];
        var columns = new List<List<int>>();
        var fullHeight = new List<bool>();
        List<int>? current = null;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.IsFullHeight)
            {
                columns.Add([i]);
                fullHeight.Add(true);
                current = null;
                continue;
            }

            if (current is null || current.Count >= rows || item.StartsNewColumn)
            {
                current = [];
                columns.Add(current);
                fullHeight.Add(false);
            }

            current.Add(i);
        }

        var x = 0d;
        for (var c = 0; c < columns.Count; c++)
        {
            var column = columns[c];
            var width = column.Max(i => items[i].Width);
            if (fullHeight[c])
            {
                var i = column[0];
                result[i] = new RibbonArrangedItem(x, 0, width, contentHeight, c, 0);
            }
            else
            {
                var gap = distributeRows && column.Count < rows ? (contentHeight - (column.Count * slotHeight)) / (column.Count + 1) : 0;
                for (var r = 0; r < column.Count; r++)
                {
                    var i = column[r];
                    var y = distributeRows ? gap + (r * (slotHeight + gap)) : r * slotHeight;
                    var height = Math.Min(items[i].Height, slotHeight);
                    result[i] = new RibbonArrangedItem(x, y + ((slotHeight - height) / 2), items[i].Width, height, c, r);
                }
            }

            x += width + (c < columns.Count - 1 ? columnSpacing : 0);
        }

        return new RibbonArrangeResult(result, Math.Max(0, x), contentHeight);
    }
}
