using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using RibbonSpace.Localization;
using Windows.Foundation;

namespace RibbonSpace.Controls;

/// <summary>Page of the customization dialog.</summary>
public enum RibbonCustomizePage
{
    /// <summary>"Customize the Ribbon".</summary>
    Ribbon,
    /// <summary>"Quick Access Toolbar".</summary>
    QuickAccessToolbar,
}

public partial class Ribbon
{
    /// <summary>Identifies <see cref="IsContextMenuEnabled"/>.</summary>
    public static readonly DependencyProperty IsContextMenuEnabledProperty = DependencyProperty.Register(nameof(IsContextMenuEnabled), typeof(bool), typeof(Ribbon), new PropertyMetadata(true));

    /// <summary>Identifies <see cref="CanCustomize"/>.</summary>
    public static readonly DependencyProperty CanCustomizeProperty = DependencyProperty.Register(nameof(CanCustomize), typeof(bool), typeof(Ribbon), new PropertyMetadata(true));

    /// <summary>Raised to build a custom context menu (add or remove entries, or set Handled).</summary>
    public event EventHandler<RibbonContextMenuEventArgs>? ContextMenuOpening;

    /// <summary>Raised when the user asks to customize the ribbon / QAT. Set Handled to show your own UI.</summary>
    public event EventHandler<RibbonCustomizeRequestedEventArgs>? CustomizeRequested;

    /// <summary>Enables the right-click context menu (Add to QAT, Collapse, Customize...).</summary>
    public bool IsContextMenuEnabled { get => (bool)GetValue(IsContextMenuEnabledProperty); set => SetValue(IsContextMenuEnabledProperty, value); }

    /// <summary>Allows customization commands (context menu, QAT "More Commands...").</summary>
    public bool CanCustomize { get => (bool)GetValue(CanCustomizeProperty); set => SetValue(CanCustomizeProperty, value); }

    /// <summary>Shows the Office context menu for an item. Returns false when disabled.</summary>
    public bool ShowItemContextMenu(FrameworkElement element, Point position)
    {
        if (!IsContextMenuEnabled)
        {
            return false;
        }

        var strings = RibbonStrings.Current;
        var menu = new MenuFlyout();
        var source = RibbonItemHelper.GetSourceItem(element) ?? element;
        if (source is IRibbonItem { CanAddToQuickAccess: true })
        {
            if (IsInQuickAccess(source))
            {
                menu.Items.Add(RibbonMenu.Item(strings.RemoveFromQuickAccessToolbar, () => RemoveFromQuickAccess(source)));
            }
            else
            {
                menu.Items.Add(RibbonMenu.Item(strings.AddToQuickAccessToolbar, () => AddToQuickAccess(source), ""));
            }

            menu.Items.Add(new MenuFlyoutSeparator());
        }

        if (CanCustomize)
        {
            menu.Items.Add(RibbonMenu.Item(strings.CustomizeQuickAccessToolbar + "...", () => ShowCustomizeDialog(RibbonCustomizePage.QuickAccessToolbar)));
        }

        menu.Items.Add(RibbonMenu.Item(QuickAccessPosition == RibbonQuickAccessPosition.AboveRibbon ? strings.ShowBelowRibbon : strings.ShowAboveRibbon, ToggleQuickAccessPosition));
        if (CanCustomize)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(RibbonMenu.Item(strings.CustomizeRibbon, () => ShowCustomizeDialog(RibbonCustomizePage.Ribbon)));
        }

        if (IsCollapsible)
        {
            menu.Items.Add(RibbonMenu.Item(VisibilityMode == RibbonVisibilityMode.AlwaysShow ? strings.CollapseRibbon : strings.PinRibbon, ToggleMinimized, VisibilityMode == RibbonVisibilityMode.AlwaysShow ? "" : ""));
        }

        var args = new RibbonContextMenuEventArgs(element, menu);
        ContextMenuOpening?.Invoke(this, args);
        if (args.Handled || menu.Items.Count == 0)
        {
            return args.Handled;
        }

        menu.ShowAt(element, new FlyoutShowOptions { Position = position });
        return true;
    }

    /// <summary>Opens the customization UI (raises <see cref="CustomizeRequested"/>, then shows <see cref="RibbonCustomizeDialog"/>).</summary>
    public async void ShowCustomizeDialog(RibbonCustomizePage page)
    {
        var args = new RibbonCustomizeRequestedEventArgs(page);
        CustomizeRequested?.Invoke(this, args);
        if (args.Handled || XamlRoot is null)
        {
            return;
        }

        try
        {
            var dialog = new RibbonCustomizeDialog(this, page) { XamlRoot = XamlRoot, RequestedTheme = ActualTheme };
            await dialog.ShowAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            // Another dialog is open.
        }
    }

    private void OnDisplayOptionsClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement anchor)
        {
            BuildDisplayOptionsMenu().ShowAt(anchor, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
        }
    }

    /// <summary>Builds the Office "Ribbon display options" menu.</summary>
    public MenuFlyout BuildDisplayOptionsMenu()
    {
        var strings = RibbonStrings.Current;
        var menu = new MenuFlyout();
        void AddMode(string text, RibbonVisibilityMode mode, string glyph)
        {
            var item = new RadioMenuFlyoutItem { Text = text, GroupName = "visibility", IsChecked = VisibilityMode == mode, Icon = new FontIcon { Glyph = glyph } };
            item.Click += (_, _) => VisibilityMode = mode;
            menu.Items.Add(item);
        }

        AddMode(strings.FullScreenMode, RibbonVisibilityMode.FullScreen, "");
        AddMode(strings.ShowTabsOnly, RibbonVisibilityMode.TabsOnly, "");
        AddMode(strings.AlwaysShowRibbon, RibbonVisibilityMode.AlwaysShow, "");
        if (IsSimplifiedModeAvailable)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            foreach (var (text, mode) in new[] { (strings.UseClassicRibbon, RibbonDisplayMode.Classic), (strings.UseSimplifiedRibbon, RibbonDisplayMode.Simplified) })
            {
                var item = new RadioMenuFlyoutItem { Text = text, GroupName = "layout", IsChecked = DisplayMode == mode };
                item.Click += (_, _) => DisplayMode = mode;
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new MenuFlyoutSeparator());
        var qat = new ToggleMenuFlyoutItem { Text = strings.ShowQuickAccessToolbar, IsChecked = IsQuickAccessVisible };
        qat.Click += (_, _) => IsQuickAccessVisible = qat.IsChecked;
        menu.Items.Add(qat);
        return menu;
    }
}

/// <summary>Arguments of <see cref="Ribbon.ContextMenuOpening"/>.</summary>
public sealed class RibbonContextMenuEventArgs(FrameworkElement element, MenuFlyout menu) : EventArgs
{
    /// <summary>Element that was right-clicked.</summary>
    public FrameworkElement Element { get; } = element;

    /// <summary>The menu (modifiable).</summary>
    public MenuFlyout Menu { get; } = menu;

    /// <summary>Suppresses the menu.</summary>
    public bool Handled { get; set; }
}

/// <summary>Arguments of <see cref="Ribbon.CustomizeRequested"/>.</summary>
public sealed class RibbonCustomizeRequestedEventArgs(RibbonCustomizePage page) : EventArgs
{
    /// <summary>Requested page.</summary>
    public RibbonCustomizePage Page { get; } = page;

    /// <summary>Set to true to show your own customization UI.</summary>
    public bool Handled { get; set; }
}
