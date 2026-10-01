using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Layout;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>
/// Lays out the groups of a tab. In classic mode groups adaptively shrink (Large → Medium → Small → Collapsed)
/// using <see cref="RibbonAdaptiveLayout"/>; in simplified mode items move into the overflow menu using
/// <see cref="RibbonSimplifiedLayout"/>.
/// </summary>
public partial class RibbonGroupsPanel : Panel, IRibbonScrollContent
{
    private readonly Dictionary<RibbonGroup, (int Version, RibbonMetrics Metrics, double[] Widths)> _classicCache = [];
    private readonly Dictionary<RibbonGroup, (int Version, RibbonMetrics Metrics, double[] Widths)> _simplifiedCache = [];
    private UIElement? _overflowButton;
    private double _extent;

    /// <summary>Simplified mode.</summary>
    public bool IsSimplified { get; set; }

    /// <summary>Metrics.</summary>
    public RibbonMetrics Metrics { get; set; } = RibbonMetrics.Comfortable;

    /// <summary>Enables adaptive resizing (disable to always show large groups and scroll).</summary>
    public bool IsAdaptive { get; set; } = true;

    /// <summary>Spacing between groups.</summary>
    public double GroupSpacing { get; set; }

    /// <summary>True when the simplified line has overflowing items.</summary>
    public bool HasOverflow { get; private set; }

    /// <summary>Raised after <see cref="HasOverflow"/> changed.</summary>
    public event EventHandler? OverflowChanged;

    /// <summary>Overflow ("More options") button placed at the end of the simplified line.</summary>
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

    /// <inheritdoc />
    public double ExtentWidth => _extent;

    /// <summary>Groups in display order.</summary>
    public IEnumerable<RibbonGroup> Groups => Children.OfType<RibbonGroup>();

    /// <summary>Drops cached measurements.</summary>
    public void InvalidateCache()
    {
        _classicCache.Clear();
        _simplifiedCache.Clear();
        InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var groups = Groups.Where(g => g.IsShown).ToArray();
        foreach (var stale in _classicCache.Keys.Where(k => !groups.Contains(k)).ToArray())
        {
            _classicCache.Remove(stale);
            _simplifiedCache.Remove(stale);
        }

        var height = IsSimplified ? Metrics.SimplifiedHeight : Metrics.GroupContentHeight + Metrics.GroupCaptionHeight + 4;
        var result = IsSimplified ? MeasureSimplified(groups, availableSize.Width, height) : MeasureClassic(groups, availableSize.Width, height);
        _extent = result.Width;
        return result;
    }

    private Size MeasureClassic(RibbonGroup[] groups, double available, double height)
    {
        SetOverflow(false);
        _overflowButton?.Measure(new Size(0, 0));
        var states = new RibbonGroupState[groups.Length];
        if (IsAdaptive && !double.IsInfinity(available))
        {
            var infos = new RibbonGroupLayoutInfo[groups.Length];
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (!_classicCache.TryGetValue(group, out var cached) || cached.Version != group.LayoutVersion || !ReferenceEquals(cached.Metrics, Metrics))
                {
                    var widths = new double[4];
                    for (var s = 0; s < 4; s++)
                    {
                        widths[s] = group.MeasureWidth((RibbonGroupState)s, Metrics, height);
                    }

                    cached = (group.LayoutVersion, Metrics, widths);

                    _classicCache[group] = cached;
                }

                infos[i] = new RibbonGroupLayoutInfo(cached.Widths, group.ReductionOrder, group.CanCollapse);
            }

            states = RibbonAdaptiveLayout.Compute(infos, available, GroupSpacing).States.ToArray();

        }

        var total = 0d;
        for (var i = 0; i < groups.Length; i++)
        {
            groups[i].ApplyState(states[i], false, Metrics);
            groups[i].Measure(new Size(double.PositiveInfinity, height));
            total += groups[i].DesiredSize.Width + (i > 0 ? GroupSpacing : 0);
        }

