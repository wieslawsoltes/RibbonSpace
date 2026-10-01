using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;
using RibbonSpace.Controls;
using RibbonSpace.Model;
using RibbonSpace.State;
using RibbonSpace.Theming;

namespace RibbonSpace.Uno.RuntimeTests;

public sealed class StateTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task State_round_trips_including_customization()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.SelectTab("insert");
        ribbon.DisplayMode = RibbonDisplayMode.Simplified;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        ribbon.AddToQuickAccess("bold");
        ribbon.ApplyCustomization(new RibbonCustomization
        {
            HiddenGroupIds = ["editing"],
            Labels = { ["home"] = "Start" },
            CustomTabs = [new RibbonCustomTab { Id = "custom.tab", Label = "My Tab", Groups = [new RibbonCustomGroup { Id = "custom.group", Label = "Mine", ItemIds = ["cut", "bold"] }] }],
        });
        await Settle();
        var json = ribbon.SaveStateToJson();

        var restored = await Mount(SampleRibbon.Create(), 1400);
        Assert.True(restored.LoadStateFromJson(json), "State should load");
        await Settle();
        Assert.Equal("insert", restored.SelectedTab?.Id);
        Assert.Equal(RibbonDisplayMode.Simplified, restored.DisplayMode);
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, restored.QuickAccessPosition);
        Assert.Equal("save,bold", string.Join(",", restored.GetQuickAccessItemIds()));
        Assert.Equal("Start", restored.FindTab("home")!.Header);
        Assert.Equal(Visibility.Collapsed, restored.Group("editing").Visibility);
        var custom = restored.FindTab("custom.tab");
        Assert.NotNull(custom, "Custom tab created");
        Assert.Equal(2, custom!.Groups[0].Items.Count);
        restored.ResetCustomization();
        await Settle();
        Assert.True(restored.FindTab("custom.tab") is null, "Reset removes custom tabs");
        Assert.Equal("Home", restored.FindTab("home")!.Header);
    }

    [RibbonTest]
    public async Task Tab_order_customization_reorders_headers()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.ApplyCustomization(new RibbonCustomization { TabOrder = ["insert", "home"] });
        await Settle();
        Assert.Equal("insert", ribbon.TabHeaders[0].Tab.Id);
    }
}

public sealed class KeyTipTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task KeyTips_navigate_levels_and_invoke()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.ShowKeyTips();
        await Settle();
        Assert.True(ribbon.IsKeyTipMode, "KeyTip mode");
        Assert.True(ribbon.ActiveKeyTips.Count >= 3, "Badges rendered: " + ribbon.ActiveKeyTips.Count);
        Assert.True(ribbon.CurrentKeyTips.Any(k => k.KeyTip == "F"), "File KeyTip");
        Assert.True(ribbon.CurrentKeyTips.Any(k => k.KeyTip == "1"), "QAT numeric KeyTip");
        var homeTip = ribbon.CurrentKeyTips.First(k => k.Target is RibbonTabHeader { Tab.Id: "home" }).KeyTip;
        foreach (var c in homeTip)
        {
            ribbon.ProcessKeyTipInput(c);
        }

        await Settle();
        Assert.True(ribbon.CurrentKeyTips.Any(k => k.KeyTip == "1" && k.Target is RibbonToggleButton { Id: "bold" }), "Explicit KeyTip '1' for Bold at level 2");
        ribbon.ProcessKeyTipInput('1');
        await Settle();
        Assert.True(ribbon.Item<RibbonToggleButton>("bold").IsChecked == true, "Bold toggled by KeyTip");
        Assert.False(ribbon.IsKeyTipMode, "KeyTip mode ends after invoking");
    }

    [RibbonTest]
    public async Task Escape_pops_levels()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        ribbon.ShowKeyTips();
        await Settle();
        ribbon.ProcessKeyTipInput('H');
        await Settle();
        Assert.True(ribbon.PopKeyTipLevel(), "Back to level 1");
        Assert.False(ribbon.PopKeyTipLevel(), "Exit");
        Assert.False(ribbon.IsKeyTipMode, "Ended");
    }
}

