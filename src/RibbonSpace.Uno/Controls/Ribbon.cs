using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Localization;
using RibbonSpace.Layout;

namespace RibbonSpace.Controls;

/// <summary>
/// Office-style ribbon: tab row with application (File) button, contextual tabs and tab-row commands,
/// adaptive classic or simplified command area, Quick Access Toolbar, backstage, KeyTips, display options,
/// customization and persistence. Use declaratively in XAML or bind <see cref="Model"/> for MVVM.
/// </summary>
[ContentProperty(Name = nameof(Tabs))]
public partial class Ribbon : Control, IRibbonItemOwner
{
    /// <summary>Identifies <see cref="SelectedTab"/>.</summary>
    public static readonly DependencyProperty SelectedTabProperty = DependencyProperty.Register(nameof(SelectedTab), typeof(RibbonTab), typeof(Ribbon), new PropertyMetadata(null, (d, e) => ((Ribbon)d).OnSelectedTabChanged((RibbonTab?)e.OldValue, (RibbonTab?)e.NewValue)));

    /// <summary>Identifies <see cref="SelectedTabId"/>.</summary>
    public static readonly DependencyProperty SelectedTabIdProperty = DependencyProperty.Register(nameof(SelectedTabId), typeof(string), typeof(Ribbon), new PropertyMetadata(null, (d, e) => ((Ribbon)d).OnSelectedTabIdChanged((string?)e.NewValue)));

    /// <summary>Identifies <see cref="DisplayMode"/>.</summary>
    public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(nameof(DisplayMode), typeof(RibbonDisplayMode), typeof(Ribbon), new PropertyMetadata(RibbonDisplayMode.Classic, (d, _) => ((Ribbon)d).OnDisplayModeChanged()));

    /// <summary>Identifies <see cref="VisibilityMode"/>.</summary>
    public static readonly DependencyProperty VisibilityModeProperty = DependencyProperty.Register(nameof(VisibilityMode), typeof(RibbonVisibilityMode), typeof(Ribbon), new PropertyMetadata(RibbonVisibilityMode.AlwaysShow, (d, _) => ((Ribbon)d).OnVisibilityModeChanged()));

