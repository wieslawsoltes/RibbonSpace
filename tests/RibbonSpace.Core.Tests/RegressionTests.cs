using System.ComponentModel;
using RibbonSpace.Commands;
using RibbonSpace.KeyTips;
using RibbonSpace.Layout;
using RibbonSpace.Localization;
using RibbonSpace.Model;
using RibbonSpace.Search;
using RibbonSpace.State;

namespace RibbonSpace.Core.Tests;

/// <summary>Regression tests for issues found in the 1.0 review.</summary>
public class RegressionTests
{
    private static void AssertPrefixFree(IReadOnlyList<string> tips)
    {
        Assert.Equal(tips.Count, tips.Distinct().Count());
        foreach (var a in tips)
        {
            Assert.NotEmpty(a);
            foreach (var b in tips)
            {
                if (a != b)
                {
                    Assert.False(b.StartsWith(a, StringComparison.Ordinal), $"{a} is a prefix of {b}");
                }
            }
        }
    }

    [Theory]
    [InlineData(36)]
    [InlineData(57)]
    [InlineData(80)]
    [InlineData(400)]
    public void Large_scopes_stay_prefix_free(int count)
    {
        var words = new[] { "Paste", "Cut", "Copy", "Format", "Bold", "Italic", "Underline", "Strike", "Subscript", "Superscript", "Highlight", "Color", "Zoom", "Find", "Replace", "Select", "Styles", "Bullets", "Numbering", "Indent", "Outdent", "Sort", "Show", "Align", "Justify", "Line", "Shading", "Borders", "Dictate", "Editor", "Reuse", "Grow", "Shrink", "Case", "Clear", "Effects", "Normal", "Heading", "Title", "Quote", "Emphasis", "Wrap", "Merge", "Insert", "Delete", "Sum", "Fill", "Filter", "Analyze", "Sensitivity", "Add-ins", "Comments", "Share", "Translate", "Read", "Track", "Accept" };
        var labels = Enumerable.Range(0, count).Select(i => words[i % words.Length] + (i >= words.Length ? $" {i}" : string.Empty)).ToArray();
        var tips = RibbonKeyTipAssigner.Assign(labels.Select(l => new RibbonKeyTipRequest(l)).ToArray());
        Assert.Equal(count, tips.Count);
        AssertPrefixFree(tips);
    }

    [Fact]
    public void Quick_access_tips_extend_beyond_44_items()
    {
        var tips = RibbonKeyTipAssigner.AssignQuickAccess(80);
        Assert.Equal(80, tips.Count);
        Assert.Equal("1", tips[0]);
        Assert.Equal("09", tips[9]);
        Assert.Equal("0A", tips[18]);
        AssertPrefixFree(tips);
        Assert.Empty(RibbonKeyTipAssigner.AssignQuickAccess(-3));
    }

    [Theory]
    [InlineData("Ctrl + +", "Add")]
    [InlineData("Ctrl+ +", "Add")]
    [InlineData("ctrl+pageup", "PageUp")]
    [InlineData("Alt+f4", "F4")]
    [InlineData("Ctrl+Backspace", "Back")]
    public void Gestures_parse_edge_cases(string text, string key)
    {
        Assert.True(RibbonKeyGesture.TryParse(text, out var gesture));
        Assert.Equal(key, gesture.Key);
    }

    [Theory]
    [InlineData("Ctrl+")]
    [InlineData("+")]
    [InlineData("Ctrl+A+B")]
    [InlineData("   ")]
    [InlineData("Ctrl++Shift")]
    public void Malformed_gestures_never_throw(string text)
    {
        var exception = Record.Exception(() => RibbonKeyGesture.TryParse(text, out _));
        Assert.Null(exception);
    }

    [Fact]
    public void State_with_null_collections_is_normalized()
    {
        var state = RibbonStateSerializer.Deserialize("""{"customization":{"customTabs":null,"labels":null,"hiddenTabIds":[null,"x"],"customGroups":[null,{"id":"g","itemIds":null}]},"quickAccessItemIds":[null,"save"]}""");
        Assert.NotNull(state);
        Assert.NotNull(state!.Customization.CustomTabs);
        Assert.NotNull(state.Customization.Labels);
        Assert.Equal(["x"], state.Customization.HiddenTabIds);
        Assert.Single(state.Customization.CustomGroups);
        Assert.NotNull(state.Customization.CustomGroups[0].ItemIds);
        Assert.Equal(["save"], state.QuickAccessItemIds);
        Assert.False(state.Customization.IsEmpty);
        Assert.DoesNotContain("isEmpty", RibbonStateSerializer.Serialize(state), StringComparison.Ordinal);
    }

    [Fact]
    public void Recent_searches_round_trip_through_state()
    {
        var json = RibbonStateSerializer.Serialize(new RibbonState { RecentSearchIds = ["bold", "paste"] });
        Assert.Equal(["bold", "paste"], RibbonStateSerializer.Deserialize(json)!.RecentSearchIds);
    }

    [Fact]
    public void Selected_tab_raises_property_changed()
    {
        var model = new RibbonModel();
        model.Tabs.Add(new RibbonTabModel("home", "Home"));
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        model.SelectedTabId = "home";
        Assert.Contains(nameof(RibbonModel.SelectedTab), changed);

        var tab = new RibbonTabModel("t");
        var tabChanged = new List<string?>();
        ((INotifyPropertyChanged)tab).PropertyChanged += (_, e) => tabChanged.Add(e.PropertyName);
        tab.ContextualGroupId = "table";
        Assert.Contains(nameof(RibbonTabModel.IsContextual), tabChanged);

        var item = new RibbonButtonModel("b");
        var itemChanged = new List<string?>();
        item.PropertyChanged += (_, e) => itemChanged.Add(e.PropertyName);
        item.Size = RibbonItemSize.Small;
        Assert.Contains(nameof(RibbonItemModel.EffectiveSizeDefinition), itemChanged);
    }

