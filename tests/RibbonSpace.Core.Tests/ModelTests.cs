using System.ComponentModel;
using RibbonSpace.Commands;
using RibbonSpace.Model;

namespace RibbonSpace.Core.Tests;

public class ModelTests
{
    private static RibbonModel CreateModel()
    {
        var model = new RibbonModel();
        var home = new RibbonTabModel("home", "Home")
        {
            Groups =
            {
                new RibbonGroupModel("clipboard", "Clipboard")
                {
                    Items =
                    {
                        new RibbonSplitButtonModel("paste", "Paste", RibbonIcons.Paste) { CommandId = "paste" },
                        new RibbonButtonModel("cut", "Cut", RibbonIcons.Cut) { CommandId = "cut" },
                    },
                },
                new RibbonGroupModel("font", "Font")
                {
                    ItemsLayout = RibbonGroupItemsLayout.Rows,
                    Items =
                    {
                        new RibbonRowModel("font-row2",
                            new RibbonButtonGroupModel("bis",
                                new RibbonToggleButtonModel("bold", "Bold", RibbonIcons.Bold) { CommandId = "bold" },
                                new RibbonToggleButtonModel("italic", "Italic", RibbonIcons.Italic))),
                    },
                },
            },
        };
        model.Tabs.Add(home);
        model.ContextualGroups.Add(new RibbonContextualGroupModel("table-tools", "Table Tools"));
        model.Tabs.Add(new RibbonTabModel("table-design", "Table Design") { ContextualGroupId = "table-tools" });
        return model;
    }

    [Fact]
    public void Finds_nested_items()
    {
        var model = CreateModel();
        Assert.IsType<RibbonToggleButtonModel>(model.FindItem("bold"));
        Assert.Equal("font", model.FindGroup("font")!.Id);
        Assert.Equal("table-tools", model.FindNode("table-tools")!.Id);
        Assert.Equal(6, model.EnumerateItems().Count());
    }

    [Fact]
    public void Contextual_tabs_are_visible_only_with_their_group()
    {
        var model = CreateModel();
        Assert.DoesNotContain(model.VisibleTabs, t => t.Id == "table-design");
        model.SetActiveContextualGroups("table-tools");
        Assert.Contains(model.VisibleTabs, t => t.Id == "table-design");
        Assert.True(model.FindTab("table-design")!.IsContextual);
    }

    [Fact]
    public void Command_state_is_pushed_by_id()
    {
        var model = CreateModel();
        model.SetCommandEnabled("cut", false);
        model.SetCommandChecked("bold", true);
        Assert.False(model.FindItem("cut")!.IsEnabled);
        Assert.True(((RibbonToggleButtonModel)model.FindItem("bold")!).IsChecked);
    }

    [Fact]
    public void Property_changes_are_notified()
    {
        var item = new RibbonToggleButtonModel("x", "X");
        var changed = new List<string?>();
        ((INotifyPropertyChanged)item).PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        item.IsChecked = true;
        item.IsChecked = true;
        item.Label = "Y";
        Assert.Equal(["IsChecked", "Label"], changed);
    }

    [Fact]
    public void Selected_tab_round_trips_by_id()
    {
        var model = CreateModel();
        model.SelectedTab = model.FindTab("home");
        Assert.Equal("home", model.SelectedTabId);
        Assert.Same(model.FindTab("home"), model.SelectedTab);
    }

    [Fact]
    public void Merge_adds_merges_replaces_removes_and_unmerges()
    {
        var target = CreateModel();
        var plugin = new RibbonModel();
        plugin.Tabs.Add(new RibbonTabModel("home", "Home")
        {
            Groups =
            {
                new RibbonGroupModel("clipboard") { Items = { new RibbonButtonModel("copy", "Copy"), new RibbonButtonModel("cut") { MergeAction = RibbonMergeAction.Remove } } },
                new RibbonGroupModel("addin", "Add-in") { Items = { new RibbonButtonModel("addin-run", "Run") } },
            },
        });
        plugin.Tabs.Add(new RibbonTabModel("addins", "Add-ins"));

        var merger = RibbonModelMerger.Merge(target, plugin);
        var clipboard = target.FindGroup("clipboard")!;
        Assert.Contains(clipboard.Items, i => i.Id == "copy");
        Assert.DoesNotContain(clipboard.Items, i => i.Id == "cut");
        Assert.NotNull(target.FindGroup("addin"));
        Assert.NotNull(target.FindTab("addins"));

        merger.Unmerge();
        Assert.DoesNotContain(clipboard.Items, i => i.Id == "copy");
        Assert.Contains(clipboard.Items, i => i.Id == "cut");
        Assert.Null(target.FindGroup("addin"));
        Assert.Null(target.FindTab("addins"));
    }

