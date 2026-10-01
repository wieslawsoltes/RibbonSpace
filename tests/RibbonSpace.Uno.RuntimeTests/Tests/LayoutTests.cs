using RibbonSpace.Controls;

namespace RibbonSpace.Uno.RuntimeTests;

public sealed class LayoutTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Wide_ribbon_shows_all_groups_large()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        Assert.True(ribbon.SelectedTab?.Id == "home", "Home should be selected");
        Assert.True(ribbon.SelectedTab!.Groups.All(g => g.State == RibbonGroupState.Large), "All groups should be Large at 1400 px");
    }

    [RibbonTest]
    public async Task Narrow_ribbon_reduces_then_collapses_groups()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        await Resize(ribbon, 620);
        var states = ribbon.SelectedTab!.Groups.Select(g => g.State).ToList();
        Assert.True(states.Any(s => s != RibbonGroupState.Large), "Some groups should shrink at 620 px: " + string.Join(",", states));
        await Resize(ribbon, 300);
        Assert.True(ribbon.SelectedTab.Groups.Count(g => g.State == RibbonGroupState.Collapsed) >= 3, "Most groups should collapse at 300 px");
        await Resize(ribbon, 1400);
        Assert.True(ribbon.SelectedTab.Groups.All(g => g.State == RibbonGroupState.Large), "Groups should grow back");
    }

    [RibbonTest]
    public async Task Reduction_order_shrinks_high_priority_group_first()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var styles = ribbon.Group("styles");
        var clipboard = ribbon.Group("clipboard");
        var width = ribbon.SelectedTab!.GroupsPanel.ExtentWidth;
        await Resize(ribbon, width - 20);
        Assert.True(styles.State != RibbonGroupState.Large, "Styles (ReductionOrder=1) should shrink first");
        Assert.Equal(RibbonGroupState.Large, clipboard.State, "Clipboard should stay large");
    }

    [RibbonTest]
    public async Task Collapsed_group_opens_popup_with_items()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 300);
        var group = ribbon.SelectedTab!.Groups.First(g => g.State == RibbonGroupState.Collapsed);
        group.OpenPopup();
        await Settle();
        Assert.True(group.IsPopupOpen, "Popup should open");
        group.ClosePopup();
        await Settle();
        Assert.False(group.IsPopupOpen, "Popup should close");
        Assert.Equal(RibbonGroupState.Collapsed, group.State);
    }

    [RibbonTest]
    public async Task Simplified_mode_moves_items_to_overflow()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.DisplayMode = RibbonDisplayMode.Simplified;
        await Settle();
        Assert.True(ribbon.SelectedTab!.Groups.All(g => g.IsSimplified), "Groups should be simplified");
        await Resize(ribbon, 360);
        Assert.True(ribbon.SelectedTab.GroupsPanel.HasOverflow, "Narrow simplified ribbon should overflow");
        Assert.True(ribbon.SelectedTab.Groups.SelectMany(g => g.GetOverflowItems()).Any(), "Overflow items expected");
        ribbon.DisplayMode = RibbonDisplayMode.Classic;
        await Settle();
        Assert.False(ribbon.SelectedTab.Groups.Any(g => g.IsSimplified), "Back to classic");
    }

    [RibbonTest]
    public async Task Density_changes_metrics()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var comfortable = ribbon.ActualHeight;
        ribbon.Density = RibbonDensity.Touch;
        await Settle();
        Assert.True(ribbon.ActualHeight > comfortable, $"Touch should be taller ({ribbon.ActualHeight} vs {comfortable})");
        ribbon.Density = RibbonDensity.Compact;
        await Settle();
        Assert.True(ribbon.ActualHeight < comfortable, "Compact should be shorter");
    }

    [RibbonTest]
    public async Task Minimized_ribbon_hides_commands_and_shows_popup()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var full = ribbon.ActualHeight;
        ribbon.ToggleMinimized();
        await Settle();
        Assert.True(ribbon.IsMinimized, "Should be minimized");
        Assert.True(ribbon.ActualHeight < full - 40, "Command area should be hidden");
        ribbon.OpenMinimizedPopup();
        await Settle();
        Assert.True(ribbon.IsMinimizedPopupOpen, "Popup should open");
        ribbon.CloseMinimizedPopup();
        await Settle();
        ribbon.VisibilityMode = RibbonVisibilityMode.AlwaysShow;
        await Settle();
        Assert.False(ribbon.IsMinimized, "Restored");
        Assert.True(Math.Abs(ribbon.ActualHeight - full) < 2, "Height restored");
    }

    [RibbonTest]
    public async Task Full_screen_mode_hides_ribbon_until_revealed()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.VisibilityMode = RibbonVisibilityMode.FullScreen;
        await Settle();
        Assert.True(ribbon.ActualHeight < 40, "Only the reveal bar should remain");
        ribbon.IsFullScreenRevealed = true;
        await Settle();
        Assert.True(ribbon.ActualHeight > 100, "Revealed ribbon");
    }
}

public sealed class SelectionTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Selecting_tabs_by_id_updates_selection()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        RibbonTabChangedEventArgs? changed = null;
        ribbon.SelectedTabChanged += (_, e) => changed = e;
        Assert.True(ribbon.SelectTab("insert"), "SelectTab should succeed");
        Assert.Equal("insert", ribbon.SelectedTabId);
        Assert.Equal("insert", changed?.NewTab?.Id);
        ribbon.SelectedTabId = "home";
        Assert.Equal("home", ribbon.SelectedTab?.Id);
        Assert.False(ribbon.SelectTab("tableDesign"), "Hidden contextual tab can't be selected");
    }

    [RibbonTest]
    public async Task Contextual_group_shows_selects_and_restores_previous_tab()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.SelectTab("insert");
        Assert.Equal(2, ribbon.VisibleTabs.Count);
        ribbon.SetActiveContextualGroups("tableTools");
        await Settle();
        Assert.Equal(3, ribbon.VisibleTabs.Count);
        Assert.Equal("tableDesign", ribbon.SelectedTab?.Id, "SelectOnShow should select the contextual tab");
        Assert.True(ribbon.TabHeaders.Last().IsContextual, "Contextual header");
        ribbon.SetActiveContextualGroups();
        await Settle();
        Assert.Equal("insert", ribbon.SelectedTab?.Id, "Previous regular tab should be restored");
    }

    [RibbonTest]
    public async Task Radio_toggle_groups_are_exclusive()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var left = ribbon.Item<RibbonToggleButton>("left");
        var center = ribbon.Item<RibbonToggleButton>("center");
        ((IRibbonItem)center).Invoke();
        await Settle();
        Assert.True(center.IsChecked == true, "Center checked");
        Assert.True(left.IsChecked == false, "Left unchecked");
        ((IRibbonItem)center).Invoke();
        Assert.True(center.IsChecked == true, "Checked radio stays checked");
    }

    [RibbonTest]
    public async Task Hidden_tab_and_group_disappear()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.FindTab("insert")!.IsTabVisible = false;
        await Settle();
        Assert.False(ribbon.VisibleTabs.Any(t => t.Id == "insert"), "Insert hidden");
        var editing = ribbon.Group("editing");
        editing.IsGroupVisible = false;
        await Settle();
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, editing.Visibility);
    }
}