    [Fact]
    public void Command_state_reaches_menus_and_backstage()
    {
        var model = new RibbonModel();
        var tab = new RibbonTabModel("home");
        var group = new RibbonGroupModel("clipboard");
        var paste = new RibbonSplitButtonModel("paste") { CommandId = "paste" };
        var special = new RibbonMenuItemModel("paste.special") { CommandId = "save", IsCheckable = true };
        paste.MenuItems.Add(special);
        group.Items.Add(paste);
        tab.Groups.Add(group);
        model.Tabs.Add(tab);
        var save = new RibbonBackstageItemModel("save") { CommandId = "save" };
        model.Backstage.Items.Add(save);

        model.SetCommandEnabled("save", false);
        Assert.False(special.IsEnabled);
        Assert.False(save.IsEnabled);
        model.SetCommandChecked("save", true);
        Assert.True(special.IsChecked);
    }

    [Fact]
    public void Merger_handles_catalog_nested_items_and_qat_actions()
    {
        var host = new RibbonModel { CommandCatalog = new RibbonCommandCatalog() };
        var hostTab = new RibbonTabModel("home");
        var hostGroup = new RibbonGroupModel("font");
        var row = new RibbonButtonGroupModel("format");
        row.Items.Add(new RibbonToggleButtonModel("bold"));
        hostGroup.Items.Add(row);
        hostTab.Groups.Add(hostGroup);
        host.Tabs.Add(hostTab);
        host.QuickAccessItems.Add(new RibbonButtonModel("undo"));

        var plugin = new RibbonModel { CommandCatalog = new RibbonCommandCatalog() };
        plugin.CommandCatalog.Register("plugin.run", "Run", _ => { });
        var pluginTab = new RibbonTabModel("home");
        var pluginGroup = new RibbonGroupModel("font");
        var pluginRow = new RibbonButtonGroupModel("format");
        pluginRow.Items.Add(new RibbonToggleButtonModel("strike"));
        pluginGroup.Items.Add(pluginRow);
        pluginTab.Groups.Add(pluginGroup);
        plugin.Tabs.Add(pluginTab);
        plugin.QuickAccessItems.Add(new RibbonButtonModel("undo") { MergeAction = RibbonMergeAction.Remove });

        var merger = RibbonModelMerger.Merge(host, plugin);
        Assert.NotNull(host.CommandCatalog!.Find("plugin.run"));
        Assert.Equal(["bold", "strike"], row.Items.Select(i => i.Id));
        Assert.Empty(host.QuickAccessItems);

        merger.Unmerge();
        Assert.Null(host.CommandCatalog.Find("plugin.run"));
        Assert.Equal(["bold"], row.Items.Select(i => i.Id));
        Assert.Equal(["undo"], host.QuickAccessItems.Select(i => i.Id));
    }

    [Fact]
    public void Search_folds_letters_without_decomposition()
    {
        Assert.Equal("lacz", RibbonSearchEngine.Normalize("Łącz"));
        Assert.Equal("strasse", RibbonSearchEngine.Normalize("Straße"));
        Assert.Equal(0, RibbonSearchEngine.ScoreEntry(new RibbonSearchEntry("x", "X"), []));
    }

    [Fact]
    public void Search_recent_list_respects_max_and_duplicates()
    {
        var engine = new RibbonSearchEngine { MaxRecent = 2 };
        engine.SetRecent(["a", "b", "a", "c"]);
        Assert.Equal(["a", "b"], engine.Recent);
        engine.Add(new RibbonSearchEntry("a", "A"));
        engine.Add(new RibbonSearchEntry("a", "A2"));
        Assert.Single(engine.Entries);
    }

    [Fact]
    public void Catalog_keeps_registration_order()
    {
        var catalog = new RibbonCommandCatalog();
        foreach (var id in new[] { "z", "a", "m", "b" })
        {
            catalog.Register(id, id, _ => { });
        }

        catalog.Register("a", "A again", _ => { });
        Assert.Equal(["z", "a", "m", "b"], catalog.Commands.Select(c => c.Id));
    }

    [Fact]
    public void Layout_tolerates_missing_widths()
    {
        var result = RibbonAdaptiveLayout.Compute([new RibbonGroupLayoutInfo([100, 80]), new RibbonGroupLayoutInfo([100, 80, 40, 20])], 120);
        Assert.Equal(2, result.States.Count);
    }

    [Fact]
    public void Override_and_register_raise_current_changed()
    {
        var raised = 0;
        void Handler(object? s, EventArgs e) => raised++;
        var previous = RibbonStrings.Current;
        RibbonStrings.CurrentChanged += Handler;
        try
        {
            RibbonStrings.Current = RibbonStrings.ForCulture(new System.Globalization.CultureInfo("en-US"));
            RibbonStrings.Current.Override("File", "Start");
            Assert.Equal("Start", RibbonStrings.Current.File);
            RibbonStrings.Register("en", new Dictionary<string, string> { ["Options"] = "Options" });
            Assert.Equal("Start", RibbonStrings.Current.File);
            Assert.True(raised >= 3);
        }
        finally
        {
            RibbonStrings.CurrentChanged -= Handler;
            RibbonStrings.Current = previous;
        }
    }
}
