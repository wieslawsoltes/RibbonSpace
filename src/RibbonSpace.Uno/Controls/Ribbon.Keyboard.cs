using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;
using RibbonSpace.KeyTips;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace RibbonSpace.Controls;

public partial class Ribbon
{
    /// <summary>Identifies <see cref="IsKeyTipsEnabled"/>.</summary>
    public static readonly DependencyProperty IsKeyTipsEnabledProperty = DependencyProperty.Register(nameof(IsKeyTipsEnabled), typeof(bool), typeof(Ribbon), new PropertyMetadata(true));

    /// <summary>Identifies <see cref="IsShortcutRoutingEnabled"/>.</summary>
    public static readonly DependencyProperty IsShortcutRoutingEnabledProperty = DependencyProperty.Register(nameof(IsShortcutRoutingEnabled), typeof(bool), typeof(Ribbon), new PropertyMetadata(false));

    private readonly RibbonKeyTipNavigator<FrameworkElement> _keyTips = new();
    private readonly Stack<Action?> _keyTipCleanup = new();
    private readonly List<RibbonKeyTip> _badges = [];
    private Popup? _keyTipPopup;
    private Canvas? _keyTipCanvas;
    private UIElement? _keyboardRoot;
    private bool _altAlone;
    private Dictionary<string, List<(RibbonKeyGesture Gesture, FrameworkElement Item)>>? _shortcutCache;
    private int _shortcutCacheVersion = -1;
    private readonly HashSet<UIElement> _popupKeyboardRoots = [];
    private object? _focusBeforeKeyTips;
    private Control? _keyTipFocusTarget;

    /// <summary>Raised when Alt+Q (or the search shortcut) is pressed; search boxes subscribe to focus themselves.</summary>
    public event EventHandler? SearchRequested;

    /// <summary>Raised when KeyTips are shown or hidden.</summary>
    public event EventHandler<bool>? KeyTipModeChanged;

    /// <summary>Enables KeyTips (Alt / F10).</summary>
    public bool IsKeyTipsEnabled { get => (bool)GetValue(IsKeyTipsEnabledProperty); set => SetValue(IsKeyTipsEnabledProperty, value); }

    /// <summary>
    /// Routes keyboard shortcuts declared on items (<c>Shortcut="Ctrl+B"</c>) to those items anywhere in the window.
    /// Plain keys are ignored while a text input has focus.
    /// </summary>
    public bool IsShortcutRoutingEnabled { get => (bool)GetValue(IsShortcutRoutingEnabledProperty); set => SetValue(IsShortcutRoutingEnabledProperty, value); }

    /// <summary>True while KeyTips are shown.</summary>
    public bool IsKeyTipMode => _keyTips.IsActive;

    /// <summary>Currently displayed KeyTip badges.</summary>
    public IReadOnlyList<RibbonKeyTip> ActiveKeyTips => _badges;

    private void InitializeKeyboard()
    {
    }

