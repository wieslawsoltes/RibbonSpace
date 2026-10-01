using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls;
using RibbonSpace.Theming;
using Windows.Foundation;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>AutoCAD-class ribbon features: CAD theme, expanded panels, minimize states, floating panels, tooltips, app menu.</summary>
public sealed class CadFeatureTests : RuntimeTestBase
{
    private static Ribbon CreateWithSlideOut(out RibbonGroup draw)
    {
        var ribbon = SampleRibbon.Create();
        draw = ribbon.Group("clipboard");
        draw.SlideOutItems.Add(new RibbonButton { Id = "pasteSpecial", Label = "Paste Special", Icon = "\uE77F" });
        draw.SlideOutItems.Add(new RibbonButton { Id = "clipboardHistory", Label = "Clipboard History", Icon = "\uE81C" });
        return ribbon;
    }

    [RibbonTest]
    public async Task Cad_style_switches_surfaces_and_back()
    {
        var resources = RibbonTheme.Resources;
        try
        {
            RibbonTheme.Apply(RibbonThemePalette.Cad, RibbonChromeStyle.Neutral, RibbonThemeStyle.Cad);
            await Settle();
            Assert.Equal(RibbonThemeStyle.Cad, RibbonTheme.Style);
            Assert.Equal(Windows.UI.Color.FromArgb(255, 0x3B, 0x44, 0x53), resources.GetBrush("RibbonCommandBarBackgroundBrush", "Dark")!.Color);
            Assert.Equal(Windows.UI.Color.FromArgb(255, 0x32, 0x3A, 0x47), resources.GetBrush("RibbonGroupCaptionBackgroundBrush", "Dark")!.Color);
            Assert.Equal(new CornerRadius(0), (CornerRadius)((ResourceDictionary)resources.ThemeDictionaries["Dark"])["RibbonCommandBarCornerRadius"]);
            var ribbon = await Mount(SampleRibbon.Create(), 1400);
            Assert.True(ribbon.ActualHeight > 0, "CAD-styled ribbon renders");
        }
        finally
        {
            RibbonTheme.Apply(RibbonThemePalette.Word, RibbonChromeStyle.Neutral, RibbonThemeStyle.Office);
        }

        Assert.Equal(Windows.UI.Color.FromArgb(0, 0xFF, 0xFF, 0xFF), resources.GetBrush("RibbonGroupCaptionBackgroundBrush", "Light")!.Color);
        Assert.Equal(new CornerRadius(8), (CornerRadius)((ResourceDictionary)resources.ThemeDictionaries["Light"])["RibbonCommandBarCornerRadius"]);
    }

    [RibbonTest]
    public async Task Expanded_panel_opens_pins_and_returns_with_its_tab()
    {
        var ribbon = await Mount(CreateWithSlideOut(out var group), 1400);
        Assert.True(group.SlideOutButton is { Visibility: Visibility.Visible }, "Panel title shows the expand arrow");
        Assert.True(ribbon.FindItem("pasteSpecial") is RibbonButton, "Slide-out items are found (search, shortcuts, QAT)");
        group.OpenSlideOut();
        await Settle();
        Assert.True(group.IsSlideOutOpen, "Expanded panel open");
        group.IsSlideOutPinned = true;
        ribbon.SelectTab("insert");
        await Settle();
        Assert.False(group.IsSlideOutOpen, "Hidden with its tab");
        ribbon.SelectTab("home");
        await Settle(400);
        Assert.True(group.IsSlideOutOpen, "Pinned panel reopens with its tab");
        group.IsSlideOutPinned = false;
        group.CloseSlideOut();
        await Settle();
        Assert.False(group.IsSlideOutOpen, "Closed");
    }

    [RibbonTest]
    public async Task Collapsed_group_popup_includes_the_expanded_part()
    {
        var ribbon = await Mount(CreateWithSlideOut(out var group), 1400);
        ribbon.VisibilityMode = RibbonVisibilityMode.PanelButtons;
        await Settle();
        group.OpenPopup();
        await Settle();
        Assert.True(group.IsPopupOpen, "Panel popup open");
        Assert.True(ribbon.FindItem("pasteSpecial") is RibbonButton { XamlRoot: not null }, "Expanded commands are shown in the popup");
        group.ClosePopup();
    }

