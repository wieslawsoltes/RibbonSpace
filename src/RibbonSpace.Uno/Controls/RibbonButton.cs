using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace RibbonSpace.Controls;

/// <summary>Ribbon push button (large, medium, small and simplified presentations).</summary>
public partial class RibbonButton : Button, IRibbonCommandSource
{
    /// <summary>Identifies <see cref="ShowChevron"/>.</summary>
    public static readonly DependencyProperty ShowChevronProperty = DependencyProperty.Register(nameof(ShowChevron), typeof(bool), typeof(RibbonButton), new PropertyMetadata(false));

    /// <summary>Creates a button.</summary>
    public RibbonButton()
    {
        DefaultStyleKey = typeof(RibbonButton);
        InitializeRibbonItem();
        Click += (_, _) => OnRibbonClick();
        RegisterPropertyChangedCallback(FlyoutProperty, (_, _) =>
        {
            RibbonItemHelper.TrackFlyoutOwner(_trackedFlyout, Flyout);
            _trackedFlyout = Flyout;
        });
    }

    private FlyoutBase? _trackedFlyout;

    /// <summary>Shows a drop-down chevron.</summary>
    public bool ShowChevron { get => (bool)GetValue(ShowChevronProperty); set => SetValue(ShowChevronProperty, value); }

    /// <summary>Internal chrome button (collapsed group, overflow): clicks are not reported as commands.</summary>
    public bool IsChromeButton { get; set; }

    /// <summary>Called when clicked.</summary>
    protected virtual void OnRibbonClick()
    {
        if (Flyout is not null || IsChromeButton)
        {
            return;
        }

        if (RibbonItemHelper.GetSourceItem(this) is RibbonButton source)
        {
            // Linked copy (QAT, custom group): run the original so its Click handlers and command execute once.
            source.PerformClick();
            return;
        }

        RibbonItemHelper.NotifyInvoked(this);
    }

    /// <summary>Creates a linked copy (QAT / custom groups).</summary>
    protected virtual FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = CreateCopyInstance();
        RibbonItemHelper.LinkCommon(this, copy);
        copy.Flyout = Flyout;
        copy.ShowChevron = ShowChevron;
        return copy;
    }

    /// <summary>
    /// Called when this linked copy (QAT, overflow menu, custom group) is discarded: releases state shared with the
    /// source item.
    /// </summary>
    protected virtual void OnUnlinkedCore() => Flyout = null;

    /// <summary>Creates the instance used for linked copies (override in derived buttons).</summary>
    protected virtual RibbonButton CreateCopyInstance() => new();

    /// <summary>Creates overflow menu entries.</summary>
    protected virtual IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
    {
        if (Flyout is MenuFlyout menu)
        {
            var sub = new MenuFlyoutSubItem { Text = Label ?? string.Empty, Icon = RibbonItemHelper.CreateMenuIcon(Icon) };
            foreach (var entry in RibbonMenuCloner.Clone(menu.Items))
            {
                sub.Items.Add(entry);
            }

            return [sub];
        }

        return [RibbonItemHelper.CreateMenuItem(this, Label, Icon, () => InvokeCore())];
    }

    /// <summary>KeyTip action.</summary>
    protected virtual RibbonKeyTipResult OnKeyTipCore()
    {
        if (Flyout is not null)
        {
            Flyout.ShowAt(this);
            return RibbonKeyTipResult.Close;
        }

        InvokeCore();
        return RibbonKeyTipResult.Close;
    }

    /// <summary>Clicks the button programmatically.</summary>
    protected virtual bool InvokeCore()
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (Flyout is not null)
        {
            Flyout.ShowAt(this);
            return true;
        }

        PerformClick();
        return true;
    }

    /// <summary>Raises Click and executes the command.</summary>
    public void PerformClick()
    {
        if (IsEnabled)
        {
            new ButtonAutomationPeer(this).Invoke();
        }
    }
}

/// <summary>Button that opens a drop-down (menu or any flyout content).</summary>
public partial class RibbonDropDownButton : RibbonButton
{
    /// <summary>Creates a drop-down button.</summary>
    public RibbonDropDownButton()
    {
        DefaultStyleKey = typeof(RibbonButton);
        ShowChevron = true;
    }

    /// <inheritdoc />
    protected override RibbonButton CreateCopyInstance() => new RibbonDropDownButton();
}

