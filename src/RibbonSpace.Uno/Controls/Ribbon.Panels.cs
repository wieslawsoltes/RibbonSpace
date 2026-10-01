using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using RibbonSpace.Localization;
using RibbonSpace.State;
using RibbonSpace.Layout;

namespace RibbonSpace.Controls;

// AutoCAD-class ribbon behaviours: minimize cycle (tabs / panel titles / panel buttons), panel titles toggle,
// floating panels, and the "Show Tabs / Show Panels / Show Panel Titles" context menus.
public partial class Ribbon
{
    /// <summary>Identifies <see cref="MinimizeBehavior"/>.</summary>
    public static readonly DependencyProperty MinimizeBehaviorProperty = DependencyProperty.Register(nameof(MinimizeBehavior), typeof(RibbonMinimizeBehavior), typeof(Ribbon), new PropertyMetadata(RibbonMinimizeBehavior.Tabs, (d, _) => ((Ribbon)d).OnPanelOptionsChanged()));

    /// <summary>Identifies <see cref="IsMinimizeButtonVisible"/>.</summary>
    public static readonly DependencyProperty IsMinimizeButtonVisibleProperty = DependencyProperty.Register(nameof(IsMinimizeButtonVisible), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, (d, _) => ((Ribbon)d).UpdateMinimizeButton()));

    /// <summary>Identifies <see cref="ShowGroupCaptions"/>.</summary>
    public static readonly DependencyProperty ShowGroupCaptionsProperty = DependencyProperty.Register(nameof(ShowGroupCaptions), typeof(bool), typeof(Ribbon), new PropertyMetadata(true, (d, _) => ((Ribbon)d).OnShowGroupCaptionsChanged()));

    /// <summary>Identifies <see cref="CanFloatGroups"/>.</summary>
    public static readonly DependencyProperty CanFloatGroupsProperty = DependencyProperty.Register(nameof(CanFloatGroups), typeof(bool), typeof(Ribbon), new PropertyMetadata(false));

    /// <summary>Identifies <see cref="ReductionStrategy"/>.</summary>
    public static readonly DependencyProperty ReductionStrategyProperty = DependencyProperty.Register(nameof(ReductionStrategy), typeof(RibbonReductionStrategy), typeof(Ribbon), new PropertyMetadata(RibbonReductionStrategy.Stepwise, (d, _) => ((Ribbon)d).ApplyPresentation()));

    /// <summary>Identifies <see cref="IsVisibilityMenuEnabled"/>.</summary>
    public static readonly DependencyProperty IsVisibilityMenuEnabledProperty = DependencyProperty.Register(nameof(IsVisibilityMenuEnabled), typeof(bool), typeof(Ribbon), new PropertyMetadata(false));

    private Button? _minimizeButton;
    private Button? _minimizeBehaviorButton;
    private FrameworkElement? _minimizeButtons;
    private FontIcon? _minimizeGlyph;

    /// <summary>Raised when a group starts or stops floating.</summary>
    public event EventHandler<RibbonGroup>? GroupFloatingChanged;

    /// <summary>
    /// What <see cref="ToggleMinimized"/> (Ctrl+F1, tab double-click, the minimize button) does: Office's tabs-only
    /// toggle, or AutoCAD's panel titles / panel buttons states or the full cycle.
    /// </summary>
    public RibbonMinimizeBehavior MinimizeBehavior { get => (RibbonMinimizeBehavior)GetValue(MinimizeBehaviorProperty); set => SetValue(MinimizeBehaviorProperty, value); }

    /// <summary>Shows the AutoCAD-style minimize button (with a behaviour drop-down) at the end of the tab row.</summary>
    public bool IsMinimizeButtonVisible { get => (bool)GetValue(IsMinimizeButtonVisibleProperty); set => SetValue(IsMinimizeButtonVisibleProperty, value); }

    /// <summary>Shows the panel (group) titles under each group ("Show Panel Titles").</summary>
    public bool ShowGroupCaptions { get => (bool)GetValue(ShowGroupCaptionsProperty); set => SetValue(ShowGroupCaptionsProperty, value); }

    /// <summary>Lets users float panels: drag a panel title away from the ribbon, or use "Float Panel" in its context menu.</summary>
    public bool CanFloatGroups { get => (bool)GetValue(CanFloatGroupsProperty); set => SetValue(CanFloatGroupsProperty, value); }

    /// <summary>Adds "Show Tabs", "Show Panels" and "Show Panel Titles" to the ribbon context menu (AutoCAD).</summary>
    public bool IsVisibilityMenuEnabled { get => (bool)GetValue(IsVisibilityMenuEnabledProperty); set => SetValue(IsVisibilityMenuEnabledProperty, value); }

    /// <summary>
    /// How groups shrink when a tab does not fit: <see cref="RibbonReductionStrategy.Stepwise"/> (Office: all groups go
    /// Medium, then Small, then collapse) or <see cref="RibbonReductionStrategy.GroupByGroup"/> (AutoCAD-like: the
    /// least important group collapses completely before the next one shrinks). <c>RibbonGroup.ReductionOrder</c>
    /// sets the importance.
    /// </summary>
    public RibbonReductionStrategy ReductionStrategy { get => (RibbonReductionStrategy)GetValue(ReductionStrategyProperty); set => SetValue(ReductionStrategyProperty, value); }

    /// <summary>Groups currently floating outside the ribbon.</summary>
    public IReadOnlyList<RibbonGroup> FloatingGroups => Tabs.SelectMany(t => t.Groups).Where(g => g.IsFloating).ToArray();

    /// <summary>Returns every floating panel to the ribbon ("Return Panels to Ribbon").</summary>
    public void ReturnAllPanelsToRibbon()
    {
        foreach (var group in FloatingGroups)
        {
            group.ReturnToRibbon();
        }
    }

    /// <summary>The visibility mode <see cref="ToggleMinimized"/> moves to from the current one.</summary>
    public RibbonVisibilityMode NextMinimizeState() => MinimizeBehavior switch
    {
        RibbonMinimizeBehavior.CycleAll => VisibilityMode switch
        {
            RibbonVisibilityMode.AlwaysShow => RibbonVisibilityMode.PanelButtons,
            RibbonVisibilityMode.PanelButtons => RibbonVisibilityMode.PanelTitles,
            RibbonVisibilityMode.PanelTitles => RibbonVisibilityMode.TabsOnly,
            _ => RibbonVisibilityMode.AlwaysShow,
        },
        _ when VisibilityMode != RibbonVisibilityMode.AlwaysShow => RibbonVisibilityMode.AlwaysShow,
        RibbonMinimizeBehavior.PanelTitles => RibbonVisibilityMode.PanelTitles,
        RibbonMinimizeBehavior.PanelButtons => RibbonVisibilityMode.PanelButtons,
        _ => RibbonVisibilityMode.TabsOnly,
    };

    /// <summary>The first minimized state of the current <see cref="MinimizeBehavior"/>.</summary>
    internal RibbonVisibilityMode MinimizedState => MinimizeBehavior switch
    {
        RibbonMinimizeBehavior.PanelTitles => RibbonVisibilityMode.PanelTitles,
        RibbonMinimizeBehavior.PanelButtons or RibbonMinimizeBehavior.CycleAll => RibbonVisibilityMode.PanelButtons,
        _ => RibbonVisibilityMode.TabsOnly,
    };

    internal static bool IsMinimizedMode(RibbonVisibilityMode mode)
        => mode is RibbonVisibilityMode.TabsOnly or RibbonVisibilityMode.PanelTitles or RibbonVisibilityMode.PanelButtons;

    internal RibbonPanelPresentation CurrentPanelPresentation => DisplayMode == RibbonDisplayMode.Simplified
        ? RibbonPanelPresentation.Full
        : VisibilityMode switch
        {
            RibbonVisibilityMode.PanelButtons => RibbonPanelPresentation.Buttons,
            RibbonVisibilityMode.PanelTitles => RibbonPanelPresentation.Titles,
            _ => RibbonPanelPresentation.Full,
        };

    internal void OnGroupFloatingChanged(RibbonGroup group)
    {
        InvalidateShortcuts();
        GroupFloatingChanged?.Invoke(this, group);
        RaiseStateChanged();
    }

    private void OnPanelOptionsChanged()
    {
        UpdateMinimizeButton();
        RaiseStateChanged();
        OnModelPanelOptionsChanged();
    }

    private void OnShowGroupCaptionsChanged()
    {
        ApplyPresentation();
        RaiseStateChanged();
        OnModelPanelOptionsChanged();
    }

    private void AttachMinimizeButtons()
    {
        if (_minimizeButton is not null)
        {
            _minimizeButton.Click -= OnMinimizeButtonClick;
        }

        if (_minimizeBehaviorButton is not null)
        {
            _minimizeBehaviorButton.Click -= OnMinimizeBehaviorClick;
        }

        _minimizeButtons = GetTemplateChild("PART_MinimizeButtons") as FrameworkElement;
        _minimizeButton = GetTemplateChild("PART_MinimizeButton") as Button;
        _minimizeBehaviorButton = GetTemplateChild("PART_MinimizeBehaviorButton") as Button;
        _minimizeGlyph = GetTemplateChild("PART_MinimizeGlyph") as FontIcon;
        if (_minimizeButton is not null)
        {
            _minimizeButton.Click += OnMinimizeButtonClick;
        }

        if (_minimizeBehaviorButton is not null)
        {
            _minimizeBehaviorButton.Click += OnMinimizeBehaviorClick;
        }

        UpdateMinimizeButton();
    }

    private void UpdateMinimizeButton()
    {
        if (_minimizeButtons is not null)
        {
            _minimizeButtons.Visibility = IsMinimizeButtonVisible && IsCollapsible ? Visibility.Visible : Visibility.Collapsed;
        }

        var strings = RibbonStrings.Current;
        var minimized = IsMinimizedMode(VisibilityMode);
        if (_minimizeGlyph is not null)
        {
            _minimizeGlyph.Glyph = minimized ? "" : "";
        }

        if (_minimizeButton is not null)
        {
            var next = NextMinimizeState();
            var text = next switch
            {
                RibbonVisibilityMode.PanelButtons => strings.MinimizeToPanelButtons,
                RibbonVisibilityMode.PanelTitles => strings.MinimizeToPanelTitles,
                RibbonVisibilityMode.TabsOnly => strings.MinimizeToTabs,
                _ => strings.ShowFullRibbon,
            };
            AutomationProperties.SetName(_minimizeButton, text);
            ToolTipService.SetToolTip(_minimizeButton, text);
        }

        if (_minimizeBehaviorButton is not null)
        {
            AutomationProperties.SetName(_minimizeBehaviorButton, strings.MinimizeRibbon);
            ToolTipService.SetToolTip(_minimizeBehaviorButton, strings.MinimizeRibbon);
        }
    }

    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e) => ToggleMinimized();

    private void OnMinimizeBehaviorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement anchor)
        {
            BuildMinimizeBehaviorMenu().ShowAt(anchor, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
        }
    }

    /// <summary>Builds the AutoCAD minimize-behaviour menu (Minimize to Tabs / Panel Titles / Panel Buttons / Cycle through All).</summary>
    public MenuFlyout BuildMinimizeBehaviorMenu()
    {
        var strings = RibbonStrings.Current;
        var menu = new MenuFlyout();
        foreach (var (text, behavior) in new[]
        {
            (strings.MinimizeToTabs, RibbonMinimizeBehavior.Tabs),
            (strings.MinimizeToPanelTitles, RibbonMinimizeBehavior.PanelTitles),
            (strings.MinimizeToPanelButtons, RibbonMinimizeBehavior.PanelButtons),
            (strings.CycleThroughAll, RibbonMinimizeBehavior.CycleAll),
        })
        {
            var item = new RadioMenuFlyoutItem { Text = text, GroupName = "minimize", IsChecked = MinimizeBehavior == behavior };
            item.Click += (_, _) => MinimizeBehavior = behavior;
            menu.Items.Add(item);
        }

        RibbonMenu.ApplyTheme(menu);
        return menu;
    }

    // Context-menu entries for groups and the AutoCAD visibility menus (added by ShowItemContextMenu).
    private void AddPanelMenuEntries(MenuFlyout menu, FrameworkElement element)
    {
        var strings = RibbonStrings.Current;
        var group = element as RibbonGroup ?? FindLocation(element).Group;
        var added = false;
        if (CanFloatGroups && group is not null)
        {
            menu.Items.Add(group.IsFloating
                ? RibbonMenu.Item(strings.ReturnPanelToRibbon, group.ReturnToRibbon, "")
                : RibbonMenu.Item(strings.FloatPanel, () => group.Float(), ""));
            added = true;
        }

        if (CanFloatGroups && FloatingGroups.Count > (group?.IsFloating == true ? 1 : 0))
        {
            menu.Items.Add(RibbonMenu.Item(strings.ReturnPanelsToRibbon, ReturnAllPanelsToRibbon));
            added = true;
        }

        if (IsVisibilityMenuEnabled)
        {
            menu.Items.Add(BuildShowTabsMenu());
            if (SelectedTab is { } tab && tab.Groups.Count > 0)
            {
                menu.Items.Add(BuildShowPanelsMenu(tab));
            }

            var titles = new ToggleMenuFlyoutItem { Text = strings.ShowGroupTitles, IsChecked = ShowGroupCaptions };
            titles.Click += (_, _) => ShowGroupCaptions = titles.IsChecked;
            menu.Items.Add(titles);
            added = true;
        }

        if (added)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
        }
    }

    /// <summary>"Show Tabs": checkable entries for every regular tab (unchecking hides it through the customization).</summary>
    public MenuFlyoutSubItem BuildShowTabsMenu()
    {
        var menu = new MenuFlyoutSubItem { Text = RibbonStrings.Current.ShowTabs };
        foreach (var tab in Tabs.Where(t => !t.IsContextual && t.EffectiveId is not null))
        {
            var id = tab.EffectiveId!;
            var item = new ToggleMenuFlyoutItem { Text = tab.Header ?? id, IsChecked = !_customization.HiddenTabIds.Contains(id) };
            item.Click += (_, _) => SetHiddenByUser(id, !item.IsChecked, isTab: true);
            menu.Items.Add(item);
        }

        return menu;
    }

    /// <summary>"Show Panels": checkable entries for the groups of a tab.</summary>
    public MenuFlyoutSubItem BuildShowPanelsMenu(RibbonTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        var menu = new MenuFlyoutSubItem { Text = RibbonStrings.Current.ShowPanels };
        foreach (var group in tab.DisplayGroups.Where(g => g.EffectiveId is not null))
        {
            var id = group.EffectiveId!;
            var item = new ToggleMenuFlyoutItem { Text = group.Header ?? id, IsChecked = !_customization.HiddenGroupIds.Contains(id) };
            item.Click += (_, _) => SetHiddenByUser(id, !item.IsChecked, isTab: false);
            menu.Items.Add(item);
        }

        return menu;
    }

    private void SetHiddenByUser(string id, bool hidden, bool isTab)
    {
        var customization = Clone(_customization);
        var list = isTab ? customization.HiddenTabIds : customization.HiddenGroupIds;
        list.Remove(id);
        if (hidden)
        {
            list.Add(id);
        }

        ApplyCustomization(customization);
    }

    /// <summary>
    /// Closes the ribbon's transient popups (collapsed-group and expanded panels, the "Show tabs only" popup, KeyTips,
    /// the backstage) and hides floating panels. Call it when an ancestor of the ribbon is collapsed (e.g. a tabbed or
    /// paged host hides the page) — the ribbon does this itself when it is unloaded or its own Visibility collapses.
    /// </summary>
    public void SuspendPopups()
    {
        CancelKeyTips();
        CloseMinimizedPopup();
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

    /// <summary>Shows floating panels (and pinned expanded panels of the selected tab) again after <see cref="SuspendPopups"/>.</summary>
    public void ResumePopups()
    {
        if (!IsLoaded || Visibility != Visibility.Visible)
        {
            return;
        }

        ShowFloatingGroups();
        foreach (var group in SelectedTab?.Groups.Where(g => g.IsSlideOutPinned) ?? [])
        {
            DispatcherQueue?.TryEnqueue(group.OpenSlideOut);
        }
    }

    private void OnRibbonVisibilityChanged()
    {
        if (Visibility == Visibility.Visible)
        {
            ResumePopups();
        }
        else
        {
            SuspendPopups();
        }
    }

    // Floating panels follow the ribbon in and out of the visual tree.
    private void ShowFloatingGroups()
    {
        foreach (var group in FloatingGroups)
        {
            group.ShowFloat();
        }
    }

    private void SuspendFloatingGroups()
    {
        foreach (var group in FloatingGroups)
        {
            group.SuspendFloat();
        }
    }

    private List<RibbonFloatingGroupState> GetFloatingGroupStates()
        => FloatingGroups.Where(g => g.EffectiveId is not null)
            .Select(g => new RibbonFloatingGroupState { GroupId = g.EffectiveId!, X = g.FloatingPosition.X, Y = g.FloatingPosition.Y })
            .ToList();

    private void ApplyFloatingGroupStates(IReadOnlyList<RibbonFloatingGroupState> states)
    {
        foreach (var group in Tabs.SelectMany(t => t.Groups))
        {
            var state = states.FirstOrDefault(s => s.GroupId == group.EffectiveId);
            if (state is null)
            {
                if (group.IsFloating)
                {
                    group.ReturnToRibbon();
                }
            }
            else
            {
                group.Float(new Windows.Foundation.Point(state.X, state.Y));
            }
        }
    }
}
