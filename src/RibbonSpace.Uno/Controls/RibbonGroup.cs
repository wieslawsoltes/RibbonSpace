using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Localization;
using Windows.Foundation;

namespace RibbonSpace.Controls;

/// <summary>
/// Ribbon group ("Clipboard", "Font"): arranges items, shows the caption and dialog launcher, adapts its size
/// (Large → Medium → Small → Collapsed) and presents itself in the simplified line.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonGroup : Control, IRibbonLayoutHost
{
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(RibbonGroup), new PropertyMetadata(null, (d, _) => ((RibbonGroup)d).OnHeaderChanged()));

    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="Id"/>.</summary>
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(RibbonGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="KeyTip"/>.</summary>
    public static readonly DependencyProperty KeyTipProperty = DependencyProperty.Register(nameof(KeyTip), typeof(string), typeof(RibbonGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="ScreenTip"/>.</summary>
    public static readonly DependencyProperty ScreenTipProperty = DependencyProperty.Register(nameof(ScreenTip), typeof(object), typeof(RibbonGroup), new PropertyMetadata(null, (d, _) => ((RibbonGroup)d).OnHeaderChanged()));

    /// <summary>Identifies <see cref="DialogLauncherCommand"/>.</summary>
    public static readonly DependencyProperty DialogLauncherCommandProperty = DependencyProperty.Register(nameof(DialogLauncherCommand), typeof(ICommand), typeof(RibbonGroup), new PropertyMetadata(null, (d, _) => ((RibbonGroup)d).UpdateLauncher()));

    /// <summary>Identifies <see cref="DialogLauncherCommandParameter"/>.</summary>
    public static readonly DependencyProperty DialogLauncherCommandParameterProperty = DependencyProperty.Register(nameof(DialogLauncherCommandParameter), typeof(object), typeof(RibbonGroup), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="IsDialogLauncherVisible"/>.</summary>
    public static readonly DependencyProperty IsDialogLauncherVisibleProperty = DependencyProperty.Register(nameof(IsDialogLauncherVisible), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(false, (d, _) => ((RibbonGroup)d).UpdateLauncher()));

    /// <summary>Identifies <see cref="DialogLauncherScreenTip"/>.</summary>
    public static readonly DependencyProperty DialogLauncherScreenTipProperty = DependencyProperty.Register(nameof(DialogLauncherScreenTip), typeof(object), typeof(RibbonGroup), new PropertyMetadata(null, (d, _) => ((RibbonGroup)d).UpdateLauncher()));

    /// <summary>Identifies <see cref="ReductionOrder"/>.</summary>
    public static readonly DependencyProperty ReductionOrderProperty = DependencyProperty.Register(nameof(ReductionOrder), typeof(int), typeof(RibbonGroup), new PropertyMetadata(0, (d, _) => ((RibbonGroup)d).InvalidateItemsLayout()));

    /// <summary>Identifies <see cref="CanCollapse"/>.</summary>
    public static readonly DependencyProperty CanCollapseProperty = DependencyProperty.Register(nameof(CanCollapse), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(true, (d, _) => ((RibbonGroup)d).InvalidateItemsLayout()));

    /// <summary>Identifies <see cref="ItemsLayout"/>.</summary>
    public static readonly DependencyProperty ItemsLayoutProperty = DependencyProperty.Register(nameof(ItemsLayout), typeof(RibbonGroupItemsLayout), typeof(RibbonGroup), new PropertyMetadata(RibbonGroupItemsLayout.Columns, (d, _) => ((RibbonGroup)d).InvalidateItemsLayout()));

    /// <summary>Identifies <see cref="RowCount"/>.</summary>
    public static readonly DependencyProperty RowCountProperty = DependencyProperty.Register(nameof(RowCount), typeof(int), typeof(RibbonGroup), new PropertyMetadata(3, (d, _) => ((RibbonGroup)d).InvalidateItemsLayout()));

    /// <summary>Identifies <see cref="State"/>.</summary>
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(RibbonGroupState), typeof(RibbonGroup), new PropertyMetadata(RibbonGroupState.Large));

    /// <summary>Identifies <see cref="IsSimplified"/>.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty = DependencyProperty.Register(nameof(IsSimplified), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(false));

    /// <summary>Identifies <see cref="SimplifiedVisibility"/>.</summary>
    public static readonly DependencyProperty SimplifiedVisibilityProperty = DependencyProperty.Register(nameof(SimplifiedVisibility), typeof(RibbonSimplifiedVisibility), typeof(RibbonGroup), new PropertyMetadata(RibbonSimplifiedVisibility.Auto, (d, _) => ((RibbonGroup)d).InvalidateItemsLayout()));

    /// <summary>Identifies <see cref="IsGroupVisible"/>.</summary>
    public static readonly DependencyProperty IsGroupVisibleProperty = DependencyProperty.Register(nameof(IsGroupVisible), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(true, (d, _) => ((RibbonGroup)d).UpdateVisibility()));

    private readonly RibbonGroupItemsPanel _itemsPanel = new();
    private Border? _itemsPresenter;
    private FrameworkElement? _header;
    private Button? _launcher;
    private RibbonButton? _collapsedButton;
    private FrameworkElement? _captionRow;
    private Popup? _popup;
    private Border? _popupPresenter;
    private TextBlock? _popupHeader;
    private Button? _popupLauncher;
    private RibbonMetrics _metrics = RibbonMetrics.Comfortable;
    private bool _hiddenByCustomization;

    /// <summary>Creates a group.</summary>
    public RibbonGroup()
    {
        DefaultStyleKey = typeof(RibbonGroup);
        RibbonTheme.EnsureResources();
        Items.CollectionChanged += OnItemsChanged;
        SlideOutItems.CollectionChanged += OnSlideOutItemsChanged;
        RightTapped += OnGroupRightTapped;
        IsTabStop = false;
        AutomationProperties.SetLandmarkType(this, AutomationLandmarkType.Custom);
    }

    /// <summary>Raised when the dialog launcher is clicked.</summary>
    public event RoutedEventHandler? DialogLauncherClick;

    /// <summary>Items.</summary>
    public ObservableCollection<UIElement> Items { get; } = [];

    /// <summary>Caption.</summary>
    public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Icon of the collapsed group button.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Stable id (customization, persistence).</summary>
    public string? Id { get => (string?)GetValue(IdProperty); set => SetValue(IdProperty, value); }

    /// <summary>KeyTip of the collapsed group button.</summary>
    public string? KeyTip { get => (string?)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

    /// <summary>
    /// ScreenTip of the group (a string description or a <see cref="RibbonScreenTip"/>), shown on the button of the
    /// collapsed group and exposed as the group's automation help text.
    /// </summary>
    public object? ScreenTip { get => GetValue(ScreenTipProperty); set => SetValue(ScreenTipProperty, value); }

    /// <summary>Dialog launcher command (shows the launcher).</summary>
    public ICommand? DialogLauncherCommand { get => (ICommand?)GetValue(DialogLauncherCommandProperty); set => SetValue(DialogLauncherCommandProperty, value); }

    /// <summary>Dialog launcher command parameter.</summary>
    public object? DialogLauncherCommandParameter { get => GetValue(DialogLauncherCommandParameterProperty); set => SetValue(DialogLauncherCommandParameterProperty, value); }

    /// <summary>Shows the dialog launcher even without a command (handle <see cref="DialogLauncherClick"/>).</summary>
    public bool IsDialogLauncherVisible { get => (bool)GetValue(IsDialogLauncherVisibleProperty); set => SetValue(IsDialogLauncherVisibleProperty, value); }

    /// <summary>ScreenTip of the dialog launcher.</summary>
    public object? DialogLauncherScreenTip { get => GetValue(DialogLauncherScreenTipProperty); set => SetValue(DialogLauncherScreenTipProperty, value); }

    /// <summary>Groups with a higher value shrink first.</summary>
    public int ReductionOrder { get => (int)GetValue(ReductionOrderProperty); set => SetValue(ReductionOrderProperty, value); }

    /// <summary>Allows collapsing into a single button.</summary>
    public bool CanCollapse { get => (bool)GetValue(CanCollapseProperty); set => SetValue(CanCollapseProperty, value); }

    /// <summary>Item arrangement.</summary>
    public RibbonGroupItemsLayout ItemsLayout { get => (RibbonGroupItemsLayout)GetValue(ItemsLayoutProperty); set => SetValue(ItemsLayoutProperty, value); }

    /// <summary>Rows per column (2 or 3).</summary>
    public int RowCount { get => (int)GetValue(RowCountProperty); set => SetValue(RowCountProperty, value); }

    /// <summary>Current adaptive state (set by the ribbon).</summary>
    public RibbonGroupState State { get => (RibbonGroupState)GetValue(StateProperty); private set => SetValue(StateProperty, value); }

    /// <summary>Simplified presentation (set by the ribbon).</summary>
    public bool IsSimplified { get => (bool)GetValue(IsSimplifiedProperty); private set => SetValue(IsSimplifiedProperty, value); }

    /// <summary>Simplified behaviour of the whole group.</summary>
    public RibbonSimplifiedVisibility SimplifiedVisibility { get => (RibbonSimplifiedVisibility)GetValue(SimplifiedVisibilityProperty); set => SetValue(SimplifiedVisibilityProperty, value); }

    /// <summary>Visibility of the group (use instead of Visibility; customization may also hide groups).</summary>
    public bool IsGroupVisible { get => (bool)GetValue(IsGroupVisibleProperty); set => SetValue(IsGroupVisibleProperty, value); }

    /// <summary>Owning tab.</summary>
    public RibbonTab? Tab { get; internal set; }

    /// <summary>Owning ribbon.</summary>
    public Ribbon? Ribbon => Tab?.Ribbon;

    /// <summary>Increments whenever cached measurements must be recomputed.</summary>
    public int LayoutVersion { get; private set; }

    /// <summary>Metrics in effect.</summary>
    public RibbonMetrics Metrics => _metrics;

    /// <summary>True when the collapsed popup is open.</summary>
    public bool IsPopupOpen => _popup?.IsOpen == true;

    /// <summary>The collapsed-state button (KeyTips, tests).</summary>
    public RibbonButton? CollapsedButton => _collapsedButton;

    /// <summary>The dialog launcher button.</summary>
    public Button? DialogLauncher => _launcher;

    /// <summary>The items panel.</summary>
    public RibbonGroupItemsPanel ItemsPanel => _itemsPanel;

    internal bool HiddenByCustomization
    {
        get => _hiddenByCustomization;
        set
        {
            _hiddenByCustomization = value;
            UpdateVisibility();
        }
    }

    internal string? OriginalHeader { get; set; }

    internal string? EffectiveId => !string.IsNullOrEmpty(Id) ? Id : !string.IsNullOrEmpty(Name) ? Name : Header;

    /// <summary>Visible and not in the Hidden simplified mode.</summary>
    internal bool IsShown => Visibility == Visibility.Visible;

    private void UpdateVisibility()
    {
        // A floating panel leaves its place in the tab (it lives in its own window-level popup).
        Visibility = IsGroupVisible && !_hiddenByCustomization && !IsFloating ? Visibility.Visible : Visibility.Collapsed;
        Tab?.InvalidateGroupsLayout();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_launcher is not null)
        {
            _launcher.Click -= OnLauncherClick;
        }

        if (_collapsedButton is not null)
        {
            _collapsedButton.Click -= OnCollapsedClick;
        }

        DetachPanelParts();
        if (_itemsPresenter is not null && ReferenceEquals(_itemsPresenter.Child, _itemsPanel))
        {
            _itemsPresenter.Child = null;
        }

        base.OnApplyTemplate();
        _itemsPresenter = GetTemplateChild("PART_ItemsPresenter") as Border;
        _header = GetTemplateChild("PART_Header") as FrameworkElement;
        _captionRow = GetTemplateChild("PART_CaptionRow") as FrameworkElement;
        _launcher = GetTemplateChild("PART_DialogLauncher") as Button;
        _collapsedButton = GetTemplateChild("PART_CollapsedButton") as RibbonButton;
        if (_itemsPresenter is not null && _popupPresenter?.Child != _itemsPanel && !IsFloating)
        {
            _itemsPresenter.Child = _itemsPanel;
        }

        AttachPanelParts();

        if (_launcher is not null)
        {
            _launcher.Click += OnLauncherClick;
        }

        if (_collapsedButton is not null)
        {
            _collapsedButton.Click += OnCollapsedClick;
            _collapsedButton.IsChromeButton = true;
            ApplyCollapsedButtonLayout();
        }

        SyncItems();
        UpdateLauncher();
        OnHeaderChanged();
        ApplyPresentation();
    }

    private void OnHeaderChanged()
    {
        if (_collapsedButton is not null)
        {
            _collapsedButton.Label = Header;
            // Panel titles (AutoCAD "Minimize to Panel Titles") show the title only.
            _collapsedButton.Icon = PanelPresentation == RibbonPanelPresentation.Titles
                ? null
                : Icon ?? Items.OfType<IRibbonItem>().FirstOrDefault(i => i.Icon is not null)?.Icon ?? "";
            _collapsedButton.ScreenTip = ScreenTip;
        }

        AutomationProperties.SetName(this, Header ?? string.Empty);
        var help = ScreenTip switch
        {
            string text => text,
            RibbonScreenTip tip => tip.Description ?? tip.Title,
            Model.RibbonScreenTip tip => tip.Description ?? tip.Title,
            _ => null,
        };
        if (string.IsNullOrEmpty(help))
        {
            ClearValue(AutomationProperties.HelpTextProperty);
        }
        else
        {
            AutomationProperties.SetHelpText(this, help);
        }

        UpdateLauncher();
        InvalidateItemsLayout();
    }

    /// <summary>True for groups created by the user's customization.</summary>
    internal bool IsCustom { get; set; }

    /// <summary>Refreshes localized texts.</summary>
    internal void OnStringsChanged() => UpdateLauncher();

    private void UpdateLauncher()
    {
        if (_launcher is null)
        {
            return;
        }

        var visible = IsDialogLauncherVisible || DialogLauncherCommand is not null;
        _launcher.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ToolTipService.SetToolTip(_launcher, CreateLauncherToolTip());
        AutomationProperties.SetName(_launcher, RibbonStrings.Current.Format(nameof(RibbonStrings.DialogLauncher), Header ?? string.Empty));
    }

    // A new tooltip per button: tooltip content is an element and can only have one parent.
    private ToolTip? CreateLauncherToolTip()
    {
        var title = RibbonStrings.Current.Format(nameof(RibbonStrings.DialogLauncher), Header ?? string.Empty);
        var tip = DialogLauncherScreenTip ?? title;
        return RibbonScreenTipService.CreateToolTip(RibbonScreenTipService.Create(title, tip is string ? null : tip, null));
    }

    private void OnLauncherClick(object sender, RoutedEventArgs e) => OpenDialogLauncher();

    /// <summary>Invokes the dialog launcher.</summary>
    public void OpenDialogLauncher()
    {
        ClosePopup();
        DialogLauncherClick?.Invoke(this, new RoutedEventArgs());
        if (DialogLauncherCommand is { } command && command.CanExecute(DialogLauncherCommandParameter))
        {
            command.Execute(DialogLauncherCommandParameter);
        }

        Ribbon?.OnItemInvoked(this, Id is null ? null : Id + ".launcher", DialogLauncherCommandParameter);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
        OnHeaderChanged();
    }

    private void SyncItems()
    {
        // Removed items: drop them from the panel and stop observing their visibility.
        foreach (var (removed, token) in _visibilityTokens.Where(p => !Items.Contains(p.Key)).ToArray())
        {
            removed.UnregisterPropertyChangedCallback(VisibilityProperty, token);
            _visibilityTokens.Remove(removed);
        }

        foreach (var stale in _itemsPanel.Children.Where(c => !Items.Contains(c)).ToArray())
        {
            _itemsPanel.Children.Remove(stale);
        }

        // Insert / reorder in place so items that stay are not unloaded and reloaded.
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var index = _itemsPanel.Children.IndexOf(item);
            if (index != i)
            {
                if (index >= 0)
                {
                    _itemsPanel.Children.RemoveAt(index);
                }
                else if (VisualTreeHelper.GetParent(item) is Panel parent)
                {
                    parent.Children.Remove(item);
                }

                _itemsPanel.Children.Insert(Math.Min(i, _itemsPanel.Children.Count), item);
            }

            if (Ribbon is { } ribbon)
            {
                RibbonItemHelper.SetOwner(item, ribbon);
            }

            if (!_visibilityTokens.ContainsKey(item))
            {
                _visibilityTokens[item] = item.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => InvalidateItemsLayout());
            }
        }

        _state = null;
        Ribbon?.InvalidateShortcuts();
        InvalidateItemsLayout();
    }

    private readonly Dictionary<UIElement, long> _visibilityTokens = [];

    internal void AttachToRibbon(Ribbon ribbon)
    {
        foreach (var item in Items)
        {
            RibbonItemHelper.SetOwner(item, ribbon);
        }

        RibbonItemHelper.SetOwner(this, ribbon);
    }

    /// <inheritdoc />
    public void InvalidateItemsLayout()
    {
        LayoutVersion++;
        InvalidateMeasure();
        _itemsPanel.InvalidateMeasure();
        Tab?.InvalidateGroupsLayout();
    }

    private (RibbonGroupState State, bool Simplified, RibbonMetrics Metrics, IReadOnlyList<bool>? Mask)? _state;

    /// <summary>Applies an adaptive state (no-op when unchanged).</summary>
    internal void ApplyState(RibbonGroupState state, bool simplified, RibbonMetrics metrics, IReadOnlyList<bool>? mask = null)
    {
        if (_state is { } current && current.State == state && current.Simplified == simplified && ReferenceEquals(current.Metrics, metrics) && MaskEquals(current.Mask, mask))
        {
            return;
        }

        _state = (state, simplified, metrics, mask);
        _metrics = metrics;
        State = state;
        IsSimplified = simplified;
        _itemsPanel.Metrics = metrics;
        _itemsPanel.IsSimplified = simplified;
        _itemsPanel.ItemsLayout = ItemsLayout;
        _itemsPanel.RowCount = RowCount;
        _itemsPanel.InLineMask = mask;
        var itemState = state == RibbonGroupState.Collapsed ? RibbonGroupState.Large : state;
        if (!IsPopupOpen)
        {
            ApplyItemLayouts(itemState, simplified, metrics);
        }

        ApplyCollapsedButtonLayout();
        ApplyPresentation();
        _itemsPanel.InvalidateMeasure();
        InvalidateMeasure();
    }

    private static bool MaskEquals(IReadOnlyList<bool>? a, IReadOnlyList<bool>? b)
        => ReferenceEquals(a, b) || (a is not null && b is not null && a.SequenceEqual(b));

    private void ApplyItemLayouts(RibbonGroupState state, bool simplified, RibbonMetrics metrics)
    {
        foreach (var item in Items.OfType<IRibbonItem>())
        {
            var size = simplified ? RibbonItemSize.Small : item.EffectiveSizeDefinition.GetSize(state);
            if (simplified && item is RibbonItemsContainer)
            {
                size = RibbonItemSize.Small;
            }

            item.ApplyLayout(new RibbonItemLayout(size, metrics, simplified, null, state));
        }
    }

    private void ApplyPresentation()
    {
        var collapsed = State == RibbonGroupState.Collapsed && !IsSimplified;
        if (_itemsPresenter is not null)
        {
            _itemsPresenter.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
            _itemsPresenter.Margin = IsSimplified ? new Thickness(2, 0, 2, 0) : new Thickness(4, 2, 4, 0);
        }

        if (_collapsedButton is not null)
        {
            _collapsedButton.Visibility = collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_captionRow is not null)
        {
            _captionRow.Visibility = IsSimplified || collapsed || !ShowsCaption ? Visibility.Collapsed : Visibility.Visible;
            _captionRow.Height = _metrics.GroupCaptionHeight;
        }

        UpdateSlideOutButton();

        VisualStateManager.GoToState(this, IsSimplified ? "Simplified" : collapsed ? "Collapsed" : "Expanded", false);
    }

    /// <summary>Measures the group width in a state (used by the adaptive layout).</summary>
    internal double MeasureWidth(RibbonGroupState state, RibbonMetrics metrics, double height)
    {
        ApplyState(state, false, metrics);
        Measure(new Size(double.PositiveInfinity, height));
        return DesiredSize.Width;
    }

    /// <summary>Measures the width of each item in the simplified line.</summary>
    internal double[] MeasureSimplifiedItemWidths(RibbonMetrics metrics)
    {
        ApplyState(RibbonGroupState.Large, true, metrics);
        var widths = new double[_itemsPanel.Children.Count];
        for (var i = 0; i < widths.Length; i++)
        {
            var child = _itemsPanel.Children[i];
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            widths[i] = child.DesiredSize.Width + _itemsPanel.SimplifiedSpacing;
        }

        return widths;
    }

    /// <summary>Items that are currently not shown in the simplified line (moved to the overflow menu).</summary>
    public IEnumerable<UIElement> GetOverflowItems()
    {
        for (var i = 0; i < _itemsPanel.Children.Count; i++)
        {
            var child = _itemsPanel.Children[i];
            if (child.Visibility == Visibility.Visible && (!_itemsPanel.IsInLine(i) || (child is IRibbonItem { SimplifiedVisibility: RibbonSimplifiedVisibility.Overflow })))
            {
                yield return child;
            }
        }
    }

    private void OnCollapsedClick(object sender, RoutedEventArgs e)
    {
        // Defer so the click that opens the popup is not treated as a light-dismiss click.
        if (DispatcherQueue is { } queue)
        {
            queue.TryEnqueue(OpenPopup);
        }
        else
        {
            OpenPopup();
        }
    }

    /// <summary>Opens the popup of a collapsed group.</summary>
    public void OpenPopup()
    {
        if (XamlRoot is null)
        {
            return;
        }

        _popupPresenter ??= new Border();
        if (_popup is null)
        {
            var header = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = _metrics.CaptionFontSize,
                Margin = new Thickness(0, 0, 0, 2),
            };
            header.SetValue(TextBlock.TextProperty, Header);
            RibbonTheme.SetThemeBrush(header, TextBlock.ForegroundProperty, "RibbonSecondaryForegroundBrush");
            _popupHeader = header;

            var launcher = new Button { Style = _launcher?.Style, Visibility = _launcher?.Visibility ?? Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Right };
            launcher.Click += OnLauncherClick;
            _popupLauncher = launcher;
            var caption = new Grid { Height = _metrics.GroupCaptionHeight, Children = { header, launcher } };
            var content = new StackPanel { Padding = new Thickness(4, 3, 4, 0) };
            content.Children.Add(_popupPresenter);
            _popupSlideOutHost = new Border { Margin = new Thickness(0, 2, 0, 0) };
            content.Children.Add(_popupSlideOutHost);
            content.Children.Add(caption);
            var chrome = new Border { Child = content, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6) };
            RibbonTheme.SetThemeBrush(chrome, Border.BackgroundProperty, "RibbonCommandBarBackgroundBrush");
            RibbonTheme.SetThemeBrush(chrome, Border.BorderBrushProperty, "RibbonPopupBorderBrush");
            chrome.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Escape && !e.Handled)
                {
                    ClosePopup();
                    _collapsedButton?.Focus(FocusState.Keyboard);
                    e.Handled = true;
                }
            };

            _popup = new Popup { Child = chrome, IsLightDismissEnabled = true };
            RibbonItemHelper.SetOwner(chrome, Ribbon);
            _popup.Closed += (_, _) => RestoreFromPopup();
        }

        if (_itemsPresenter is not null && ReferenceEquals(_itemsPresenter.Child, _itemsPanel))
        {
            _itemsPresenter.Child = null;
        }

        ApplyItemLayouts(RibbonGroupState.Large, false, _metrics);
        _itemsPanel.IsSimplified = false;
        _popupPresenter.Child = _itemsPanel;
        MoveSlideOutIntoPopup();
        _popup.XamlRoot = XamlRoot;
        if (_popup.Child is FrameworkElement popupChild)
        {
            popupChild.RequestedTheme = ActualTheme;
        }

        if (_popupHeader is not null)
        {
            _popupHeader.Text = Header ?? string.Empty;
        }

        if (_popupLauncher is not null)
        {
            _popupLauncher.Visibility = _launcher?.Visibility ?? Visibility.Collapsed;
            if (_launcher is not null)
            {
                AutomationProperties.SetName(_popupLauncher, AutomationProperties.GetName(_launcher));
                ToolTipService.SetToolTip(_popupLauncher, CreateLauncherToolTip());
            }
        }

        var anchor = (FrameworkElement?)_collapsedButton ?? this;
        RibbonPopupPlacement.PlaceBelow(_popup, anchor);
        Ribbon?.AttachPopupKeyboard(_popup.Child);
        _popup.IsOpen = true;
    }

    /// <summary>Closes the popup of a collapsed group.</summary>
    public void ClosePopup()
    {
        if (_popup is { IsOpen: true })
        {
            _popup.IsOpen = false;
        }
    }

    private void RestoreFromPopup()
    {
        MoveSlideOutOutOfPopup();
        if (_popupPresenter is not null && ReferenceEquals(_popupPresenter.Child, _itemsPanel))
        {
            _popupPresenter.Child = null;
        }

        if (_itemsPresenter is not null)
        {
            _itemsPresenter.Child = _itemsPanel;
        }

        var state = _state;
        _state = null;
        if (state is { } s)
        {
            ApplyState(s.State, s.Simplified, s.Metrics, s.Mask);
        }
    }

    /// <summary>Items reachable by KeyTips in the current presentation.</summary>
    internal IEnumerable<FrameworkElement> GetKeyTipTargets()
    {
        if (State == RibbonGroupState.Collapsed && !IsSimplified && !IsPopupOpen)
        {
            if (_collapsedButton is not null)
            {
                yield return _collapsedButton;
            }

            yield break;
        }

        for (var i = 0; i < _itemsPanel.Children.Count; i++)
        {
            var child = _itemsPanel.Children[i];
            if (child.Visibility != Visibility.Visible || (IsSimplified && !_itemsPanel.IsInLine(i)))
            {
                continue;
            }

            foreach (var target in Flatten(child))
            {
                yield return target;
            }
        }

        if (IsPopupOpen)
        {
            foreach (var target in SlideOutItems.Where(i => i.Visibility == Visibility.Visible).SelectMany(Flatten))
            {
                yield return target;
            }
        }
        else if (!IsSimplified && _slideOutButton is { Visibility: Visibility.Visible })
        {
            yield return _slideOutButton;
        }

        if (IsPopupOpen && _popupLauncher is { Visibility: Visibility.Visible })
        {
            yield return _popupLauncher;
        }
        else if (!IsSimplified && _launcher is { Visibility: Visibility.Visible })
        {
            yield return _launcher;
        }
    }

    /// <summary>The launcher shown in the popup of a collapsed group.</summary>
    internal Button? PopupLauncher => _popupLauncher;

    internal static IEnumerable<FrameworkElement> Flatten(UIElement element)
    {
        if (element is RibbonItemsContainer container)
        {
            foreach (var child in container.Items)
            {
                if (child.Visibility == Visibility.Visible)
                {
                    foreach (var nested in Flatten(child))
                    {
                        yield return nested;
                    }
                }
            }
        }
        else if (element is FrameworkElement fe and (IRibbonItem or Control) && element is not RibbonSeparator and not RibbonLabel)
        {
            yield return fe;
        }
    }

    /// <summary>All items including nested ones.</summary>
    public IEnumerable<FrameworkElement> GetAllItems() => Items.Concat(SlideOutItems).SelectMany(Flatten);

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGroupAutomationPeer(this);
}

/// <summary>Automation peer of <see cref="RibbonGroup"/>.</summary>
public sealed partial class RibbonGroupAutomationPeer(RibbonGroup owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(RibbonGroup);

    /// <inheritdoc />
    protected override string GetNameCore() => ((RibbonGroup)Owner).Header ?? base.GetNameCore();
}
