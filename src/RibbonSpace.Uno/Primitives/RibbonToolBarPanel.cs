using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>Arranges toolbar items horizontally or in vertical columns and moves the rest to an overflow button.</summary>
public partial class RibbonToolBarPanel : Panel
{
    private UIElement? _overflowButton;
    private readonly List<UIElement> _overflow = [];

    /// <summary>Orientation.</summary>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>Columns (vertical).</summary>
    public int Columns { get; set; } = 1;

    /// <summary>Overflow enabled.</summary>
    public bool IsOverflowEnabled { get; set; } = true;

    /// <summary>Spacing between items.</summary>
    public double Spacing { get; set; } = 2;

    /// <summary>Items that did not fit.</summary>
    public IReadOnlyList<UIElement> OverflowItems => _overflow;

    /// <summary>Overflow button.</summary>
    public UIElement? OverflowButton
    {
        get => _overflowButton;
        set
        {
            if (_overflowButton is not null)
            {
                Children.Remove(_overflowButton);
            }

            _overflowButton = value;
            if (value is not null)
            {
                Children.Add(value);
            }
        }
    }

    private IEnumerable<UIElement> Items => Children.Where(c => !ReferenceEquals(c, _overflowButton) && c.Visibility == Visibility.Visible);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
        foreach (var child in Children)
        {
            child.Measure(infinite);
        }

        _overflow.Clear();
        var horizontal = Orientation == Orientation.Horizontal;
        var limit = horizontal ? availableSize.Width : availableSize.Height;
        var overflowSize = _overflowButton is null ? 0 : (horizontal ? _overflowButton.DesiredSize.Width : _overflowButton.DesiredSize.Height) + Spacing;
        var items = Items.ToList();
        var extents = Rows(items, CellWidth(items)).ToList();
        var total = extents.Sum(r => r.Extent + Spacing) - (extents.Count > 0 ? Spacing : 0);
        var fitLimit = IsOverflowEnabled && total > limit ? limit - overflowSize : double.PositiveInfinity;
        var used = 0d;
        var cross = 0d;
        foreach (var row in extents)
        {
            if (used + row.Extent > fitLimit && _overflow.Count == 0 && used > 0 || _overflow.Count > 0)
            {
                _overflow.AddRange(row.Items);
                continue;
            }

            used += row.Extent + Spacing;
            cross = Math.Max(cross, row.Cross);
        }

        used = Math.Max(0, used - Spacing);
        if (_overflow.Count > 0)
        {
            used += overflowSize;
            cross = Math.Max(cross, horizontal ? _overflowButton!.DesiredSize.Height : _overflowButton!.DesiredSize.Width);
        }

        return horizontal ? new Size(used, cross) : new Size(cross, used);
    }

    // Vertical toolbars give every (non-separator) item the width of the widest one.
    private double CellWidth(List<UIElement> items)
        => Orientation == Orientation.Horizontal ? 0 : items.Where(i => i is not RibbonSeparator).Select(i => i.DesiredSize.Width).DefaultIfEmpty(0).Max();

    // Cross size of a vertical row as arranged: cells are at least cellWidth wide; separators span the row.
    private static double RowCross(List<UIElement> row, double cellWidth)
        => row.Sum(i => i is RibbonSeparator ? i.DesiredSize.Width : Math.Max(cellWidth, i.DesiredSize.Width));

    private IEnumerable<(List<UIElement> Items, double Extent, double Cross)> Rows(List<UIElement> items, double cellWidth)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        if (horizontal)
        {
            foreach (var item in items)
            {
                yield return ([item], item.DesiredSize.Width, item.DesiredSize.Height);
            }

            yield break;
        }

        var columns = Math.Max(1, Columns);
        var row = new List<UIElement>();
        foreach (var item in items)
        {
            var fullRow = item is RibbonSeparator;
            if (fullRow && row.Count > 0)
            {
                yield return (row, row.Max(i => i.DesiredSize.Height), RowCross(row, cellWidth));
                row = [];
            }

            row.Add(item);
            if (fullRow || row.Count >= columns)
            {
                yield return (row, row.Max(i => i.DesiredSize.Height), RowCross(row, cellWidth));
                row = [];
            }
        }

        if (row.Count > 0)
        {
            yield return (row, row.Max(i => i.DesiredSize.Height), RowCross(row, cellWidth));
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var position = 0d;
        var hidden = new Rect(0, 0, 0, 0);
        var items = Items.ToList();
        var cellWidth = CellWidth(items);
        foreach (var row in Rows(items, cellWidth))
        {
            if (row.Items.Any(_overflow.Contains))
            {
                // Overflowed items live in the overflow menu: keep them out of tab navigation.
                foreach (var item in row.Items)
                {
                    item.Arrange(hidden);
                    RibbonHiddenFocus.Set(item, true);
                }

                continue;
            }

            foreach (var item in row.Items)
            {
                RibbonHiddenFocus.Set(item, false);
            }

            if (horizontal)
            {
                var item = row.Items[0];
                item.Arrange(new Rect(position, (finalSize.Height - item.DesiredSize.Height) / 2, item.DesiredSize.Width, item.DesiredSize.Height));
                position += item.DesiredSize.Width + Spacing;
            }
            else
            {
                var x = 0d;
                foreach (var item in row.Items)
                {
                    var w = item is RibbonSeparator ? finalSize.Width : Math.Max(cellWidth, item.DesiredSize.Width);
                    item.Arrange(new Rect(x, position, w, item.DesiredSize.Height));
                    x += w;
                }

                position += row.Extent + Spacing;
            }
        }

        if (_overflowButton is not null)
        {
            if (_overflow.Count > 0)
            {
                var s = _overflowButton.DesiredSize;
                _overflowButton.Arrange(horizontal ? new Rect(position, (finalSize.Height - s.Height) / 2, s.Width, s.Height) : new Rect(0, position, s.Width, s.Height));
                RibbonHiddenFocus.Set(_overflowButton, false);
            }
            else
            {
                _overflowButton.Arrange(hidden);
                RibbonHiddenFocus.Set(_overflowButton, true);
            }
        }

        return finalSize;
    }
}