    [Fact]
    public void Merge_respects_order()
    {
        var target = new RibbonModel();
        target.Tabs.Add(new RibbonTabModel("a") { Order = 10 });
        target.Tabs.Add(new RibbonTabModel("c") { Order = 30 });
        var source = new RibbonModel();
        source.Tabs.Add(new RibbonTabModel("b") { Order = 20 });
        RibbonModelMerger.Merge(target, source);
        Assert.Equal(["a", "b", "c"], target.Tabs.Select(t => t.Id));
    }

    [Fact]
    public void Model_icon_implicit_conversion_and_factories()
    {
        RibbonIcon icon = RibbonIcons.Bold;
        Assert.Equal(RibbonIconKind.Glyph, icon.Kind);
        Assert.Equal(RibbonIconKind.Path, RibbonIcon.Path("M0,0 L1,1").Kind);
        Assert.Equal("#FF0000", RibbonIcon.Glyph("x").WithForeground("#FF0000").Foreground);
    }
}

public class CommandTests
{
    [Fact]
    public void Relay_command_respects_can_execute()
    {
        var count = 0;
        var enabled = false;
        var command = new RibbonRelayCommand(() => count++, () => enabled);
        command.Execute(null);
        Assert.Equal(0, count);
        enabled = true;
        command.Execute(null);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Typed_command_passes_parameter()
    {
        string? received = null;
        new RibbonRelayCommand<string>(s => received = s).Execute("hello");
        Assert.Equal("hello", received);
    }

    [Fact]
    public async Task Async_command_disables_while_running_and_reports_errors()
    {
        var tcs = new TaskCompletionSource();
        var command = new RibbonAsyncCommand(() => tcs.Task);
        var run = command.ExecuteAsync(null);
        Assert.False(command.CanExecute(null));
        tcs.SetResult();
        await run;
        Assert.True(command.CanExecute(null));

        var failing = new RibbonAsyncCommand(() => throw new InvalidOperationException("boom"));
        Exception? reported = null;
        failing.Failed += (_, e) => { reported = e.Exception; e.Handled = true; };
        await failing.ExecuteAsync(null);
        Assert.IsType<InvalidOperationException>(reported);
    }

    [Fact]
    public void Catalog_registers_executes_and_pushes_state()
    {
        var catalog = new RibbonCommandCatalog();
        var executed = 0;
        catalog.Register("bold", "Bold", _ => executed++, shortcut: "Ctrl+B");
        var changes = new List<string?>();
        catalog.CommandStateChanged += (_, e) => changes.Add(e.PropertyName);
        Assert.True(catalog.Execute("BOLD"));
        catalog.SetEnabled("bold", false);
        Assert.False(catalog.Execute("bold"));
        catalog.SetChecked("bold", true);
        Assert.Equal(1, executed);
        Assert.Equal(["IsEnabled", "IsChecked"], changes);
        Assert.False(catalog.Execute("unknown"));
        Assert.True(catalog.Unregister("bold"));
    }

    [Theory]
    [InlineData("Ctrl+Shift+L", RibbonModifierKeys.Control | RibbonModifierKeys.Shift, "L")]
    [InlineData("F5", RibbonModifierKeys.None, "F5")]
    [InlineData("Alt+Down", RibbonModifierKeys.Alt, "Down")]
    [InlineData("Ctrl+]", RibbonModifierKeys.Control, "OemCloseBracket")]
    [InlineData("Ctrl+Plus", RibbonModifierKeys.Control, "Add")]
    [InlineData("Ctrl+1", RibbonModifierKeys.Control, "Number1")]
    [InlineData("cmd+s", RibbonModifierKeys.Meta, "S")]
    public void Key_gesture_parsing(string text, RibbonModifierKeys modifiers, string key)
    {
        var gesture = RibbonKeyGesture.Parse(text);
        Assert.Equal(modifiers, gesture.Modifiers);
        Assert.Equal(key, gesture.Key);
    }

    [Fact]
    public void Key_gesture_matching_and_display()
    {
        var gesture = RibbonKeyGesture.Parse("Ctrl+B");
        Assert.True(gesture.Matches("B", RibbonModifierKeys.Control));
        Assert.True(gesture.Matches("B", RibbonModifierKeys.Meta));
        Assert.False(gesture.Matches("B", RibbonModifierKeys.Control | RibbonModifierKeys.Shift));
        Assert.True(RibbonKeyGesture.Parse("Ctrl+1").Matches("NumberPad1", RibbonModifierKeys.Control));
        Assert.Equal("Ctrl+Shift+]", RibbonKeyGesture.Parse("ctrl+shift+]").ToString());
        Assert.Equal("⌘⇧L", RibbonKeyGesture.Parse("Ctrl+Shift+L").ToDisplayString(true));
        Assert.False(RibbonKeyGesture.TryParse("Ctrl+Shift", out _));
        Assert.False(RibbonKeyGesture.TryParse("A+B", out _));
    }
}
