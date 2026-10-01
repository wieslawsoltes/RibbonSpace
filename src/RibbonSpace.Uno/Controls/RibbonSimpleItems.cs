using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace RibbonSpace.Controls;

/// <summary>Vertical separator between items (horizontal inside vertical toolbars and menus).</summary>
public partial class RibbonSeparator : RibbonControlBase
{
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(RibbonSeparator), new PropertyMetadata(Orientation.Vertical, (d, _) => ((RibbonSeparator)d).UpdateOrientation()));

    /// <summary>Creates a separator.</summary>
    public RibbonSeparator()
    {
        DefaultStyleKey = typeof(RibbonSeparator);
        CanAddToQuickAccess = false;
        IsTabStop = false;
        Size = RibbonItemSize.Large;
    }

    /// <summary>Orientation of the line.</summary>
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateOrientation();
    }

    /// <inheritdoc />
    protected override void OnLayoutAppliedCore(RibbonItemLayout layout) => UpdateOrientation();

    private void UpdateOrientation()
    {
        VisualStateManager.GoToState(this, Orientation == Orientation.Vertical ? "Vertical" : "Horizontal", false);
        var isVertical = Orientation == Orientation.Vertical;
        HorizontalAlignment = isVertical ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        VerticalAlignment = isVertical ? VerticalAlignment.Center : VerticalAlignment.Top;
        if (GetTemplateChild("PART_Line") is FrameworkElement line)
        {
            var vertical = isVertical;
            line.Width = vertical ? 1 : double.NaN;
            line.Height = vertical ? (IsSimplified ? Metrics.SimplifiedItemHeight - 8 : Metrics.GroupContentHeight - 6) : 1;
            line.Margin = vertical ? new Thickness(3, 3, 3, 3) : new Thickness(4, 3, 4, 3);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore() => [new MenuFlyoutSeparator()];

    /// <inheritdoc />
    protected override bool InvokeCore() => false;
}

/// <summary>Static text item.</summary>
public partial class RibbonLabel : RibbonControlBase
{
    /// <summary>Creates a label.</summary>
    public RibbonLabel()
    {
        DefaultStyleKey = typeof(RibbonLabel);
        CanAddToQuickAccess = false;
        IsTabStop = false;
    }

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore() => [];

    /// <inheritdoc />
    protected override bool InvokeCore() => false;
}

/// <summary>Base of ribbon item containers (button groups, stacks, rows).</summary>
[ContentProperty(Name = nameof(Items))]
public abstract partial class RibbonItemsContainer : RibbonControlBase, IRibbonLayoutHost
{
    private Panel? _host;

    /// <summary>Creates the container.</summary>
    protected RibbonItemsContainer()
    {
        Items.CollectionChanged += OnItemsChanged;
        CanAddToQuickAccess = false;
        IsTabStop = false;
    }

    /// <summary>Child items.</summary>
    public ObservableCollection<UIElement> Items { get; } = [];

    /// <summary>Panel hosting the children.</summary>
    protected Panel? Host => _host;

    /// <summary>Creates an empty container of the same kind (linked copies).</summary>
    protected abstract RibbonItemsContainer CreateContainerCopy();

    /// <summary>Size applied to children for a layout.</summary>
    protected abstract RibbonItemLayout GetChildLayout(IRibbonItem child, RibbonItemLayout layout);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        _host?.Children.Clear();
        base.OnApplyTemplate();
        _host = GetTemplateChild("PART_ItemsHost") as Panel;
        if (_host is not null)
        {
            foreach (var item in Items)
            {
                Detach(item);
                _host.Children.Add(item);
            }
        }

        ApplyChildLayouts();
    }

    private static void Detach(UIElement item)
    {
        if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(item) is Panel panel)
        {
            panel.Children.Remove(item);
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_host is not null)
        {
            _host.Children.Clear();
            foreach (var item in Items)
            {
                Detach(item);
                _host.Children.Add(item);
            }
        }

        ApplyChildLayouts();
        RibbonItemHelper.InvalidateHostLayout(this);
    }

    private RibbonItemLayout _lastLayout = new(RibbonItemSize.Small, RibbonMetrics.Comfortable);

    /// <inheritdoc />
    protected override void OnLayoutAppliedCore(RibbonItemLayout layout)
    {
        _lastLayout = layout;
        ApplyChildLayouts();
    }

    private void ApplyChildLayouts()
    {
        foreach (var child in Items.OfType<IRibbonItem>())
        {
            child.ApplyLayout(GetChildLayout(child, _lastLayout));
        }
    }

    /// <inheritdoc />
    public void InvalidateItemsLayout() => RibbonItemHelper.InvalidateHostLayout(this);

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
        => Items.OfType<IRibbonItem>().SelectMany(i => i.CreateOverflowMenuItems());

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = CreateContainerCopy();
        if (copy is RibbonStackPanel stack && this is RibbonStackPanel source)
        {
            stack.Orientation = source.Orientation;
            stack.Spacing = source.Spacing;
        }

        foreach (var item in Items)
        {
            if (item is IRibbonItem ribbonItem && ribbonItem.CreateLinkedCopy() is { } child)
            {
                RibbonItemHelper.SetSourceItem(child, (FrameworkElement)item);
                copy.Items.Add(child);
            }
        }

        return copy.Items.Count > 0 ? copy : null;
    }

    /// <inheritdoc />
    protected override RibbonKeyTipResult OnKeyTipCore() => RibbonKeyTipResult.Close;

    /// <inheritdoc />
    protected override bool InvokeCore() => false;
}

/// <summary>Joined row of small buttons (Bold / Italic / Underline, alignment).</summary>
public partial class RibbonButtonGroup : RibbonItemsContainer
{
    /// <summary>Creates a button group.</summary>
    public RibbonButtonGroup()
    {
        DefaultStyleKey = typeof(RibbonButtonGroup);
        Size = RibbonItemSize.Small;
    }

    /// <inheritdoc />
    protected override RibbonItemLayout GetChildLayout(IRibbonItem child, RibbonItemLayout layout)
        => layout with { Size = RibbonItemSize.Small, ShowLabel = false };

    /// <inheritdoc />
    protected override RibbonItemsContainer CreateContainerCopy() => new RibbonButtonGroup();
}

/// <summary>
/// Stack of items acting as one item: a horizontal row (used by groups with <see cref="RibbonGroupItemsLayout.Rows"/>)
/// or a vertical column.
/// </summary>
public partial class RibbonStackPanel : RibbonItemsContainer
{
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(RibbonStackPanel), new PropertyMetadata(Orientation.Horizontal));

    /// <summary>Identifies <see cref="Spacing"/>.</summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(RibbonStackPanel), new PropertyMetadata(3d));

    /// <summary>Creates a stack panel.</summary>
    public RibbonStackPanel()
    {
        DefaultStyleKey = typeof(RibbonStackPanel);
        Size = RibbonItemSize.Small;
    }

    /// <summary>Orientation.</summary>
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Spacing between children.</summary>
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <inheritdoc />
    protected override RibbonItemsContainer CreateContainerCopy() => new RibbonStackPanel();

    /// <inheritdoc />
    protected override RibbonItemLayout GetChildLayout(IRibbonItem child, RibbonItemLayout layout)
        => layout with { Size = layout.IsSimplified ? RibbonItemSize.Small : child.EffectiveSizeDefinition.GetSize(layout.GroupState), ShowLabel = null };
}