public sealed class SearchAndBackstageTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Search_finds_and_executes_items()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var results = ribbon.Search("bold");
        Assert.True(results.Count > 0 && results[0].Entry.Label == "Bold", "Bold should rank first");
        Assert.Equal("Home › Font", results[0].Entry.Path);
        Assert.True(ribbon.ExecuteSearchEntry(results[0].Entry), "Execute");
        Assert.True(ribbon.Item<RibbonToggleButton>("bold").IsChecked == true, "Bold toggled by search");
        Assert.True(ribbon.Search("").Any(r => r.Entry.Label == "Bold"), "Recent entries");
        Assert.True(ribbon.Search("info").Any(r => r.Entry.Target is RibbonBackstageItem), "Backstage items searchable");
    }

    [RibbonTest]
    public async Task Backstage_opens_selects_first_page_and_closes()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var opened = false;
        ribbon.BackstageOpened += (_, _) => opened = true;
        ribbon.InvokeApplicationButton();
        await Settle();
        Assert.True(ribbon.IsBackstageOpen && opened, "Backstage open");
        Assert.Equal("info", ribbon.Backstage!.SelectedItem?.Id);
        ribbon.Backstage.Invoke(ribbon.Backstage.Items[1]);
        await Settle();
        Assert.False(ribbon.IsBackstageOpen, "Action item closes the backstage");
    }
}

public sealed class InputTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Combo_box_commit_text_selects_matching_item()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var size = ribbon.Item<RibbonFontSizeComboBox>("fontSize");
        RibbonComboBoxCommittedEventArgs? committed = null;
        size.Committed += (_, e) => committed = e;
        size.CommitText("14");
        Assert.Equal(14d, size.SelectedItem);
        size.CommitText("13.5");
        Assert.True(size.SelectedItem is null && committed?.Text == "13.5", "Free text accepted");
    }

    [RibbonTest]
    public async Task Spinner_clamps_and_formats_units()
    {
        var spinner = await Mount(new RibbonSpinner { Label = "Left:", Minimum = 0, Maximum = 10, Unit = "cm", Format = "0.0", Value = 2 });
        spinner.Value = 42;
        Assert.Equal(10d, spinner.Value);
        Assert.Equal("10.0 cm", spinner.FormatValue(spinner.Value).Replace(',', '.'));
    }

    [RibbonTest]
    public async Task Gallery_pick_executes_command_and_adapts_columns()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var gallery = ribbon.Item<RibbonGallery>("stylesGallery");
        object? executed = null;
        gallery.Command = new RibbonRelayCommand(p => executed = p);
        Assert.Equal(5, gallery.Columns);
        gallery.Pick(gallery.Items[2]);
        Assert.Equal("Heading 1", executed);
        Assert.Equal("Heading 1", gallery.SelectedItem);
        var width = ribbon.SelectedTab!.GroupsPanel.ExtentWidth;
        await Resize(ribbon, width - 30);
        Assert.Equal(2, gallery.Columns, "Medium state uses MinColumns");
    }

    [RibbonTest]
    public async Task Color_picker_reports_color()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var picker = ribbon.Item<RibbonColorPicker>("fontColor");
        Windows.UI.Color? picked = null;
        picker.ColorSelected += (_, c) => picked = c;
        picker.Palette.Select(Microsoft.UI.Colors.Green);
        Assert.Equal(Microsoft.UI.Colors.Green, picked);
        Assert.Equal(Microsoft.UI.Colors.Green, picker.SelectedColor);
        Assert.True(picker.Palette.RecentColors.Contains(Microsoft.UI.Colors.Green), "Custom colors become recent");
    }
}

