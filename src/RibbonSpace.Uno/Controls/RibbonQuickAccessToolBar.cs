using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;

namespace RibbonSpace.Controls;

/// <summary>Quick Access Toolbar: small command buttons plus the customize drop-down.</summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonQuickAccessToolBar : Control, IRibbonLayoutHost
{
    /// <summary>Identifies <see cref="ShowCustomizeButton"/>.</summary>
    public static readonly DependencyProperty ShowCustomizeButtonProperty = DependencyProperty.Register(nameof(ShowCustomizeButton), typeof(bool), typeof(RibbonQuickAccessToolBar), new PropertyMetadata(true, (d, _) => ((RibbonQuickAccessToolBar)d).UpdateCustomizeButton()));

    /// <summary>Identifies <see cref="ShowLabels"/>.</summary>
    public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(nameof(ShowLabels), typeof(bool), typeof(RibbonQuickAccessToolBar), new PropertyMetadata(false, (d, _) => ((RibbonQuickAccessToolBar)d).ApplyLayouts()));

    /// <summary>Identifies <see cref="Metrics"/>.</summary>
    public static readonly DependencyProperty MetricsProperty = DependencyProperty.Register(nameof(Metrics), typeof(RibbonMetrics), typeof(RibbonQuickAccessToolBar), new PropertyMetadata(RibbonMetrics.Comfortable, (d, _) => ((RibbonQuickAccessToolBar)d).ApplyLayouts()));

    private Panel? _host;
    private Button? _customizeButton;
    private readonly HashSet<UIElement> _knownItems = [];

    /// <summary>Creates the toolbar.</summary>
    public RibbonQuickAccessToolBar()
    {
        DefaultStyleKey = typeof(RibbonQuickAccessToolBar);
        RibbonTheme.EnsureResources();
        IsTabStop = false;
        Items.CollectionChanged += OnItemsChanged;
        AutomationProperties.SetName(this, RibbonStrings.Current.QuickAccessToolbar);
    }

    /// <summary>Items (usually small buttons; linked copies of ribbon items are added by "Add to Quick Access Toolbar").</summary>
    public ObservableCollection<UIElement> Items { get; } = [];

    /// <summary>Shows the customize drop-down.</summary>
    public bool ShowCustomizeButton { get => (bool)GetValue(ShowCustomizeButtonProperty); set => SetValue(ShowCustomizeButtonProperty, value); }

    /// <summary>Shows labels next to icons (Office "Show command labels").</summary>
    public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

    /// <summary>Metrics.</summary>
    public RibbonMetrics Metrics { get => (RibbonMetrics)GetValue(MetricsProperty); set => SetValue(MetricsProperty, value); }

    /// <summary>Owning ribbon.</summary>
    public Ribbon? Ribbon { get; internal set; }

    /// <summary>The customize button.</summary>
    public Button? CustomizeButton => _customizeButton;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_customizeButton is not null)
        {
            _customizeButton.Click -= OnCustomizeClick;
        }

        _host?.Children.Clear();
        base.OnApplyTemplate();
        _host = GetTemplateChild("PART_ItemsHost") as Panel;
        _customizeButton = GetTemplateChild("PART_CustomizeButton") as Button;
        if (_customizeButton is not null)
        {
            _customizeButton.Click += OnCustomizeClick;
            ToolTipService.SetToolTip(_customizeButton, RibbonStrings.Current.CustomizeQuickAccessToolbar);
            AutomationProperties.SetName(_customizeButton, RibbonStrings.Current.CustomizeQuickAccessToolbar);
        }

        SyncItems();
        UpdateCustomizeButton();
    }

    private void UpdateCustomizeButton()
    {
        if (_customizeButton is not null)
        {
            _customizeButton.Visibility = ShowCustomizeButton ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnCustomizeClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is not null && _customizeButton is not null)
        {
            Ribbon.BuildQuickAccessCustomizeMenu().ShowAt(_customizeButton);
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Linked copies that left the toolbar (Remove, Replace or Clear) stop listening to their source item.
        foreach (var removed in _knownItems.Where(i => !Items.Contains(i)).ToArray())
        {
            if (RibbonItemHelper.GetSourceItem(removed) is not null)
            {
                RibbonItemHelper.UnlinkTree(removed);
            }
        }

        _knownItems.Clear();
        _knownItems.UnionWith(Items);
        SyncItems();
        Ribbon?.OnQuickAccessItemsChanged();
    }

    private void SyncItems()
    {
        if (_host is null)
        {
            return;
        }

        _host.Children.Clear();
        foreach (var item in Items)
        {
            if (VisualTreeHelper.GetParent(item) is Panel parent)
            {
                parent.Children.Remove(item);
            }

            if (Ribbon is not null)
            {
                RibbonItemHelper.SetOwner(item, Ribbon);
            }

            _host.Children.Add(item);
        }

        ApplyLayouts();
    }

    internal void ApplyLayouts()
    {
        foreach (var item in Items.OfType<IRibbonItem>())
        {
            item.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Small, Metrics, true, ShowLabels));
        }
    }

    /// <inheritdoc />
    public void InvalidateItemsLayout() => InvalidateMeasure();

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonToolBarAutomationPeer(this);
}

/// <summary>Automation peer for toolbars (ToolBar control type).</summary>
public sealed partial class RibbonToolBarAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    /// <inheritdoc />
    protected override string GetClassNameCore() => Owner.GetType().Name;
}
