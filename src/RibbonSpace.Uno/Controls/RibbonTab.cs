using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Localization;
using Windows.UI;

namespace RibbonSpace.Controls;

/// <summary>Ribbon tab: a header in the tab row and a set of groups.</summary>
[ContentProperty(Name = nameof(Groups))]
public partial class RibbonTab : Control
{
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(RibbonTab), new PropertyMetadata(null, (d, _) => ((RibbonTab)d).Ribbon?.OnTabsChanged()));

    /// <summary>Identifies <see cref="Id"/>.</summary>
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(RibbonTab), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="KeyTip"/>.</summary>
    public static readonly DependencyProperty KeyTipProperty = DependencyProperty.Register(nameof(KeyTip), typeof(string), typeof(RibbonTab), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsTabVisible"/>.</summary>
    public static readonly DependencyProperty IsTabVisibleProperty = DependencyProperty.Register(nameof(IsTabVisible), typeof(bool), typeof(RibbonTab), new PropertyMetadata(true, (d, _) => ((RibbonTab)d).Ribbon?.OnTabsChanged()));

    /// <summary>Identifies <see cref="ContextualGroupId"/>.</summary>
    public static readonly DependencyProperty ContextualGroupIdProperty = DependencyProperty.Register(nameof(ContextualGroupId), typeof(string), typeof(RibbonTab), new PropertyMetadata(null, (d, _) => ((RibbonTab)d).Ribbon?.OnTabsChanged()));

    /// <summary>Identifies <see cref="IsSelected"/>.</summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RibbonTab), new PropertyMetadata(false));

    /// <summary>Identifies <see cref="ScreenTip"/>.</summary>
    public static readonly DependencyProperty ScreenTipProperty = DependencyProperty.Register(nameof(ScreenTip), typeof(object), typeof(RibbonTab), new PropertyMetadata(null, (d, _) => ((RibbonTab)d).HeaderElement?.UpdateText()));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonTab), new PropertyMetadata(null, (d, _) => ((RibbonTab)d).HeaderElement?.UpdateText()));

    private readonly RibbonGroupsPanel _groupsPanel = new();
    private readonly RibbonButton _overflowButton;
    private RibbonScrollPanel? _scrollHost;
    private bool _hiddenByCustomization;

    /// <summary>Creates a tab.</summary>
    public RibbonTab()
    {
        DefaultStyleKey = typeof(RibbonTab);
        RibbonTheme.EnsureResources();
        IsTabStop = false;
        Groups.CollectionChanged += OnGroupsChanged;
        IsEnabledChanged += (_, _) => Ribbon?.OnTabsChanged();
        _overflowButton = new RibbonButton { Icon = "", Label = RibbonStrings.Current.MoreOptions, ShowLabel = false, CanAddToQuickAccess = false, Id = "ribbon.overflow" };
        _overflowButton.IsChromeButton = true;
        _overflowButton.Click += (_, _) => DispatcherQueue?.TryEnqueue(ShowOverflow);
        ToolTipService.SetToolTip(_overflowButton, RibbonStrings.Current.MoreOptions);
        _groupsPanel.OverflowButton = _overflowButton;
    }

    /// <summary>Groups.</summary>
    public ObservableCollection<RibbonGroup> Groups { get; } = [];

    private IReadOnlyList<string>? _groupOrder;

    /// <summary>User group order from the customization (<c>RibbonCustomization.GroupOrder</c>); <c>null</c> keeps <see cref="Groups"/> order.</summary>
    internal IReadOnlyList<string>? GroupOrder
    {
        get => _groupOrder;
        set
        {
            if (ReferenceEquals(_groupOrder, value) || (_groupOrder is not null && value is not null && _groupOrder.SequenceEqual(value)))
            {
                return;
            }

            _groupOrder = value;
            SyncGroups();
        }
    }

    /// <summary>Groups in display order (the user's group order applied to <see cref="Groups"/>).</summary>
    public IReadOnlyList<RibbonGroup> DisplayGroups
    {
        get
        {
            if (_groupOrder is not { Count: > 0 } order)
            {
                return Groups;
            }

            var listed = order.Select(id => Groups.FirstOrDefault(g => !g.IsCustom && g.EffectiveId == id)).OfType<RibbonGroup>().Distinct().ToList();
            return listed.Concat(Groups.Where(g => !listed.Contains(g) && !g.IsCustom)).Concat(Groups.Where(g => g.IsCustom)).ToArray();
        }
    }

    /// <summary>Optional icon shown before the header text (glyph, path, image or IconSource, like item icons).</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Refreshes localized texts after <see cref="RibbonStrings.Current"/> changes.</summary>
    internal void OnStringsChanged()
    {
        _overflowButton.Label = RibbonStrings.Current.MoreOptions;
        ToolTipService.SetToolTip(_overflowButton, RibbonStrings.Current.MoreOptions);
        HeaderElement?.UpdateText();
        foreach (var group in Groups)
        {
            group.OnStringsChanged();
        }
    }

    /// <summary>Header text.</summary>
    public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Stable id.</summary>
    public string? Id { get => (string?)GetValue(IdProperty); set => SetValue(IdProperty, value); }

    /// <summary>KeyTip.</summary>
    public string? KeyTip { get => (string?)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

    /// <summary>Visibility of the tab (independent of selection).</summary>
    public bool IsTabVisible { get => (bool)GetValue(IsTabVisibleProperty); set => SetValue(IsTabVisibleProperty, value); }

    /// <summary>Id of the <see cref="RibbonContextualTabGroup"/> owning this tab.</summary>
    public string? ContextualGroupId { get => (string?)GetValue(ContextualGroupIdProperty); set => SetValue(ContextualGroupIdProperty, value); }

    /// <summary>True for the selected tab.</summary>
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); internal set => SetValue(IsSelectedProperty, value); }

    /// <summary>Tooltip of the tab header.</summary>
    public object? ScreenTip { get => GetValue(ScreenTipProperty); set => SetValue(ScreenTipProperty, value); }

    /// <summary>Owning ribbon.</summary>
    public Ribbon? Ribbon { get; internal set; }

    /// <summary>The generated header in the tab row.</summary>
    public RibbonTabHeader? HeaderElement { get; internal set; }

    /// <summary>Contextual group.</summary>
    public RibbonContextualTabGroup? ContextualGroup => ContextualGroupId is null ? null : Ribbon?.FindContextualGroup(ContextualGroupId);

    /// <summary>True for contextual tabs.</summary>
    public bool IsContextual => !string.IsNullOrEmpty(ContextualGroupId);

    /// <summary>The adaptive groups panel.</summary>
    public RibbonGroupsPanel GroupsPanel => _groupsPanel;

    /// <summary>The simplified overflow ("More options") button.</summary>
    public RibbonButton OverflowButton => _overflowButton;

    internal string? EffectiveId => !string.IsNullOrEmpty(Id) ? Id : !string.IsNullOrEmpty(Name) ? Name : Header;

    internal string? OriginalHeader { get; set; }

    internal bool HiddenByCustomization
    {
        get => _hiddenByCustomization;
        set
        {
            _hiddenByCustomization = value;
            Ribbon?.OnTabsChanged();
        }
    }

    internal bool IsCustom { get; set; }

    /// <summary>True when the tab should appear in the tab row.</summary>
    public bool IsAvailable => IsTabVisible && !_hiddenByCustomization && (!IsContextual || ContextualGroup?.IsVisible == true);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_scrollHost is not null)
        {
            _scrollHost.Content = null;
        }

        base.OnApplyTemplate();
        _scrollHost = GetTemplateChild("PART_ScrollHost") as RibbonScrollPanel;
        if (_scrollHost is not null)
        {
            _scrollHost.Content = _groupsPanel;
        }

        SyncGroups();
    }

    private void OnGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncGroups();

    private void SyncGroups()
    {
        var overflow = _groupsPanel.OverflowButton;
        _groupsPanel.OverflowButton = null;
        _groupsPanel.Children.Clear();
        foreach (var group in DisplayGroups)
        {
            if (VisualTreeHelper.GetParent(group) is Panel parent)
            {
                parent.Children.Remove(group);
            }

            group.Tab = this;
            if (Ribbon is not null)
            {
                group.AttachToRibbon(Ribbon);
            }

            _groupsPanel.Children.Add(group);
            group.ApplyTemplate();
        }

        _groupsPanel.OverflowButton = overflow;
        _groupsPanel.InvalidateCache();
        Ribbon?.OnGroupsChanged(this);
    }

    internal void AttachToRibbon(Ribbon ribbon)
    {
        Ribbon = ribbon;
        RibbonItemHelper.SetOwner(this, ribbon);
        RibbonItemHelper.SetOwner(_overflowButton, ribbon);
        foreach (var group in Groups)
        {
            group.Tab = this;
            group.AttachToRibbon(ribbon);
        }
    }

    /// <summary>Invalidates the adaptive layout of the groups.</summary>
    public void InvalidateGroupsLayout()
    {
        _groupsPanel.InvalidateMeasure();
        _scrollHost?.InvalidateMeasure();
        InvalidateMeasure();
    }

    internal void ApplyPresentation(RibbonMetrics metrics, bool simplified, bool adaptive)
    {
        var changed = !ReferenceEquals(_groupsPanel.Metrics, metrics) || _groupsPanel.IsSimplified != simplified || _groupsPanel.IsAdaptive != adaptive;
        _groupsPanel.Metrics = metrics;
        _groupsPanel.IsSimplified = simplified;
        _groupsPanel.IsAdaptive = adaptive;
        _overflowButton.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Small, metrics, true, false));
        if (changed)
        {
            _groupsPanel.InvalidateCache();
            InvalidateGroupsLayout();
        }
    }

    /// <summary>Opens the simplified overflow menu.</summary>
    public void ShowOverflow()
    {
        var panel = new StackPanel { Spacing = 1, MinWidth = 220, Padding = new Thickness(2) };
        foreach (var group in DisplayGroups.Where(g => g.IsShown))
        {
            var items = group.GetOverflowItems().ToArray();
            if (items.Length == 0)
            {
                continue;
            }

            if (panel.Children.Count > 0)
            {
                panel.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Margin = new Thickness(4, 4, 4, 4), Fill = (Brush)Application.Current.Resources["RibbonSeparatorBrush"] });
            }

            panel.Children.Add(new TextBlock
            {
                Text = group.Header,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(6, 2, 6, 2),
                Foreground = (Brush)Application.Current.Resources["RibbonSecondaryForegroundBrush"],
            });
            foreach (var item in items)
            {
                if (item is IRibbonItem ribbonItem && (item is RibbonGallery gallery ? CreateGalleryCopy(gallery) : ribbonItem.CreateLinkedCopy()) is { } copy)
                {
                    RibbonItemHelper.SetOwner(copy, Ribbon);
                    if (copy is IRibbonItem copyItem)
                    {
                        copyItem.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Ribbon?.Metrics ?? RibbonMetrics.Comfortable, false, true));
                    }

                    copy.HorizontalAlignment = HorizontalAlignment.Stretch;
                    panel.Children.Add(copy);
                }
            }
        }

        var flyout = new Flyout { Content = new ScrollViewer { Content = panel, MaxHeight = 520, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
        if (Application.Current.Resources.TryGetValue("RibbonFlyoutPresenterStyle", out var style) && style is Style presenterStyle)
        {
            flyout.FlyoutPresenterStyle = presenterStyle;
        }

        RibbonItemHelper.SetOwner(panel, Ribbon);
        flyout.Closed += (_, _) => RibbonItemHelper.UnlinkTree(panel);
        flyout.ShowAt(_overflowButton);
    }

    private static FrameworkElement? CreateGalleryCopy(RibbonGallery gallery)
        => ((IRibbonItem)gallery).CreateLinkedCopy();

    /// <summary>Items reachable by KeyTips.</summary>
    internal IEnumerable<FrameworkElement> GetKeyTipTargets()
    {
        foreach (var group in DisplayGroups.Where(g => g.IsShown))
        {
            foreach (var target in group.GetKeyTipTargets())
            {
                yield return target;
            }
        }

        if (_groupsPanel.IsSimplified && _groupsPanel.HasOverflow)
        {
            yield return _overflowButton;
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}

/// <summary>Header of a tab in the ribbon tab row.</summary>
public partial class RibbonTabHeader : Button
{
    /// <summary>Identifies <see cref="IsSelected"/>.</summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RibbonTabHeader), new PropertyMetadata(false, (d, _) => ((RibbonTabHeader)d).UpdateStates()));

    /// <summary>Identifies <see cref="ContextualBrush"/>.</summary>
    public static readonly DependencyProperty ContextualBrushProperty = DependencyProperty.Register(nameof(ContextualBrush), typeof(Brush), typeof(RibbonTabHeader), new PropertyMetadata(null, (d, _) => ((RibbonTabHeader)d).UpdateStates()));

    /// <summary>Identifies <see cref="IsContextual"/>.</summary>
    public static readonly DependencyProperty IsContextualProperty = DependencyProperty.Register(nameof(IsContextual), typeof(bool), typeof(RibbonTabHeader), new PropertyMetadata(false, (d, _) => ((RibbonTabHeader)d).UpdateStates()));

    /// <summary>Identifies <see cref="ContextualHeader"/>.</summary>
    public static readonly DependencyProperty ContextualHeaderProperty = DependencyProperty.Register(nameof(ContextualHeader), typeof(string), typeof(RibbonTabHeader), new PropertyMetadata(null));

    /// <summary>Creates a tab header.</summary>
    public RibbonTabHeader(RibbonTab tab)
    {
        DefaultStyleKey = typeof(RibbonTabHeader);
        Tab = tab;
        Content = tab.Header;
        AutomationProperties.SetName(this, tab.Header ?? string.Empty);
        if (tab.EffectiveId is { } id)
        {
            AutomationProperties.SetAutomationId(this, "RibbonTab_" + id);
        }
    }

    /// <summary>The tab.</summary>
    public RibbonTab Tab { get; }

    /// <summary>Selected state.</summary>
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>Selects the tab (UI Automation SelectionItem pattern, keyboard).</summary>
    public void Select() => Tab.Ribbon?.SelectTab(Tab);

    /// <summary>Contextual accent.</summary>
    public Brush? ContextualBrush { get => (Brush?)GetValue(ContextualBrushProperty); set => SetValue(ContextualBrushProperty, value); }

    /// <summary>True for contextual tabs.</summary>
    public bool IsContextual { get => (bool)GetValue(IsContextualProperty); set => SetValue(IsContextualProperty, value); }

    /// <summary>Header of the contextual group ("Table Tools").</summary>
    public string? ContextualHeader { get => (string?)GetValue(ContextualHeaderProperty); set => SetValue(ContextualHeaderProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateStates();
    }

    private void UpdateStates()
    {
        var contextual = IsContextual && ContextualBrush is not null;
        VisualStateManager.GoToState(this, !IsSelected ? "Unselected" : contextual ? "SelectedContextual" : "Selected", true);
        VisualStateManager.GoToState(this, IsContextual ? "Contextual" : "Regular", true);
        if (GetTemplateChild("PART_Indicator") is Microsoft.UI.Xaml.Shapes.Rectangle indicator && contextual)
        {
            indicator.Fill = ContextualBrush;
        }

        if (GetTemplateChild("PART_ContextualBand") is Microsoft.UI.Xaml.Shapes.Rectangle band && ContextualBrush is not null)
        {
            band.Fill = ContextualBrush;
        }

        switch (GetTemplateChild("PART_Text"))
        {
            case ContentPresenter presenter when contextual:
                presenter.Foreground = ContextualBrush;
                break;
            case ContentPresenter presenter:
                presenter.ClearValue(ContentPresenter.ForegroundProperty);
                break;
            case TextBlock text when contextual:
                text.Foreground = ContextualBrush;
                break;
            case TextBlock text:
                text.ClearValue(TextBlock.ForegroundProperty);
                break;
        }

        UpdateText();
    }

    /// <summary>Refreshes header text, tooltip and automation name (rename, customization, language change).</summary>
    internal void UpdateText()
    {
        if (!Equals(Content, Tab.Header))
        {
            Content = Tab.Header;
        }

        AutomationProperties.SetName(this, Tab.Header ?? string.Empty);
        if (GetTemplateChild("PART_Icon") is RibbonIconPresenter icon)
        {
            icon.Icon = Tab.Icon;
            icon.Visibility = Tab.Icon is null ? Visibility.Collapsed : Visibility.Visible;
        }

        ToolTipService.SetToolTip(this, IsContextual && !string.IsNullOrEmpty(ContextualHeader) ? $"{ContextualHeader} › {Tab.Header}" : Tab.ScreenTip);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabHeaderAutomationPeer(this);
}

/// <summary>Automation peer of <see cref="RibbonTabHeader"/> (TabItem with the SelectionItem pattern).</summary>
public sealed partial class RibbonTabHeaderAutomationPeer(RibbonTabHeader owner) : ButtonAutomationPeer(owner), Microsoft.UI.Xaml.Automation.Provider.ISelectionItemProvider
{
    private RibbonTabHeader Header => (RibbonTabHeader)Owner;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(RibbonTabHeader);

    /// <inheritdoc />
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.SelectionItem ? this : base.GetPatternCore(patternInterface);

    /// <inheritdoc />
    public bool IsSelected => Header.IsSelected;

    /// <inheritdoc />
    public Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple? SelectionContainer
        => Header.Tab.Ribbon is { } ribbon && FrameworkElementAutomationPeer.FromElement(ribbon) is { } peer ? ProviderFromPeer(peer) : null;

    /// <inheritdoc />
    public void AddToSelection() => Header.Select();

    /// <inheritdoc />
    public void RemoveFromSelection()
    {
    }

    /// <inheritdoc />
    public void Select() => Header.Select();
}

/// <summary>Contextual tab group ("Table Tools") shown for a matching selection.</summary>
public partial class RibbonContextualTabGroup : DependencyObject
{
    /// <summary>Identifies <see cref="Id"/>.</summary>
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(RibbonContextualTabGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(RibbonContextualTabGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(Color), typeof(RibbonContextualTabGroup), new PropertyMetadata(Color.FromArgb(255, 0x0F, 0x6C, 0xBD), (d, _) => ((RibbonContextualTabGroup)d).Changed?.Invoke(d, EventArgs.Empty)));

    /// <summary>Identifies <see cref="IsVisible"/>.</summary>
    public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(RibbonContextualTabGroup), new PropertyMetadata(false, (d, _) => ((RibbonContextualTabGroup)d).Changed?.Invoke(d, EventArgs.Empty)));

    /// <summary>Identifies <see cref="Activation"/>.</summary>
    public static readonly DependencyProperty ActivationProperty = DependencyProperty.Register(nameof(Activation), typeof(RibbonContextualActivation), typeof(RibbonContextualTabGroup), new PropertyMetadata(RibbonContextualActivation.None));

    /// <summary>Identifies <see cref="KeyTip"/>.</summary>
    public static readonly DependencyProperty KeyTipProperty = DependencyProperty.Register(nameof(KeyTip), typeof(string), typeof(RibbonContextualTabGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsEnabled"/>.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(RibbonContextualTabGroup), new PropertyMetadata(true, (d, _) => ((RibbonContextualTabGroup)d).Changed?.Invoke(d, EventArgs.Empty)));

    internal event EventHandler? Changed;

    /// <summary>Id referenced by <see cref="RibbonTab.ContextualGroupId"/>.</summary>
    public string? Id { get => (string?)GetValue(IdProperty); set => SetValue(IdProperty, value); }

    /// <summary>Header ("Table Tools").</summary>
    public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Accent color.</summary>
    public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Visibility.</summary>
    public bool IsVisible { get => (bool)GetValue(IsVisibleProperty); set => SetValue(IsVisibleProperty, value); }

    /// <summary>
    /// KeyTip prefix of the group's tabs (Office uses "J": "JT" Table Design, "JL" Layout). Tabs without an explicit
    /// KeyTip get the prefix plus the first letter of their header.
    /// </summary>
    public string? KeyTip { get => (string?)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

    /// <summary>Enables the group's tab headers (disabled headers stay visible but cannot be selected).</summary>
    public bool IsEnabled { get => (bool)GetValue(IsEnabledProperty); set => SetValue(IsEnabledProperty, value); }

    /// <summary>Selection behaviour when shown.</summary>
    public RibbonContextualActivation Activation { get => (RibbonContextualActivation)GetValue(ActivationProperty); set => SetValue(ActivationProperty, value); }
}