/// <summary>Toggle button; toggles sharing a <see cref="GroupName"/> behave like radio buttons.</summary>
public partial class RibbonToggleButton : ToggleButton, IRibbonCommandSource, IRibbonCheckable
{
    /// <summary>Identifies <see cref="GroupName"/>.</summary>
    public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(RibbonToggleButton), new PropertyMetadata(null));

    /// <summary>Identifies <see cref="ShowChevron"/>.</summary>
    public static readonly DependencyProperty ShowChevronProperty = DependencyProperty.Register(nameof(ShowChevron), typeof(bool), typeof(RibbonToggleButton), new PropertyMetadata(false));

    /// <summary>Creates a toggle button.</summary>
    public RibbonToggleButton()
    {
        DefaultStyleKey = typeof(RibbonToggleButton);
        InitializeRibbonItem();
        Checked += (_, _) => RibbonToggleGroups.OnChecked(this);
        Click += (_, _) =>
        {
            if (RibbonItemHelper.GetSourceItem(this) is null)
            {
                RibbonItemHelper.NotifyInvoked(this, IsChecked);
            }
        };
    }

    /// <summary>Radio group name (scope: the owning group, toolbar or ribbon).</summary>
    public string? GroupName { get => (string?)GetValue(GroupNameProperty); set => SetValue(GroupNameProperty, value); }

    /// <summary>Shows a drop-down chevron.</summary>
    public bool ShowChevron { get => (bool)GetValue(ShowChevronProperty); set => SetValue(ShowChevronProperty, value); }

    /// <inheritdoc />
    protected override void OnToggle()
    {
        // Radio semantics: a checked member of a group stays checked when clicked again.
        if (!string.IsNullOrEmpty(GroupName) && IsChecked == true)
        {
            return;
        }

        base.OnToggle();
    }

    /// <summary>Creates a linked copy.</summary>
    protected virtual FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonToggleButton();
        RibbonItemHelper.LinkCommon(this, copy);

        // The copy keeps the radio semantics of its source (a checked member stays checked when clicked again).
        RibbonItemHelper.Link(this, copy, GroupNameProperty);
        RibbonItemHelper.Link(this, copy, ShowChevronProperty);
        RibbonItemHelper.Link(this, copy, IsCheckedProperty, twoWay: true);
        copy.Click += (_, _) =>
        {
            RibbonItemHelper.Execute(this, CommandParameter);
            RibbonItemHelper.NotifyInvoked(this, IsChecked);
        };
        return copy;
    }

    /// <summary>Overflow entries.</summary>
    protected virtual IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
        => [RibbonItemHelper.CreateMenuItem(this, Label, Icon, () => InvokeCore(), IsChecked == true)];

    /// <summary>Called when this linked copy is discarded: releases state shared with the source item.</summary>
    protected virtual void OnUnlinkedCore()
    {
    }

    /// <summary>KeyTip action.</summary>
    protected virtual RibbonKeyTipResult OnKeyTipCore()
    {
        InvokeCore();
        return RibbonKeyTipResult.Close;
    }

    /// <summary>Toggles programmatically.</summary>
    protected virtual bool InvokeCore()
    {
        if (!IsEnabled)
        {
            return false;
        }

        new ToggleButtonAutomationPeer(this).Toggle();
        RibbonItemHelper.Execute(this, CommandParameter);
        RibbonItemHelper.NotifyInvoked(this, IsChecked);
        return true;
    }
}

/// <summary>Compact ribbon check box.</summary>
public partial class RibbonCheckBox : CheckBox, IRibbonCommandSource, IRibbonCheckable
{
    /// <summary>Creates a check box.</summary>
    public RibbonCheckBox()
    {
        DefaultStyleKey = typeof(RibbonCheckBox);
        InitializeRibbonItem();
        RegisterPropertyChangedCallback(IsCheckedProperty, (_, _) => UpdateCheckVisual());
        IsEnabledChanged += (_, _) => UpdateCheckVisual();
        ActualThemeChanged += (_, _) => UpdateCheckVisual();
        Click += (_, _) =>
        {
            if (RibbonItemHelper.GetSourceItem(this) is null)
            {
                RibbonItemHelper.NotifyInvoked(this, IsChecked);
            }
        };
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateCheckVisual();
    }

    /// <inheritdoc />
    protected override void OnToggle()
    {
        base.OnToggle();
        UpdateCheckVisual();
    }

