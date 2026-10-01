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
using RibbonSpace.Localization;
using Windows.Foundation;
using Windows.System;

namespace RibbonSpace.Controls;

/// <summary>
/// Stand-alone command toolbar built from ribbon items: horizontal command bars, tool option bars, vertical tool
/// palettes (1 or 2 columns) and activity rails. Items that do not fit move into a "⋯" overflow menu.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonToolBar : Control, IRibbonItemOwner, IRibbonLayoutHost
{
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(RibbonToolBar), new PropertyMetadata(Orientation.Horizontal, (d, _) => ((RibbonToolBar)d).ApplyLayouts()));

    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int), typeof(RibbonToolBar), new PropertyMetadata(1, (d, _) => ((RibbonToolBar)d).ApplyLayouts()));

    /// <summary>Identifies <see cref="ShowLabels"/>.</summary>
    public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(nameof(ShowLabels), typeof(bool), typeof(RibbonToolBar), new PropertyMetadata(false, (d, _) => ((RibbonToolBar)d).ApplyLayouts()));

    /// <summary>Identifies <see cref="IsOverflowEnabled"/>.</summary>
    public static readonly DependencyProperty IsOverflowEnabledProperty = DependencyProperty.Register(nameof(IsOverflowEnabled), typeof(bool), typeof(RibbonToolBar), new PropertyMetadata(true, (d, _) => ((RibbonToolBar)d).ApplyLayouts()));

    /// <summary>Identifies <see cref="Density"/>.</summary>
    public static readonly DependencyProperty DensityProperty = DependencyProperty.Register(nameof(Density), typeof(RibbonDensity), typeof(RibbonToolBar), new PropertyMetadata(RibbonDensity.Comfortable, (d, _) => ((RibbonToolBar)d).ApplyLayouts()));

    /// <summary>Identifies <see cref="CommandCatalog"/>.</summary>
    public static readonly DependencyProperty CommandCatalogProperty = DependencyProperty.Register(nameof(CommandCatalog), typeof(RibbonCommandCatalog), typeof(RibbonToolBar), new PropertyMetadata(null, (d, _) => ((RibbonToolBar)d).ResolveCommands()));

    /// <summary>Identifies <see cref="Ribbon"/>.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(nameof(Ribbon), typeof(Ribbon), typeof(RibbonToolBar), new PropertyMetadata(null, (d, _) => ((RibbonToolBar)d).ResolveCommands()));

    private readonly Primitives.RibbonToolBarPanel _panel = new();
    private readonly RibbonButton _overflowButton;
    private readonly Dictionary<UIElement, bool> _hiddenByOverflow = [];
    private Border? _host;

    /// <summary>Creates a toolbar.</summary>
    public RibbonToolBar()
    {
        DefaultStyleKey = typeof(RibbonToolBar);
        RibbonTheme.EnsureResources();
        IsTabStop = false;
        Items.CollectionChanged += OnItemsChanged;
        _overflowButton = new RibbonButton { Icon = "", Label = RibbonStrings.Current.MoreOptions, ShowLabel = false, CanAddToQuickAccess = false, Id = "toolbar.overflow" };
        ToolTipService.SetToolTip(_overflowButton, RibbonStrings.Current.MoreOptions);
        _overflowButton.IsChromeButton = true;
        _overflowButton.Click += (_, _) => DispatcherQueue?.TryEnqueue(ShowOverflow);
        RibbonItemHelper.SetOwner(_overflowButton, this);
        _panel.OverflowButton = _overflowButton;
        AutomationProperties.SetName(this, "Toolbar");
        KeyDown += OnKeyDown;
        LayoutUpdated += (_, _) => UpdateOverflowAccessibility();
    }

    /// <summary>Raised when an item is invoked.</summary>
    public event EventHandler<RibbonItemInvokedEventArgs>? ItemInvoked;

    /// <summary>Items.</summary>
    public ObservableCollection<UIElement> Items { get; } = [];

    /// <summary>Orientation.</summary>
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Columns of vertical toolbars (tool palettes: 1 or 2).</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Shows labels next to icons.</summary>
    public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

    /// <summary>Moves items that do not fit into the overflow menu (otherwise they are clipped).</summary>
    public bool IsOverflowEnabled { get => (bool)GetValue(IsOverflowEnabledProperty); set => SetValue(IsOverflowEnabledProperty, value); }

    /// <summary>Density.</summary>
    public RibbonDensity Density { get => (RibbonDensity)GetValue(DensityProperty); set => SetValue(DensityProperty, value); }

    /// <summary>Command catalog for <c>CommandId</c> resolution.</summary>
    public RibbonCommandCatalog? CommandCatalog { get => (RibbonCommandCatalog?)GetValue(CommandCatalogProperty); set => SetValue(CommandCatalogProperty, value); }

    /// <summary>Optional ribbon (context menu, shared command state).</summary>
    public Ribbon? Ribbon { get => (Ribbon?)GetValue(RibbonProperty); set => SetValue(RibbonProperty, value); }

    /// <inheritdoc />
    public RibbonMetrics Metrics => Ribbon?.Metrics ?? RibbonMetrics.For(Density);

    /// <summary>Items currently in the overflow menu.</summary>
    public IEnumerable<UIElement> OverflowItems => _panel.OverflowItems;

    Ribbon? IRibbonItemOwner.OwnerRibbon => Ribbon;

    /// <inheritdoc />
    public void OnItemInvoked(FrameworkElement item, string? commandId, object? parameter)
    {
        ItemInvoked?.Invoke(this, new RibbonItemInvokedEventArgs(item, commandId, parameter));
        Ribbon?.OnItemInvoked(item, commandId, parameter);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_host is not null)
        {
            _host.Child = null;
        }

        base.OnApplyTemplate();
        _host = GetTemplateChild("PART_ItemsHost") as Border;
        if (_host is not null)
        {
            _host.Child = _panel;
        }

        Sync();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Sync();

    private void Sync()
    {
        var overflow = _panel.OverflowButton;
        _panel.OverflowButton = null;
        _panel.Children.Clear();
        foreach (var item in Items)
        {
            if (VisualTreeHelper.GetParent(item) is Panel parent)
            {
                parent.Children.Remove(item);
            }

            RibbonItemHelper.SetOwner(item, this);
            _panel.Children.Add(item);
        }

        foreach (var stale in _hiddenByOverflow.Keys.Where(i => !Items.Contains(i)).ToList())
        {
            RestoreAccessibility(stale);
        }

        _panel.OverflowButton = overflow;
        ResolveCommands();
        ApplyLayouts();
    }

    private void ResolveCommands()
    {
        // CommandId items resolve through this toolbar's catalog (or the ribbon's).
        foreach (var item in Items.OfType<FrameworkElement>().SelectMany(RibbonGroup.Flatten))
        {
            RibbonItemHelper.ResolveCommand(item);
        }
    }

    /// <summary>
    /// Items moved to the overflow menu (and the hidden overflow button) are arranged with a zero size: keep them out
    /// of the tab order and of the automation tree.
    /// </summary>
    private void UpdateOverflowAccessibility()
    {
        var overflowed = _panel.OverflowItems;
        foreach (var item in Items)
        {
            var hidden = overflowed.Contains(item);
            if (hidden && !_hiddenByOverflow.ContainsKey(item))
            {
                _hiddenByOverflow[item] = item is Control { IsTabStop: true };
                if (item is Control control)
                {
                    control.IsTabStop = false;
                }

                AutomationProperties.SetAccessibilityView(item, AccessibilityView.Raw);
            }
            else if (!hidden && _hiddenByOverflow.ContainsKey(item))
            {
                RestoreAccessibility(item);
            }
        }

        var showButton = overflowed.Count > 0;
        if (_overflowButton.IsTabStop != showButton)
        {
            _overflowButton.IsTabStop = showButton;
            AutomationProperties.SetAccessibilityView(_overflowButton, showButton ? AccessibilityView.Content : AccessibilityView.Raw);
        }
    }

    private void RestoreAccessibility(UIElement item)
    {
        if (_hiddenByOverflow.Remove(item, out var wasTabStop))
        {
            if (item is Control control && wasTabStop)
            {
                control.IsTabStop = true;
            }

            item.ClearValue(AutomationProperties.AccessibilityViewProperty);
        }
    }

    private void ApplyLayouts()
    {
        var metrics = Metrics;
        _panel.Orientation = Orientation;
        _panel.Columns = Math.Max(1, Columns);
        _panel.IsOverflowEnabled = IsOverflowEnabled;
        foreach (var item in Items)
        {
            if (item is RibbonSeparator separator)
            {
                separator.Orientation = Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
            }

            if (item is IRibbonItem ribbonItem)
            {
                ribbonItem.ApplyLayout(new RibbonItemLayout(ShowLabels ? RibbonItemSize.Medium : RibbonItemSize.Small, metrics, true, ShowLabels ? null : false));
            }
        }

        _overflowButton.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Small, metrics, true, false));
        _panel.InvalidateMeasure();
    }

    /// <inheritdoc />
    public void InvalidateItemsLayout() => _panel.InvalidateMeasure();

    /// <summary>Shows the overflow menu.</summary>
    public void ShowOverflow()
    {
        var panel = new StackPanel { Spacing = 1, MinWidth = 200, Padding = new Thickness(2), RequestedTheme = ActualTheme };
        foreach (var item in _panel.OverflowItems)
        {
            if (item is RibbonSeparator)
            {
                var line = new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Margin = new Thickness(4) };
                RibbonTheme.SetThemeBrush(line, Microsoft.UI.Xaml.Shapes.Shape.FillProperty, "RibbonSeparatorBrush");
                panel.Children.Add(line);
                continue;
            }

            if (item is IRibbonItem ribbonItem && ribbonItem.CreateLinkedCopy() is { } copy)
            {
                RibbonItemHelper.SetOwner(copy, this);
                (copy as IRibbonItem)?.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Metrics, false, true));
                copy.HorizontalAlignment = HorizontalAlignment.Stretch;
                panel.Children.Add(copy);
            }
            else if (item.Visibility == Visibility.Visible && CreateOverflowPlaceholder(item) is { } placeholder)
            {
                // Arbitrary content cannot be cloned: list it (disabled) so the user knows it needs more room.
                panel.Children.Add(placeholder);
            }
        }

        var flyout = new Flyout { Content = new ScrollViewer { Content = panel, MaxHeight = 520 }, Placement = Orientation == Orientation.Horizontal ? FlyoutPlacementMode.BottomEdgeAlignedRight : FlyoutPlacementMode.RightEdgeAlignedBottom };
        if (Application.Current.Resources.TryGetValue("RibbonFlyoutPresenterStyle", out var style))
        {
            flyout.FlyoutPresenterStyle = (Style)style;
        }

        flyout.Closed += (_, _) => RibbonItemHelper.UnlinkTree(panel);
        flyout.ShowAt(_overflowButton);
    }

    private static FrameworkElement? CreateOverflowPlaceholder(UIElement item)
    {
        var name = AutomationProperties.GetName(item);
        if (string.IsNullOrEmpty(name) && item is FrameworkElement { Name.Length: > 0 } named)
        {
            name = named.Name;
        }

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var text = new TextBlock { Text = name, Margin = new Thickness(8, 4, 8, 4), TextTrimming = TextTrimming.CharacterEllipsis };
        RibbonTheme.SetThemeBrush(text, TextBlock.ForegroundProperty, "RibbonDisabledForegroundBrush");
        return text;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var forward = Orientation == Orientation.Horizontal ? VirtualKey.Right : VirtualKey.Down;
        var back = Orientation == Orientation.Horizontal ? VirtualKey.Left : VirtualKey.Up;
        if ((e.Key != forward && e.Key != back) || XamlRoot is null)
        {
            return;
        }

        // Only roving focus between the toolbar's own children: composite items (combo boxes, split buttons, text
        // boxes, segmented controls) keep their arrow keys.
        var focusables = _panel.Children.OfType<Control>().Where(c => c.Visibility == Visibility.Visible && c.IsTabStop && c.IsEnabled && c.ActualWidth > 0).ToList();
        var current = FocusManager.GetFocusedElement(XamlRoot) as Control;
        if (current is null || current is TextBox || !ReferenceEquals(e.OriginalSource, current) || current is RibbonInputBase)
        {
            return;
        }

        var index = focusables.IndexOf(current);
        if (index < 0)
        {
            return;
        }

        var next = Math.Clamp(index + (e.Key == forward ? 1 : -1), 0, focusables.Count - 1);
        if (next != index)
        {
            focusables[next].Focus(FocusState.Keyboard);
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonToolBarAutomationPeer(this);
}

/// <summary>
/// Hosts several toolbars (or any content) and shows the one matching <see cref="ActiveContext"/> — Photoshop /
/// Illustrator tool option bars, selection-dependent editor toolbars.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonContextualToolBar : Control
{
    /// <summary>Identifies <see cref="ActiveContext"/>.</summary>
    public static readonly DependencyProperty ActiveContextProperty = DependencyProperty.Register(nameof(ActiveContext), typeof(string), typeof(RibbonContextualToolBar), new PropertyMetadata(null, (d, _) => ((RibbonContextualToolBar)d).Update()));

    /// <summary>Identifies the Context attached property.</summary>
    public static readonly DependencyProperty ContextProperty = DependencyProperty.RegisterAttached("Context", typeof(string), typeof(RibbonContextualToolBar), new PropertyMetadata(null));

    private Grid? _host;

    /// <summary>Creates the host.</summary>
    public RibbonContextualToolBar()
    {
        DefaultStyleKey = typeof(RibbonContextualToolBar);
        Items.CollectionChanged += (_, _) => Sync();
        IsTabStop = false;
    }

    /// <summary>Raised after the active context changed.</summary>
    public event EventHandler<string?>? ContextChanged;

    /// <summary>Contextual contents (set RibbonContextualToolBar.Context on each).</summary>
    public ObservableCollection<UIElement> Items { get; } = [];

    /// <summary>Active context key (tool, selection kind, view).</summary>
    public string? ActiveContext { get => (string?)GetValue(ActiveContextProperty); set => SetValue(ActiveContextProperty, value); }

    /// <summary>Gets the context key of an element.</summary>
    public static string? GetContext(DependencyObject element) => (string?)element.GetValue(ContextProperty);

    /// <summary>Sets the context key of an element.</summary>
    public static void SetContext(DependencyObject element, string? value) => element.SetValue(ContextProperty, value);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        _host?.Children.Clear();
        base.OnApplyTemplate();
        _host = GetTemplateChild("PART_Host") as Grid;
        Sync();
    }

    private void Sync()
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

            _host.Children.Add(item);
        }

        Update();
    }

    private void Update()
    {
        foreach (var item in Items)
        {
            var context = GetContext(item);
            item.Visibility = string.Equals(context, ActiveContext, StringComparison.Ordinal) || (context is null && ActiveContext is null) ? Visibility.Visible : Visibility.Collapsed;
        }

        ContextChanged?.Invoke(this, ActiveContext);
    }
}

