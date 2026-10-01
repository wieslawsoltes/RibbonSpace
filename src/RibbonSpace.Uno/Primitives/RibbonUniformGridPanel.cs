using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>Uniform wrapping grid (galleries, swatches).</summary>
public partial class RibbonUniformGridPanel : Panel
{
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int), typeof(RibbonUniformGridPanel), new PropertyMetadata(4, (d, _) => ((UIElement)d).InvalidateMeasure()));

    /// <summary>Identifies <see cref="ItemWidth"/>.</summary>
    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(RibbonUniformGridPanel), new PropertyMetadata(64d, (d, _) => ((UIElement)d).InvalidateMeasure()));

    /// <summary>Identifies <see cref="ItemHeight"/>.</summary>
    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(RibbonUniformGridPanel), new PropertyMetadata(56d, (d, _) => ((UIElement)d).InvalidateMeasure()));

    /// <summary>Identifies <see cref="Spacing"/>.</summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(RibbonUniformGridPanel), new PropertyMetadata(2d, (d, _) => ((UIElement)d).InvalidateMeasure()));

    /// <summary>Columns.</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Cell width.</summary>
    public double ItemWidth { get => (double)GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }

    /// <summary>Cell height.</summary>
    public double ItemHeight { get => (double)GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }

    /// <summary>Spacing between cells.</summary>
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>First visible row (for row-scrolled in-ribbon galleries); rows before it are not arranged.</summary>
    public int FirstRow { get; set; }

    /// <summary>Maximum rows arranged (0 = all).</summary>
    public int MaxRows { get; set; }

    /// <summary>Number of rows needed for all children.</summary>
    public int RowCount => Columns <= 0 ? 0 : (int)Math.Ceiling(Children.Count(c => c.Visibility == Visibility.Visible) / (double)Columns);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(ItemWidth, ItemHeight);
        foreach (var child in Children)
        {
            child.Measure(size);
        }

        var rows = MaxRows > 0 ? Math.Min(MaxRows, Math.Max(1, RowCount)) : RowCount;
        var columns = Math.Max(1, Columns);
        return new Size((columns * ItemWidth) + ((columns - 1) * Spacing), Math.Max(0, (rows * ItemHeight) + ((rows - 1) * Spacing)));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Math.Max(1, Columns);
        var index = 0;
        foreach (var child in Children)
        {
            if (child.Visibility != Visibility.Visible)
            {
                continue;
            }

            var row = (index / columns) - FirstRow;
            var column = index % columns;
            index++;
            if (row < 0 || (MaxRows > 0 && row >= MaxRows))
            {
                // Rows scrolled out of the window are not reachable with Tab.
                child.Arrange(new Rect(0, 0, 0, 0));
                RibbonHiddenFocus.Set(child, true);
                continue;
            }

            child.Arrange(new Rect(column * (ItemWidth + Spacing), row * (ItemHeight + Spacing), ItemWidth, ItemHeight));
            RibbonHiddenFocus.Set(child, false);
        }

        return finalSize;
    }
}