    private void UpdateCheckVisual()
    {
        var box = GetTemplateChild("PART_Box") as Microsoft.UI.Xaml.Controls.Border;
        var check = GetTemplateChild("PART_Check") as UIElement;
        var indeterminate = GetTemplateChild("PART_Indeterminate") as UIElement;
        if (box is null)
        {
            return;
        }

        var on = IsChecked != false;
        box.Background = RibbonTheme.GetBrush(this, on ? "RibbonAccentBrush" : "RibbonInputBackgroundBrush");
        box.BorderBrush = RibbonTheme.GetBrush(this, on ? "RibbonAccentBrush" : "RibbonInputHoverBorderBrush");
        if (check is not null)
        {
            check.Visibility = IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        if (indeterminate is not null)
        {
            indeterminate.Visibility = IsChecked is null ? Visibility.Visible : Visibility.Collapsed;
        }

        box.Opacity = IsEnabled ? 1 : 0.5;
        if (!IsEnabled)
        {
            _isPointerOver = false;
        }

        UpdateRibbonStates();
    }

    private bool _isPointerOver;

    // Hover and enabled visuals live in their own state groups (the template's), so the Background stays a style /
    // binding value and disabled items never highlight.
    private void UpdateRibbonStates()
    {
        VisualStateManager.GoToState(this, IsEnabled ? "ItemEnabled" : "ItemDisabled", false);
        VisualStateManager.GoToState(this, _isPointerOver && IsEnabled ? "HoverPointerOver" : "HoverNormal", false);
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateRibbonStates();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        UpdateRibbonStates();
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        UpdateRibbonStates();
    }

    /// <summary>Creates a linked copy.</summary>
    protected virtual FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonCheckBox();
        RibbonItemHelper.LinkCommon(this, copy);
        RibbonItemHelper.Link(this, copy, IsCheckedProperty, twoWay: true);
        copy.Click += (_, _) =>
        {
            RibbonItemHelper.Execute(this, CommandParameter);
            RibbonItemHelper.NotifyInvoked(this, IsChecked);
        };
        return copy;
    }

    /// <summary>Overflow entries.</summary>
    protected virtual IEnumerable<MenuFlyoutItemBase> CreateOverflowMenuItemsCore()
        => [RibbonItemHelper.CreateMenuItem(this, Label, Icon, () => InvokeCore(), IsChecked == true)];

    /// <summary>Called when this linked copy is discarded: releases state shared with the source item.</summary>
    protected virtual void OnUnlinkedCore()
    {
    }

    /// <summary>KeyTip action.</summary>
    protected virtual RibbonKeyTipResult OnKeyTipCore()
    {
        InvokeCore();
        return RibbonKeyTipResult.Close;
    }

    /// <summary>Toggles programmatically.</summary>
    protected virtual bool InvokeCore()
    {
        if (!IsEnabled)
        {
            return false;
        }

        new ToggleButtonAutomationPeer(this).Toggle();
        RibbonItemHelper.Execute(this, CommandParameter);
        RibbonItemHelper.NotifyInvoked(this, IsChecked);
        return true;
    }
}

/// <summary>Mutual exclusion of <see cref="RibbonToggleButton.GroupName"/>.</summary>
internal static class RibbonToggleGroups
{
    public static void OnChecked(RibbonToggleButton button)
    {
        // Linked copies (QAT, custom groups) mirror IsChecked to their source, which enforces the exclusion in its own scope.
        if (string.IsNullOrEmpty(button.GroupName) || RibbonItemHelper.GetSourceItem(button) is not null)
        {
            return;
        }

        // Scope: the nearest owning group, then toolbar, then ribbon.
        var scope = (DependencyObject?)RibbonItemHelper.FindAncestor<RibbonGroup>(button)
            ?? (DependencyObject?)RibbonItemHelper.FindAncestor<RibbonToolBar>(button)
            ?? RibbonItemHelper.FindAncestor<Ribbon>(button)
            ?? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(button);
        if (scope is null)
        {
            return;
        }

        foreach (var other in Descendants(scope).OfType<RibbonToggleButton>())
        {
            if (!ReferenceEquals(other, button) && other.GroupName == button.GroupName && other.IsChecked == true)
            {
                other.IsChecked = false;
            }
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < count; i++)
            {
                stack.Push(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, i));
            }
        }
    }
}

/// <summary>Clones menu entries (for overflow menus and linked copies).</summary>
public static class RibbonMenuCloner
{
    /// <summary>Clones <see cref="MenuFlyoutItemBase"/> entries (text, icon, command, checked state, sub menus).</summary>
    public static IEnumerable<MenuFlyoutItemBase> Clone(IEnumerable<MenuFlyoutItemBase> items)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case MenuFlyoutSeparator:
                    yield return new MenuFlyoutSeparator();
                    break;
                case MenuFlyoutSubItem sub:
                    var copy = new MenuFlyoutSubItem { Text = sub.Text, Icon = RibbonItemHelper.CreateMenuIcon(sub.Icon) };
                    foreach (var child in Clone(sub.Items))
                    {
                        copy.Items.Add(child);
                    }

