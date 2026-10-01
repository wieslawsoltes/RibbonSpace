using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using RibbonSpace.Localization;
using Windows.System;

namespace RibbonSpace.Controls;

/// <summary>
/// Split button: a primary action plus a drop-down part (Office Paste, Bullets, Shapes, Font Color). Large items
/// stack the parts vertically; medium / small items place the arrow to the right.
/// </summary>
[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Flyout))]
public partial class RibbonSplitButton : RibbonControlBase, IRibbonCheckable
{
    /// <summary>Identifies <see cref="Flyout"/>.</summary>
    public static readonly DependencyProperty FlyoutProperty = DependencyProperty.Register(nameof(Flyout), typeof(FlyoutBase), typeof(RibbonSplitButton), new PropertyMetadata(null, (d, e) => ((RibbonSplitButton)d).OnFlyoutChanged((FlyoutBase?)e.OldValue, (FlyoutBase?)e.NewValue)));

    /// <summary>Identifies <see cref="IsCheckable"/>.</summary>
    public static readonly DependencyProperty IsCheckableProperty = DependencyProperty.Register(nameof(IsCheckable), typeof(bool), typeof(RibbonSplitButton), new PropertyMetadata(false, (d, _) => ((RibbonSplitButton)d).UpdateStates()));

    /// <summary>Identifies <see cref="IsChecked"/>.</summary>
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(RibbonSplitButton), new PropertyMetadata((bool?)false, (d, _) => ((RibbonSplitButton)d).UpdateStates()));

    /// <summary>Identifies <see cref="FollowLastChoice"/>.</summary>
    public static readonly DependencyProperty FollowLastChoiceProperty = DependencyProperty.Register(nameof(FollowLastChoice), typeof(bool), typeof(RibbonSplitButton), new PropertyMetadata(false, (d, _) => ((RibbonSplitButton)d).UpdateContents()));

    /// <summary>Identifies <see cref="LastChoice"/>.</summary>
    public static readonly DependencyProperty LastChoiceProperty = DependencyProperty.Register(nameof(LastChoice), typeof(MenuFlyoutItem), typeof(RibbonSplitButton), new PropertyMetadata(null, (d, _) => ((RibbonSplitButton)d).OnLastChoiceChanged()));

    /// <summary>Identifies <see cref="ColorBar"/>.</summary>
    public static readonly DependencyProperty ColorBarProperty = DependencyProperty.Register(nameof(ColorBar), typeof(Microsoft.UI.Xaml.Media.Brush), typeof(RibbonSplitButton), new PropertyMetadata(null, (d, _) => ((RibbonSplitButton)d).UpdateContents()));

    private readonly List<MenuFlyoutItem> _hookedChoices = [];
    private Button? _primary;
    private Button? _secondary;
    private Grid? _root;

    /// <summary>Creates a split button.</summary>
    public RibbonSplitButton()
    {
        DefaultStyleKey = typeof(RibbonSplitButton);
        Size = RibbonItemSize.Large;
        IsEnabledChanged += (_, _) => UpdateStates();
    }

    /// <summary>Raised when the primary part is clicked.</summary>
    public event RoutedEventHandler? Click;

    /// <summary>Drop-down flyout (MenuFlyout or Flyout with any content).</summary>
    public FlyoutBase? Flyout { get => (FlyoutBase?)GetValue(FlyoutProperty); set => SetValue(FlyoutProperty, value); }

    /// <summary>The primary part toggles (Bullets, Numbering).</summary>
    public bool IsCheckable { get => (bool)GetValue(IsCheckableProperty); set => SetValue(IsCheckableProperty, value); }

    /// <summary>Checked state.</summary>
    public bool? IsChecked { get => (bool?)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }

    /// <summary>
    /// When a menu item is chosen, the primary part adopts its icon, label and action (tool split buttons). The chosen
    /// item is exposed as <see cref="LastChoice"/>; <c>Label</c> and <c>Icon</c> are not modified.
    /// </summary>
    public bool FollowLastChoice { get => (bool)GetValue(FollowLastChoiceProperty); set => SetValue(FollowLastChoiceProperty, value); }

    /// <summary>Menu item chosen last (<see cref="FollowLastChoice"/>); the primary part shows and invokes it.</summary>
    public MenuFlyoutItem? LastChoice { get => (MenuFlyoutItem?)GetValue(LastChoiceProperty); set => SetValue(LastChoiceProperty, value); }

    /// <summary>Color bar under the icon (Font Color, Highlight).</summary>
    public Microsoft.UI.Xaml.Media.Brush? ColorBar { get => (Microsoft.UI.Xaml.Media.Brush?)GetValue(ColorBarProperty); set => SetValue(ColorBarProperty, value); }

    /// <summary>True while the drop-down is open.</summary>
    public bool IsDropDownOpen => Flyout?.IsOpen == true;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_primary is not null)
        {
            _primary.Click -= OnPrimaryClick;
        }

        if (_secondary is not null)
        {
            _secondary.Click -= OnSecondaryClick;
        }

        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _primary = GetTemplateChild("PART_PrimaryButton") as Button;
        _secondary = GetTemplateChild("PART_SecondaryButton") as Button;
        if (_primary is not null)
        {
            _primary.Click += OnPrimaryClick;
        }

        if (_secondary is not null)
        {
            _secondary.Click += OnSecondaryClick;
        }

        ApplyPartLayout();
        UpdateStates();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSplitButtonAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        // Alt+Down / F4 open the drop-down from either part (like a combo box).
        var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (e.Key == VirtualKey.F4 || (alt && e.Key == VirtualKey.Down))
        {
            OpenDropDown();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnLayoutAppliedCore(RibbonItemLayout layout) => ApplyPartLayout();

    private void ApplyPartLayout()
    {
        if (_root is null || _primary is null || _secondary is null)
        {
            return;
        }

        var vertical = CurrentSize == RibbonItemSize.Large && !IsSimplified;
        _root.RowDefinitions.Clear();
        _root.ColumnDefinitions.Clear();
        if (vertical)
        {
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_primary, 0);
            Grid.SetColumn(_primary, 0);
            Grid.SetRow(_secondary, 1);
            Grid.SetColumn(_secondary, 0);
        }
        else
        {
            _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(_primary, 0);
            Grid.SetColumn(_primary, 0);
            Grid.SetRow(_secondary, 0);
            Grid.SetColumn(_secondary, 1);
        }

        VisualStateManager.GoToState(this, vertical ? "Vertical" : "Horizontal", false);
        UpdateContents();
        InvalidateMeasure();
    }

    private void UpdateContents()
    {
        if (GetTemplateChild("PART_PrimaryContent") is not Primitives.RibbonItemContent primary || GetTemplateChild("PART_SecondaryContent") is not Primitives.RibbonItemContent secondary)
        {
            return;
        }

        var vertical = CurrentSize == RibbonItemSize.Large && !IsSimplified;
        foreach (var content in new[] { primary, secondary })
        {
            content.Metrics = Metrics;
            content.IsSimplified = IsSimplified;
            content.ItemSize = CurrentSize;
        }

        var (label, icon, largeIcon) = GetPrimaryPresentation();
        primary.Icon = icon;
        primary.LargeIcon = largeIcon;
        primary.ColorBar = ColorBar;
        if (vertical)
        {
            primary.Part = Primitives.RibbonItemContentPart.IconOnly;
            primary.Label = label;
            primary.ShowChevron = false;
            secondary.Part = Primitives.RibbonItemContentPart.LabelAndChevron;
            secondary.Icon = null;
            secondary.Label = Label;
            secondary.ShowChevron = true;
        }
        else
        {
            primary.Part = Primitives.RibbonItemContentPart.All;
            primary.Label = label;
            primary.ShowLabel = ActualShowLabel;
            primary.ShowChevron = false;
            secondary.Part = Primitives.RibbonItemContentPart.All;
            secondary.Icon = null;
            secondary.Label = null;
            secondary.ShowChevron = true;
        }

        UpdatePartNames(label);
    }

    private void OnLastChoiceChanged()
    {
        UpdateContents();
        RibbonItemHelper.InvalidateHostLayout(this);
    }

    /// <summary>Label and icons shown by the primary part (the last choice of tool split buttons, else the item's own).</summary>
    private (string? Label, object? Icon, object? LargeIcon) GetPrimaryPresentation()
    {
        if (!FollowLastChoice || LastChoice is not { } choice)
        {
            return (Label, Icon, LargeIcon);
        }

        var icon = choice.Icon is FontIcon fontIcon ? fontIcon.Glyph : choice.Tag ?? Icon;
        return (string.IsNullOrEmpty(choice.Text) ? Label : choice.Text, icon, choice.Icon is FontIcon || choice.Tag is not null ? icon : LargeIcon);
    }

    private void UpdatePartNames(string? label)
    {
        var name = (label ?? Label)?.Replace('\n', ' ') ?? string.Empty;
        if (_primary is not null)
        {
            AutomationProperties.SetName(_primary, name);
        }

        if (_secondary is not null)
        {
            AutomationProperties.SetName(_secondary, FormatOptionsName(name));
        }
    }

    private static string FormatOptionsName(string label)
    {
        // Overridable localized format ("{0} options"); fall back when an override drops the placeholder.
        var format = RibbonStrings.Current.SplitButtonOptions;
        if (!format.Contains("{0}", StringComparison.Ordinal))
        {
            format = "{0} options";
        }

        return string.Format(RibbonStrings.Current.Culture, format, label).Trim();
    }

    /// <inheritdoc />
    protected override void OnRibbonPropertyChanged(DependencyPropertyChangedEventArgs e) => UpdateContents();

    private void OnFlyoutChanged(FlyoutBase? oldValue, FlyoutBase? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.Opening -= OnFlyoutOpening;
            oldValue.Opened -= OnFlyoutOpenedOrClosed;
            oldValue.Closed -= OnFlyoutOpenedOrClosed;
        }

        UnhookChoices();
        RibbonItemHelper.TrackFlyoutOwner(oldValue, newValue);
        if (newValue is not null)
        {
            // Keep a placement chosen by the application.
            if (newValue.ReadLocalValue(FlyoutBase.PlacementProperty) == DependencyProperty.UnsetValue)
            {
                newValue.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
            }

            newValue.Opening += OnFlyoutOpening;
            newValue.Opened += OnFlyoutOpenedOrClosed;
            newValue.Closed += OnFlyoutOpenedOrClosed;
        }
    }

    private void OnFlyoutOpenedOrClosed(object? sender, object e)
    {
        UpdateStates();
        if (FrameworkElementAutomationPeer.FromElement(this) is RibbonSplitButtonAutomationPeer peer)
        {
            var open = IsDropDownOpen;
            peer.RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                open ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded,
                open ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }

    private void OnFlyoutOpening(object? sender, object e)
    {
        // Linked copies share the source's flyout; the source tracks the choice and the copy mirrors LastChoice.
        if (!FollowLastChoice || Flyout is not MenuFlyout menu || RibbonItemHelper.GetSourceItem(this) is not null)
        {
            return;
        }

        UnhookChoices();
        HookChoices(menu.Items);
    }

    private void HookChoices(IEnumerable<MenuFlyoutItemBase> items)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case MenuFlyoutSubItem sub:
                    HookChoices(sub.Items);
                    break;
                case MenuFlyoutItem menuItem:
                    menuItem.Click += OnMenuChoice;
                    _hookedChoices.Add(menuItem);
                    break;
            }
        }
    }

    private void UnhookChoices()
    {
        foreach (var item in _hookedChoices)
        {
            item.Click -= OnMenuChoice;
        }

        _hookedChoices.Clear();
    }

    private void OnMenuChoice(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            LastChoice = item;
        }
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e) => InvokePrimary();

    /// <summary>
    /// Runs the primary action (toggle, last choice or command, then <see cref="Click"/>). Override to change what the
    /// primary part does, e.g. open the drop-down instead.
    /// </summary>
    protected virtual void InvokePrimary()
    {
        if (IsCheckable)
        {
            IsChecked = IsChecked != true;
        }

        if (FollowLastChoice && LastChoice is { } lastChoice)
        {
            if (RibbonMenu.GetInvoke(lastChoice) is { } invoke)
            {
                invoke();
            }
            else if (lastChoice.Command is { } command && command.CanExecute(lastChoice.CommandParameter))
            {
                command.Execute(lastChoice.CommandParameter);
            }

            RibbonItemHelper.NotifyInvoked(this, lastChoice.CommandParameter);
        }
        else
        {
            ExecuteCommand(IsCheckable ? IsChecked : null);
        }

        Click?.Invoke(this, new RoutedEventArgs());
    }

    private void OnSecondaryClick(object sender, RoutedEventArgs e) => OpenDropDown();

    /// <summary>Opens the drop-down.</summary>
    public void OpenDropDown()
    {
        if (Flyout is not null && IsEnabled)
        {
            Flyout.ShowAt(this, new FlyoutShowOptions { Placement = Flyout.Placement });
        }
    }

    /// <summary>Closes the drop-down.</summary>
    public void CloseDropDown() => Flyout?.Hide();

    private void UpdateStates()
    {
        VisualStateManager.GoToState(this, IsCheckable && IsChecked == true ? "Checked" : "Unchecked", true);
        VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);
        VisualStateManager.GoToState(this, IsDropDownOpen ? "DropDownOpen" : "DropDownClosed", true);
    }

    /// <inheritdoc />
    protected override bool UsesCommandCanExecute => true;

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonSplitButton();
        RibbonItemHelper.LinkCommon(this, copy);
        RibbonItemHelper.Link(this, copy, IsCheckableProperty);
        RibbonItemHelper.Link(this, copy, IsCheckedProperty, twoWay: true);
        RibbonItemHelper.Link(this, copy, ColorBarProperty);
        RibbonItemHelper.Link(this, copy, FollowLastChoiceProperty);
        RibbonItemHelper.Link(this, copy, LastChoiceProperty);
        copy.Flyout = Flyout;
        copy.Click += (_, _) => InvokePrimaryFromCopy();
        copy.Command = null;
        return copy;
    }

    /// <inheritdoc />
    protected override void OnUnlinkedCore()
    {
        base.OnUnlinkedCore();

        // Detaches the copy from the source's flyout (Opening / Opened / Closed handlers).
        Flyout = null;
    }

    private void InvokePrimaryFromCopy()
    {
        // The copy already toggled the shared IsChecked; run the original action without toggling again.
        var checkable = IsCheckable;
        if (checkable)
        {
            ExecuteCommand(IsChecked);
            Click?.Invoke(this, new RoutedEventArgs());
            return;
        }

        InvokePrimary();
    }

    /// <summary>Raises <see cref="Click"/> (for derived split buttons that replace <see cref="InvokePrimary"/>).</summary>
    protected void RaiseClick() => Click?.Invoke(this, new RoutedEventArgs());

    /// <inheritdoc />
    protected override IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
    {
        if (Flyout is MenuFlyout menu)
        {
            var sub = new MenuFlyoutSubItem { Text = Label ?? string.Empty, Icon = RibbonItemHelper.CreateMenuIcon(Icon) };
            sub.Items.Add(RibbonMenu.Item(Label ?? string.Empty, InvokePrimary, Icon));
            sub.Items.Add(new MenuFlyoutSeparator());
            foreach (var entry in RibbonMenuCloner.Clone(menu.Items))
            {
                sub.Items.Add(entry);
            }

            return [sub];
        }

        return [RibbonItemHelper.CreateMenuItem(this, Label, Icon, InvokePrimary, IsCheckable ? IsChecked == true : null)];
    }

    /// <inheritdoc />
    protected override RibbonKeyTipResult OnKeyTipCore()
    {
        OpenDropDown();
        return RibbonKeyTipResult.Close;
    }

    /// <inheritdoc />
    protected override bool InvokeCore()
    {
        if (!IsEnabled)
        {
            return false;
        }

        InvokePrimary();
        return true;
    }
}

/// <summary>Automation peer of <see cref="RibbonSplitButton"/> (SplitButton control type with Invoke and ExpandCollapse patterns).</summary>
public partial class RibbonSplitButtonAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
{
    /// <summary>Creates the peer.</summary>
    public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner)
        : base(owner)
    {
    }

    private RibbonSplitButton Button => (RibbonSplitButton)Owner;

    /// <inheritdoc />
    public ExpandCollapseState ExpandCollapseState => Button.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc />
    public void Invoke()
    {
        if (Button.IsEnabled)
        {
            ((IRibbonItem)Button).Invoke();
        }
    }

    /// <inheritdoc />
    public void Expand() => Button.OpenDropDown();

    /// <inheritdoc />
    public void Collapse() => Button.CloseDropDown();

    /// <inheritdoc />
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface is PatternInterface.Invoke or PatternInterface.ExpandCollapse ? this : base.GetPatternCore(patternInterface);

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(RibbonSplitButton);

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? Button.Label?.Replace('\n', ' ') ?? string.Empty : name;
    }
}
