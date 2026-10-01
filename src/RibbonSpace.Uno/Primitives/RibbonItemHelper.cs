using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;

namespace RibbonSpace.Controls;

/// <summary>Label visibility of an item in the simplified ribbon.</summary>
public enum RibbonSimplifiedLabel
{
    /// <summary>Items whose preferred size is Large show their label.</summary>
    Auto,
    /// <summary>Always show the label.</summary>
    Show,
    /// <summary>Icon only.</summary>
    Hide,
}

/// <summary>Host that arranges ribbon items and caches their measurements (groups, toolbars, QAT).</summary>
public interface IRibbonLayoutHost
{
    /// <summary>Invalidates cached item measurements.</summary>
    void InvalidateItemsLayout();
}

internal interface IRibbonItemInternal : IRibbonItem
{
    string? Shortcut { get; }

    object? ScreenTip { get; }

    IReadOnlyList<DependencyProperty> LinkedProperties { get; }

    /// <summary>Called when a linked copy is discarded (see <see cref="RibbonItemHelper.Unlink"/>).</summary>
    void OnUnlinked();
}

/// <summary>Items that expose a command (implemented by all RibbonSpace command items).</summary>
public interface IRibbonCommandSource
{
    /// <summary>Command.</summary>
    ICommand? Command { get; set; }

    /// <summary>Command parameter.</summary>
    object? CommandParameter { get; set; }
}

/// <summary>Items with a checked state (toggle buttons, check boxes, checkable split buttons).</summary>
public interface IRibbonCheckable
{
    /// <summary>Checked state.</summary>
    bool? IsChecked { get; set; }
}

/// <summary>Shared behaviour of ribbon item controls: owner discovery, commands, ScreenTips, automation, context menus.</summary>
public static class RibbonItemHelper
{
    /// <summary>Identifies the Owner attached property.</summary>
    public static readonly DependencyProperty OwnerProperty = DependencyProperty.RegisterAttached("Owner", typeof(IRibbonItemOwner), typeof(RibbonItemHelper), new PropertyMetadata(null));

    /// <summary>Identifies the IsLinkedCopy attached property (items created for the QAT / custom groups).</summary>
    public static readonly DependencyProperty SourceItemProperty = DependencyProperty.RegisterAttached("SourceItem", typeof(FrameworkElement), typeof(RibbonItemHelper), new PropertyMetadata(null));

    private static readonly ConditionalWeakTable<FrameworkElement, CommandSubscription> Subscriptions = new();
    private static readonly ConditionalWeakTable<DependencyObject, List<Action>> LinkRegistrations = new();

    // Commands assigned from the command catalog (so a user-set Command is never replaced or cleared).
    private static readonly ConditionalWeakTable<FrameworkElement, ICommand> AppliedCommands = new();

    // Automation strings / tooltips written by UpdateToolTip (so values set by the application are left alone).
    private static readonly ConditionalWeakTable<FrameworkElement, Dictionary<DependencyProperty, object>> AppliedValues = new();

    /// <summary>Gets the explicit owner.</summary>
    public static IRibbonItemOwner? GetOwner(DependencyObject element) => (IRibbonItemOwner?)element.GetValue(OwnerProperty);

    /// <summary>Sets the explicit owner.</summary>
    public static void SetOwner(DependencyObject element, IRibbonItemOwner? value) => element.SetValue(OwnerProperty, value);

    /// <summary>Gets the ribbon item a linked copy was created from.</summary>
    public static FrameworkElement? GetSourceItem(DependencyObject element) => (FrameworkElement?)element.GetValue(SourceItemProperty);

    /// <summary>Sets the ribbon item a linked copy was created from.</summary>
    public static void SetSourceItem(DependencyObject element, FrameworkElement? value) => element.SetValue(SourceItemProperty, value);

    /// <summary>Finds the owner of an element (explicit owner, then visual ancestors).</summary>
    public static IRibbonItemOwner? FindOwner(DependencyObject? element)
    {
        var current = element;
        var guard = 0;
        while (current is not null && guard++ < 256)
        {
            if (GetOwner(current) is { } owner)
            {
                return owner;
            }

            if (current is IRibbonItemOwner self && !ReferenceEquals(current, element))
            {
                return self;
            }

            var parent = VisualTreeHelper.GetParent(current);
            if (parent is null && current is FrameworkElement fe)
            {
                parent = fe.Parent;
            }

            current = parent;
        }

        return null;
    }