                    yield return copy;
                    break;
                case RadioMenuFlyoutItem radio:
                    var r = new RadioMenuFlyoutItem { Text = radio.Text, GroupName = radio.GroupName, IsChecked = radio.IsChecked, Command = radio.Command, CommandParameter = radio.CommandParameter, Icon = RibbonItemHelper.CreateMenuIcon(radio.Icon) };
                    r.Click += (_, _) => radio.IsChecked = true;
                    yield return r;
                    break;
                case ToggleMenuFlyoutItem toggle:
                    var t = new ToggleMenuFlyoutItem { Text = toggle.Text, IsChecked = toggle.IsChecked, Command = toggle.Command, CommandParameter = toggle.CommandParameter, Icon = RibbonItemHelper.CreateMenuIcon(toggle.Icon), KeyboardAcceleratorTextOverride = toggle.KeyboardAcceleratorTextOverride };
                    t.Click += (_, _) => toggle.IsChecked = t.IsChecked;
                    yield return t;
                    break;
                case MenuFlyoutItem menuItem:
                    var m = new MenuFlyoutItem { Text = menuItem.Text, Command = menuItem.Command, CommandParameter = menuItem.CommandParameter, Icon = RibbonItemHelper.CreateMenuIcon(menuItem.Icon), KeyboardAcceleratorTextOverride = menuItem.KeyboardAcceleratorTextOverride, IsEnabled = menuItem.IsEnabled, Tag = menuItem.Tag };
                    if (RibbonMenu.GetInvoke(menuItem) is { } invoke)
                    {
                        m.Click += (_, _) => invoke();
                        RibbonMenu.SetInvoke(m, invoke);
                    }

                    yield return m;
                    break;
            }
        }
    }
}

/// <summary>Menu helpers.</summary>
public static class RibbonMenu
{
    /// <summary>
    /// Identifies the <c>RibbonMenu.Icon</c> attached property: any RibbonSpace icon value (glyph, path, layered path,
    /// image, RibbonIcon) for a <see cref="MenuFlyoutItem"/> / <see cref="MenuFlyoutSubItem"/> declared in XAML,
    /// converted with <see cref="RibbonItemHelper.CreateMenuIcon"/> (and <see cref="RibbonItemHelper.MenuIconConverter"/>).
    /// </summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached("Icon", typeof(object), typeof(RibbonMenu), new PropertyMetadata(null, OnIconChanged));

    /// <summary>Gets the attached menu icon.</summary>
    public static object? GetIcon(DependencyObject element) => element.GetValue(IconProperty);

    /// <summary>Sets the attached menu icon.</summary>
    public static void SetIcon(DependencyObject element, object? value) => element.SetValue(IconProperty, value);

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var icon = RibbonItemHelper.CreateMenuIcon(e.NewValue);
        switch (d)
        {
            case MenuFlyoutItem item:
                item.Icon = icon;
                break;
            case MenuFlyoutSubItem sub:
                sub.Icon = icon;
                break;
        }
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MenuFlyoutItem, Action> Invokers = new();

    /// <summary>Registers the click action of a menu item so clones (overflow menus) can invoke it.</summary>
    public static void SetInvoke(MenuFlyoutItem item, Action action)
    {
        Invokers.AddOrUpdate(item, action);
    }

    /// <summary>Gets the registered click action.</summary>
    public static Action? GetInvoke(MenuFlyoutItem item) => Invokers.TryGetValue(item, out var action) ? action : null;

    /// <summary>
    /// Gives a menu or flyout the ribbon popup look (theme colours, popup corner radius) unless the application set its
    /// own presenter style. Applied automatically to the flyouts of ribbon items and to the ribbon's own menus.
    /// </summary>
    public static void ApplyTheme(FlyoutBase? flyout)
    {
        switch (flyout)
        {
            case MenuFlyout menu when menu.MenuFlyoutPresenterStyle is null && TryGetStyle("RibbonMenuFlyoutPresenterStyle", out var menuStyle):
                menu.MenuFlyoutPresenterStyle = menuStyle;
                break;
            case Flyout content when content.FlyoutPresenterStyle is null && TryGetStyle("RibbonFlyoutPresenterStyle", out var flyoutStyle):
                content.FlyoutPresenterStyle = flyoutStyle;
                break;
        }
    }

    private static bool TryGetStyle(string key, out Style style)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Style found)
        {
            style = found;
            return true;
        }

        style = null!;
        return false;
    }

    /// <summary>Creates a menu item with a click action that survives cloning.</summary>
    public static MenuFlyoutItem Item(string text, Action action, object? icon = null, string? shortcut = null)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = RibbonItemHelper.CreateMenuIcon(icon) };
        if (shortcut is not null)
        {
            item.KeyboardAcceleratorTextOverride = shortcut;
        }

        item.Click += (_, _) => action();
        SetInvoke(item, action);
        return item;
    }
}