/// <summary>Top-level entry of a <see cref="RibbonMenuBar"/>.</summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonMenuBarItem : DependencyObject
{
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(RibbonMenuBarItem), new PropertyMetadata(null, (d, _) => ((RibbonMenuBarItem)d).HeaderChanged?.Invoke(d, EventArgs.Empty)));

    private MenuFlyout? _menu;

    /// <summary>Creates a menu bar entry.</summary>
    public RibbonMenuBarItem()
    {
        Items.CollectionChanged += (_, _) => SyncMenu();
    }

    /// <summary>Header ("File", "Edit").</summary>
    public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Menu entries (changes apply to the menu immediately).</summary>
    public ObservableCollection<MenuFlyoutItemBase> Items { get; } = [];

    /// <summary>Raised before the menu opens (refresh enabled / checked states).</summary>
    public event EventHandler? Opening;

    internal event EventHandler? HeaderChanged;

    /// <summary>The menu showing <see cref="Items"/> (one per entry, reused by every rebuild of the menu bar).</summary>
    internal MenuFlyout Menu
    {
        get
        {
            if (_menu is null)
            {
                _menu = new MenuFlyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
                _menu.Opening += (_, _) => Opening?.Invoke(this, EventArgs.Empty);
                SyncMenu();
            }

            return _menu;
        }
    }

    private void SyncMenu()
    {
        if (_menu is null)
        {
            return;
        }

        // The entries stay in Items (the menu only displays them).
        _menu.Items.Clear();
        foreach (var entry in Items)
        {
            _menu.Items.Add(entry);
        }
    }
}

