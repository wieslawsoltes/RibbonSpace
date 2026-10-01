using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Commands;
using RibbonSpace.Controls;
using RibbonSpace.Controls.Primitives;

namespace RibbonSpace.Uno.RuntimeTests;

public sealed class ItemTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Spinner_copy_executes_command_once_and_only_for_user_changes()
    {
        var executed = new List<object?>();
        var spinner = new RibbonSpinner { Label = "Size", Minimum = 0, Maximum = 10, Command = new RibbonRelayCommand(p => executed.Add(p)) };
        var copy = (RibbonSpinner)((IRibbonItem)spinner).CreateLinkedCopy()!;
        var panel = new StackPanel { Children = { spinner, copy } };
        await Mount(panel);

        spinner.Value = 4;
        await Settle();
        Assert.Equal(0, executed.Count, "Programmatic value changes do not execute the command");
        Assert.Equal(4d, copy.Value, "The copy follows the source");

        typeof(RibbonSpinner).GetMethod("Step", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(copy, [1d]);
        await Settle();
        Assert.Equal(1, executed.Count, "A step on the copy executes the source command once");
        Assert.Equal(5d, spinner.Value);
    }

    [RibbonTest]
    public async Task Spinner_coerces_bound_values_and_range_changes()
    {
        var spinner = await Mount(new RibbonSpinner { Minimum = 0, Maximum = 10 });
        spinner.SetValue(RibbonSpinner.ValueProperty, 25d);
        Assert.Equal(10d, spinner.Value, "Values set through the property system are clamped");
        spinner.Maximum = 5;
        Assert.Equal(5d, spinner.Value, "Lowering Maximum re-coerces the value");
    }

    [RibbonTest]
    public async Task Slider_does_not_execute_command_for_programmatic_changes()
    {
        var executed = 0;
        var slider = new RibbonSlider { Value = 30, Command = new RibbonRelayCommand(() => executed++) };
        await Mount(slider);
        slider.Value = 60;
        slider.Maximum = 50;
        await Settle();
        Assert.Equal(0, executed, "Template load, value and range changes are not user interaction");
        Assert.Equal(50d, slider.Value, "Value is coerced into the new range");
    }

    [RibbonTest]
    public async Task Non_split_color_picker_opens_palette_instead_of_applying()
    {
        var executed = 0;
        var picker = new RibbonColorPicker { IsSplit = false, Label = "Fill", Command = new RibbonRelayCommand(() => executed++) };
        await Mount(picker);
        ((IRibbonItem)picker).Invoke();
        await Settle();
        Assert.Equal(0, executed, "The color is not applied");
        Assert.True(picker.IsDropDownOpen, "The palette opens");
        picker.Flyout!.Hide();
        await Settle();
    }

    [RibbonTest]
    public async Task Gallery_element_items_survive_rebuilds()
    {
        var element = new Border { Width = 20, Height = 20 };
        var gallery = new RibbonGallery { Label = "Shapes", MaxColumns = 4 };
        gallery.Items.Add(element);
        await Mount(gallery);
        gallery.Items.Add("Text item");
        gallery.ItemTemplate = null;
        await Settle();
        Assert.True(HasAncestor<Viewbox>(element) && HasAncestor<RibbonGallery>(element), "The element is hosted again after a rebuild");
    }

    [RibbonTest]
    public async Task Icon_presenter_accepts_relative_image_paths_and_element_icons()
    {
        var presenter = await Mount(new RibbonIconPresenter { Icon = "Assets/cut.png" });
        Assert.True(presenter.HasIcon, "A relative path becomes an ms-appx image");

        var shared = new Border { Width = 10, Height = 10 };
        presenter.Icon = shared;
        presenter.IconSize = 24;
        await Settle();
        Assert.True(HasAncestor<RibbonIconPresenter>(shared), "An element icon is re-hosted when the presenter rebuilds");
    }

    [RibbonTest]
    public async Task Control_command_can_execute_combines_with_is_enabled()
    {
        var canExecute = false;
        var command = new RibbonRelayCommand(() => { }, () => canExecute);
        var split = await Mount(new RibbonSplitButton { Label = "Paste", Command = command });
        Assert.False(split.IsEnabled, "CanExecute false disables");
        canExecute = true;
        command.NotifyCanExecuteChanged();
        Assert.True(split.IsEnabled, "CanExecute true re-enables");
        canExecute = false;
        command.NotifyCanExecuteChanged();
        split.Command = null;
        Assert.True(split.IsEnabled, "Clearing the command restores the enabled state");
        split.IsEnabled = false;
        split.Command = command;
        canExecute = true;
        command.NotifyCanExecuteChanged();
        Assert.False(split.IsEnabled, "A command never enables an item the application disabled");
    }

    [RibbonTest]
    public async Task Catalog_command_does_not_replace_user_command()
    {
        var ribbon = SampleRibbon.Create();
        var catalog = new RibbonCommandCatalog();
        catalog.Register("copy", "Copy", _ => { });
        ribbon.CommandCatalog = catalog;
        var own = new RibbonRelayCommand(() => { });
        var button = ribbon.Item<RibbonButton>("copy");
        button.Command = own;
        await Mount(ribbon, 1400);
        Assert.True(ReferenceEquals(button.Command, own), "A user-set command is kept");
        button.Command = null;
        button.CommandId = null;
        button.CommandId = "copy";
        Assert.NotNull(button.Command, "The catalog command is assigned when none is set");
        button.CommandId = null;
        Assert.True(button.Command is null, "Clearing CommandId drops the catalog command");
    }

    [RibbonTest]
    public async Task Screen_tips_are_not_shared_between_item_and_copy()
    {
        var button = new RibbonButton { Label = "Cut", ScreenTip = new RibbonScreenTip { Description = "Cut it", DisabledReason = "Nothing selected" } };
        var copy = (RibbonButton)((IRibbonItem)button).CreateLinkedCopy()!;
        await Mount(new StackPanel { Children = { button, copy } });
        var tip = ToolTipService.GetToolTip(button) as RibbonScreenTip;
        var copyTip = ToolTipService.GetToolTip(copy) as RibbonScreenTip;
        Assert.NotNull(tip, "Item tooltip");
        Assert.NotNull(copyTip, "Copy tooltip");
        Assert.False(ReferenceEquals(tip, copyTip), "Each element gets its own tooltip");
        Assert.True(string.IsNullOrEmpty(tip!.DisabledReason), "No disabled reason while enabled");
        button.IsEnabled = false;
        await Settle();
        Assert.Equal("Nothing selected", (ToolTipService.GetToolTip(button) as RibbonScreenTip)?.DisabledReason);
    }

    [RibbonTest]
    public async Task Segmented_control_executes_only_for_user_selection()
    {
        var executed = 0;
        var segmented = new RibbonSegmentedControl { Command = new RibbonRelayCommand(() => executed++) };
        segmented.Segments.Add(new RibbonSegment { Label = "A" });
        segmented.Segments.Add(new RibbonSegment { Label = "B" });
        await Mount(segmented);
        segmented.SelectedIndex = 1;
        segmented.SelectedIndex = 7;
        Assert.Equal(0, executed, "Programmatic selection does not execute");
        ((IRibbonItem)segmented).Invoke();
        Assert.Equal(1, executed, "User selection executes");
    }

    [RibbonTest]
    public async Task Linked_toggle_copy_keeps_radio_semantics()
    {
        var left = new RibbonToggleButton { Label = "Left", GroupName = "align", IsChecked = true };
        var copy = (RibbonToggleButton)((IRibbonItem)left).CreateLinkedCopy()!;
        await Mount(new StackPanel { Children = { left, copy } });
        Assert.Equal("align", copy.GroupName, "GroupName is linked");
        ((IRibbonItem)copy).Invoke();
        await Settle();
        Assert.True(left.IsChecked == true, "Clicking the checked copy keeps the radio member checked");
    }

    private static bool HasAncestor<T>(DependencyObject element)
    {
        // A Viewbox child's visual parent is an internal container: walk visual and logical parents.
        var current = VisualTreeHelper.GetParent(element) ?? (element as FrameworkElement)?.Parent;
        while (current is not null)
        {
            if (current is T)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
        }

        return false;
    }
}