    /// <summary>Identifies <see cref="IsMinimized"/>.</summary>
    public static readonly DependencyProperty IsMinimizedProperty = DependencyProperty.Register(nameof(IsMinimized), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, (d, e) => ((Ribbon)d).OnIsMinimizedChanged((bool)e.NewValue)));

    /// <summary>Identifies <see cref="Density"/>.</summary>
    public static readonly DependencyProperty DensityProperty = DependencyProperty.Register(nameof(Density), typeof(RibbonDensity), typeof(Ribbon), new PropertyMetadata(RibbonDensity.Comfortable, (d, _) => ((Ribbon)d).OnMetricsChanged()));

    /// <summary>Identifies <see cref="CustomMetrics"/>.</summary>
    public static readonly DependencyProperty CustomMetricsProperty = DependencyProperty.Register(nameof(CustomMetrics), typeof(RibbonMetrics), typeof(Ribbon), new PropertyMetadata(null, (d, _) => ((Ribbon)d).OnMetricsChanged()));

    /// <summary>Identifies <see cref="IsApplicationButtonVisible"/>.</summary>
    public static readonly DependencyProperty IsApplicationButtonVisibleProperty = DependencyProperty.Register(nameof(IsApplicationButtonVisible), typeof(bool), typeof(Ribbon), new PropertyMetadata(true, (d, _) => ((Ribbon)d).UpdateChrome()));

    /// <summary>Identifies <see cref="ApplicationButtonLabel"/>.</summary>
    public static readonly DependencyProperty ApplicationButtonLabelProperty = DependencyProperty.Register(nameof(ApplicationButtonLabel), typeof(string), typeof(Ribbon), new PropertyMetadata(null, (d, _) => ((Ribbon)d).UpdateChrome()));

    /// <summary>Identifies <see cref="ApplicationButtonContent"/>.</summary>
    public static readonly DependencyProperty ApplicationButtonContentProperty = DependencyProperty.Register(nameof(ApplicationButtonContent), typeof(object), typeof(Ribbon), new PropertyMetadata(null, (d, _) => ((Ribbon)d).UpdateChrome()));

    /// <summary>Identifies <see cref="ApplicationMenu"/>.</summary>
    public static readonly DependencyProperty ApplicationMenuProperty = DependencyProperty.Register(nameof(ApplicationMenu), typeof(FlyoutBase), typeof(Ribbon), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="ApplicationButtonKeyTip"/>.</summary>
    public static readonly DependencyProperty ApplicationButtonKeyTipProperty = DependencyProperty.Register(nameof(ApplicationButtonKeyTip), typeof(string), typeof(Ribbon), new PropertyMetadata("F"));

    /// <summary>Identifies <see cref="TabStripStartContent"/>.</summary>
    public static readonly DependencyProperty TabStripStartContentProperty = DependencyProperty.Register(nameof(TabStripStartContent), typeof(object), typeof(Ribbon), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="TabStripEndContent"/>.</summary>
    public static readonly DependencyProperty TabStripEndContentProperty = DependencyProperty.Register(nameof(TabStripEndContent), typeof(object), typeof(Ribbon), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsDisplayOptionsButtonVisible"/>.</summary>
    public static readonly DependencyProperty IsDisplayOptionsButtonVisibleProperty = DependencyProperty.Register(nameof(IsDisplayOptionsButtonVisible), typeof(bool), typeof(Ribbon), new PropertyMetadata(true, (d, _) => ((Ribbon)d).UpdateChrome()));

    /// <summary>Identifies <see cref="IsSimplifiedModeAvailable"/>.</summary>
    public static readonly DependencyProperty IsSimplifiedModeAvailableProperty = DependencyProperty.Register(nameof(IsSimplifiedModeAvailable), typeof(bool), typeof(Ribbon), new PropertyMetadata(true));

    /// <summary>Identifies <see cref="IsAdaptiveLayoutEnabled"/>.</summary>
    public static readonly DependencyProperty IsAdaptiveLayoutEnabledProperty = DependencyProperty.Register(nameof(IsAdaptiveLayoutEnabled), typeof(bool), typeof(Ribbon), new PropertyMetadata(true, (d, _) => ((Ribbon)d).ApplyPresentation()));

    /// <summary>Identifies <see cref="IsCollapsible"/>.</summary>
    public static readonly DependencyProperty IsCollapsibleProperty = DependencyProperty.Register(nameof(IsCollapsible), typeof(bool), typeof(Ribbon), new PropertyMetadata(true));

    /// <summary>Identifies <see cref="CommandCatalog"/>.</summary>
    public static readonly DependencyProperty CommandCatalogProperty = DependencyProperty.Register(nameof(CommandCatalog), typeof(RibbonCommandCatalog), typeof(Ribbon), new PropertyMetadata(null, (d, _) => ((Ribbon)d).OnCommandCatalogChanged()));

    /// <summary>Identifies <see cref="ContextualActivation"/>.</summary>
    public static readonly DependencyProperty ContextualActivationProperty = DependencyProperty.Register(nameof(ContextualActivation), typeof(RibbonContextualActivation), typeof(Ribbon), new PropertyMetadata(RibbonContextualActivation.None));

    /// <summary>Identifies <see cref="IsFullScreenRevealed"/>.</summary>
    public static readonly DependencyProperty IsFullScreenRevealedProperty = DependencyProperty.Register(nameof(IsFullScreenRevealed), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, (d, _) => ((Ribbon)d).UpdateChrome()));

    private readonly StackPanel _tabStrip = new() { Orientation = Orientation.Horizontal, Spacing = 0 };
    private readonly List<RibbonTabHeader> _headers = [];
    private Grid? _tabContentHost;
    private RibbonScrollPanel? _tabStripScroll;
    private Button? _applicationButton;
    private Button? _displayOptionsButton;
    private Button? _revealButton;
    private FrameworkElement? _tabRow;
    private Border? _commandBar;
    private FrameworkElement? _revealBar;
    private Popup? _minimizedPopup;
    private Border? _minimizedChrome;
    private Panel? _commandBarParent;
    private int _commandBarIndex;
    private RibbonTab? _lastRegularTab;
    private bool _updatingSelection;
    private bool _compactTabStrip;

    /// <summary>Below this width tab-row items (Comments, Share...) hide their labels.</summary>
    public double CompactTabStripWidth { get; set; } = 900;

    /// <summary>Creates a ribbon.</summary>
    public Ribbon()
    {
        DefaultStyleKey = typeof(Ribbon);
        RibbonTheme.EnsureResources();
        IsTabStop = false;
        Tabs.CollectionChanged += OnTabsCollectionChanged;
        ContextualGroups.CollectionChanged += OnContextualGroupsChanged;
        TabStripItems.CollectionChanged += (_, _) => SyncTabStripItems();
        InitializeQuickAccess();
        InitializeKeyboard();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, e) =>
        {
            CloseMinimizedPopup();
            var compact = e.NewSize.Width < CompactTabStripWidth;
            if (compact != _compactTabStrip)
            {
                _compactTabStrip = compact;
                ApplyPresentation();
            }
        };
        ActualThemeChanged += (_, _) => OnActualThemeChanged();
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => OnRibbonVisibilityChanged());
        AutomationProperties.SetName(this, "Ribbon");
    }

    /// <summary>Raised when the selected tab changes.</summary>
    public event EventHandler<RibbonTabChangedEventArgs>? SelectedTabChanged;

    /// <summary>Raised when any item is invoked (buttons, menu items, galleries...). Ideal for string-id command routing.</summary>
    public event EventHandler<RibbonItemInvokedEventArgs>? ItemInvoked;

    /// <summary>Raised when the application (File) button is clicked. Set <c>Handled</c> to suppress the default action.</summary>
    public event EventHandler<RibbonHandledEventArgs>? ApplicationButtonClick;

    /// <summary>Raised when the display mode, visibility mode, QAT or customization changes (for auto-saving state).</summary>
    public event EventHandler? StateChanged;

    /// <summary>Tabs (regular and contextual).</summary>
    public ObservableCollection<RibbonTab> Tabs { get; } = [];

    /// <summary>Contextual tab groups.</summary>
    public ObservableCollection<RibbonContextualTabGroup> ContextualGroups { get; } = [];

    /// <summary>Items at the right end of the tab row (Comments, Share, Editing mode...).</summary>
    public ObservableCollection<UIElement> TabStripItems { get; } = [];

    /// <summary>Selected tab.</summary>
    public RibbonTab? SelectedTab { get => (RibbonTab?)GetValue(SelectedTabProperty); set => SetValue(SelectedTabProperty, value); }

    /// <summary>Id of the selected tab (two-way bindable).</summary>
    public string? SelectedTabId { get => (string?)GetValue(SelectedTabIdProperty); set => SetValue(SelectedTabIdProperty, value); }

    /// <summary>Classic or simplified layout.</summary>
    public RibbonDisplayMode DisplayMode { get => (RibbonDisplayMode)GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }

    /// <summary>Always show / tabs only / full screen.</summary>
    public RibbonVisibilityMode VisibilityMode { get => (RibbonVisibilityMode)GetValue(VisibilityModeProperty); set => SetValue(VisibilityModeProperty, value); }

    /// <summary>Collapsed to the tab row (same as <see cref="RibbonVisibilityMode.TabsOnly"/>).</summary>
    public bool IsMinimized { get => (bool)GetValue(IsMinimizedProperty); set => SetValue(IsMinimizedProperty, value); }

    /// <summary>Spacing density.</summary>
    public RibbonDensity Density { get => (RibbonDensity)GetValue(DensityProperty); set => SetValue(DensityProperty, value); }

    /// <summary>Fully custom metrics (overrides <see cref="Density"/>).</summary>
    public RibbonMetrics? CustomMetrics { get => (RibbonMetrics?)GetValue(CustomMetricsProperty); set => SetValue(CustomMetricsProperty, value); }

    /// <summary>Metrics in effect.</summary>
    public RibbonMetrics Metrics => CustomMetrics ?? RibbonMetrics.For(Density);

    /// <summary>Shows the application (File) button.</summary>
    public bool IsApplicationButtonVisible { get => (bool)GetValue(IsApplicationButtonVisibleProperty); set => SetValue(IsApplicationButtonVisibleProperty, value); }

    /// <summary>Label of the application button (defaults to the localized "File").</summary>
    public string? ApplicationButtonLabel { get => (string?)GetValue(ApplicationButtonLabelProperty); set => SetValue(ApplicationButtonLabelProperty, value); }

    /// <summary>Custom content of the application button (logo, text).</summary>
    public object? ApplicationButtonContent { get => GetValue(ApplicationButtonContentProperty); set => SetValue(ApplicationButtonContentProperty, value); }

    /// <summary>Menu shown by the application button when no backstage is set (classic application menu).</summary>
    public FlyoutBase? ApplicationMenu { get => (FlyoutBase?)GetValue(ApplicationMenuProperty); set => SetValue(ApplicationMenuProperty, value); }

    /// <summary>KeyTip of the application button.</summary>
    public string? ApplicationButtonKeyTip { get => (string?)GetValue(ApplicationButtonKeyTipProperty); set => SetValue(ApplicationButtonKeyTipProperty, value); }

    /// <summary>Content placed before the tabs.</summary>
    public object? TabStripStartContent { get => GetValue(TabStripStartContentProperty); set => SetValue(TabStripStartContentProperty, value); }

    /// <summary>Content placed at the right end of the tab row.</summary>
    public object? TabStripEndContent { get => GetValue(TabStripEndContentProperty); set => SetValue(TabStripEndContentProperty, value); }

    /// <summary>Shows the ribbon display options button.</summary>
    public bool IsDisplayOptionsButtonVisible { get => (bool)GetValue(IsDisplayOptionsButtonVisibleProperty); set => SetValue(IsDisplayOptionsButtonVisibleProperty, value); }

    /// <summary>Offers the simplified layout in the display options.</summary>
    public bool IsSimplifiedModeAvailable { get => (bool)GetValue(IsSimplifiedModeAvailableProperty); set => SetValue(IsSimplifiedModeAvailableProperty, value); }

    /// <summary>Enables adaptive group resizing.</summary>
    public bool IsAdaptiveLayoutEnabled { get => (bool)GetValue(IsAdaptiveLayoutEnabledProperty); set => SetValue(IsAdaptiveLayoutEnabledProperty, value); }

    /// <summary>Allows collapsing (double-click on a tab, Ctrl+F1, display options).</summary>
    public bool IsCollapsible { get => (bool)GetValue(IsCollapsibleProperty); set => SetValue(IsCollapsibleProperty, value); }

    /// <summary>Command catalog resolving <c>CommandId</c> references.</summary>
    public RibbonCommandCatalog? CommandCatalog { get => (RibbonCommandCatalog?)GetValue(CommandCatalogProperty); set => SetValue(CommandCatalogProperty, value); }

    /// <summary>Default activation of contextual groups when they become visible.</summary>
    public RibbonContextualActivation ContextualActivation { get => (RibbonContextualActivation)GetValue(ContextualActivationProperty); set => SetValue(ContextualActivationProperty, value); }

    /// <summary>True while the ribbon is temporarily revealed in full-screen mode.</summary>
    public bool IsFullScreenRevealed { get => (bool)GetValue(IsFullScreenRevealedProperty); set => SetValue(IsFullScreenRevealedProperty, value); }

    /// <summary>Tabs currently shown in the tab row, in display order.</summary>
    public IReadOnlyList<RibbonTab> VisibleTabs => _headers.Select(h => h.Tab).ToArray();

    /// <summary>Headers of the visible tabs.</summary>
    public IReadOnlyList<RibbonTabHeader> TabHeaders => _headers;

    /// <summary>The application (File) button.</summary>
    public Button? ApplicationButton => _applicationButton;

    /// <summary>The display options button.</summary>
    public Button? DisplayOptionsButton => _displayOptionsButton;

    /// <summary>True while the minimized ribbon shows its commands in a temporary popup.</summary>
    public bool IsMinimizedPopupOpen => _minimizedPopup?.IsOpen == true;

    Ribbon? IRibbonItemOwner.OwnerRibbon => this;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_applicationButton is not null)
        {
            _applicationButton.Click -= OnApplicationButtonClick;
        }

        if (_displayOptionsButton is not null)
        {
            _displayOptionsButton.Click -= OnDisplayOptionsClick;
        }

        if (_revealButton is not null)
        {
            _revealButton.Click -= OnRevealClick;
        }

        if (_tabStripScroll is not null)
        {
            _tabStripScroll.Content = null;
        }

        _tabContentHost?.Children.Clear();
        base.OnApplyTemplate();
        _tabContentHost = GetTemplateChild("PART_TabContentHost") as Grid;
        _tabStripScroll = GetTemplateChild("PART_TabStripScroll") as RibbonScrollPanel;
        _applicationButton = GetTemplateChild("PART_ApplicationButton") as Button;
        _displayOptionsButton = GetTemplateChild("PART_DisplayOptionsButton") as Button;
        AttachMinimizeButtons();
        _revealButton = GetTemplateChild("PART_RevealButton") as Button;
        _tabRow = GetTemplateChild("PART_TabRow") as FrameworkElement;
        _commandBar = GetTemplateChild("PART_CommandBar") as Border;
        _revealBar = GetTemplateChild("PART_RevealBar") as FrameworkElement;
        _quickAccessAboveHost = GetTemplateChild("PART_QuickAccessAboveHost") as Border;
        _quickAccessBelowHost = GetTemplateChild("PART_QuickAccessBelowHost") as Border;
        _tabStripItemsHost = GetTemplateChild("PART_TabStripItemsHost") as Panel;
        if (_tabStripScroll is not null)
        {
            _tabStripScroll.Content = _tabStrip;
        }

        if (_applicationButton is not null)
        {
            _applicationButton.Click += OnApplicationButtonClick;
        }

        if (_displayOptionsButton is not null)
        {
            _displayOptionsButton.Click += OnDisplayOptionsClick;
            ToolTipService.SetToolTip(_displayOptionsButton, RibbonStrings.Current.RibbonDisplayOptions);
            AutomationProperties.SetName(_displayOptionsButton, RibbonStrings.Current.RibbonDisplayOptions);
        }

        if (_revealButton is not null)
        {
            _revealButton.Click += OnRevealClick;
        }

        SyncTabContent();
        SyncTabStripItems();
        OnTabsChanged();
        UpdateQuickAccessPlacement();
        UpdateChrome();
        ApplyPresentation();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachKeyboard();
        // Static / shared sources are subscribed only while loaded so they never keep an unloaded ribbon alive.
        RibbonStrings.CurrentChanged -= OnStringsChanged;
        RibbonStrings.CurrentChanged += OnStringsChanged;
        AttachCommandCatalog();
        OnModelLoaded();
        CaptureQuickAccessDefaults();
        if (SelectedTab is null)
        {
            SelectFirstTab();
        }

        if (IsBackstageOpen)
        {
            OpenBackstageCore();
        }

        ShowFloatingGroups();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachKeyboard();
        HideKeyTips();
        CloseMinimizedPopup();
        RibbonStrings.CurrentChanged -= OnStringsChanged;
        DetachCommandCatalog();
        OnModelUnloaded();
        if (IsBackstageOpen)
        {
            IsBackstageOpen = false;
        }

        foreach (var group in Tabs.SelectMany(t => t.Groups))
        {
            group.ClosePopup();
            group.CloseSlideOut();
        }

        SuspendFloatingGroups();
    }

    private void OnStringsChanged(object? sender, EventArgs e) => DispatcherQueue?.TryEnqueue(() =>
    {
        UpdateChrome();
        if (_displayOptionsButton is not null)
        {
            ToolTipService.SetToolTip(_displayOptionsButton, RibbonStrings.Current.RibbonDisplayOptions);
            AutomationProperties.SetName(_displayOptionsButton, RibbonStrings.Current.RibbonDisplayOptions);
        }

        foreach (var tab in Tabs)
        {
            tab.OnStringsChanged();
        }
    });

    private void OnActualThemeChanged()
    {
        // Theme-dependent values computed in code (contextual tab colours, popup themes) are refreshed.
        OnTabsChanged();
        UpdateChrome();
        if (_backstagePopup?.Child is FrameworkElement backstage)
        {
            backstage.RequestedTheme = ActualTheme;
        }
    }

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var tab in Tabs)
        {
            tab.AttachToRibbon(this);
        }

        SyncTabContent();
        OnTabsChanged();
    }

    private void OnContextualGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Compare with the subscribed set: Reset (Clear) carries no OldItems.
        foreach (var group in _subscribedContextualGroups.Where(g => !ContextualGroups.Contains(g)).ToArray())
        {
            group.Changed -= OnContextualGroupChanged;
            _subscribedContextualGroups.Remove(group);
        }

        foreach (var group in ContextualGroups)
        {
            if (_subscribedContextualGroups.Add(group))
            {
                group.Changed += OnContextualGroupChanged;
            }
        }

        OnTabsChanged();
    }

    private readonly HashSet<RibbonContextualTabGroup> _subscribedContextualGroups = [];

    private void OnContextualGroupChanged(object? sender, EventArgs e)
    {
        var group = (RibbonContextualTabGroup)sender!;
        var wasVisible = _headers.Any(h => h.Tab.ContextualGroupId == group.Id);
        OnTabsChanged();
        var activation = group.Activation == RibbonContextualActivation.None ? ContextualActivation : group.Activation;
        if (group.IsVisible && !wasVisible && activation == RibbonContextualActivation.SelectOnShow)
        {
            var first = _headers.FirstOrDefault(h => h.Tab.ContextualGroupId == group.Id)?.Tab;
            if (first is not null)
            {
                SelectedTab = first;
            }
        }
    }

    /// <summary>Finds a contextual group by id.</summary>
    public RibbonContextualTabGroup? FindContextualGroup(string id) => ContextualGroups.FirstOrDefault(g => g.Id == id);

    /// <summary>Shows exactly the given contextual groups and hides the others.</summary>
    public void SetActiveContextualGroups(params string[] ids)
    {
        foreach (var group in ContextualGroups)
        {
            group.IsVisible = ids.Contains(group.Id);
        }
    }

    private void SyncTabContent()
    {
        if (_tabContentHost is null)
        {
            return;
        }

        // Incremental: tabs that stay keep their place (re-parenting would unload and reload all their items).
        foreach (var stale in _tabContentHost.Children.OfType<RibbonTab>().Where(t => !Tabs.Contains(t)).ToArray())
        {
            _tabContentHost.Children.Remove(stale);
        }

        foreach (var tab in Tabs)
        {
            tab.Visibility = ReferenceEquals(tab, SelectedTab) ? Visibility.Visible : Visibility.Collapsed;
            if (ReferenceEquals(VisualTreeHelper.GetParent(tab), _tabContentHost))
            {
                continue;
            }

            if (VisualTreeHelper.GetParent(tab) is Panel parent)
            {
                parent.Children.Remove(tab);
            }

            _tabContentHost.Children.Add(tab);
            tab.ApplyTemplate();
        }

        ApplyPresentation();
    }

    internal void OnGroupsChanged(RibbonTab tab)
    {
        InvalidateShortcuts();
        ApplyCustomizationToTab(tab);
        foreach (var item in tab.Groups.SelectMany(g => g.GetAllItems()))
        {
            ApplyCommandState(item);
        }
    }

    /// <summary>Rebuilds the tab row (visibility, contextual tabs, order).</summary>
    internal void OnTabsChanged()
    {
        if (_applyingCustomization)
        {
            // ApplyCustomization rebuilds the tab row once at the end.
            return;
        }

        var ordered = Tabs.Where(t => t.IsAvailable && !t.IsContextual)
            .Concat(ContextualGroups.Where(g => g.IsVisible).SelectMany(g => Tabs.Where(t => t.ContextualGroupId == g.Id && t.IsAvailable)))
            .ToList();
        ordered = ApplyTabOrder(ordered);

        _tabStrip.Children.Clear();
        _headers.Clear();
        foreach (var tab in ordered)
        {
            var header = tab.HeaderElement ??= CreateHeader(tab);
            header.UpdateText();
            var group = tab.ContextualGroup;
            header.IsContextual = group is not null;
            header.ContextualHeader = group?.Header;
            header.ContextualBrush = group is null ? null : new SolidColorBrush(ActualTheme == ElementTheme.Dark ? RibbonTheme.ToColor(RibbonTheme.ToRibbonColor(group.Color).Lighten(0.35)) : group.Color);
            header.IsSelected = ReferenceEquals(tab, SelectedTab);
            header.IsEnabled = tab.IsEnabled && (group?.IsEnabled ?? true);
            header.IsTabStop = header.IsSelected;
            _headers.Add(header);
            _tabStrip.Children.Add(header);
        }

        if (SelectedTab is not null && !ordered.Contains(SelectedTab))
        {
            SelectedTab = _lastRegularTab is { IsAvailable: true } last && ordered.Contains(last) ? last : ordered.FirstOrDefault();
        }
        else if (SelectedTab is null && IsLoaded)
        {
            SelectFirstTab();
        }
    }

    private RibbonTabHeader CreateHeader(RibbonTab tab)
    {
        var header = new RibbonTabHeader(tab);
        header.Click += (_, _) => OnHeaderClicked(tab);
        header.DoubleTapped += (_, e) =>
        {
            if (IsCollapsible)
            {
                ToggleMinimized();
                e.Handled = true;
            }
        };
        header.RightTapped += (_, e) =>
        {
            ShowItemContextMenu(header, e.GetPosition(header));
            e.Handled = true;
        };
        header.KeyDown += (_, e) => e.Handled = OnHeaderKeyDown(header, e.Key);
        return header;
    }

    private bool OnHeaderKeyDown(RibbonTabHeader header, Windows.System.VirtualKey key)
    {
        var index = _headers.IndexOf(header);
        if (index < 0 || _headers.Count == 0)
        {
            return false;
        }

        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var target = key switch
        {
            Windows.System.VirtualKey.Left => rtl ? index + 1 : index - 1,
            Windows.System.VirtualKey.Right => rtl ? index - 1 : index + 1,
            Windows.System.VirtualKey.Home => 0,
            Windows.System.VirtualKey.End => _headers.Count - 1,
            _ => -2,
        };
        if (target == -2)
        {
            if (key == Windows.System.VirtualKey.Down && SelectedTab is { } tab)
            {
                // Down moves into the commands of the selected tab.
                if (VisibilityMode == RibbonVisibilityMode.TabsOnly)
                {
                    OpenMinimizedPopup();
                }

                return FocusManager.FindFirstFocusableElement(tab) is Control first && first.Focus(FocusState.Keyboard);
            }

            return false;
        }

        target = (target + _headers.Count) % _headers.Count;
        var next = _headers[target];
        SelectedTab = next.Tab;
        next.Focus(FocusState.Keyboard);
        return true;
    }

    private void OnHeaderClicked(RibbonTab tab)
    {
        var wasSelected = ReferenceEquals(tab, SelectedTab);
        SelectedTab = tab;
        if (VisibilityMode == RibbonVisibilityMode.TabsOnly)
        {
            if (wasSelected && IsMinimizedPopupOpen)
            {
                CloseMinimizedPopup();
            }
            else
            {
                DispatcherQueue?.TryEnqueue(OpenMinimizedPopup);
            }
        }
    }

    private void SelectFirstTab()
    {
        if (SelectedTabId is { } id && FindTab(id) is { IsAvailable: true } byId)
        {
            SelectedTab = byId;
            return;
        }

        SelectedTab = _headers.FirstOrDefault()?.Tab ?? Tabs.FirstOrDefault(t => t.IsAvailable);
    }

    /// <summary>Finds a tab by id (or header).</summary>
    public RibbonTab? FindTab(string id) => Tabs.FirstOrDefault(t => t.EffectiveId == id || t.Header == id);

    /// <summary>Selects a tab by id. Returns false when unknown or hidden.</summary>
    public bool SelectTab(string id)
    {
        var tab = FindTab(id);
        if (tab is null || !tab.IsAvailable)
        {
            return false;
        }

        SelectedTab = tab;
        return true;
    }

    /// <summary>Selects a tab. Returns false when it is not part of this ribbon or hidden.</summary>
    public bool SelectTab(RibbonTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (!Tabs.Contains(tab) || !tab.IsAvailable)
        {
            return false;
        }

        SelectedTab = tab;
        return true;
    }

    private void OnSelectedTabIdChanged(string? id)
    {
        if (_updatingSelection || id is null)
        {
            return;
        }

        // Hidden tabs (inactive contextual, hidden by customization) cannot be selected through the id.
        if (FindTab(id) is { IsAvailable: true } tab && !ReferenceEquals(tab, SelectedTab))
        {
            SelectedTab = tab;
        }
        else if (SelectedTab is { } current && current.EffectiveId != id)
        {
            _updatingSelection = true;
            SelectedTabId = current.EffectiveId;
            _updatingSelection = false;
        }
    }

    private void OnSelectedTabChanged(RibbonTab? oldTab, RibbonTab? newTab)
    {
        if (oldTab is not null)
        {
            oldTab.IsSelected = false;
            oldTab.Visibility = Visibility.Collapsed;
            foreach (var group in oldTab.Groups)
            {
                group.ClosePopup();
                group.CloseSlideOut();
            }
        }

        if (newTab is not null)
        {
            newTab.IsSelected = true;
            newTab.Visibility = Visibility.Visible;
            if (!newTab.IsContextual)
            {
                _lastRegularTab = newTab;
            }

            newTab.ApplyPresentation(Metrics, DisplayMode == RibbonDisplayMode.Simplified, IsAdaptiveLayoutEnabled, CurrentPanelPresentation, ShowGroupCaptions, ReductionStrategy);

            // Pinned expanded panels come back with their tab.
            foreach (var group in newTab.Groups.Where(g => g.IsSlideOutPinned))
            {
                DispatcherQueue?.TryEnqueue(group.OpenSlideOut);
            }
        }

        foreach (var header in _headers)
        {
            header.IsSelected = ReferenceEquals(header.Tab, newTab);
            // Roving tab stop: Tab enters the tab row once, arrow keys move between tabs.
            header.IsTabStop = header.IsSelected || (newTab is null && ReferenceEquals(header, _headers[0]));
        }

        _updatingSelection = true;
        SelectedTabId = newTab?.EffectiveId;
        _updatingSelection = false;
        if (newTab?.HeaderElement is { } element)
        {
            _tabStripScroll?.BringIntoView(element);
        }

        SelectedTabChanged?.Invoke(this, new RibbonTabChangedEventArgs(oldTab, newTab));
        OnModelSelectedTabChanged(newTab);
    }

    private void OnMetricsChanged()
    {
        OnModelDisplayModeChanged();
        ApplyPresentation();
        ApplyQuickAccessLayout();
        RaiseStateChanged();
    }

    private void OnDisplayModeChanged()
    {
        CloseMinimizedPopup();
        ApplyPresentation();
        UpdateChrome();
        RaiseStateChanged();
        OnModelDisplayModeChanged();
    }

    /// <summary>Applies metrics, display mode and adaptive options to every tab.</summary>
    private void ApplyPresentation()
    {
        var metrics = Metrics;
        var simplified = DisplayMode == RibbonDisplayMode.Simplified;
        var presentation = CurrentPanelPresentation;
        foreach (var tab in Tabs)
        {
            tab.ApplyPresentation(metrics, simplified, IsAdaptiveLayoutEnabled, presentation, ShowGroupCaptions, ReductionStrategy);
        }

        foreach (var item in TabStripItems.OfType<IRibbonItem>())
        {
            item.ApplyLayout(new RibbonItemLayout(_compactTabStrip ? RibbonItemSize.Small : RibbonItemSize.Medium, metrics, true, _compactTabStrip ? false : null));
        }

        if (_tabRow is not null)
        {
            _tabRow.MinHeight = metrics.TabHeight;
        }

        VisualStateManager.GoToState(this, simplified ? "Simplified" : "Classic", false);
    }

    private void OnVisibilityModeChanged()
    {
        IsMinimized = IsMinimizedMode(VisibilityMode);
        if (VisibilityMode != RibbonVisibilityMode.FullScreen)
        {
            IsFullScreenRevealed = false;
        }

        CloseMinimizedPopup();
        foreach (var group in Tabs.SelectMany(t => t.Groups))
        {
            group.ClosePopup();
            group.CloseSlideOut();
        }

        ApplyPresentation();
        UpdateChrome();
        UpdateMinimizeButton();
        RaiseStateChanged();
        OnModelVisibilityModeChanged();
    }

    private void OnIsMinimizedChanged(bool value)
    {
        if (value && VisibilityMode == RibbonVisibilityMode.AlwaysShow)
        {
            VisibilityMode = MinimizedState;
        }
        else if (!value && IsMinimizedMode(VisibilityMode))
        {
            VisibilityMode = RibbonVisibilityMode.AlwaysShow;
        }
    }

    /// <summary>
    /// Minimizes or restores the ribbon according to <see cref="MinimizeBehavior"/>: full ribbon ↔ tabs only (Office),
    /// ↔ panel titles / panel buttons, or the AutoCAD cycle full → panel buttons → panel titles → tabs → full.
    /// </summary>
    public void ToggleMinimized()
    {
        if (!IsCollapsible)
        {
            return;
        }

        VisibilityMode = NextMinimizeState();
    }

    private void UpdateChrome()
    {
        if (_applicationButton is not null)
        {
            _applicationButton.Visibility = IsApplicationButtonVisible ? Visibility.Visible : Visibility.Collapsed;
            _applicationButton.Content = ApplicationButtonContent ?? ApplicationButtonLabel ?? RibbonStrings.Current.File;
            AutomationProperties.SetName(_applicationButton, ApplicationButtonLabel ?? RibbonStrings.Current.File);
        }

        if (_displayOptionsButton is not null)
        {
            _displayOptionsButton.Visibility = IsDisplayOptionsButtonVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        var fullScreen = VisibilityMode == RibbonVisibilityMode.FullScreen;
        var showRibbon = !fullScreen || IsFullScreenRevealed;
        if (_tabRow is not null)
        {
            _tabRow.Visibility = showRibbon ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_commandBar is not null && !IsMinimizedPopupOpen)
        {
            _commandBar.Visibility = showRibbon && VisibilityMode != RibbonVisibilityMode.TabsOnly ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_revealBar is not null)
        {
            _revealBar.Visibility = fullScreen ? Visibility.Visible : Visibility.Collapsed;
        }

        UpdateQuickAccessPlacement();
    }

    private void OnRevealClick(object sender, RoutedEventArgs e) => IsFullScreenRevealed = !IsFullScreenRevealed;

    /// <summary>Opens the temporary command popup of a minimized ribbon.</summary>
    public void OpenMinimizedPopup()
    {
        // The mode may have changed while the open was queued (e.g. double-click pins the ribbon).
        if (_commandBar is null || XamlRoot is null || _tabRow is null || VisibilityMode != RibbonVisibilityMode.TabsOnly)
        {
            return;
        }

        if (_minimizedPopup is null)
        {
            _minimizedChrome = new Border();
            // Dismissal is handled by the ribbon (OnRootPointerPressed) so clicks on the tab row toggle the popup
            // instead of being swallowed by a light-dismiss layer.
            _minimizedPopup = new Popup { Child = _minimizedChrome, IsLightDismissEnabled = false };
            RibbonItemHelper.SetOwner(_minimizedChrome, this);
            _minimizedPopup.Closed += (_, _) => RestoreCommandBar();
            _minimizedChrome.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    CloseMinimizedPopup();
                    SelectedTab?.HeaderElement?.Focus(FocusState.Keyboard);
                    e.Handled = true;
                }
            };
        }

        if (!IsMinimizedPopupOpen)
        {
            _commandBarParent = VisualTreeHelper.GetParent(_commandBar) as Panel;
            _commandBarIndex = _commandBarParent?.Children.IndexOf(_commandBar) ?? -1;
            _commandBarParent?.Children.Remove(_commandBar);
            _commandBar.Visibility = Visibility.Visible;
            _minimizedChrome!.Child = _commandBar;
            _minimizedChrome.Width = ActualWidth;
            _minimizedChrome.RequestedTheme = ActualTheme;
        }

        AttachPopupKeyboard(_minimizedChrome);
        var origin = _tabRow.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, _tabRow.ActualHeight));
        var self = TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
        _minimizedPopup.XamlRoot = XamlRoot;
        _minimizedPopup.HorizontalOffset = self.X;
        _minimizedPopup.VerticalOffset = origin.Y;
        _minimizedPopup.IsOpen = true;
    }

    /// <summary>Closes the temporary command popup of a minimized ribbon.</summary>
    public void CloseMinimizedPopup()
    {
        if (_minimizedPopup is { IsOpen: true })
        {
            _minimizedPopup.IsOpen = false;
        }
    }

    private void RestoreCommandBar()
    {
        if (_commandBar is null || _minimizedChrome is null || !ReferenceEquals(_minimizedChrome.Child, _commandBar))
        {
            return;
        }

        _minimizedChrome.Child = null;
        if (_commandBarParent is not null)
        {
            _commandBarParent.Children.Insert(Math.Clamp(_commandBarIndex, 0, _commandBarParent.Children.Count), _commandBar);
        }

        UpdateChrome();
    }

    private void OnApplicationButtonClick(object sender, RoutedEventArgs e) => InvokeApplicationButton();

    /// <summary>Runs the application button action (backstage, application menu or the click event).</summary>
    public void InvokeApplicationButton() => InvokeApplicationButton(null);

    /// <summary>
    /// Runs the application button action anchored at <paramref name="anchor"/> (e.g. the application icon of a
    /// <c>RibbonTitleBar</c>, as in AutoCAD); <c>null</c> uses the File button.
    /// </summary>
    public void InvokeApplicationButton(FrameworkElement? anchor)
    {
        HideKeyTips();
        var args = new RibbonHandledEventArgs();
        ApplicationButtonClick?.Invoke(this, args);
        if (args.Handled)
        {
            return;
        }

        if (Backstage is not null)
        {
            IsBackstageOpen = true;
        }
        else if (ApplicationMenu is not null && (anchor ?? _applicationButton) is { } target)
        {
            RibbonMenu.ApplyTheme(ApplicationMenu);
            ApplicationMenu.ShowAt(target);
        }
    }

    private void SyncTabStripItems()
    {
        if (_tabStripItemsHost is null)
        {
            return;
        }

        _tabStripItemsHost.Children.Clear();
        foreach (var item in TabStripItems)
        {
            if (VisualTreeHelper.GetParent(item) is Panel parent)
            {
                parent.Children.Remove(item);
            }

            RibbonItemHelper.SetOwner(item, this);
            _tabStripItemsHost.Children.Add(item);
            if (item is IRibbonItem ribbonItem)
            {
                ribbonItem.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Metrics, true));
            }
        }
    }

    private Panel? _tabStripItemsHost;

    /// <inheritdoc />
    public void OnItemInvoked(FrameworkElement item, string? commandId, object? parameter)
    {
        if (IsMinimizedPopupOpen && item is not RibbonTabHeader && !(item is Button { Flyout: not null }) && item is not RibbonSplitButton { IsDropDownOpen: true })
        {
            CloseMinimizedPopup();
        }

        foreach (var group in SelectedTab?.Groups ?? [])
        {
            if (group.IsPopupOpen && !ReferenceEquals(item, group))
            {
                group.ClosePopup();
            }
        }

        if (VisibilityMode == RibbonVisibilityMode.FullScreen)
        {
            IsFullScreenRevealed = false;
        }

        if (item is IRibbonItem { Id: { } id })
        {
            SearchEngine.MarkUsed(id);
        }

        ItemInvoked?.Invoke(this, new RibbonItemInvokedEventArgs(item, commandId, parameter));
    }

    /// <summary>Raises <see cref="StateChanged"/>.</summary>
    protected internal void RaiseStateChanged()
    {
        if (IsLoaded)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>All items of all tabs, the tab row and the QAT.</summary>
    public IEnumerable<FrameworkElement> GetAllItems()
        => Tabs.SelectMany(t => t.Groups).SelectMany(g => g.GetAllItems())
            .Concat(TabStripItems.SelectMany(RibbonGroup.Flatten));

    /// <summary>Finds an item by id.</summary>
    public FrameworkElement? FindItem(string id)
        => GetAllItems().FirstOrDefault(i => i is IRibbonItem ri && string.Equals(ri.Id, id, StringComparison.Ordinal));

    /// <summary>Finds the tab and group containing an item.</summary>
    public (RibbonTab? Tab, RibbonGroup? Group) FindLocation(FrameworkElement item)
    {
        foreach (var tab in Tabs)
        {
            foreach (var group in tab.Groups)
            {
                if (group.GetAllItems().Contains(item))
                {
                    return (tab, group);
                }
            }
        }

        return (null, null);
    }

    /// <summary>Forces the adaptive layout to be recomputed (e.g. after changing item content in code).</summary>
    public void InvalidateLayout()
    {
        foreach (var tab in Tabs)
        {
            foreach (var group in tab.Groups)
            {
                group.InvalidateItemsLayout();
            }

            tab.GroupsPanel.InvalidateCache();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);
}

/// <summary>Automation peer of <see cref="Ribbon"/> (a Tab control).</summary>
public sealed partial class RibbonAutomationPeer(Ribbon owner)
    : FrameworkElementAutomationPeer(owner), Microsoft.UI.Xaml.Automation.Provider.ISelectionProvider, Microsoft.UI.Xaml.Automation.Provider.IExpandCollapseProvider
{
    private Ribbon Ribbon => (Ribbon)Owner;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(Ribbon);

    /// <inheritdoc />
    protected override object? GetPatternCore(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Selection => this,
        PatternInterface.ExpandCollapse when Ribbon.IsCollapsible => this,
        _ => base.GetPatternCore(patternInterface),
    };

    /// <inheritdoc />
    public bool CanSelectMultiple => false;

    /// <inheritdoc />
    public bool IsSelectionRequired => true;

    /// <inheritdoc />
    public Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple[] GetSelection()
        => Ribbon.SelectedTab?.HeaderElement is { } header && FromElement(header) is { } peer ? [ProviderFromPeer(peer)] : [];

    /// <inheritdoc />
    public ExpandCollapseState ExpandCollapseState => Ribbon.IsMinimized ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;

    /// <inheritdoc />
    public void Expand()
    {
        if (Ribbon.IsMinimized)
        {
            Ribbon.ToggleMinimized();
        }
    }

    /// <inheritdoc />
    public void Collapse()
    {
        if (!Ribbon.IsMinimized)
        {
            Ribbon.ToggleMinimized();
        }
    }
}

/// <summary>Arguments of <see cref="Ribbon.SelectedTabChanged"/>.</summary>
public sealed class RibbonTabChangedEventArgs(RibbonTab? oldTab, RibbonTab? newTab) : EventArgs
{
    /// <summary>Previously selected tab.</summary>
    public RibbonTab? OldTab { get; } = oldTab;

    /// <summary>Selected tab.</summary>
    public RibbonTab? NewTab { get; } = newTab;
}

/// <summary>Arguments of <see cref="Ribbon.ItemInvoked"/>.</summary>
public sealed class RibbonItemInvokedEventArgs(FrameworkElement item, string? commandId, object? parameter) : EventArgs
{
    /// <summary>Invoked item (the original item for QAT / overflow copies).</summary>
    public FrameworkElement Item { get; } = item;

    /// <summary>Command id of the item.</summary>
    public string? CommandId { get; } = commandId;

    /// <summary>Id of the item.</summary>
    public string? ItemId { get; } = (item as IRibbonItem)?.Id ?? (item as RibbonGroup)?.Id;

    /// <summary>Parameter (checked state, selected gallery value, color, text...).</summary>
    public object? Parameter { get; } = parameter;
}

/// <summary>Handled event arguments.</summary>
public sealed class RibbonHandledEventArgs : EventArgs
{
    /// <summary>Suppresses the default action.</summary>
    public bool Handled { get; set; }
}