    private void AttachKeyboard()
    {
        var root = XamlRoot?.Content;
        if (root is null || ReferenceEquals(root, _keyboardRoot))
        {
            return;
        }

        DetachKeyboard();
        _keyboardRoot = root;
        root.AddHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown), true);
        root.AddHandler(KeyUpEvent, new KeyEventHandler(OnRootKeyUp), true);
        root.AddHandler(PointerPressedEvent, new PointerEventHandler(OnRootPointerPressed), true);
    }

    /// <summary>
    /// Routes keyboard input of a ribbon popup (backstage, collapsed group, minimized ribbon) to the ribbon: popups
    /// are separate visual roots, so their key events never reach the window content.
    /// </summary>
    internal void AttachPopupKeyboard(UIElement? popupChild)
    {
        if (popupChild is null || _popupKeyboardRoots.Contains(popupChild))
        {
            return;
        }

        _popupKeyboardRoots.Add(popupChild);
        popupChild.AddHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown), true);
        popupChild.AddHandler(KeyUpEvent, new KeyEventHandler(OnRootKeyUp), true);
    }

    private void DetachKeyboard()
    {
        foreach (var popupChild in _popupKeyboardRoots)
        {
            popupChild.RemoveHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown));
            popupChild.RemoveHandler(KeyUpEvent, new KeyEventHandler(OnRootKeyUp));
        }

        _popupKeyboardRoots.Clear();
        if (_keyboardRoot is null)
        {
            return;
        }

        _keyboardRoot.RemoveHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown));
        _keyboardRoot.RemoveHandler(KeyUpEvent, new KeyEventHandler(OnRootKeyUp));
        _keyboardRoot.RemoveHandler(PointerPressedEvent, new PointerEventHandler(OnRootPointerPressed));
        _keyboardRoot = null;
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _altAlone = false;
        if (IsKeyTipMode)
        {
            CancelKeyTips();
        }

        // Clicks outside the tab row dismiss the "Show tabs only" popup (clicks on headers toggle it themselves).
        if (IsMinimizedPopupOpen && _tabRow is not null && !IsWithin(e, _tabRow))
        {
            CloseMinimizedPopup();
        }

        // A full-screen ribbon revealed temporarily hides again when the user clicks back into the document.
        if (IsFullScreenRevealed && !IsWithin(e, this))
        {
            IsFullScreenRevealed = false;
        }
    }

    private static bool IsWithin(PointerRoutedEventArgs e, FrameworkElement element)
    {
        var point = e.GetCurrentPoint(element).Position;
        return point.X >= 0 && point.Y >= 0 && point.X <= element.ActualWidth && point.Y <= element.ActualHeight;
    }

    private static bool IsDown(VirtualKey key)
        => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

    private static RibbonModifierKeys CurrentModifiers()
    {
        var modifiers = RibbonModifierKeys.None;
        if (IsDown(VirtualKey.Control)) modifiers |= RibbonModifierKeys.Control;
        if (IsDown(VirtualKey.Shift)) modifiers |= RibbonModifierKeys.Shift;
        if (IsDown(VirtualKey.Menu)) modifiers |= RibbonModifierKeys.Alt;
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows)) modifiers |= RibbonModifierKeys.Meta;
        return modifiers;
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key;
        if (key is VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu)
        {
            _altAlone = true;
            return;
        }

        _altAlone = false;
        if (IsKeyTipMode)
        {
            HandleKeyTipKey(e);
            return;
        }

        // Keys already handled by the focused control (or another ribbon on the same root) are left alone.
        if (e.Handled || !IsEffectivelyVisible(this))
        {
            return;
        }

        var modifiers = CurrentModifiers();
        if (key == VirtualKey.F1 && modifiers == RibbonModifierKeys.Control && IsCollapsible)
        {
            ToggleMinimized();
            e.Handled = true;
            return;
        }

        if (key == VirtualKey.F10 && modifiers == RibbonModifierKeys.None && IsKeyTipsEnabled)
        {
            ShowKeyTips();
            e.Handled = true;
            return;
        }

        if (key == VirtualKey.Q && modifiers == RibbonModifierKeys.Alt && SearchRequested is not null)
        {
            SearchRequested.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (key == VirtualKey.Escape && IsBackstageOpen)
        {
            IsBackstageOpen = false;
            e.Handled = true;
            return;
        }

        if (IsShortcutRoutingEnabled && TryRouteShortcut(key, modifiers))
        {
            e.Handled = true;
        }
    }

    private void OnRootKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu && _altAlone && IsKeyTipsEnabled && !e.Handled)
        {
            _altAlone = false;
            if (IsKeyTipMode)
            {
                CancelKeyTips();
                e.Handled = true;
            }
            else if (IsLoaded && IsEffectivelyVisible(this))
            {
                ShowKeyTips();
                e.Handled = true;
            }
        }
    }

    private bool TryRouteShortcut(VirtualKey key, RibbonModifierKeys modifiers)
    {
        if (modifiers == RibbonModifierKeys.None && key is < VirtualKey.F1 or > VirtualKey.F24)
        {
            return false;
        }

        if (modifiers is RibbonModifierKeys.Shift or RibbonModifierKeys.None && FocusManager.GetFocusedElement(XamlRoot!) is TextBox or PasswordBox or RichEditBox or AutoSuggestBox)
        {
            return false;
        }

        if (_shortcutCache is null || _shortcutCacheVersion != RibbonItemHelper.ItemsVersion)
        {
            _shortcutCache = BuildShortcutCache();
            _shortcutCacheVersion = RibbonItemHelper.ItemsVersion;
        }

        var name = key.ToString();
        foreach (var entries in _shortcutCache.Values)
        {
            // Several items may share a gesture (e.g. the same command on two tabs): the first reachable one wins.
            foreach (var (gesture, item) in entries)
            {
                if (gesture.Matches(name, modifiers) && item is IRibbonItem ribbonItem && IsShortcutReachable(item))
                {
                    return ribbonItem.Invoke();
                }
            }
        }

        return false;
    }

    private Dictionary<string, List<(RibbonKeyGesture, FrameworkElement)>> BuildShortcutCache()
    {
        var cache = new Dictionary<string, List<(RibbonKeyGesture, FrameworkElement)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in GetAllItems().Concat(QuickAccessToolBar?.Items.OfType<FrameworkElement>() ?? []))
        {
            if (item is not IRibbonItemInternal { Shortcut: { Length: > 0 } shortcut })
            {
                continue;
            }

            foreach (var text in shortcut.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!RibbonKeyGesture.TryParse(text, out var gesture))
                {
                    continue;
                }

                var source = RibbonItemHelper.GetSourceItem(item) ?? item;
                if (!cache.TryGetValue(gesture.ToString(), out var list))
                {
                    cache[gesture.ToString()] = list = [];
                }

                if (!list.Any(e => ReferenceEquals(e.Item2, source)))
                {
                    list.Add((gesture, source));
                }
            }
        }

        return cache;
    }

    /// <summary>
    /// True when an item can be reached by the user: its tab is available (not an inactive contextual tab or hidden by
    /// customization), its group is shown and the item and its containers are visible and enabled.
    /// </summary>
    private bool IsShortcutReachable(FrameworkElement item)
    {
        if (item is Control { IsEnabled: false })
        {
            return false;
        }

        var (tab, group) = FindLocation(item);
        if (tab is not null && !tab.IsAvailable)
        {
            return false;
        }

        if (group is not null && !group.IsShown)
        {
            return false;
        }

        // Walk up to the group (tabs that are not selected are collapsed but their items stay reachable).
        DependencyObject? current = item;
        while (current is not null && current is not RibbonGroup && current is not RibbonTab && current is not Ribbon)
        {
            if (current is UIElement { Visibility: Visibility.Collapsed })
            {
                return false;
            }

            current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
        }

        return tab is not null || group is not null || TabStripItems.Contains(item) || IsEffectivelyVisible(item);
    }

    private static bool IsEffectivelyVisible(UIElement element)
    {
        if (element.XamlRoot is null)
        {
            return false;
        }

        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: Visibility.Collapsed })
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Invalidates the shortcut routing cache (called automatically when tabs or items change).</summary>
    public void InvalidateShortcuts() => _shortcutCache = null;

    /// <summary>Shows the first KeyTip level (application button, QAT, tabs, tab-row items).</summary>
    public void ShowKeyTips()
    {
        if (!IsKeyTipsEnabled || XamlRoot is null)
        {
            return;
        }

        HideKeyTips();
        if (IsBackstageOpen && Backstage is { } backstage)
        {
            PushScope(backstage.GetKeyTipTargets(), null);
        }
        else
        {
            PushScope(GetTopLevelKeyTipTargets(), null);
            MoveFocusIntoRibbon();
        }

        KeyTipModeChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Like Office, KeyTip mode takes keyboard focus (so typed letters never reach the document or a text box) and
    /// gives it back when the mode ends without the invoked command moving focus elsewhere.
    /// </summary>
    private void MoveFocusIntoRibbon()
    {
        if (XamlRoot is null)
        {
            return;
        }

        var focused = FocusManager.GetFocusedElement(XamlRoot);
        var target = SelectedTab?.HeaderElement ?? _headers.FirstOrDefault();
        if (target is null || ReferenceEquals(focused, target))
        {
            return;
        }

        _focusBeforeKeyTips = focused;
        if (target.Focus(FocusState.Programmatic))
        {
            _keyTipFocusTarget = target;
        }
    }

    private void RestoreFocusAfterKeyTips()
    {
        var before = _focusBeforeKeyTips;
        var target = _keyTipFocusTarget;
        _focusBeforeKeyTips = null;
        _keyTipFocusTarget = null;
        if (target is null || XamlRoot is null || !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), target))
        {
            return;
        }

        switch (before)
        {
            case UIElement element when element.XamlRoot is not null:
                element.Focus(FocusState.Programmatic);
                break;
            case Microsoft.UI.Xaml.Documents.Hyperlink link:
                link.Focus(FocusState.Programmatic);
                break;
        }
    }

    /// <summary>Leaves KeyTip mode and closes every popup KeyTip navigation opened (Alt, F10, click elsewhere).</summary>
    public void CancelKeyTips()
    {
        while (_keyTipCleanup.Count > 0)
        {
            _keyTipCleanup.Pop()?.Invoke();
        }

        HideKeyTips();
    }

    /// <summary>Hides KeyTips (popups opened by navigation stay open; see <see cref="CancelKeyTips"/>).</summary>
    public void HideKeyTips()
    {
        var wasActive = IsKeyTipMode;
        _keyTips.Clear();
        _keyTipCleanup.Clear();
        ClearBadges();
        if (_keyTipPopup is { IsOpen: true })
        {
            _keyTipPopup.IsOpen = false;
        }

        if (wasActive)
        {
            // Let the invoked command move focus first (open a menu, focus an input) before giving it back.
            DispatcherQueue?.TryEnqueue(RestoreFocusAfterKeyTips);
            KeyTipModeChanged?.Invoke(this, false);
        }
    }

    private IEnumerable<FrameworkElement> GetTopLevelKeyTipTargets()
    {
        if (_applicationButton is { Visibility: Visibility.Visible } app && (_tabRow?.Visibility ?? Visibility.Visible) == Visibility.Visible)
        {
            yield return app;
        }

        if (IsQuickAccessVisible && QuickAccessToolBar is { } qat && qat.XamlRoot is not null)
        {
            foreach (var item in qat.Items.OfType<FrameworkElement>().Where(i => i.Visibility == Visibility.Visible))
            {
                yield return item;
            }
        }

        foreach (var header in _headers)
        {
            yield return header;
        }

        foreach (var item in TabStripItems.SelectMany(RibbonGroup.Flatten))
        {
            yield return item;
        }
    }

    private void PushScope(IEnumerable<FrameworkElement> targets, Action? cleanup)
    {
        var list = targets.Where(t => t.Visibility == Visibility.Visible && t.XamlRoot is not null).Distinct().ToList();
        var scope = new RibbonKeyTipScope<FrameworkElement>();
        var qatItems = QuickAccessToolBar?.Items.OfType<FrameworkElement>().ToHashSet() ?? [];
        var qatTips = RibbonKeyTipAssigner.AssignQuickAccess(list.Count(qatItems.Contains));
        var reserved = new List<string>();
        var requests = new List<RibbonKeyTipRequest>();
        var others = new List<FrameworkElement>();
        var qatIndex = 0;
        foreach (var target in list)
        {
            if (qatItems.Contains(target))
            {
                var tip = GetExplicitKeyTip(target) ?? qatTips[qatIndex];
                qatIndex++;
                reserved.Add(tip);
                scope.Add(tip, target);
            }
            else
            {
                others.Add(target);
                requests.Add(new RibbonKeyTipRequest(GetKeyTipLabel(target), GetExplicitKeyTip(target)));
            }
        }

        var tips = RibbonKeyTipAssigner.Assign(requests, reserved);
        for (var i = 0; i < others.Count; i++)
        {
            scope.Add(tips[i], others[i]);
        }

        _keyTips.Push(scope);
        _keyTipCleanup.Push(cleanup);
        DispatcherQueue?.TryEnqueue(RenderBadges);
    }

    private string? GetExplicitKeyTip(FrameworkElement target)
    {
        if (RibbonKeyTip.GetKeyTip(target) is { Length: > 0 } attached)
        {
            return attached;
        }

        if (ReferenceEquals(target, _applicationButton))
        {
            return ApplicationButtonKeyTip;
        }

        return target switch
        {
            RibbonTabHeader header => header.Tab.KeyTip ?? ContextualKeyTip(header.Tab),
            IRibbonItem { KeyTip: { Length: > 0 } tip } => tip,
            Button { Tag: RibbonBackstageItem item } => item.KeyTip,
            _ => FindGroupOfCollapsedButton(target)?.KeyTip,
        };
    }

    private static string? ContextualKeyTip(RibbonTab tab)
        => tab.ContextualGroup?.KeyTip is { Length: > 0 } prefix && RibbonKeyTipAssigner.Normalize(tab.Header) is { Length: > 0 } letters
            ? prefix + letters[0]
            : null;

    private string? GetKeyTipLabel(FrameworkElement target)
    {
        if (ReferenceEquals(target, _applicationButton))
        {
            return ApplicationButtonLabel ?? Localization.RibbonStrings.Current.File;
        }

        if (FindGroupOfLauncher(target) is { } launcherGroup)
        {
            return launcherGroup.Header + " launcher";
        }

        return target switch
        {
            MenuFlyoutItem entry => entry.Text,
            MenuFlyoutSubItem sub => sub.Text,
            RibbonTabHeader header => header.Tab.Header,
            IRibbonItem item => item.Label ?? item.Id,
            Button { Tag: RibbonBackstageItem item } => item.Header,
            _ => Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(target),
        };
    }

    private RibbonGroup? FindGroupOfCollapsedButton(FrameworkElement target)
        => Tabs.SelectMany(t => t.Groups).FirstOrDefault(g => ReferenceEquals(g.CollapsedButton, target));

    private RibbonGroup? FindGroupOfLauncher(FrameworkElement target)
        => Tabs.SelectMany(t => t.Groups).FirstOrDefault(g => ReferenceEquals(g.DialogLauncher, target) || ReferenceEquals(g.PopupLauncher, target));

    private void HandleKeyTipKey(KeyRoutedEventArgs e)
    {
        e.Handled = true;
        var key = e.Key;
        if (key == VirtualKey.Escape)
        {
            PopKeyTipLevel();
            return;
        }

        if (key == VirtualKey.F10)
        {
            CancelKeyTips();
            return;
        }

        if (key == VirtualKey.Back)
        {
            _keyTips.Current?.Backspace();
            UpdateBadgeVisibility();
            return;
        }

        if (KeyToChar(key) is { } ch)
        {
            ProcessKeyTipInput(ch);
        }
    }

    /// <summary>
    /// Feeds one character to KeyTip mode (as if typed). Returns true when it matched a KeyTip (partially or
    /// completely). Useful for UI automation and tests.
    /// </summary>
    public bool ProcessKeyTipInput(char ch)
    {
        if (_keyTips.Current is not { } scope)
        {
            return false;
        }

        switch (scope.Process(ch, out var target))
        {
            case RibbonKeyTipMatch.Partial:
                UpdateBadgeVisibility();
                return true;
            case RibbonKeyTipMatch.Complete when target is not null:
                ActivateKeyTip(target);
                return true;
            default:
                UpdateBadgeVisibility();
                return false;
        }
    }

    /// <summary>Leaves the current KeyTip level (like Esc). Returns false when KeyTip mode ended.</summary>
    public bool PopKeyTipLevel()
    {
        var cleanup = _keyTipCleanup.Count > 0 ? _keyTipCleanup.Pop() : null;
        cleanup?.Invoke();
        if (!_keyTips.Pop())
        {
            HideKeyTips();
            return false;
        }

        DispatcherQueue?.TryEnqueue(RenderBadges);
        return true;
    }

    /// <summary>KeyTips of the current level (text and target), for automation and tests.</summary>
    public IReadOnlyList<(string KeyTip, FrameworkElement Target)> CurrentKeyTips
        => _keyTips.Current?.Entries.ToArray() ?? [];

    private static char? KeyToChar(VirtualKey key)
    {
        if (key is >= VirtualKey.A and <= VirtualKey.Z)
        {
            return (char)('A' + (key - VirtualKey.A));
        }

        if (key is >= VirtualKey.Number0 and <= VirtualKey.Number9)
        {
            return (char)('0' + (key - VirtualKey.Number0));
        }

        if (key is >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9)
        {
            return (char)('0' + (key - VirtualKey.NumberPad0));
        }

        return null;
    }

    private void ActivateKeyTip(FrameworkElement target)
    {
        if (target is Control { IsEnabled: false })
        {
            return;
        }

        if (ReferenceEquals(target, _applicationButton))
        {
            ClearBadges();
            InvokeApplicationButton();
            if (IsBackstageOpen && Backstage is { } backstage)
            {
                _keyTips.Clear();
                _keyTipCleanup.Clear();
                PushScope(backstage.GetKeyTipTargets(), null);
            }
            else
            {
                HideKeyTips();
            }

            return;
        }

        switch (target)
        {
            case RibbonTabHeader header:
                SelectedTab = header.Tab;
                Action? cleanup = null;
                if (VisibilityMode == RibbonVisibilityMode.TabsOnly)
                {
                    OpenMinimizedPopup();
                    cleanup = CloseMinimizedPopup;
                }
                else if (VisibilityMode == RibbonVisibilityMode.FullScreen && !IsFullScreenRevealed)
                {
                    IsFullScreenRevealed = true;
                    cleanup = () => IsFullScreenRevealed = false;
                }

                ClearBadges();
                DispatcherQueue?.TryEnqueue(() => PushScope(header.Tab.GetKeyTipTargets(), cleanup));
                return;
        }

        if (FindGroupOfCollapsedButton(target) is { } collapsedGroup)
        {
            collapsedGroup.OpenPopup();
            ClearBadges();
            DispatcherQueue?.TryEnqueue(() => PushScope(collapsedGroup.GetKeyTipTargets(), collapsedGroup.ClosePopup));
            return;
        }

        if (FindGroupOfLauncher(target) is { } launcherGroup)
        {
            HideKeyTips();
            launcherGroup.OpenDialogLauncher();
            return;
        }

        if (target is Button { Tag: RibbonBackstageItem backstageItem } && Backstage is { } owner)
        {
            HideKeyTips();
            owner.Invoke(backstageItem);
            return;
        }

        if (target is IRibbonItem item)
        {
            var result = item.OnKeyTip();
            if (result.NextScope is { } next)
            {
                ClearBadges();
                DispatcherQueue?.TryEnqueue(() => PushScope(next(), null));
                return;
            }

            // Drop-down and split buttons open a menu: continue with KeyTips on its entries (Office behaviour).
            if (GetMenuFlyout(target) is { } menu && ContinueInMenu(menu))
            {
                return;
            }

            HideKeyTips();
            return;
        }

        if (target is MenuFlyoutItemBase entry)
        {
            HideKeyTips();
            InvokeMenuEntry(entry);
            return;
        }

        HideKeyTips();
        if (target is Button button)
        {
            new ButtonAutomationPeer(button).Invoke();
        }
        else if (target is Control control)
        {
            control.Focus(FocusState.Keyboard);
        }
    }

    private static MenuFlyout? GetMenuFlyout(FrameworkElement target) => target switch
    {
        Button { Flyout: MenuFlyout menu } => menu,
        RibbonSplitButton { Flyout: MenuFlyout menu } => menu,
        _ => null,
    };

    /// <summary>Pushes a KeyTip level for the entries of a menu opened by a KeyTip.</summary>
    private bool ContinueInMenu(MenuFlyout menu)
    {
        var entries = menu.Items.OfType<MenuFlyoutItemBase>().Where(i => i is MenuFlyoutItem or MenuFlyoutSubItem).ToList();
        if (entries.Count == 0)
        {
            return false;
        }

        ClearBadges();
        void Push()
        {
            if (!IsKeyTipMode)
            {
                return;
            }

            // The menu is a separate popup: route its key input to the ribbon while KeyTips are shown.
            if (entries[0] is DependencyObject first)
            {
                DependencyObject? root = first;
                while (VisualTreeHelper.GetParent(root) is { } parent)
                {
                    root = parent;
                }

                AttachPopupKeyboard(root as UIElement);
            }

            var scopeDepth = _keyTips.Depth;
            void OnClosed(object? sender, object e)
            {
                menu.Closed -= OnClosed;
                // Closed by the user (click, Esc in the menu): leave the menu level.
                if (IsKeyTipMode && _keyTips.Depth == scopeDepth + 1)
                {
                    HideKeyTips();
                }
            }

            menu.Closed += OnClosed;
            PushScope(entries.Where(e => e.Visibility == Visibility.Visible), () => menu.Hide());
        }

        if (menu.IsOpen)
        {
            DispatcherQueue?.TryEnqueue(Push);
        }
        else
        {
            void OnOpened(object? sender, object e)
            {
                menu.Opened -= OnOpened;
                DispatcherQueue?.TryEnqueue(Push);
            }

            menu.Opened += OnOpened;
        }

        return true;
    }

    private static void InvokeMenuEntry(MenuFlyoutItemBase entry)
    {
        switch (entry)
        {
            case { IsEnabled: false }:
                return;
            case ToggleMenuFlyoutItem toggle:
                new ToggleMenuFlyoutItemAutomationPeer(toggle).Toggle();
                break;
            case MenuFlyoutItem item:
                new MenuFlyoutItemAutomationPeer(item).Invoke();
                break;
            case MenuFlyoutSubItem sub:
                // Sub-menus open on keyboard focus + Right / Enter.
                sub.Focus(FocusState.Keyboard);
                break;
        }
    }

    private void ClearBadges()
    {
        _keyTipCanvas?.Children.Clear();
        _badges.Clear();
    }

    private void RenderBadges()
    {
        if (_keyTips.Current is not { } scope || XamlRoot is null)
        {
            return;
        }

        if (_keyTipPopup is null)
        {
            _keyTipCanvas = new Canvas { IsHitTestVisible = false };
            _keyTipPopup = new Popup { Child = _keyTipCanvas, IsHitTestVisible = false, IsLightDismissEnabled = false };
        }

        ClearBadges();
        _keyTipPopup.XamlRoot = XamlRoot;
        _keyTipCanvas!.Width = XamlRoot.Size.Width;
        _keyTipCanvas.Height = XamlRoot.Size.Height;
        _keyTipCanvas.RequestedTheme = ActualTheme;
        foreach (var (tip, target) in scope.Entries)
        {
            if (target.XamlRoot is null || target.ActualWidth <= 0 || target.Visibility != Visibility.Visible)
            {
                continue;
            }

            Rect bounds;
            try
            {
                bounds = target.TransformToVisual(null).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
            }
            catch (ArgumentException)
            {
                continue;
            }

            var badge = new RibbonKeyTip { Text = tip, Target = target, Opacity = target is Control { IsEnabled: false } ? 0.5 : 1 };
            badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = badge.DesiredSize;
            var large = target is RibbonTabHeader || (target is IRibbonItem { CurrentSize: RibbonItemSize.Large } ri && target is not RibbonItemsContainer && !IsSimplifiedItem(ri)) || FindGroupOfCollapsedButton(target) is not null || QuickAccessToolBar?.Items.Contains(target) == true;
            double x;
            double y;
            if (large)
            {
                x = bounds.X + ((bounds.Width - size.Width) / 2);
                y = bounds.Bottom - (size.Height / 2) - 2;
            }
            else
            {
                x = bounds.X + 4;
                y = bounds.Y + (bounds.Height / 2) - 1;
            }

            Canvas.SetLeft(badge, Math.Clamp(x, 0, Math.Max(0, XamlRoot.Size.Width - size.Width)));
            Canvas.SetTop(badge, Math.Clamp(y, 0, Math.Max(0, XamlRoot.Size.Height - size.Height)));
            _keyTipCanvas.Children.Add(badge);
            _badges.Add(badge);
        }

        _keyTipPopup.IsOpen = true;
        UpdateBadgeVisibility();
    }

    private static bool IsSimplifiedItem(IRibbonItem item) => item is RibbonButton { IsSimplified: true } or RibbonToggleButton { IsSimplified: true } or RibbonControlBase { IsSimplified: true };

    private void UpdateBadgeVisibility()
    {
        var typed = _keyTips.Current?.Typed ?? string.Empty;
        foreach (var badge in _badges)
        {
            badge.Visibility = badge.Text.StartsWith(typed, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}


