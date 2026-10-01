using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Layout;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>Arranges the items of a <see cref="RibbonGroup"/> (columns, rows or the simplified line).</summary>
public partial class RibbonGroupItemsPanel : Panel
{
    private RibbonArrangeResult? _columns;
    private readonly List<UIElement> _visible = [];

    /// <summary>Item arrangement.</summary>
    public RibbonGroupItemsLayout ItemsLayout { get; set; }

    /// <summary>Rows per column.</summary>
    public int RowCount { get; set; } = 3;

    /// <summary>Simplified single-line presentation.</summary>
    public bool IsSimplified { get; set; }

    /// <summary>Metrics.</summary>
    public RibbonMetrics Metrics { get; set; } = RibbonMetrics.Comfortable;

    /// <summary>In simplified mode: which children stay in the line (null = all).</summary>
    public IReadOnlyList<bool>? InLineMask { get; set; }

    /// <summary>Spacing between items in the simplified line.</summary>
    public double SimplifiedSpacing { get; set; } = 1;

    internal bool IsInLine(int index) => InLineMask is null || index >= InLineMask.Count || InLineMask[index];

    /// <summary>True when a child is presented as a full-height item.</summary>
    public bool IsFullHeight(UIElement child)
        => child switch
        {
            RibbonSeparator separator => separator.Orientation == Orientation.Vertical,
            RibbonItemsContainer => child.DesiredSize.Height > Metrics.RowHeight * 1.6,
            IRibbonItem item when item.CurrentSize == RibbonItemSize.Large => true,
            _ => child.DesiredSize.Height > Metrics.RowHeight * 1.6,
        };

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var metrics = Metrics;
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
        _visible.Clear();
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(infinite);
            _visible.Add(child);
        }

        if (IsSimplified)
        {
            var x = 0d;
            var count = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.Visibility == Visibility.Collapsed || !IsInLine(i))
                {
                    continue;
                }

                x += child.DesiredSize.Width;
                count++;
            }

            return new Size(x + (Math.Max(0, count - 1) * SimplifiedSpacing), metrics.SimplifiedItemHeight);
        }

        var contentHeight = metrics.GroupContentHeight;
        if (ItemsLayout == RibbonGroupItemsLayout.Rows)
        {
            var width = _visible.Count == 0 ? 0 : _visible.Max(c => c.DesiredSize.Width);
            return new Size(width, contentHeight);
        }

        _columns = RibbonGroupItemsArranger.Arrange(
            _visible.Select(c => new RibbonArrangeItem(c.DesiredSize.Width, c.DesiredSize.Height, IsFullHeight(c))).ToArray(),
            contentHeight,
            RowCount,
            columnSpacing: 2);
        return new Size(_columns.Width, contentHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var hidden = new Rect(0, 0, 0, 0);
        if (IsSimplified)
        {
            var x = 0d;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (!IsInLine(i))
                {
                    // Moved to the overflow menu: not reachable with Tab.
                    child.Arrange(hidden);
                    RibbonHiddenFocus.Set(child, true);
                    continue;
                }

                var h = child.DesiredSize.Height;
                child.Arrange(new Rect(x, (finalSize.Height - h) / 2, child.DesiredSize.Width, h));
                RibbonHiddenFocus.Set(child, false);
                x += child.DesiredSize.Width + SimplifiedSpacing;
            }

            return finalSize;
        }

        // Classic layouts show every visible item (restores items hidden by the simplified line).
        foreach (var child in _visible)
        {
            RibbonHiddenFocus.Set(child, false);
        }

        if (ItemsLayout == RibbonGroupItemsLayout.Rows)
        {
            var rows = Math.Max(1, _visible.Count);
            var slot = finalSize.Height / Math.Max(rows, RowCount == 3 && rows < 3 ? rows : RowCount);
            var total = slot * rows;
            var top = (finalSize.Height - total) / 2;
            for (var i = 0; i < _visible.Count; i++)
            {
                var child = _visible[i];
                var h = Math.Min(child.DesiredSize.Height, slot);
                child.Arrange(new Rect(0, top + (i * slot) + ((slot - h) / 2), child.DesiredSize.Width, h));
            }

            return finalSize;
        }

        if (_columns is not null)
        {
            for (var i = 0; i < _visible.Count && i < _columns.Items.Count; i++)
            {
                var placement = _columns.Items[i];
                _visible[i].Arrange(new Rect(placement.X, placement.Y, placement.Width, placement.Height));
            }
        }

        return finalSize;
    }
}