    /// <summary>
    /// Makes the content of a drop-down flyout find the owner of the element the flyout opens at (flyout content lives
    /// in a popup outside the ribbon's tree), so items inside it (pickers, buttons) report invocations to the ribbon.
    /// </summary>
    internal static void TrackFlyoutOwner(FlyoutBase? oldFlyout, FlyoutBase? newFlyout)
    {
        if (oldFlyout is not null)
        {
            oldFlyout.Opening -= OnHostedFlyoutOpening;
        }

        if (newFlyout is not null)
        {
            RibbonMenu.ApplyTheme(newFlyout);

            // Static handler: a flyout shared by an item and its linked copies does not keep any of them alive.
            newFlyout.Opening -= OnHostedFlyoutOpening;
            newFlyout.Opening += OnHostedFlyoutOpening;
        }
    }

    private static void OnHostedFlyoutOpening(object? sender, object e)
    {
        if (sender is Flyout { Content: FrameworkElement content } flyout && flyout.Target is { } target
            && content.ReadLocalValue(OwnerProperty) == DependencyProperty.UnsetValue && FindOwner(target) is { } owner)
        {
            SetOwner(content, owner);
        }
    }

    /// <summary>Finds the closest ancestor of a type.</summary>
    public static T? FindAncestor<T>(DependencyObject? element)
        where T : class
    {
        var current = element is null ? null : VisualTreeHelper.GetParent(element) ?? (element as FrameworkElement)?.Parent;
        var guard = 0;
        while (current is not null && guard++ < 256)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
        }