    [RibbonTest]
    public async Task Minimize_states_cycle_like_autocad()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var full = ribbon.ActualHeight;
        ribbon.MinimizeBehavior = RibbonMinimizeBehavior.CycleAll;
        ribbon.ToggleMinimized();
        await Settle();
        Assert.Equal(RibbonVisibilityMode.PanelButtons, ribbon.VisibilityMode);
        Assert.True(ribbon.IsMinimized, "Panel buttons count as minimized");
        Assert.True(ribbon.Group("font").State == RibbonGroupState.Collapsed && ribbon.Group("font").CollapsedButton is { Visibility: Visibility.Visible }, "Groups become panel buttons");
        ribbon.ToggleMinimized();
        await Settle();
        Assert.Equal(RibbonVisibilityMode.PanelTitles, ribbon.VisibilityMode);
        Assert.True(ribbon.Group("font").CollapsedButton!.Icon is null, "Panel titles show text only");
        Assert.True(ribbon.ActualHeight < full - 20, "Panel titles are much shorter than the full ribbon");
        ribbon.ToggleMinimized();
        await Settle();
        Assert.Equal(RibbonVisibilityMode.TabsOnly, ribbon.VisibilityMode);
        ribbon.ToggleMinimized();
        await Settle();
        Assert.Equal(RibbonVisibilityMode.AlwaysShow, ribbon.VisibilityMode);
        Assert.True(ribbon.Group("font").State != RibbonGroupState.Collapsed, "Full ribbon restored");