        foreach (var hidden in Groups.Where(g => !g.IsShown))
        {
            hidden.Measure(new Size(0, 0));
        }

        return new Size(total, height);
    }

    private Size MeasureSimplified(RibbonGroup[] groups, double available, double height)
    {
        var overflowWidth = 0d;
        if (_overflowButton is not null)
        {
            _overflowButton.Measure(new Size(double.PositiveInfinity, height));
            overflowWidth = _overflowButton.DesiredSize.Width + 4;
        }

        var model = new RibbonSimplifiedGroup[groups.Length];
        var chrome = new double[groups.Length];
        for (var i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            if (!_simplifiedCache.TryGetValue(group, out var cached) || cached.Version != group.LayoutVersion || !ReferenceEquals(cached.Metrics, Metrics))
            {
                cached = (group.LayoutVersion, Metrics, group.MeasureSimplifiedItemWidths(Metrics));
                _simplifiedCache[group] = cached;
            }

            var children = group.ItemsPanel.Children;
            var items = new RibbonSimplifiedItem[cached.Widths.Length];
            for (var j = 0; j < items.Length; j++)
            {
                var child = j < children.Count ? children[j] : null;
                var visibility = group.SimplifiedVisibility == RibbonSimplifiedVisibility.Auto ? (child as IRibbonItem)?.SimplifiedVisibility ?? RibbonSimplifiedVisibility.Auto : group.SimplifiedVisibility;
                if (child is null || child.Visibility == Visibility.Collapsed)
                {
                    visibility = RibbonSimplifiedVisibility.Hidden;
                }

                items[j] = new RibbonSimplifiedItem(cached.Widths[j], visibility);
            }

            model[i] = new RibbonSimplifiedGroup(items, group.ReductionOrder);
            chrome[i] = 9; // group padding + separator
        }

        var chromeTotal = chrome.Sum();
        var result = double.IsInfinity(available)
            ? RibbonSimplifiedLayout.Compute(model, double.MaxValue, overflowWidth)
            : RibbonSimplifiedLayout.Compute(model, Math.Max(0, available - chromeTotal), overflowWidth);
        var total = 0d;
        for (var i = 0; i < groups.Length; i++)
        {
            var mask = result.InLine[i];
            groups[i].ApplyState(RibbonGroupState.Large, true, Metrics, mask.ToArray());
            groups[i].Measure(new Size(double.PositiveInfinity, height));
            var anyInLine = mask.Any(v => v);
            if (!anyInLine)
            {
                continue;
            }

            total += groups[i].DesiredSize.Width;
        }

        SetOverflow(result.HasOverflow);
        if (HasOverflow)
        {
            total += overflowWidth;
        }

        return new Size(total, height);
    }

    private void SetOverflow(bool value)
    {
        if (HasOverflow == value)
        {
            return;
        }

        HasOverflow = value;
        DispatcherQueue?.TryEnqueue(() => OverflowChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0d;
        foreach (var child in Children)
        {
            if (ReferenceEquals(child, _overflowButton))
            {
                continue;
            }

            if (child is RibbonGroup group && (!group.IsShown || (IsSimplified && group.ItemsPanel.InLineMask?.Any(v => v) == false)))
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                RibbonHiddenFocus.Set(child, true);
                continue;
            }

            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
            RibbonHiddenFocus.Set(child, false);
            x += child.DesiredSize.Width + GroupSpacing;
        }

        if (_overflowButton is not null)
        {
            if (IsSimplified && HasOverflow)
            {
                var w = _overflowButton.DesiredSize.Width;
                var h = _overflowButton.DesiredSize.Height;
                var ox = Math.Max(x, finalSize.Width - w - 2);
                _overflowButton.Visibility = Visibility.Visible;
                _overflowButton.Arrange(new Rect(ox, (finalSize.Height - h) / 2, w, h));
            }
            else
            {
                _overflowButton.Arrange(new Rect(0, 0, 0, 0));
            }
        }

        return finalSize;
    }
}
