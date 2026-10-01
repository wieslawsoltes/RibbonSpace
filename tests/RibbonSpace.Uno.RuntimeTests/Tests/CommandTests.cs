using RibbonSpace.Commands;
using RibbonSpace.Controls;

namespace RibbonSpace.Uno.RuntimeTests;

public sealed class CommandTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Command_id_resolves_through_catalog_and_state_is_pushed()
    {
        var ribbon = SampleRibbon.Create();
        var catalog = new RibbonCommandCatalog();
        var executed = 0;
        catalog.Register("copy", "Copy", _ => executed++);
        ribbon.CommandCatalog = catalog;
        await Mount(ribbon, 1400);
        var copy = ribbon.Item<RibbonButton>("copy");
        Assert.NotNull(copy.Command, "Command should be resolved from the catalog");
        copy.PerformClick();
        await Settle();
        Assert.Equal(1, executed);
        catalog.SetEnabled("copy", false);
        await Settle();
        Assert.False(copy.IsEnabled, "Descriptor IsEnabled should disable the button");
    }

    [RibbonTest]
    public async Task Item_invoked_reports_ids_and_parameters()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var invoked = new List<RibbonItemInvokedEventArgs>();
        ribbon.ItemInvoked += (_, e) => invoked.Add(e);
        ribbon.Item<RibbonButton>("cut").PerformClick();
        ((IRibbonItem)ribbon.Item<RibbonToggleButton>("bold")).Invoke();
        await Settle();
        Assert.True(invoked.Any(e => e.ItemId == "cut"), "cut reported");
        Assert.True(invoked.Any(e => e.ItemId == "bold" && Equals(e.Parameter, true)), "bold reported with checked state");
    }

    [RibbonTest]
    public async Task Set_command_enabled_by_id_applies_to_items()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.SetCommandEnabled("copy", false);
        Assert.False(ribbon.Item<RibbonButton>("copy").IsEnabled, "Disabled by id");
        ribbon.SetCommandEnabled("copy", true);
        Assert.True(ribbon.Item<RibbonButton>("copy").IsEnabled, "Enabled by id");
    }

    [RibbonTest]
    public async Task Shortcut_routing_invokes_items()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.IsShortcutRoutingEnabled = true;
        var bold = ribbon.Item<RibbonToggleButton>("bold");
        var method = typeof(Ribbon).GetMethod("TryRouteShortcut", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var handled = (bool)method.Invoke(ribbon, [Windows.System.VirtualKey.B, RibbonModifierKeys.Control])!;
        Assert.True(handled, "Ctrl+B should be routed");
        Assert.True(bold.IsChecked == true, "Bold toggled by shortcut");
    }
}

public sealed class QuickAccessTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Add_and_remove_linked_copies()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var bold = ribbon.Item<RibbonToggleButton>("bold");
        Assert.True(ribbon.AddToQuickAccess(bold), "Add should succeed");
        Assert.False(ribbon.AddToQuickAccess(bold), "Duplicates are rejected");
        await Settle();
        var copy = (RibbonToggleButton)ribbon.FindQuickAccessCopy(bold)!;
        copy.IsChecked = true;
        Assert.True(bold.IsChecked == true, "Copy state flows to the original");
        bold.IsChecked = false;
        Assert.True(copy.IsChecked == false, "Original state flows to the copy");
        Assert.Equal("save,bold", string.Join(",", ribbon.GetQuickAccessItemIds()));
        Assert.True(ribbon.RemoveFromQuickAccess(bold), "Remove via original");
        Assert.Equal("save", string.Join(",", ribbon.GetQuickAccessItemIds()));
        bold.IsChecked = true;
        bold.Label = "Strong";
        Assert.True(copy.IsChecked == false, "Removed copy is unlinked from the original");
        Assert.Equal("Bold", copy.Label);
    }

    [RibbonTest]
    public async Task Clicking_copy_runs_original_click_once()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var cut = ribbon.Item<RibbonButton>("cut");
        var clicks = 0;
        var invoked = 0;
        cut.Click += (_, _) => clicks++;
        ribbon.ItemInvoked += (_, e) => invoked += e.ItemId == "cut" ? 1 : 0;
        ribbon.AddToQuickAccess(cut);
        await Settle();
        ((RibbonButton)ribbon.FindQuickAccessCopy(cut)!).PerformClick();
        await Settle();
        Assert.Equal(1, clicks, "Original Click raised once");
        Assert.Equal(1, invoked, "ItemInvoked raised once");
    }

    [RibbonTest]
    public async Task Position_moves_between_hosts()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var above = ribbon.ActualHeight;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        await Settle();
        Assert.True(ribbon.QuickAccessToolBar!.XamlRoot is not null, "QAT still in the tree");
        ribbon.IsQuickAccessVisible = false;
        await Settle();
        Assert.True(ribbon.ActualHeight < above, "Hidden QAT reduces height");
    }
}
