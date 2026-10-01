using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Controls;
using RibbonSpace.State;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>Ribbon behaviours fixed in the 1.0 review (selection, KeyTips, backstage, customization).</summary>
public sealed class RibbonBehaviorTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Hidden_tabs_cannot_be_selected_by_id()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.SelectedTabId = "tableDesign";
        await Settle();
        Assert.Equal("home", ribbon.SelectedTab?.Id);
        Assert.Equal("home", ribbon.SelectedTabId);
    }

    [RibbonTest]
    public async Task Split_button_keytip_continues_into_its_menu()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.ShowKeyTips();
        await Settle();
        Type(ribbon, ribbon.CurrentKeyTips.First(k => k.Target is RibbonTabHeader { Tab.Id: "home" }).KeyTip);
        await Settle();
        Type(ribbon, ribbon.CurrentKeyTips.First(k => k.Target is RibbonSplitButton { Id: "paste" }).KeyTip);
        await Settle(500);
        Assert.True(ribbon.IsKeyTipMode, "Still in KeyTip mode inside the menu");
        Assert.True(ribbon.CurrentKeyTips.Any(k => k.Target is MenuFlyoutItem { Text: "Keep Source" }), "Menu entries have KeyTips");
        ribbon.CancelKeyTips();
        await Settle();
        Assert.False(ribbon.IsKeyTipMode, "Cancelled");
        Assert.False(ribbon.Item<RibbonSplitButton>("paste").IsDropDownOpen, "Cancel closes the menu KeyTips opened");
    }

    [RibbonTest]
    public async Task Backstage_requested_before_load_opens_once_loaded()
    {
        var ribbon = SampleRibbon.Create();
        ribbon.IsBackstageOpen = true;
        await Mount(ribbon, 1400);
        Assert.True(ribbon.IsBackstageOpen, "Still requested");
        ribbon.IsBackstageOpen = false;
        await Settle();

        var empty = SampleRibbon.Create();
        empty.Backstage = null;
        await Mount(empty, 1400);
        empty.IsBackstageOpen = true;
        await Settle();
        Assert.False(empty.IsBackstageOpen, "Without a backstage the request is refused");
    }

    [RibbonTest]
    public async Task Customization_keeps_selected_custom_tab()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var customization = new RibbonCustomization
        {
            CustomTabs = [new RibbonCustomTab { Id = "custom.tab", Label = "Mine", Groups = [new RibbonCustomGroup { Id = "custom.group", Label = "G", ItemIds = ["cut"] }] }],
        };
        ribbon.ApplyCustomization(customization);
        await Settle();
        Assert.True(ribbon.SelectTab("custom.tab"), "Custom tab selectable");
        customization.HiddenGroupIds.Add("editing");
        ribbon.ApplyCustomization(customization);
        await Settle();
        Assert.Equal("custom.tab", ribbon.SelectedTab?.Id);
    }

    [RibbonTest]
    public async Task Tab_headers_expose_selection_patterns()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var header = ribbon.TabHeaders.First(h => h.Tab.Id == "insert");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(header);
        Assert.True(peer.GetPattern(PatternInterface.SelectionItem) is not null, "SelectionItem pattern");
        ((Microsoft.UI.Xaml.Automation.Provider.ISelectionItemProvider)peer).Select();
        await Settle();
        Assert.Equal("insert", ribbon.SelectedTab?.Id);
        Assert.True(header.IsTabStop, "Selected header is the roving tab stop");
        Assert.False(ribbon.TabHeaders.First(h => h.Tab.Id == "home").IsTabStop, "Other headers are not tab stops");
        var ribbonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(ribbon);
        Assert.True(ribbonPeer.GetPattern(PatternInterface.Selection) is not null, "Selection pattern");
    }

    [RibbonTest]
    public async Task Group_items_change_in_place()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var group = ribbon.Group("editing");
        var find = ribbon.Item<RibbonButton>("find");
        var unloaded = 0;
        find.Unloaded += (_, _) => unloaded++;
        group.Items.Add(new RibbonButton { Id = "select", Label = "Select", Icon = "" });
        group.Items.Move(2, 0);
        await Settle();
        Assert.Equal(0, unloaded, "Existing items are not reloaded");
        Assert.True(ribbon.FindItem("select") is not null, "New item found");
    }

    [RibbonTest]
    public async Task Group_order_customization_reorders_groups()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.ApplyCustomization(new RibbonCustomization { GroupOrder = { ["home"] = ["editing", "clipboard"] } });
        await Settle();
        var home = ribbon.FindTab("home")!;
        Assert.Equal("editing,clipboard,font,paragraph,styles", string.Join(",", home.DisplayGroups.Select(g => g.Id)));
        Assert.Equal("clipboard", home.Groups[0].Id, "Groups collection untouched");
        var json = ribbon.SaveStateToJson();
        ribbon.ResetCustomization();
        await Settle();
        Assert.Equal("clipboard", home.DisplayGroups[0].Id);
        ribbon.LoadStateFromJson(json);
        await Settle();
        Assert.Equal("editing", home.DisplayGroups[0].Id);
    }

    [RibbonTest]
    public async Task Contextual_group_keytip_prefix_and_enabled_state()
    {
        var ribbon = SampleRibbon.Create();
        var tools = ribbon.ContextualGroups[0];
        tools.KeyTip = "J";
        tools.IsVisible = true;
        await Mount(ribbon, 1400);
        ribbon.ShowKeyTips();
        await Settle();
        Assert.True(ribbon.CurrentKeyTips.Any(k => k.KeyTip == "JT" && k.Target is RibbonTabHeader { Tab.Id: "tableDesign" }), "Prefixed contextual KeyTip");
        ribbon.CancelKeyTips();
        tools.IsEnabled = false;
        await Settle();
        Assert.False(ribbon.FindTab("tableDesign")!.HeaderElement!.IsEnabled, "Disabled contextual group disables its headers");
    }

    [RibbonTest]
    public async Task Tab_icon_group_screentip_and_combo_display_member()
    {
        var ribbon = SampleRibbon.Create();
        ribbon.FindTab("insert")!.Icon = "\uE710";
        var editing = ribbon.Group("editing");
        editing.ScreenTip = "Find and replace text";
        var combo = new RibbonComboBox { Id = "people", DisplayMemberPath = "Name", ItemsSource = new[] { new Person("Ada"), new Person("Linus") } };
        editing.Items.Add(combo);
        await Mount(ribbon, 1400);
        Assert.Equal("Find and replace text", Microsoft.UI.Xaml.Automation.AutomationProperties.GetHelpText(editing));
        Assert.Equal("Ada", combo.GetItemText(new Person("Ada")));
        combo.SelectedItem = combo.EffectiveItems[1];
        await Settle();
        Assert.Equal("Linus", combo.Text);
    }

    public sealed record Person(string Name);

    private static void Type(Ribbon ribbon, string keys)
    {
        foreach (var c in keys)
        {
            ribbon.ProcessKeyTipInput(c);
        }
    }
}