        return null;
    }

    /// <summary>Effective id of an item.</summary>
    public static string? GetId(FrameworkElement item, string? id, string? commandId)
        => !string.IsNullOrEmpty(id) ? id : !string.IsNullOrEmpty(item.Name) ? item.Name : !string.IsNullOrEmpty(commandId) ? commandId : null;

    internal static void Attach(FrameworkElement item)
    {
        item.RightTapped += OnRightTapped;
        if (item is Control control)
        {
            // ScreenTips show the disabled reason only while the item is disabled.
            control.IsEnabledChanged += (_, _) => UpdateToolTip(control);
        }

        UpdateAutomation(item);
    }

    private static void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.Handled || sender is not FrameworkElement item)
        {
            return;
        }

        var ribbon = FindOwner(item)?.OwnerRibbon;
        if (ribbon is not null && ribbon.ShowItemContextMenu(item, e.GetPosition(item)))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Incremented whenever items are loaded / unloaded or a shortcut changes; caches such as the ribbon's
    /// shortcut map compare it to know when to rebuild.
    /// </summary>
    internal static int ItemsVersion;

    internal static void OnLoaded(FrameworkElement item)
    {
        ItemsVersion++;
        ResolveCommand(item);
        UpdateToolTip(item);
        UpdateAutomation(item);
        if (item is IRibbonItem ribbonItem && FindOwner(item) is { } owner && !ReferenceEquals(ribbonItem, owner))
        {
            var metrics = owner.Metrics;
            if (item is IRibbonItemInternal && ReadMetrics(item) != metrics && FindAncestor<IRibbonLayoutHost>(item) is null)
            {
                ribbonItem.ApplyLayout(new RibbonItemLayout(ribbonItem.CurrentSize, metrics));
            }
        }
    }

    private static RibbonMetrics? ReadMetrics(FrameworkElement item) => item switch
    {
        RibbonButton b => b.Metrics,
        RibbonToggleButton t => t.Metrics,
        RibbonCheckBox c => c.Metrics,
        RibbonControlBase r => r.Metrics,
        _ => null,
    };

    internal static void OnUnloaded(FrameworkElement item)
    {
        ItemsVersion++;
        if (Subscriptions.TryGetValue(item, out var subscription))
        {
            subscription.Dispose();
            Subscriptions.Remove(item);
        }
    }

    /// <summary>Invalidates cached layout of the hosting group / toolbar.</summary>
    public static void InvalidateHostLayout(FrameworkElement item)
    {
        item.InvalidateMeasure();
        FindAncestor<IRibbonLayoutHost>(item)?.InvalidateItemsLayout();
    }

    /// <summary>Updates the ScreenTip / tooltip and automation help text of an item.</summary>
    public static void UpdateToolTip(FrameworkElement item)
    {
        if (item is not IRibbonItemInternal ribbonItem)
        {
            return;
        }

        // A fresh tooltip element is built every time: a UIElement can only have one parent, and the source item and
        // its linked copies (QAT, custom groups) each need their own instance.
        var isEnabled = item is not Control control || control.IsEnabled;
        var tip = RibbonScreenTipService.CreateToolTip(RibbonScreenTipService.Create(ribbonItem.Label, ribbonItem.ScreenTip, ribbonItem.Shortcut, isEnabled));
        SetOwnedValue(item, ToolTipService.ToolTipProperty, tip);
        SetOwnedValue(item, AutomationProperties.NameProperty, string.IsNullOrEmpty(ribbonItem.Label) ? null : ribbonItem.Label.Replace('\n', ' '));
        SetOwnedValue(item, AutomationProperties.AcceleratorKeyProperty, string.IsNullOrEmpty(ribbonItem.Shortcut) ? null : ribbonItem.Shortcut);
        SetOwnedValue(item, AutomationProperties.AccessKeyProperty, string.IsNullOrEmpty(ribbonItem.KeyTip) ? null : ribbonItem.KeyTip);
    }

    /// <summary>
    /// Sets (or clears, for null) a property value the helper manages, unless the application set its own value: a local
    /// value is only replaced when it is the one written last time by this helper.
    /// </summary>
    private static void SetOwnedValue(FrameworkElement item, DependencyProperty property, object? value)
    {
        var applied = AppliedValues.GetOrCreateValue(item);
        var local = item.ReadLocalValue(property);
        var owned = local == DependencyProperty.UnsetValue || local is null || (applied.TryGetValue(property, out var previous) && Equals(previous, local));
        if (!owned)
        {
            return;
        }

        if (value is null)
        {
            if (applied.Remove(property))
            {
                item.ClearValue(property);
            }

            return;
        }

        applied[property] = value;
        item.SetValue(property, value);
    }

    /// <summary>Updates automation id / name.</summary>
    public static void UpdateAutomation(FrameworkElement item)
    {
        if (item is IRibbonItem ribbonItem && ribbonItem.Id is { Length: > 0 } id && item.ReadLocalValue(AutomationProperties.AutomationIdProperty) == DependencyProperty.UnsetValue)
        {
            AutomationProperties.SetAutomationId(item, id);
        }
    }

    /// <summary>Resolves <c>CommandId</c> through the owner's command catalog and subscribes to its state.</summary>
    public static void ResolveCommand(FrameworkElement item)
    {
        if (item is not IRibbonCommandSource source)
        {
            return;
        }

        if (Subscriptions.TryGetValue(item, out var existing))
        {
            existing.Dispose();
            Subscriptions.Remove(item);
        }

        var commandId = (item as IRibbonItem)?.CommandId;
        var owner = string.IsNullOrEmpty(commandId) ? null : FindOwner(item);
        var descriptor = string.IsNullOrEmpty(commandId) ? null : owner?.CommandCatalog?.Find(commandId);
        if (descriptor?.Command is null)
        {
            // CommandId cleared or no longer in the catalog: drop the command assigned from the catalog (never a user-set one).
            ReleaseAppliedCommand(item, source);
        }

        if (string.IsNullOrEmpty(commandId))
        {
            return;
        }

        if (descriptor is null)
        {
            owner?.OwnerRibbon?.ApplyCommandState(item);
            return;
        }

        var subscription = new CommandSubscription(item, descriptor);
        Subscriptions.Add(item, subscription);
        subscription.Apply();
    }

    private static void ReleaseAppliedCommand(FrameworkElement item, IRibbonCommandSource source)
    {
        if (AppliedCommands.TryGetValue(item, out var applied))
        {
            AppliedCommands.Remove(item);
            if (ReferenceEquals(source.Command, applied))
            {
                source.Command = null;
            }
        }
    }

    /// <summary>Assigns a catalog command unless the application set its own <c>Command</c>.</summary>
    private static void ApplyCatalogCommand(FrameworkElement item, IRibbonCommandSource source, ICommand command)
    {
        var current = source.Command;
        if (ReferenceEquals(current, command))
        {
            AppliedCommands.AddOrUpdate(item, command);
            return;
        }

        if (current is null || (AppliedCommands.TryGetValue(item, out var applied) && ReferenceEquals(current, applied)))
        {
            source.Command = command;
            AppliedCommands.AddOrUpdate(item, command);
        }
    }

    /// <summary>Raises the owner's invoked notification.</summary>
    public static void NotifyInvoked(FrameworkElement item, object? parameter = null)
    {
        var source = GetSourceItem(item) ?? item;
        var commandId = (item as IRibbonItem)?.CommandId;
        var owner = FindOwner(item) ?? FindOwner(source);
        if (owner is null)
        {
            return;
        }

        var effectiveParameter = parameter ?? (item as IRibbonCommandSource)?.CommandParameter;
        owner.OnItemInvoked(source, commandId, effectiveParameter);
        if (commandId is not null && item is IRibbonCommandSource { Command: null } && owner.CommandCatalog is { } catalog)
        {
            catalog.Execute(commandId, effectiveParameter);
        }
    }

    /// <summary>Executes a command source (Command.Execute with CanExecute check).</summary>
    public static bool Execute(IRibbonCommandSource source, object? parameter)
    {
        var command = source.Command;
        if (command is null || !command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        return true;
    }

    /// <summary>Creates a menu item representing a command item in overflow menus.</summary>
    public static MenuFlyoutItemBase CreateMenuItem(FrameworkElement item, string? label, object? icon, Action invoke, bool? isChecked = null)
    {
        MenuFlyoutItem menuItem = isChecked is null ? new MenuFlyoutItem() : new ToggleMenuFlyoutItem { IsChecked = isChecked.Value };
        menuItem.Text = label ?? string.Empty;
        menuItem.Icon = CreateMenuIcon(icon);
        menuItem.IsEnabled = item.IsEnabled() ;
        menuItem.Click += (_, _) => invoke();
        if (item is IRibbonItemInternal { Shortcut: { Length: > 0 } shortcut })
        {
            menuItem.KeyboardAcceleratorTextOverride = shortcut;
        }

        return menuItem;
    }

    private static bool IsEnabled(this FrameworkElement element) => element is not Control control || control.IsEnabled;

    /// <summary>Creates an <see cref="IconElement"/> suitable for menus from any icon description.</summary>
    public static IconElement? CreateMenuIcon(object? icon)
    {
        if (icon is not null && MenuIconConverter?.Invoke(icon) is { } converted)
        {
            return converted;
        }

        switch (icon)
        {
            case null:
                return null;
            case string s when Primitives.RibbonIconPresenter.Classify(s) == Model.RibbonIconKind.Glyph:
                return new FontIcon { Glyph = s };
            case string s when Primitives.RibbonIconPresenter.Classify(s) == Model.RibbonIconKind.Path:
                return CreatePathMenuIcon(s);
            case string s when Primitives.RibbonIconPresenter.Classify(s) == Model.RibbonIconKind.Image:
                return CreateImageMenuIcon(s);
            case Model.RibbonIcon { Kind: Model.RibbonIconKind.Glyph } ri:
                return new FontIcon { Glyph = ri.Value };
            case Model.RibbonIcon { Kind: Model.RibbonIconKind.Path } ri:
                return CreatePathMenuIcon(ri.Value);
            case Model.RibbonIcon { Kind: Model.RibbonIconKind.Image } ri:
                return CreateImageMenuIcon(ri.Value);
            case FontIconSource f:
                return new FontIcon { Glyph = f.Glyph };
            case SymbolIconSource s:
                return new SymbolIcon(s.Symbol);
            case PathIconSource p:
                return new PathIcon { Data = p.Data };
            case BitmapIconSource b:
                return new BitmapIcon { UriSource = b.UriSource, ShowAsMonochrome = b.ShowAsMonochrome };
            case ImageIconSource i:
                return new ImageIcon { Source = i.ImageSource };
            case FontIcon fi:
                return new FontIcon { Glyph = fi.Glyph };
            case SymbolIcon si:
                return new SymbolIcon(si.Symbol);
            case BitmapIcon bi:
                return new BitmapIcon { UriSource = bi.UriSource, ShowAsMonochrome = bi.ShowAsMonochrome };
            case ImageIcon ii:
                return new ImageIcon { Source = ii.Source };
            default:
                return null;
        }
    }

    /// <summary>
    /// Optional converter for menu icons, tried before the built-in conversions. Menu items take a single-colour
    /// <see cref="IconElement"/>; filled path icons become a <see cref="PathIcon"/>, but stroked or multi-colour layered
    /// icons cannot be expressed that way, so apps can supply e.g. a rendered <see cref="ImageIcon"/> here.
    /// </summary>
    public static Func<object, IconElement?>? MenuIconConverter { get; set; }

    private static IconElement? CreatePathMenuIcon(string value)
    {
        var layers = Model.RibbonIconLayer.Parse(value);
        if (layers.Count == 0 || layers.Any(l => l.StrokeThickness > 0))
        {
            // Stroked line art has no PathIcon equivalent (PathIcon only fills).
            return null;
        }

        try
        {
            var data = string.Join(" ", layers.Select(l => l.Data));
            var escaped = System.Security.SecurityElement.Escape(data);
            return (PathIcon)Microsoft.UI.Xaml.Markup.XamlReader.Load($"<PathIcon xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Data=\"{escaped}\" />");
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IconElement? CreateImageMenuIcon(string value)
    {
        var uri = Uri.TryCreate(value, UriKind.Absolute, out var absolute) ? absolute : Uri.TryCreate("ms-appx:///" + value.TrimStart('/'), UriKind.Absolute, out var packaged) ? packaged : null;
        return uri is null ? null : new ImageIcon { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri) };
    }

    /// <summary>Binds a DP of a linked copy to the same DP of the source item (one or two way).</summary>
    /// <remarks>
    /// The source only holds a weak reference to the copy, and every registration is recorded on the copy so
    /// <see cref="Unlink"/> (or <see cref="UnlinkTree"/>) can remove the callbacks when the copy is discarded.
    /// </remarks>
    public static void Link(DependencyObject source, DependencyObject target, DependencyProperty property, bool twoWay = false)
    {
        target.SetValue(property, source.GetValue(property));
        var weakSource = new WeakReference<DependencyObject>(source);
        var weakTarget = new WeakReference<DependencyObject>(target);
        var syncing = false;
        var sourceRegistered = true;
        var targetRegistered = twoWay;
        long sourceToken = 0;
        long targetToken = 0;
        sourceToken = source.RegisterPropertyChangedCallback(property, (s, dp) =>
        {
            if (syncing)
            {
                return;
            }

            if (!weakTarget.TryGetTarget(out var t))
            {
                // The copy was collected without being unlinked: stop listening.
                if (sourceRegistered)
                {
                    sourceRegistered = false;
                    DeferUnregister(s, dp, sourceToken);
                }

                return;
            }

            syncing = true;
            try
            {
                t.SetValue(dp, s.GetValue(dp));
            }
            finally
            {
                syncing = false;
            }
        });
        if (twoWay)
        {
            targetToken = target.RegisterPropertyChangedCallback(property, (t, dp) =>
            {
                if (syncing)
                {
                    return;
                }

                if (!weakSource.TryGetTarget(out var s))
                {
                    if (targetRegistered)
                    {
                        targetRegistered = false;
                        DeferUnregister(t, dp, targetToken);
                    }

                    return;
                }

                syncing = true;
                try
                {
                    s.SetValue(dp, t.GetValue(dp));
                }
                finally
                {
                    syncing = false;
                }
            });
        }

        LinkRegistrations.GetOrCreateValue(target).Add(() =>
        {
            if (sourceRegistered && weakSource.TryGetTarget(out var s))
            {
                sourceRegistered = false;
                s.UnregisterPropertyChangedCallback(property, sourceToken);
            }

            if (targetRegistered && weakTarget.TryGetTarget(out var t))
            {
                targetRegistered = false;
                t.UnregisterPropertyChangedCallback(property, targetToken);
            }
        });
    }

    // Unregistering a callback while it is being invoked is not safe on every platform: defer it to the dispatcher.
    private static void DeferUnregister(DependencyObject owner, DependencyProperty property, long token)
    {
        if (owner.DispatcherQueue is { } queue && queue.TryEnqueue(() => owner.UnregisterPropertyChangedCallback(property, token)))
        {
            return;
        }

        owner.UnregisterPropertyChangedCallback(property, token);
    }

    /// <summary>Removes every link registered for a linked copy so the source no longer updates it.</summary>
    public static void Unlink(DependencyObject target)
    {
        if (LinkRegistrations.TryGetValue(target, out var registrations))
        {
            LinkRegistrations.Remove(target);
            foreach (var unregister in registrations)
            {
                unregister();
            }
        }

        if (target is FrameworkElement element && Subscriptions.TryGetValue(element, out var subscription))
        {
            Subscriptions.Remove(element);
            subscription.Dispose();
        }

        // Links are gone first, so whatever the copy resets while detaching does not flow back to the source.
        if (target is IRibbonItemInternal copy && GetSourceItem(target) is not null)
        {
            copy.OnUnlinked();
        }
    }

    /// <summary>Unlinks a discarded copy and every linked copy nested inside it (containers, overflow panels).</summary>
    public static void UnlinkTree(DependencyObject? root)
    {
        switch (root)
        {
            case null:
                return;
            case RibbonItemsContainer container:
                foreach (var child in container.Items)
                {
                    UnlinkTree(child);
                }

                break;
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    UnlinkTree(child);
                }

                break;
            case ScrollViewer viewer:
                UnlinkTree(viewer.Content as DependencyObject);
                break;
            case Border border:
                UnlinkTree(border.Child);
                break;
        }

        Unlink(root);
    }

    /// <summary>Links common item properties (label, icons, enabled, command, tooltip, visibility).</summary>
    public static void LinkCommon(Control source, Control target)
    {
        SetSourceItem(target, source);
        Link(source, target, Control.IsEnabledProperty);
        Link(source, target, UIElement.VisibilityProperty);
        if (source is IRibbonItemInternal si && target is IRibbonItemInternal ti && source.GetType() == target.GetType())
        {
            foreach (var dp in si.LinkedProperties)
            {
                Link(source, target, dp);
            }
        }

        target.DataContext = source.DataContext;
    }

    private sealed class CommandSubscription : IDisposable
    {
        private readonly WeakReference<FrameworkElement> _item;
        private readonly RibbonCommandDescriptor _descriptor;

        public CommandSubscription(FrameworkElement item, RibbonCommandDescriptor descriptor)
        {
            _item = new WeakReference<FrameworkElement>(item);
            _descriptor = descriptor;
            _descriptor.PropertyChanged += OnChanged;
        }

        public void Apply()
        {
            if (!_item.TryGetTarget(out var item))
            {
                return;
            }

            if (item is Control control)
            {
                control.IsEnabled = _descriptor.IsEnabled;
            }

            if (_descriptor.IsChecked is { } isChecked && item is IRibbonCheckable checkable)
            {
                checkable.IsChecked = isChecked;
            }

            if (item is IRibbonCommandSource source && _descriptor.Command is not null)
            {
                ApplyCatalogCommand(item, source, _descriptor.Command);
            }
        }

        private void OnChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_item.TryGetTarget(out var item))
            {
                Dispose();
                return;
            }

            if (item.DispatcherQueue is { } queue && !queue.HasThreadAccess)
            {
                queue.TryEnqueue(Apply);
            }
            else
            {
                Apply();
            }
        }

        public void Dispose() => _descriptor.PropertyChanged -= OnChanged;
    }
}