        ribbon.MinimizeBehavior = RibbonMinimizeBehavior.PanelTitles;
        ribbon.ToggleMinimized();
        Assert.Equal(RibbonVisibilityMode.PanelTitles, ribbon.VisibilityMode);
        ribbon.ToggleMinimized();
        Assert.Equal(RibbonVisibilityMode.AlwaysShow, ribbon.VisibilityMode);
    }

    [RibbonTest]
    public async Task Panel_titles_can_be_hidden()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var withTitles = ribbon.ActualHeight;
        ribbon.ShowGroupCaptions = false;
        await Settle();
        Assert.True(ribbon.ActualHeight < withTitles, $"Hiding panel titles reduces height ({ribbon.ActualHeight} < {withTitles})");
        Assert.False(ribbon.GetState().ShowGroupCaptions, "Persisted");
        ribbon.ShowGroupCaptions = true;
        await Settle();
        Assert.Equal(withTitles, ribbon.ActualHeight);
    }

    [RibbonTest]
    public async Task Panels_float_persist_and_return()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.CanFloatGroups = true;
        var group = ribbon.Group("paragraph");
        group.Float(new Point(80, 320));
        await Settle();
        Assert.True(group.IsFloating && group.IsFloatingPanelOpen, "Floating panel shown");
        Assert.Equal(Visibility.Collapsed, group.Visibility);
        Assert.True(ribbon.FloatingGroups.Contains(group), "Listed");
        Assert.True(ribbon.Item<RibbonToggleButton>("center").XamlRoot is not null, "Items live in the floating panel");
        ribbon.SelectTab("insert");
        await Settle();
        Assert.True(group.IsFloatingPanelOpen, "Floating panels stay when switching tabs");
        var json = ribbon.SaveStateToJson();

        ribbon.ReturnAllPanelsToRibbon();
        ribbon.SelectTab("home");
        await Settle();
        Assert.False(group.IsFloating || group.IsFloatingPanelOpen, "Returned");
        Assert.Equal(Visibility.Visible, group.Visibility);

        var restored = await Mount(SampleRibbon.Create(), 1400);
        restored.LoadStateFromJson(json);
        await Settle();
        var restoredGroup = restored.Group("paragraph");
        Assert.True(restoredGroup.IsFloating, "Floating restored from state");
        Assert.Equal(80d, restoredGroup.FloatingPosition.X);
        restoredGroup.ReturnToRibbon();
    }

    [RibbonTest]
    public async Task Visibility_menus_list_tabs_and_panels()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.IsVisibilityMenuEnabled = true;
        var tabs = ribbon.BuildShowTabsMenu();
        Assert.True(tabs.Items.OfType<ToggleMenuFlyoutItem>().Any(i => i.Text == "Insert" && i.IsChecked), "Show Tabs lists tabs");
        var panels = ribbon.BuildShowPanelsMenu(ribbon.FindTab("home")!);
        Assert.Equal(5, panels.Items.Count);
        var behavior = ribbon.BuildMinimizeBehaviorMenu();
        Assert.Equal(4, behavior.Items.Count);
    }

    [RibbonTest]
    public async Task Progressive_tooltip_extends_after_the_delay()
    {
        var previous = RibbonScreenTipService.ExtendedDelay;
        try
        {
            RibbonScreenTipService.ExtendedDelay = TimeSpan.FromMilliseconds(900);
            var tip = new RibbonScreenTip { Title = "Line", Description = "Creates straight line segments.", ExtendedDescription = "Specify points to draw connected segments.", ExtendedImage = "\uE8C6" };
            await Mount(tip, 400);
            Assert.False(tip.IsExtended, "Basic tooltip first");
            await Settle(1000);
            Assert.True(tip.IsExtended, "Extended after the delay");
            var copy = (RibbonScreenTip)RibbonScreenTipService.Create("Line", tip, "L")!;
            Assert.Equal(tip.ExtendedDescription, copy.ExtendedDescription);
        }
        finally
        {
            RibbonScreenTipService.ExtendedDelay = previous;
        }
    }

    [RibbonTest]
    public async Task Application_menu_shows_sub_commands_recent_items_and_search()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var menu = new RibbonApplicationMenu { Ribbon = ribbon };
        var saveAs = new RibbonApplicationMenuItem { Id = "saveAs", Label = "Save As" };
        var template = new RibbonApplicationMenuItem { Id = "saveTemplate", Label = "Drawing Template", Description = "Save as a template." };
        saveAs.Items.Add(template);
        menu.Items.Add(new RibbonApplicationMenuItem { Id = "new", Label = "New" });
        menu.Items.Add(saveAs);
        menu.RecentItems.Add(new RibbonApplicationMenuRecentItem { Title = "Floor Plan.dwg", Path = "C:\\Projects", IsPinned = true });
        RibbonApplicationMenuItem? invoked = null;
        menu.ItemInvoked += (_, e) => invoked = e.Item;
        await Mount(menu, 640);
        Assert.True(menu.ShownItem is null, "Recent documents by default");
        menu.Invoke(saveAs);
        Assert.True(ReferenceEquals(menu.ShownItem, saveAs), "Items with sub-commands open them");
        Assert.True(invoked is null, "Opening sub-commands does not invoke");
        menu.Invoke(template);
        Assert.True(ReferenceEquals(invoked, template), "Sub-command invoked");
        Assert.True(menu.SearchMenu("template").Any(r => ReferenceEquals(r.Entry.Target, template)), "Search finds sub-commands");
        Assert.True(menu.SearchMenu("bold").Any(r => r.Entry.Target is RibbonToggleButton { Id: "bold" }), "Search finds ribbon commands");
    }

    [RibbonTest]
    public async Task Combo_selection_box_template_shows_the_selected_item()
    {
        var template = (DataTemplate)XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"{Binding}\" /></DataTemplate>");
        var combo = new RibbonComboBox { ItemsSource = new[] { "0", "Walls", "Doors" }, SelectionBoxTemplate = template, InputWidth = 160 };
        await Mount(combo, 300);
        combo.SelectedItem = "Walls";
        await Settle();
        var box = FindNamed<ContentPresenter>(combo, "PART_SelectionBox");
        Assert.True(box is { Visibility: Visibility.Visible }, "Templated selection box shown");
        Assert.Equal("Walls", box!.Content as string);
        combo.IsEditable = true;
        await Settle();
        Assert.Equal(Visibility.Collapsed, box.Visibility);
    }

    [RibbonTest]
    public async Task Layered_stroke_icons_render_paths()
    {
        var presenter = new RibbonSpace.Controls.Primitives.RibbonIconPresenter { Icon = "{stroke=1.8}M4,28 L28,4|{color=#3DA9F5}M2,26 h4 v4 h-4 Z", IconSize = 32, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) };
        await Mount(presenter, 64);
        var paths = Descendants(presenter).OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
        Assert.Equal(2, paths.Length);
        Assert.True(paths[0].StrokeThickness > 1 && paths[0].Stroke is not null && paths[0].Fill is null, $"First layer stroked with the theme brush (thickness {paths[0].StrokeThickness}, stroke {paths[0].Stroke}, fill {paths[0].Fill})");
        Assert.True(paths[1].Fill is SolidColorBrush { Color.B: 0xF5 }, "Second layer filled with its fixed color");
    }

    [RibbonTest]
    public async Task Group_by_group_reduction_keeps_important_groups_large()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 640);
        ribbon.ReductionStrategy = RibbonSpace.Layout.RibbonReductionStrategy.GroupByGroup;
        await Settle();
        Assert.Equal(RibbonGroupState.Collapsed, ribbon.Group("styles").State, "Highest ReductionOrder collapses first");
        Assert.Equal(RibbonGroupState.Large, ribbon.Group("clipboard").State, "Leftmost group stays large");
        ribbon.ReductionStrategy = RibbonSpace.Layout.RibbonReductionStrategy.Stepwise;
        await Settle();
        Assert.True(ribbon.Group("clipboard").State != RibbonGroupState.Large || ribbon.Group("styles").State != RibbonGroupState.Collapsed, "Stepwise shrinks evenly");
    }

    [RibbonTest]
    public async Task Floating_panels_follow_ribbon_visibility()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var group = ribbon.Group("editing");
        group.Float(new Point(60, 300));
        await Settle();
        Assert.True(group.IsFloatingPanelOpen, "Floating");
        ribbon.Visibility = Visibility.Collapsed;
        await Settle();
        Assert.False(group.IsFloatingPanelOpen, "Hidden with the ribbon");
        Assert.True(group.IsFloating, "Still floating");
        ribbon.Visibility = Visibility.Visible;
        await Settle();
        Assert.True(group.IsFloatingPanelOpen, "Shown again");
        ribbon.SuspendPopups();
        Assert.False(group.IsFloatingPanelOpen, "SuspendPopups hides floating panels");
        ribbon.ResumePopups();
        Assert.True(group.IsFloatingPanelOpen, "ResumePopups shows them");
        group.ReturnToRibbon();
    }

    [RibbonTest]
    public Task Menu_icons_convert_paths_images_and_use_the_converter()
    {
        Assert.True(RibbonItemHelper.CreateMenuIcon("M0,0 L10,0 L10,10 Z") is PathIcon, "Filled path → PathIcon");
        Assert.True(RibbonItemHelper.CreateMenuIcon("[stroke=1.5]M0,0 L10,10") is null, "Stroked path has no PathIcon form");
        Assert.True(RibbonItemHelper.CreateMenuIcon("ms-appx:///Assets/icon.png") is ImageIcon, "Image → ImageIcon");
        var entry = new MenuFlyoutItem { Text = "Open" };
        RibbonMenu.SetIcon(entry, "\uE8E5");
        Assert.True(entry.Icon is FontIcon, "RibbonMenu.Icon attached property converts icons for declared entries");
        try
        {
            RibbonItemHelper.MenuIconConverter = icon => icon is string { Length: > 0 } text && text.StartsWith('[') ? new FontIcon { Glyph = "\uE8C6" } : null;
            Assert.True(RibbonItemHelper.CreateMenuIcon("[stroke=1.5]M0,0 L10,10") is FontIcon, "Converter used first");
        }
        finally
        {
            RibbonItemHelper.MenuIconConverter = null;
        }

        return Task.CompletedTask;
    }

    [RibbonTest]
    public async Task Status_bar_can_be_icon_only()
    {
        var grid = new RibbonToggleButton { Id = "grid", Label = "Grid", Icon = "\uE80A" };
        var bar = new RibbonStatusBar { Items = { grid } };
        await Mount(bar, 600);
        Assert.True(grid.ActualShowLabel, "Labels by default");
        bar.ShowLabels = false;
        await Settle();
        Assert.False(grid.ActualShowLabel, "Icon-only");
    }

    [RibbonTest]
    public async Task Title_bar_icon_opens_the_application_menu()
    {
        var ribbon = SampleRibbon.Create();
        ribbon.Backstage = null;
        var menu = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "New" } } };
        ribbon.ApplicationMenu = menu;
        var titleBar = new RibbonTitleBar { Ribbon = ribbon, AppIcon = "\uE8A5", IsAppIconMenuEnabled = true };
        var host = new StackPanel { Children = { titleBar, ribbon } };
        await Mount(host, 1400);
        var button = FindNamed<Button>(titleBar, "PART_AppButton");
        Assert.True(button is { Visibility: Visibility.Visible }, "App icon is a button");
        var clicked = false;
        titleBar.AppIconClick += (_, _) => clicked = true;
        new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(button!).Invoke();
        await Settle();
        Assert.True(clicked, "AppIconClick raised");
        Assert.True(menu.IsOpen, "Application menu opened from the title bar");
        Assert.True(menu.MenuFlyoutPresenterStyle is not null, "Ribbon menu look applied");
        menu.Hide();
    }

    private static T? FindNamed<T>(DependencyObject root, string name)
        where T : FrameworkElement
        => Descendants(root).OfType<T>().FirstOrDefault(e => e.Name == name);

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in Descendants(child))
            {
                yield return nested;
            }
        }
    }
}
