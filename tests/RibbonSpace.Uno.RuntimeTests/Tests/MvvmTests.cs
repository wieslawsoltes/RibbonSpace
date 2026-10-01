using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Commands;
using RibbonSpace.Controls;
using RibbonSpace.Model;
using RibbonSpace.Theming;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>Regression tests of the MVVM layer (element factory, incremental sync, command execution).</summary>
public sealed class MvvmRegressionTests : RuntimeTestBase
{
    private static (RibbonModel Model, RibbonGroupModel Group) CreateModel(RibbonCommandCatalog? catalog = null)
    {
        var group = new RibbonGroupModel("g", "Group");
        var model = new RibbonModel { CommandCatalog = catalog };
        model.Tabs.Add(new RibbonTabModel("home", "Home") { Groups = { group } });
        return (model, group);
    }

    [RibbonTest]
    public async Task Command_items_follow_model_enabled_state_both_ways()
    {
        var catalog = new RibbonCommandCatalog();
        catalog.Register("save", "Save", _ => { });
        var (model, group) = CreateModel(catalog);
        group.Items.Add(new RibbonButtonModel("save", "Save") { CommandId = "save" });
        var ribbon = await Mount(new Ribbon { Model = model }, 1200);
        var button = ribbon.Item<RibbonButton>("save");
        model.SetCommandEnabled("save", false);
        await Settle();
        Assert.False(button.IsEnabled, "Disabled through the model");
        model.SetCommandEnabled("save", true);
        await Settle();
        Assert.True(button.IsEnabled, "Re-enabled through the model");
    }

    [RibbonTest]
    public async Task Gallery_and_color_picker_execute_catalog_command_once_with_model_parameters()
    {
        var catalog = new RibbonCommandCatalog();
        var galleryCalls = new List<object?>();
        var colorCalls = new List<object?>();
        catalog.Register("style", "Style", p => galleryCalls.Add(p));
        catalog.Register("fill", "Fill", p => colorCalls.Add(p));
        var (model, group) = CreateModel(catalog);
        var gallery = new RibbonGalleryModel("styles", "Styles") { CommandId = "style" };
        var item = new RibbonGalleryItemModel("normal", "Normal", value: "normal-style");
        gallery.Items.Add(item);
        group.Items.Add(gallery);
        group.Items.Add(new RibbonColorPickerModel("fillColor", "Fill") { CommandId = "fill" });
        var ribbon = await Mount(new Ribbon { Model = model }, 1400);
        ribbon.Item<RibbonGallery>("styles").Pick(item);
        ribbon.Item<RibbonColorPicker>("fillColor").Palette.Select(Microsoft.UI.Colors.Blue);
        await Settle();
        Assert.Equal(1, galleryCalls.Count);
        Assert.Equal<object?>("normal-style", galleryCalls[0]);
        Assert.Equal(1, colorCalls.Count);
        Assert.Equal<object?>(RibbonColor.Parse("#0000FF"), colorCalls[0]);
    }

    [RibbonTest]
    public async Task Collection_changes_keep_existing_elements_and_relink_quick_access()
    {
        var (model, group) = CreateModel();
        var cut = new RibbonButtonModel("cut", "Cut", RibbonIcons.Cut);
        group.Items.Add(cut);
        model.QuickAccessItems.Add(cut);
        var ribbon = await Mount(new Ribbon { Model = model }, 1200);
        var tab = ribbon.FindTab("home");
        var cutElement = ribbon.Item<RibbonButton>("cut");
        group.Items.Add(new RibbonButtonModel("copy", "Copy", RibbonIcons.Copy));
        model.Tabs.Add(new RibbonTabModel("view", "View"));
        await Settle();
        Assert.True(ReferenceEquals(tab, ribbon.FindTab("home")), "Tab element kept");
        Assert.True(ReferenceEquals(cutElement, ribbon.Item<RibbonButton>("cut")), "Item element kept");

        // Moving the model to another group replaces its element: the QAT copy follows the new element.
        var other = new RibbonGroupModel("g2", "Other");
        model.Tabs[0].Groups.Add(other);
        group.Items.Remove(cut);
        other.Items.Add(cut);
        await Settle();
        var newElement = ribbon.Item<RibbonButton>("cut");
        Assert.False(ReferenceEquals(cutElement, newElement), "New element for the moved model");
        var copy = ribbon.QuickAccessToolBar!.Items.OfType<FrameworkElement>().Single();
        Assert.True(ReferenceEquals(RibbonItemHelper.GetSourceItem(copy), newElement), "QAT copy re-linked");
    }

    [RibbonTest]
    public async Task Menu_rebuilds_without_reparenting_errors()
    {
        var (model, group) = CreateModel();
        var content = new TextBlock { Text = "Custom" };
        var dropDown = new RibbonDropDownButtonModel("more", "More") { DropDownContent = content };
        dropDown.MenuItems.Add(new RibbonMenuItemModel("a", "A"));
        group.Items.Add(dropDown);
        var ribbon = await Mount(new Ribbon { Model = model }, 1200);
        dropDown.MenuItems.Add(new RibbonMenuItemModel("b", "B"));
        dropDown.MenuItems.Add(new RibbonMenuItemModel("c", "C"));
        await Settle();
        var flyout = (Flyout)ribbon.Item<RibbonDropDownButton>("more").Flyout!;
        Assert.True(((StackPanel)flyout.Content).Children.Contains(content), "Content moved to the rebuilt flyout");
    }

    [RibbonTest]
    public async Task Backstage_items_added_later_replace_and_restore_xaml_backstage()
    {
        var (model, _) = CreateModel();
        var xaml = new RibbonBackstage();
        var ribbon = new Ribbon { Backstage = xaml, Model = model };
        await Mount(ribbon, 1200);
        Assert.True(ReferenceEquals(xaml, ribbon.Backstage), "Empty model keeps the XAML backstage");
        model.Backstage.Items.Add(new RibbonBackstageItemModel("info", "Info", content: "Info page"));
        await Settle();
        Assert.False(ReferenceEquals(xaml, ribbon.Backstage), "Generated backstage assigned");
        ribbon.Model = null;
        await Settle();
        Assert.True(ReferenceEquals(xaml, ribbon.Backstage), "XAML backstage restored");
    }

    [RibbonTest]
    public async Task Backstage_item_changes_refresh_navigation()
    {
        var backstage = new RibbonBackstage();
        var item = new RibbonBackstageItem { Header = "Info", Content = "Page" };
        backstage.Items.Add(item);
        await Mount(backstage, 800);
        item.Header = "Information";
        item.IsEnabled = false;
        Assert.Equal("Information", Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(item.NavigationButton!));
        Assert.False(item.NavigationButton!.IsEnabled, "Enabled state pushed to the navigation button");
    }

    [RibbonTest]
    public async Task Menu_bar_rebuild_keeps_entries()
    {
        var file = new RibbonMenuBarItem { Header = "File" };
        file.Items.Add(new MenuFlyoutItem { Text = "Open" });
        var bar = new RibbonMenuBar();
        bar.Items.Add(file);
        await Mount(bar, 600);
        bar.Items.Add(new RibbonMenuBarItem { Header = "Edit" });
        file.Items.Add(new MenuFlyoutItem { Text = "Save" });
        await Settle();
        Assert.Equal(2, file.Items.Count);
        Assert.True(bar.OpenMenu("File"), "Menu opens");
        await Settle();
        Assert.True(bar.IsMenuOpen, "Menu is open");
    }
}