public sealed class MvvmTests : RuntimeTestBase
{
    private static RibbonModel CreateModel(out RibbonToggleButtonModel bold, out RibbonContextualGroupModel chart)
    {
        var model = new RibbonModel();
        bold = new RibbonToggleButtonModel("bold", "Bold", RibbonIcons.Bold) { KeyTip = "1" };
        var home = new RibbonTabModel("home", "Home")
        {
            Groups =
            {
                new RibbonGroupModel("font", "Font") { Items = { bold, new RibbonButtonModel("cut", "Cut", RibbonIcons.Cut) } },
                new RibbonGroupModel("colors", "Colors") { Items = { new RibbonColorPickerModel("fill", "Fill"), new RibbonComboBoxModel("size", "Size", ["8", "10", "12"]) { Text = "10", IsEditable = true } } },
            },
        };
        model.Tabs.Add(home);
        model.Tabs.Add(new RibbonTabModel("view", "View") { Groups = { new RibbonGroupModel("show", "Show") { Items = { new RibbonCheckBoxModel("ruler", "Ruler") } } } });
        chart = new RibbonContextualGroupModel("chart", "Chart Tools") { Activation = RibbonContextualActivation.SelectOnShow };
        model.ContextualGroups.Add(chart);
        model.Tabs.Add(new RibbonTabModel("chartDesign", "Chart Design") { ContextualGroupId = "chart", Groups = { new RibbonGroupModel("type", "Type") { Items = { new RibbonButtonModel("changeType", "Change Type") } } } });
        return model;
    }

    [RibbonTest]
    public async Task Model_generates_elements_and_syncs_two_way()
    {
        var model = CreateModel(out var bold, out _);
        var ribbon = await Mount(new Ribbon { Model = model }, 1400);
        Assert.Equal(2, ribbon.VisibleTabs.Count);
        var element = ribbon.Item<RibbonToggleButton>("bold");
        bold.IsChecked = true;
        Assert.True(element.IsChecked == true, "Model → element");
        element.IsChecked = false;
        Assert.False(bold.IsChecked, "Element → model");
        bold.Label = "Strong";
        Assert.Equal("Strong", element.Label);
        bold.IsVisible = false;
        Assert.Equal(Visibility.Collapsed, element.Visibility);
    }

    [RibbonTest]
    public async Task Model_selection_display_mode_and_contextual_groups_sync()
    {
        var model = CreateModel(out _, out var chart);
        var ribbon = await Mount(new Ribbon { Model = model }, 1400);
        model.SelectedTabId = "view";
        Assert.Equal("view", ribbon.SelectedTab?.Id);
        ribbon.SelectTab("home");
        Assert.Equal("home", model.SelectedTabId);
        model.DisplayMode = RibbonDisplayMode.Simplified;
        Assert.Equal(RibbonDisplayMode.Simplified, ribbon.DisplayMode);
        ribbon.VisibilityMode = RibbonVisibilityMode.TabsOnly;
        Assert.Equal(RibbonVisibilityMode.TabsOnly, model.VisibilityMode);
        chart.IsVisible = true;
        await Settle();
        Assert.Equal("chartDesign", ribbon.SelectedTab?.Id);
    }

    [RibbonTest]
    public async Task Model_collections_regenerate_and_qat_syncs_back()
    {
        var model = CreateModel(out var bold, out _);
        var ribbon = await Mount(new Ribbon { Model = model }, 1400);
        model.Tabs.Add(new RibbonTabModel("review", "Review"));
        await Settle();
        Assert.NotNull(ribbon.FindTab("review"), "New tab generated");
        model.FindGroup("font")!.Items.Add(new RibbonButtonModel("italic", "Italic", RibbonIcons.Italic));
        await Settle();
        Assert.NotNull(ribbon.FindItem("italic"), "New item generated");
        ribbon.AddToQuickAccess("bold");
        await Settle();
        Assert.True(model.QuickAccessItems.Contains(bold), "UI QAT change syncs to the model");
        model.QuickAccessItems.Clear();
        await Settle();
        Assert.Equal(0, ribbon.GetQuickAccessItemIds().Count);
    }