/// <summary>Classic menu bar (File, Edit, View...) with hover switching and keyboard navigation.</summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonMenuBar : Control
{
    private readonly List<(Button Header, RibbonMenuBarItem Item)> _menus = [];
    private StackPanel? _host;
    private MenuFlyout? _open;

    /// <summary>Creates a menu bar.</summary>
    public RibbonMenuBar()
    {
        DefaultStyleKey = typeof(RibbonMenuBar);
        RibbonTheme.EnsureResources();
        Items.CollectionChanged += (_, _) => Build();
        IsTabStop = false;
    }

    /// <summary>Menus.</summary>
    public ObservableCollection<RibbonMenuBarItem> Items { get; } = [];

    /// <summary>True while a menu is open.</summary>
    public bool IsMenuOpen => _open is not null;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        _host?.Children.Clear();
        base.OnApplyTemplate();
        _host = GetTemplateChild("PART_ItemsHost") as StackPanel;
        Build();
    }

    private void Build()
    {
        foreach (var (_, item) in _menus)
        {
            item.HeaderChanged -= OnItemHeaderChanged;
            item.Menu.Opened -= OnMenuOpened;
            item.Menu.Closed -= OnMenuClosed;
        }

        _menus.Clear();
        _host?.Children.Clear();
        if (_host is null)
        {
            return;
        }

        foreach (var item in Items)
        {
            var header = new Button { Content = item.Header, Padding = new Thickness(8, 2, 8, 2) };
            if (Application.Current.Resources.TryGetValue("RibbonChromeButtonStyle", out var style))
            {
                header.Style = (Style)style;
            }

            AutomationProperties.SetName(header, item.Header ?? string.Empty);
            var menu = item.Menu;

            // Pointer input over the bar reaches the headers while a menu is open (hover switching).
            menu.OverlayInputPassThroughElement = this;
            menu.Opened += OnMenuOpened;
            menu.Closed += OnMenuClosed;
            item.HeaderChanged += OnItemHeaderChanged;
            header.Click += (_, _) =>
            {
                if (ReferenceEquals(_open, menu))
                {
                    menu.Hide();
                }
                else
                {
                    Open(header, menu);
                }
            };
            header.PointerEntered += (_, _) =>
            {
                if (_open is not null && !ReferenceEquals(_open, menu))
                {
                    Open(header, menu);
                }
            };
            header.KeyDown += (_, e) => OnHeaderKey(header, e);
            _menus.Add((header, item));
            _host.Children.Add(header);
        }
    }

    private void OnMenuOpened(object? sender, object e) => _open = sender as MenuFlyout;

    private void OnMenuClosed(object? sender, object e)
    {
        if (ReferenceEquals(_open, sender))
        {
            _open = null;
        }
    }

    private void OnItemHeaderChanged(object? sender, EventArgs e)
    {
        foreach (var (header, item) in _menus)
        {
            if (ReferenceEquals(item, sender))
            {
                header.Content = item.Header;
                AutomationProperties.SetName(header, item.Header ?? string.Empty);
            }
        }
    }

    private void Open(Button header, MenuFlyout menu)
    {
        if (_open is { } open && !ReferenceEquals(open, menu))
        {
            open.Hide();
        }

        _open = menu;
        menu.ShowAt(header);
    }

    private void OnHeaderKey(Button header, KeyRoutedEventArgs e)
    {
        var index = _menus.FindIndex(m => ReferenceEquals(m.Header, header));
        if (index < 0)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Right:
                _menus[(index + 1) % _menus.Count].Header.Focus(FocusState.Keyboard);
                e.Handled = true;
                break;
            case VirtualKey.Left:
                _menus[(index - 1 + _menus.Count) % _menus.Count].Header.Focus(FocusState.Keyboard);
                e.Handled = true;
                break;
            case VirtualKey.Down:
                Open(header, _menus[index].Item.Menu);
                e.Handled = true;
                break;
        }
    }

    /// <summary>Focuses the first menu header (F10 / Alt activation).</summary>
    public void FocusFirst() => _menus.FirstOrDefault().Header?.Focus(FocusState.Keyboard);

    /// <summary>Opens a menu by header.</summary>
    public bool OpenMenu(string header)
    {
        var index = _menus.FindIndex(m => string.Equals(m.Item.Header, header, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        Open(_menus[index].Header, _menus[index].Item.Menu);
        return true;
    }
}