    [RibbonTest]
    public async Task Model_inputs_sync()
    {
        var model = CreateModel(out _, out _);
        var ribbon = await Mount(new Ribbon { Model = model }, 1400);
        var sizeModel = (RibbonComboBoxModel)model.FindItem("size")!;
        var size = ribbon.Item<RibbonComboBox>("size");
        size.CommitText("12");
        Assert.Equal("12", sizeModel.SelectedItem as string);
        var fillModel = (RibbonColorPickerModel)model.FindItem("fill")!;
        ribbon.Item<RibbonColorPicker>("fill").Palette.Select(Microsoft.UI.Colors.Blue);
        Assert.Equal(RibbonColor.Parse("#0000FF"), fillModel.SelectedColor);
        var ruler = (RibbonCheckBoxModel)model.FindItem("ruler")!;
        ribbon.SelectTab("view");
        await Settle();
        ((IRibbonItem)ribbon.Item<RibbonCheckBox>("ruler")).Invoke();
        Assert.True(ruler.IsChecked == true, "Check box → model");
    }
}

public sealed class ThemeAndToolbarTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Palette_changes_accent_brush_in_place()
    {
        await Mount(SampleRibbon.Create(), 1400);
        var brush = RibbonTheme.Resources.GetBrush("RibbonAccentBrush")!;
        RibbonTheme.ApplyPalette(RibbonThemePalette.Excel);
        Assert.Equal(RibbonThemePalette.Excel.Accent.ToColor(), brush.Color);
        RibbonTheme.ApplyPalette(RibbonThemePalette.Word);
        Assert.Equal(RibbonThemePalette.Word.Accent.ToColor(), brush.Color);
    }

    [RibbonTest]
    public async Task Localized_strings_are_used_by_menus()
    {
        var ribbon = await Mount(SampleRibbon.Create(), 1400);
        var previous = Localization.RibbonStrings.Current;
        try
        {
            Localization.RibbonStrings.Current = Localization.RibbonStrings.ForCulture(new System.Globalization.CultureInfo("de-DE"));
            var menu = ribbon.BuildDisplayOptionsMenu();
            Assert.True(menu.Items.OfType<Microsoft.UI.Xaml.Controls.MenuFlyoutItem>().Any(i => i.Text == "Vollbildmodus"), "German display options");
        }
        finally
        {
            Localization.RibbonStrings.Current = previous;
        }
    }

    [RibbonTest]
    public async Task Toolbar_overflows_and_vertical_columns()
    {
        var toolbar = new RibbonToolBar();
        for (var i = 0; i < 20; i++)
        {
            toolbar.Items.Add(new RibbonButton { Id = "b" + i, Label = "Button " + i, Icon = "" });
        }

        await Mount(toolbar, 240);
        Assert.True(toolbar.OverflowItems.Any(), "Narrow toolbar overflows");
        var vertical = new RibbonToolBar { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical, Columns = 2 };
        for (var i = 0; i < 6; i++)
        {
            vertical.Items.Add(new RibbonToggleButton { Label = "Tool " + i, Icon = "", GroupName = "tools" });
        }

        await Mount(vertical);
        Assert.False(vertical.OverflowItems.Any(), "Vertical palette fits");
        Assert.True(vertical.ActualWidth > vertical.ActualHeight / 3, "Two columns");
    }

    [RibbonTest]
    public async Task Title_bar_hosts_quick_access_toolbar()
    {
        var ribbon = SampleRibbon.Create();
        var titleBar = new RibbonTitleBar { Title = "Doc" };
        var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Children = { titleBar, ribbon } };
        await Mount(panel, 1400);
        titleBar.Ribbon = ribbon;
        await Settle();
        Assert.True(ribbon.IsQuickAccessHostedExternally, "Ribbon knows the QAT is hosted by the title bar");
        Assert.True(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(ribbon.QuickAccessToolBar!) is Microsoft.UI.Xaml.Controls.Border b && b.Name == "PART_QuickAccessHost", "QAT is in the title bar");
    }
}
