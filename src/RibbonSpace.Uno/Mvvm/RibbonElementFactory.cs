using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Layout;
using RibbonSpace.Model;
using RibbonSpace.Theming;
using Windows.UI;

namespace RibbonSpace.Controls.Mvvm;

/// <summary>
/// Creates ribbon elements from <see cref="RibbonModel"/> nodes and keeps them synchronized (two-way where the user
/// can change state: checked, selection, text, values, visibility of contextual groups...). Collections are applied
/// incrementally: elements of models that stay in a collection keep their identity, bindings and linked copies.
/// Override the virtual methods to customize or add element types.
/// </summary>
/// <remarks>
/// Commands are executed by the generated controls (exactly once per user action). Galleries receive the clicked
/// item's <see cref="RibbonGalleryItemModel.Value"/> (or the item) and colour pickers a <see cref="RibbonColor"/>
/// (<c>null</c> for "No Color"), whether the command comes from <see cref="RibbonItemModel.Command"/> or from the
/// catalog through <see cref="RibbonItemModel.CommandId"/>.
/// </remarks>
public class RibbonElementFactory
{
    private static readonly ConditionalWeakTable<DependencyObject, RibbonNodeModel> Models = new();
    private readonly ChangeToken _commands = new();

    /// <summary>Owning ribbon (for templates and the command catalog).</summary>
    public Ribbon? Ribbon { get; internal set; }

    /// <summary>Selects templates for <see cref="RibbonCustomItemModel"/> and backstage page contents.</summary>
    public DataTemplateSelector? ContentTemplateSelector { get; set; }

    /// <summary>Returns the model an element was created from.</summary>
    public static RibbonNodeModel? GetModel(DependencyObject element) => Models.TryGetValue(element, out var model) ? model : null;

    /// <summary>Associates an element with its model.</summary>
    protected static void SetModel(DependencyObject element, RibbonNodeModel model) => Models.AddOrUpdate(element, model);

    /// <summary>
    /// Re-resolves every <c>CommandId</c> reference of the generated elements (called by the ribbon when its command
    /// catalog changes).
    /// </summary>
    public void InvalidateCommands() => _commands.Raise();

    /// <summary>Creates a tab.</summary>
    public virtual RibbonTab CreateTab(RibbonTabModel model, RibbonModelBindings bindings)
    {
        var tab = new RibbonTab();
        SetModel(tab, model);
        var explicitAutomationId = false;
        bindings.OneWay(model, () =>
        {
            tab.Id = model.Id;
            tab.Header = model.Label;
            tab.KeyTip = model.KeyTip;
            tab.IsTabVisible = model.IsVisible;
            tab.IsEnabled = model.IsEnabled;
            tab.ContextualGroupId = model.ContextualGroupId;
            tab.Icon = model.Icon ?? model.LargeIcon;
            tab.ScreenTip = model.ScreenTip is null ? model.Description : RibbonScreenTipService.Create(model.Label, model.ScreenTip, null);
            ApplyAutomationId(tab, model.AutomationId, ref explicitAutomationId);
        });
        WatchScreenTip(bindings, model, () => tab.ScreenTip = RibbonScreenTipService.Create(model.Label, model.ScreenTip, null));
        BindKeywords(tab, model, bindings);
        bindings.Items(model.Groups, tab.Groups, CreateGroup, changed: OnStructureChanged);
        return tab;
    }

    /// <summary>Creates a group.</summary>
    public virtual RibbonGroup CreateGroup(RibbonGroupModel model, RibbonModelBindings bindings)
    {
        var group = new RibbonGroup();
        SetModel(group, model);
        var explicitAutomationId = false;
        bindings.OneWay(model, () =>
        {
            group.Id = model.Id;
            group.Header = model.Label;
            group.Icon = model.LargeIcon ?? model.Icon;
            group.KeyTip = model.KeyTip;
            group.IsGroupVisible = model.IsVisible;
            group.IsEnabled = model.IsEnabled;
            group.ReductionOrder = model.ReductionOrder;
            group.CanCollapse = model.CanCollapse;
            group.ItemsLayout = model.ItemsLayout;
            group.RowCount = model.RowCount;
            group.SimplifiedVisibility = model.SimplifiedVisibility;
            group.DialogLauncherScreenTip = model.DialogLauncherScreenTip;
            ApplyAutomationId(group, model.AutomationId, ref explicitAutomationId);

            // Shown on the collapsed-group button and exposed as the group's automation help text.
            group.ScreenTip = (object?)model.ScreenTip ?? model.Description;
        });
        BindCommands(bindings, model, () =>
        {
            group.DialogLauncherCommand = model.DialogLauncherCommand ?? ResolveCommand(model.DialogLauncherCommandId);
            group.IsDialogLauncherVisible = model.DialogLauncherCommand is not null || model.DialogLauncherCommandId is not null;
        }, nameof(RibbonGroupModel.DialogLauncherCommand), nameof(RibbonGroupModel.DialogLauncherCommandId));
        WatchScreenTip(bindings, model, () =>
        {
            // Same instance with changed content: reset so the collapsed button rebuilds its tooltip.
            group.ScreenTip = null;
            group.ScreenTip = (object?)model.ScreenTip ?? model.Description;
        });
        WatchInner(bindings, model, () => model.DialogLauncherScreenTip, nameof(RibbonGroupModel.DialogLauncherScreenTip), () =>
        {
            // Same instance: reset so the launcher rebuilds its tooltip from the changed content.
            group.DialogLauncherScreenTip = null;
            group.DialogLauncherScreenTip = model.DialogLauncherScreenTip;
        });
        BindKeywords(group, model, bindings);
        bindings.Items<RibbonItemModel, UIElement>(model.Items, group.Items, CreateItem, changed: OnStructureChanged);
        bindings.Items<RibbonItemModel, UIElement>(model.SlideOutItems, group.SlideOutItems, CreateItem, changed: OnStructureChanged);
        return group;
    }

    /// <summary>Creates the element for an item model (returns null for unsupported models).</summary>
    public virtual FrameworkElement? CreateItem(RibbonItemModel model, RibbonModelBindings bindings)
    {
        FrameworkElement? element = model switch
        {
            RibbonColorPickerModel m => CreateColorPicker(m, bindings),
            RibbonGalleryModel m => CreateGallery(m, bindings),
            RibbonSplitButtonModel m => CreateSplitButton(m, bindings),
            RibbonDropDownButtonModel m => CreateDropDownButton(m, bindings),
            RibbonToggleButtonModel m => CreateToggleButton(m, bindings),
            RibbonButtonModel m => CreateButton(m, bindings),
            RibbonCheckBoxModel m => CreateCheckBox(m, bindings),
            RibbonComboBoxModel m => CreateComboBox(m, bindings),
            RibbonSpinnerModel m => CreateSpinner(m, bindings),
            RibbonTextBoxModel m => CreateTextBox(m, bindings),
            RibbonSliderModel m => CreateSlider(m, bindings),
            RibbonLabelModel => new RibbonLabel(),
            RibbonSeparatorModel => new RibbonSeparator(),
            RibbonRowModel m => CreateContainer(new RibbonStackPanel(), m, bindings),
            RibbonButtonGroupModel m => CreateContainer(new RibbonButtonGroup(), m, bindings),
            RibbonGridPickerModel m => CreateGridPicker(m, bindings),
            RibbonSegmentedModel m => CreateSegmented(m, bindings),
            RibbonCustomItemModel m => CreateCustom(m, bindings),
            _ => null,
        };
        if (element is null)
        {
            return null;
        }

        SetModel(element, model);
        BindCommon(element, model, bindings);
        return element;
    }

    /// <summary>
    /// Binds the properties shared by all items. Generated combo boxes never show their label (it is used for
    /// ScreenTips, search and automation), matching Office.
    /// </summary>
    protected virtual void BindCommon(FrameworkElement element, RibbonItemModel model, RibbonModelBindings bindings)
    {
        var explicitAutomationId = false;
        bindings.OneWay(model, () =>
        {
            if (element is IRibbonItemEditable item)
            {
                item.Id = model.Id;
                item.Label = model.Label;
                item.Icon = model.Icon;
                item.LargeIcon = model.LargeIcon;
                item.Size = model.Size;
                item.SizeDefinition = model.SizeDefinition?.ToString();
                item.KeyTip = model.KeyTip;
                item.ScreenTip = (object?)model.ScreenTip ?? model.Description;

                // The table picker host only opens its drop-down: the command belongs to the hosted picker.
                item.CommandId = model is RibbonGridPickerModel ? null : model.CommandId;
                item.Shortcut = model.Shortcut;
                item.SimplifiedVisibility = model.SimplifiedVisibility;
                item.SimplifiedLabel = model.ShowLabelInSimplified switch { true => RibbonSimplifiedLabel.Show, false => RibbonSimplifiedLabel.Hide, _ => RibbonSimplifiedLabel.Auto };
                item.ShowLabel = model.ShowLabel && element is not RibbonComboBox;
                item.CanAddToQuickAccess = model.CanAddToQuickAccess;
            }

            ApplyAutomationId(element, model.AutomationId, ref explicitAutomationId);
        });
        bindings.OneWay(model, () => element.Visibility = model.IsVisible ? Visibility.Visible : Visibility.Collapsed, nameof(RibbonNodeModel.IsVisible));

        // The model state is always applied; the command / catalog state is combined by the item and command layer.
        if (element is Control control)
        {
            bindings.OneWay(model, () => control.IsEnabled = model.IsEnabled, nameof(RibbonNodeModel.IsEnabled));
        }

        if (element is IRibbonCommandSource source && model is not RibbonGridPickerModel)
        {
            BindCommands(bindings, model, () =>
            {
                source.Command = CreateItemCommand(element, model);
                if (element is not RibbonColorPicker)
                {
                    // Colour pickers use the selected colour as their parameter.
                    source.CommandParameter = model.CommandParameter;
                }
            }, nameof(RibbonItemModel.Command), nameof(RibbonItemModel.CommandId), nameof(RibbonItemModel.CommandParameter));
            if (element is RibbonControlBase templated && ParameterConverter(element) is { } convert)
            {
                // The catalog may assign its raw command to the control: wrap it so the parameter convention stays the same.
                var token = templated.RegisterPropertyChangedCallback(RibbonControlBase.CommandProperty, (_, _) =>
                {
                    if (templated.Command is { } command and not ParameterCommand)
                    {
                        templated.Command = new ParameterCommand(command, convert);
                    }
                });
                bindings.Add(() => templated.UnregisterPropertyChangedCallback(RibbonControlBase.CommandProperty, token));
            }
        }

        BindKeywords(element, model, bindings);
        WatchScreenTip(bindings, model, () => RibbonItemHelper.UpdateToolTip(element));
    }

    /// <summary>Resolves a catalog command.</summary>
    protected ICommand? ResolveCommand(string? id) => id is not null && Ribbon?.CommandCatalog?.Find(id)?.Command is { } command ? command : null;

    /// <summary>
    /// Applies <paramref name="apply"/> now, when one of <paramref name="properties"/> changes and whenever the command
    /// catalog changes (use for assignments calling <see cref="ResolveCommand"/>).
    /// </summary>
    protected void BindCommands(RibbonModelBindings bindings, INotifyPropertyChanged model, Action apply, params string[] properties)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        bindings.OneWay(model, apply, properties);
        bindings.Watch(_commands, apply);
    }

    /// <summary>Command assigned to an item control (galleries and colour pickers translate their parameter).</summary>
    protected virtual ICommand? CreateItemCommand(FrameworkElement element, RibbonItemModel model)
    {
        var command = model.Command ?? ResolveCommand(model.CommandId);
        return command is not null && ParameterConverter(element) is { } convert ? new ParameterCommand(command, convert) : command;
    }

    private static Func<object?, object?>? ParameterConverter(FrameworkElement element) => element switch
    {
        RibbonGallery => GalleryParameter,
        RibbonColorPicker => ColorParameter,
        _ => null,
    };

    private static object? GalleryParameter(object? parameter) => parameter is RibbonGalleryItemModel item ? item.Value ?? item : parameter;

    private static object? ColorParameter(object? parameter) => parameter switch
    {
        Color color => color.ToRibbonColor(),
        _ when ReferenceEquals(parameter, RibbonColorPicker.NoColorParameter) => null,
        _ => parameter,
    };

    /// <summary>Creates a push button.</summary>
    protected virtual FrameworkElement CreateButton(RibbonButtonModel model, RibbonModelBindings bindings) => new RibbonButton();

    /// <summary>Creates a toggle button.</summary>
    protected virtual FrameworkElement CreateToggleButton(RibbonToggleButtonModel model, RibbonModelBindings bindings)
    {
        var toggle = new RibbonToggleButton();
        bindings.OneWay(model, () => toggle.GroupName = model.GroupName, nameof(RibbonToggleButtonModel.GroupName));
        bindings.TwoWay(model, nameof(RibbonToggleButtonModel.IsChecked), () => toggle.IsChecked = model.IsChecked, toggle, ToggleButton.IsCheckedProperty, () => model.IsChecked = toggle.IsChecked == true);
        return toggle;
    }

    /// <summary>Creates a drop-down button.</summary>
    protected virtual FrameworkElement CreateDropDownButton(RibbonDropDownButtonModel model, RibbonModelBindings bindings)
    {
        var button = new RibbonDropDownButton();
        BindFlyout(model, bindings, flyout => button.Flyout = flyout);
        return button;
    }

    /// <summary>Creates a split button.</summary>
    protected virtual FrameworkElement CreateSplitButton(RibbonSplitButtonModel model, RibbonModelBindings bindings)
    {
        var split = new RibbonSplitButton();
        bindings.OneWay(model, () =>
        {
            split.IsCheckable = model.IsCheckable;
            split.FollowLastChoice = model.FollowLastChoice;
        }, nameof(RibbonSplitButtonModel.IsCheckable), nameof(RibbonSplitButtonModel.FollowLastChoice));
        bindings.TwoWay(model, nameof(RibbonSplitButtonModel.IsChecked), () => split.IsChecked = model.IsChecked, split, RibbonSplitButton.IsCheckedProperty, () => model.IsChecked = split.IsChecked == true);
        BindFlyout(model, bindings, flyout => split.Flyout = flyout);
        return split;
    }

    /// <summary>
    /// Builds the drop-down of a menu button and rebuilds it (in a fresh child scope) when its structure changes: menu
    /// entries added / removed, <see cref="RibbonDropDownButtonModel.DropDownContent"/>, sub menus, checkable / radio
    /// kinds. Other properties of the entries are updated in place.
    /// </summary>
    protected void BindFlyout(RibbonDropDownButtonModel model, RibbonModelBindings bindings, Action<FlyoutBase?> assign)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(assign);
        RibbonModelBindings? scope = null;
        List<object?>? signature = null;
        void Rebuild()
        {
            var next = new List<object?> { model.DropDownContent };
            AppendMenuSignature(next, model.MenuItems);
            if (signature is not null && next.SequenceEqual(signature))
            {
                return;
            }

            signature = next;
            scope?.Dispose();
            scope = bindings.Child();
            assign(CreateFlyout(model.MenuItems, model.DropDownContent, scope));
            WatchMenuStructure(model.MenuItems, scope, Rebuild);
        }

        bindings.WatchCollection(model.MenuItems, Rebuild);
        bindings.OneWay(model, Rebuild, nameof(RibbonDropDownButtonModel.DropDownContent));
    }

    private static void AppendMenuSignature(List<object?> signature, IEnumerable<RibbonNodeModel> entries)
    {
        foreach (var entry in entries)
        {
            signature.Add(entry);
            if (entry is RibbonMenuItemModel item)
            {
                signature.Add(item.IsCheckable);
                signature.Add(item.GroupName);
                signature.Add(item.Items.Count);
                AppendMenuSignature(signature, item.Items);
            }
        }
    }

    private static void WatchMenuStructure(IEnumerable<RibbonNodeModel> entries, RibbonModelBindings scope, Action rebuild)
    {
        foreach (var item in entries.OfType<RibbonMenuItemModel>())
        {
            scope.Watch(item, rebuild, nameof(RibbonMenuItemModel.IsCheckable), nameof(RibbonMenuItemModel.GroupName));
            scope.WatchCollection(item.Items, rebuild);
            WatchMenuStructure(item.Items, scope, rebuild);
        }
    }

    /// <summary>Creates a check box.</summary>
    protected virtual FrameworkElement CreateCheckBox(RibbonCheckBoxModel model, RibbonModelBindings bindings)
    {
        var check = new RibbonCheckBox();
        bindings.OneWay(model, () => check.IsThreeState = model.IsThreeState, nameof(RibbonCheckBoxModel.IsThreeState));
        bindings.TwoWay(model, nameof(RibbonCheckBoxModel.IsChecked), () => check.IsChecked = model.IsChecked, check, ToggleButton.IsCheckedProperty, () => model.IsChecked = check.IsChecked);
        return check;
    }

    /// <summary>Creates a combo box (the control executes the command with the committed item or text).</summary>
    protected virtual FrameworkElement CreateComboBox(RibbonComboBoxModel model, RibbonModelBindings bindings)
    {
        var combo = model.PreviewFontFamily ? new RibbonFontComboBox() : new RibbonComboBox();
        combo.Items.Clear();
        combo.ItemsSource = model.Items;
        bindings.OneWay(model, () =>
        {
            combo.IsEditable = model.IsEditable;
            combo.InputWidth = model.InputWidth;
            combo.PlaceholderText = model.Placeholder;
            combo.IsFontPreview = model.PreviewFontFamily;
            combo.MaxDropDownHeight = model.MaxDropDownHeight;
            combo.DisplayMemberPath = model.DisplayMemberPath;
        }, nameof(RibbonComboBoxModel.IsEditable), nameof(RibbonComboBoxModel.InputWidth), nameof(RibbonComboBoxModel.Placeholder), nameof(RibbonComboBoxModel.PreviewFontFamily), nameof(RibbonComboBoxModel.MaxDropDownHeight), nameof(RibbonComboBoxModel.DisplayMemberPath));
        bindings.Watch(model, () =>
        {
            if ((combo is RibbonFontComboBox) != model.PreviewFontFamily)
            {
                // Font previews use a different control type.
                bindings.RequestRecreate();
            }
        }, nameof(RibbonComboBoxModel.PreviewFontFamily));
        bindings.TwoWay(model, nameof(RibbonComboBoxModel.SelectedItem), () => combo.SelectedItem = model.SelectedItem, combo, RibbonComboBox.SelectedItemProperty, () => model.SelectedItem = combo.SelectedItem);
        bindings.TwoWay(model, nameof(RibbonComboBoxModel.Text), () => combo.Text = model.Text, combo, RibbonComboBox.TextProperty, () => model.Text = combo.Text);
        return combo;
    }

    /// <summary>Creates a spinner.</summary>
    protected virtual FrameworkElement CreateSpinner(RibbonSpinnerModel model, RibbonModelBindings bindings)
    {
        var spinner = new RibbonSpinner();
        bindings.OneWay(model, () =>
        {
            spinner.Minimum = model.Minimum;
            spinner.Maximum = model.Maximum;
            spinner.Increment = model.Increment;
            spinner.Format = model.Format;
            spinner.Unit = model.Unit;
            spinner.InputWidth = model.InputWidth;
        }, nameof(RibbonSpinnerModel.Minimum), nameof(RibbonSpinnerModel.Maximum), nameof(RibbonSpinnerModel.Increment), nameof(RibbonSpinnerModel.Format), nameof(RibbonSpinnerModel.Unit), nameof(RibbonSpinnerModel.InputWidth));
        bindings.TwoWay(model, nameof(RibbonSpinnerModel.Value), () => spinner.Value = model.Value, spinner, RibbonSpinner.ValueProperty, () => model.Value = spinner.Value);
        return spinner;
    }

    /// <summary>Creates a text box.</summary>
    protected virtual FrameworkElement CreateTextBox(RibbonTextBoxModel model, RibbonModelBindings bindings)
    {
        var box = new RibbonTextBox();
        bindings.OneWay(model, () =>
        {
            box.PlaceholderText = model.Placeholder;
            box.InputWidth = model.InputWidth;
        }, nameof(RibbonTextBoxModel.Placeholder), nameof(RibbonTextBoxModel.InputWidth));
        bindings.TwoWay(model, nameof(RibbonTextBoxModel.Text), () => box.Text = model.Text ?? string.Empty, box, RibbonTextBox.TextProperty, () => model.Text = box.Text);
        return box;
    }

    /// <summary>Creates a slider.</summary>
    protected virtual FrameworkElement CreateSlider(RibbonSliderModel model, RibbonModelBindings bindings)
    {
        var slider = new RibbonSlider();
        bindings.OneWay(model, () =>
        {
            slider.Minimum = model.Minimum;
            slider.Maximum = model.Maximum;
            slider.StepFrequency = model.StepFrequency;
            slider.InputWidth = model.SliderWidth;
        }, nameof(RibbonSliderModel.Minimum), nameof(RibbonSliderModel.Maximum), nameof(RibbonSliderModel.StepFrequency), nameof(RibbonSliderModel.SliderWidth));
        bindings.TwoWay(model, nameof(RibbonSliderModel.Value), () => slider.Value = model.Value, slider, RibbonSlider.ValueProperty, () => model.Value = slider.Value);
        return slider;
    }

    /// <summary>Creates a container (row / button group).</summary>
    protected virtual FrameworkElement CreateContainer(RibbonItemsContainer container, RibbonButtonGroupModel model, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(container);
        bindings.Items<RibbonItemModel, UIElement>(model.Items, container.Items, CreateItem, changed: OnStructureChanged);
        return container;
    }

    /// <summary>Creates a gallery (the control executes the command with the item's value).</summary>
    protected virtual FrameworkElement CreateGallery(RibbonGalleryModel model, RibbonModelBindings bindings)
    {
        var gallery = new RibbonGallery { ItemsSource = model.Items };
        bindings.OneWay(model, () =>
        {
            gallery.MinColumns = model.MinColumns;
            gallery.MaxColumns = model.MaxColumns;
            gallery.DropDownColumns = model.DropDownColumns;
            gallery.ItemWidth = model.ItemWidth;
            gallery.ItemHeight = model.ItemHeight;
            gallery.IsFilterEnabled = model.IsFilterEnabled;
            gallery.ShowItemLabels = model.ShowLabels;
            gallery.Rows = model.Rows;
        }, nameof(RibbonGalleryModel.MinColumns), nameof(RibbonGalleryModel.MaxColumns), nameof(RibbonGalleryModel.DropDownColumns), nameof(RibbonGalleryModel.ItemWidth), nameof(RibbonGalleryModel.ItemHeight), nameof(RibbonGalleryModel.IsFilterEnabled), nameof(RibbonGalleryModel.ShowLabels), nameof(RibbonGalleryModel.Rows));
        bindings.TwoWay(model, nameof(RibbonGalleryModel.SelectedItem), () => gallery.SelectedItem = model.SelectedItem, gallery, RibbonGallery.SelectedItemProperty, () => model.SelectedItem = gallery.SelectedItem as RibbonGalleryItemModel);

        // Live preview is not executed by the control (no PreviewCommand is assigned): run it here exactly once.
        void OnPreview(object? sender, RibbonGalleryItemEventArgs e) => Execute(model.PreviewCommand, GalleryParameter(e.Item));
        gallery.ItemPreview += OnPreview;
        bindings.Add(() => gallery.ItemPreview -= OnPreview);
        bindings.Items<RibbonNodeModel, UIElement>(model.MenuItems, gallery.FooterItems, CreateMenuElement);
        return gallery;
    }

    /// <summary>Creates a color picker (the control executes the command with the picked colour).</summary>
    protected virtual FrameworkElement CreateColorPicker(RibbonColorPickerModel model, RibbonModelBindings bindings)
    {
        var picker = new RibbonColorPicker();
        bindings.OneWay(model, () =>
        {
            picker.ShowAutomatic = model.ShowAutomatic;
            picker.ShowNoColor = model.ShowNoColor;
            picker.ShowMoreColors = model.ShowMoreColors;
            picker.IsSplit = model.IsSplit;
            picker.AutomaticColor = model.AutomaticColor.ToColor();
        }, nameof(RibbonColorPickerModel.ShowAutomatic), nameof(RibbonColorPickerModel.ShowNoColor), nameof(RibbonColorPickerModel.ShowMoreColors), nameof(RibbonColorPickerModel.IsSplit), nameof(RibbonColorPickerModel.AutomaticColor));
        BindPalette(picker.Palette, model, bindings);
        bindings.TwoWay(model, nameof(RibbonColorPickerModel.SelectedColor), () => picker.SelectedColor = model.SelectedColor?.ToColor(), picker, RibbonColorPicker.SelectedColorProperty, () => model.SelectedColor = picker.SelectedColor?.ToRibbonColor());
        return picker;
    }

    /// <summary>Synchronizes the colour collections of a palette with a colour picker model (recent colours two-way, at most 10).</summary>
    protected void BindPalette(RibbonColorPalette palette, RibbonColorPickerModel model, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(bindings);
        void ApplyNames(IEnumerable<Theming.RibbonColorSwatch> swatches)
        {
            foreach (var swatch in swatches)
            {
                palette.SetColorName(swatch.Color.ToColor(), swatch.Name);
            }
        }

        bindings.Collection(model.ThemeColors, () =>
        {
            ApplyNames(model.ThemeColors);
            Replace(palette.ThemeColors, model.ThemeColors.Select(s => s.Color.ToColor()));
        });
        bindings.Collection(model.StandardColors, () =>
        {
            ApplyNames(model.StandardColors);
            Replace(palette.StandardColors, model.StandardColors.Select(s => s.Color.ToColor()));
        });
        var syncing = false;
        bindings.Collection(model.RecentColors, () =>
        {
            if (syncing)
            {
                return;
            }

            syncing = true;
            try
            {
                Replace(palette.RecentColors, model.RecentColors.Take(10).Select(c => c.ToColor()));
            }
            finally
            {
                syncing = false;
            }
        });
        void OnPaletteRecentChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (syncing || bindings.IsSuspended)
            {
                return;
            }

            syncing = true;
            try
            {
                Replace(model.RecentColors, palette.RecentColors.Take(10).Select(c => c.ToRibbonColor()));
            }
            finally
            {
                syncing = false;
            }
        }

        palette.RecentColors.CollectionChanged += OnPaletteRecentChanged;
        bindings.Add(() => palette.RecentColors.CollectionChanged -= OnPaletteRecentChanged);
    }

    private static void Replace<T>(IList<T> target, IEnumerable<T> values)
    {
        var list = values.ToList();
        if (target.SequenceEqual(list))
        {
            return;
        }

        target.Clear();
        foreach (var value in list)
        {
            target.Add(value);
        }
    }

    /// <summary>Creates a table grid picker (drop-down button hosting a <see cref="RibbonGridPicker"/>).</summary>
    protected virtual FrameworkElement CreateGridPicker(RibbonGridPickerModel model, RibbonModelBindings bindings)
    {
        var picker = new RibbonGridPicker();
        var flyout = new Flyout { Content = picker };
        ApplyFlyoutStyle(flyout);
        bindings.OneWay(model, () =>
        {
            picker.Rows = model.Rows;
            picker.Columns = model.Columns;
        }, nameof(RibbonGridPickerModel.Rows), nameof(RibbonGridPickerModel.Columns));
        BindCommands(bindings, model, () => picker.Command = model.Command ?? ResolveCommand(model.CommandId), nameof(RibbonItemModel.Command), nameof(RibbonItemModel.CommandId));
        void OnPicked(object? sender, RibbonGridSize size) => flyout.Hide();
        picker.SizePicked += OnPicked;
        bindings.Add(() => picker.SizePicked -= OnPicked);
        var button = new RibbonDropDownButton { Flyout = flyout };
        return button;
    }

    /// <summary>Creates a segmented control (programmatic selection changes never execute the command).</summary>
    protected virtual FrameworkElement CreateSegmented(RibbonSegmentedModel model, RibbonModelBindings bindings)
    {
        var control = new RibbonSegmentedControl();
        var syncing = false;
        int ModelIndex() => model.SelectedSegment is null ? -1 : model.Segments.IndexOf(model.SelectedSegment);
        void ApplySelection()
        {
            var index = ModelIndex();
            if (control.SelectedIndex != index)
            {
                syncing = true;
                try
                {
                    control.SelectedIndex = index;
                }
                finally
                {
                    syncing = false;
                }
            }
        }

        RibbonModelBindings? segmentScope = null;
        bindings.Collection(model.Segments, () =>
        {
            segmentScope?.Dispose();
            segmentScope = bindings.Child();
            var scope = segmentScope;
            syncing = true;
            try
            {
                control.Segments.Clear();
                foreach (var segment in model.Segments)
                {
                    control.Segments.Add(CreateSegment(segment));
                    var captured = segment;

                    // RibbonSegment has no change notification: replace it so the control rebuilds that button.
                    scope.Watch(captured, () =>
                    {
                        var index = model.Segments.IndexOf(captured);
                        if (index >= 0 && index < control.Segments.Count)
                        {
                            control.Segments[index] = CreateSegment(captured);
                        }
                    }, nameof(RibbonNodeModel.Label), nameof(RibbonNodeModel.Icon), nameof(RibbonSegmentModel.Value));
                }
            }
            finally
            {
                syncing = false;
            }

            // Keep the selection on the same segment after the rebuild.
            ApplySelection();
        });
        bindings.OneWay(model, ApplySelection, nameof(RibbonSegmentedModel.SelectedSegment));
        var token = control.RegisterPropertyChangedCallback(RibbonSegmentedControl.SelectedIndexProperty, (_, _) =>
        {
            if (syncing || bindings.IsSuspended)
            {
                return;
            }

            var index = control.SelectedIndex;
            model.SelectedSegment = index >= 0 && index < model.Segments.Count ? model.Segments[index] : null;
        });
        bindings.Add(() => control.UnregisterPropertyChangedCallback(RibbonSegmentedControl.SelectedIndexProperty, token));
        return control;
    }

    private static RibbonSegment CreateSegment(RibbonSegmentModel segment) => new() { Label = segment.Label, Icon = segment.Icon, Value = segment.Value };

    /// <summary>Creates custom content (elements are hosted directly, other content through a template).</summary>
    protected virtual FrameworkElement CreateCustom(RibbonCustomItemModel model, RibbonModelBindings bindings)
    {
        if (model.Content is FrameworkElement element)
        {
            DetachFromParent(element);
            bindings.Watch(model, () =>
            {
                if (!ReferenceEquals(model.Content, element))
                {
                    bindings.RequestRecreate();
                }
            }, nameof(RibbonCustomItemModel.Content));
            return element;
        }

        var presenter = new ContentPresenter();
        bindings.OneWay(model, () =>
        {
            if (model.Content is FrameworkElement)
            {
                // Elements are hosted directly (they may be ribbon items taking part in the layout).
                bindings.RequestRecreate();
                return;
            }

            presenter.Content = model.Content;
            presenter.ContentTemplate = FindTemplate(model.TemplateKey, model.Content);
        }, nameof(RibbonCustomItemModel.Content), nameof(RibbonCustomItemModel.TemplateKey));
        return presenter;
    }

    /// <summary>Finds a DataTemplate by key (ribbon resources, application resources) or through the selector.</summary>
    protected DataTemplate? FindTemplate(string? key, object? content)
    {
        if (key is not null)
        {
            if (Ribbon?.Resources.TryGetValue(key, out var local) == true && local is DataTemplate t1)
            {
                return t1;
            }

            if (Application.Current.Resources.TryGetValue(key, out var global) && global is DataTemplate t2)
            {
                return t2;
            }
        }

        return ContentTemplateSelector?.SelectTemplate(content);
    }

    /// <summary>Creates a flyout for menu entries (MenuFlyout when possible, otherwise a rich Flyout).</summary>
    public virtual FlyoutBase? CreateFlyout(IReadOnlyList<RibbonNodeModel> entries, object? dropDownContent, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(bindings);
        if (entries.Count == 0 && dropDownContent is null)
        {
            return null;
        }

        var simple = dropDownContent is null && entries.All(e => e is RibbonMenuItemModel or RibbonMenuSeparatorModel or RibbonMenuHeaderModel);
        if (simple)
        {
            var menu = new MenuFlyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
            foreach (var item in CreateMenuItems(entries, bindings))
            {
                menu.Items.Add(item);
            }

            return menu;
        }

        var panel = new StackPanel { Spacing = 1, MinWidth = 180 };
        var flyout = new Flyout { Content = panel, Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
        ApplyFlyoutStyle(flyout);
        if (dropDownContent is UIElement element)
        {
            // The content survives flyout rebuilds: detach it from the previous flyout first.
            DetachFromParent(element);
            panel.Children.Add(element);
            bindings.Add(() =>
            {
                if (panel.Children.Contains(element))
                {
                    panel.Children.Remove(element);
                }
            });
        }
        else if (dropDownContent is not null)
        {
            panel.Children.Add(new ContentPresenter { Content = dropDownContent, ContentTemplate = ContentTemplateSelector?.SelectTemplate(dropDownContent) });
        }

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case RibbonMenuSeparatorModel separator:
                    var line = new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Margin = new Thickness(4) };
                    RibbonTheme.SetThemeBrush(line, Microsoft.UI.Xaml.Shapes.Shape.FillProperty, "RibbonSeparatorBrush");
                    bindings.OneWay(separator, () => line.Visibility = separator.IsVisible ? Visibility.Visible : Visibility.Collapsed, nameof(RibbonNodeModel.IsVisible));
                    panel.Children.Add(line);
                    break;
                case RibbonMenuHeaderModel header:
                    var text = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(8, 4, 8, 2) };
                    bindings.OneWay(header, () =>
                    {
                        text.Text = header.Label ?? string.Empty;
                        text.Visibility = header.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                    }, nameof(RibbonNodeModel.Label), nameof(RibbonNodeModel.IsVisible));
                    panel.Children.Add(text);
                    break;
                case RibbonMenuItemModel item:
                    if (CreateMenuElement(item, bindings) is { } menuElement)
                    {
                        if (menuElement is ButtonBase button && item.Items.Count == 0)
                        {
                            button.Click += (_, _) => flyout.Hide();
                        }

                        panel.Children.Add(menuElement);
                    }

                    break;
                case RibbonGridPickerModel grid:
                    var picker = new RibbonGridPicker();
                    bindings.OneWay(grid, () =>
                    {
                        picker.Rows = grid.Rows;
                        picker.Columns = grid.Columns;
                        picker.Visibility = grid.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                        picker.IsEnabled = grid.IsEnabled;
                    }, nameof(RibbonGridPickerModel.Rows), nameof(RibbonGridPickerModel.Columns), nameof(RibbonNodeModel.IsVisible), nameof(RibbonNodeModel.IsEnabled));
                    BindCommands(bindings, grid, () => picker.Command = grid.Command ?? ResolveCommand(grid.CommandId), nameof(RibbonItemModel.Command), nameof(RibbonItemModel.CommandId));
                    picker.SizePicked += (_, _) => flyout.Hide();
                    panel.Children.Add(picker);
                    break;
                case RibbonColorPickerModel colors:
                    panel.Children.Add(CreateFlyoutPalette(colors, flyout, bindings));
                    break;
                case RibbonItemModel itemModel when CreateItem(itemModel, bindings) is { } itemElement:
                    if (itemElement is IRibbonItem ri)
                    {
                        ri.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Ribbon?.Metrics ?? RibbonMetrics.Comfortable, false, true));
                    }

                    panel.Children.Add(itemElement);
                    break;
            }
        }

        return flyout;
    }

    private RibbonColorPalette CreateFlyoutPalette(RibbonColorPickerModel colors, Flyout flyout, RibbonModelBindings bindings)
    {
        var palette = new RibbonColorPalette();
        bindings.OneWay(colors, () =>
        {
            palette.ShowAutomatic = colors.ShowAutomatic;
            palette.ShowNoColor = colors.ShowNoColor;
            palette.ShowMoreColors = colors.ShowMoreColors;
            palette.AutomaticColor = colors.AutomaticColor.ToColor();
            palette.SelectedColor = colors.SelectedColor?.ToColor();
            palette.Visibility = colors.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            palette.IsEnabled = colors.IsEnabled;
        }, nameof(RibbonColorPickerModel.ShowAutomatic), nameof(RibbonColorPickerModel.ShowNoColor), nameof(RibbonColorPickerModel.ShowMoreColors), nameof(RibbonColorPickerModel.AutomaticColor), nameof(RibbonColorPickerModel.SelectedColor), nameof(RibbonNodeModel.IsVisible), nameof(RibbonNodeModel.IsEnabled));
        BindPalette(palette, colors, bindings);

        // No control executes this palette's command: it is executed here, once, with the picked RibbonColor.
        void OnSelected(object? sender, Color? color)
        {
            colors.SelectedColor = color?.ToRibbonColor();
            Execute(colors.Command ?? ResolveCommand(colors.CommandId), color?.ToRibbonColor());
            flyout.Hide();
        }

        async void OnMoreColors(object? sender, EventArgs e)
        {
            var root = palette.XamlRoot;
            var theme = palette.ActualTheme;
            flyout.Hide();
            try
            {
                if (await RibbonColorDialog.ShowAsync(root, palette.SelectedColor ?? Microsoft.UI.Colors.Black, theme) is { } picked)
                {
                    palette.Select(picked);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Another dialog is open (only one ContentDialog may be shown at a time).
            }
        }

        palette.ColorSelected += OnSelected;
        palette.MoreColorsRequested += OnMoreColors;
        bindings.Add(() =>
        {
            palette.ColorSelected -= OnSelected;
            palette.MoreColorsRequested -= OnMoreColors;
        });
        return palette;
    }

    /// <summary>Creates menu entries from menu models.</summary>
    public virtual IEnumerable<MenuFlyoutItemBase> CreateMenuItems(IEnumerable<RibbonNodeModel> entries, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(bindings);
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case RibbonMenuSeparatorModel separator:
                    var line = new MenuFlyoutSeparator();
                    bindings.OneWay(separator, () => line.Visibility = separator.IsVisible ? Visibility.Visible : Visibility.Collapsed, nameof(RibbonNodeModel.IsVisible));
                    yield return line;
                    break;
                case RibbonMenuHeaderModel header:
                    var title = new MenuFlyoutItem { IsEnabled = false, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
                    bindings.OneWay(header, () =>
                    {
                        title.Text = header.Label ?? string.Empty;
                        title.Visibility = header.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                    }, nameof(RibbonNodeModel.Label), nameof(RibbonNodeModel.IsVisible));
                    yield return title;
                    break;
                case RibbonMenuItemModel item when item.Items.Count > 0:
                    var sub = new MenuFlyoutSubItem();
                    SetModel(sub, item);
                    bindings.OneWay(item, () =>
                    {
                        sub.Text = item.Label ?? string.Empty;
                        sub.Icon = RibbonItemHelper.CreateMenuIcon(item.Icon);
                        sub.IsEnabled = item.IsEnabled;
                        sub.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                        ApplyMenuDecorations(sub, item);
                    });
                    WatchScreenTip(bindings, item, () => ApplyMenuDecorations(sub, item));
                    foreach (var child in CreateMenuItems(item.Items, bindings))
                    {
                        sub.Items.Add(child);
                    }

                    yield return sub;
                    break;
                case RibbonMenuItemModel item:
                    yield return CreateMenuItem(item, bindings);
                    break;
            }
        }
    }

    private static void ApplyMenuDecorations(MenuFlyoutItemBase element, RibbonMenuItemModel item)
    {
        RibbonKeyTip.SetKeyTip(element, item.KeyTip);
        var tip = item.ScreenTip is not null ? RibbonScreenTipService.Create(item.Label, item.ScreenTip, item.Shortcut) : null;
        ToolTipService.SetToolTip(element, tip);
        var help = item.ScreenTip?.Description ?? item.Description;
        if (string.IsNullOrEmpty(help))
        {
            element.ClearValue(AutomationProperties.HelpTextProperty);
        }
        else
        {
            AutomationProperties.SetHelpText(element, help);
        }
    }

    /// <summary>Creates one menu item.</summary>
    protected virtual MenuFlyoutItem CreateMenuItem(RibbonMenuItemModel item, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(bindings);
        MenuFlyoutItem menuItem = item.IsCheckable
            ? item.GroupName is { } groupName ? new RadioMenuFlyoutItem { GroupName = groupName } : new ToggleMenuFlyoutItem()
            : new MenuFlyoutItem();
        SetModel(menuItem, item);
        bindings.OneWay(item, () =>
        {
            menuItem.Text = item.Label ?? string.Empty;
            menuItem.Icon = RibbonItemHelper.CreateMenuIcon(item.Icon);
            menuItem.IsEnabled = item.IsEnabled;
            menuItem.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            menuItem.KeyboardAcceleratorTextOverride = item.Shortcut ?? string.Empty;
            ApplyMenuDecorations(menuItem, item);
            if (menuItem is ToggleMenuFlyoutItem toggle)
            {
                toggle.IsChecked = item.IsChecked;
            }
            else if (menuItem is RadioMenuFlyoutItem radio)
            {
                radio.IsChecked = item.IsChecked;
            }
        });
        WatchScreenTip(bindings, item, () => ApplyMenuDecorations(menuItem, item));
        BindCommands(bindings, item, () =>
        {
            menuItem.Command = item.Command ?? ResolveCommand(item.CommandId);
            menuItem.CommandParameter = item.CommandParameter;
        }, nameof(RibbonMenuItemModel.Command), nameof(RibbonMenuItemModel.CommandId), nameof(RibbonMenuItemModel.CommandParameter));
        menuItem.Click += (_, _) =>
        {
            if (menuItem is ToggleMenuFlyoutItem toggle)
            {
                item.IsChecked = toggle.IsChecked;
            }
            else if (menuItem is RadioMenuFlyoutItem radio)
            {
                item.IsChecked = radio.IsChecked;
            }

            Ribbon?.OnItemInvoked(menuItem, item.CommandId, item.CommandParameter);
        };
        RibbonMenu.SetInvoke(menuItem, () =>
        {
            Execute(item.Command ?? ResolveCommand(item.CommandId), item.CommandParameter);
            if (item.IsCheckable)
            {
                item.IsChecked = item.GroupName is not null || !item.IsChecked;
            }

            Ribbon?.OnItemInvoked(menuItem, item.CommandId, item.CommandParameter);
        });
        return menuItem;
    }

    /// <summary>
    /// Creates the element of a menu entry in rich flyouts and gallery footers: a button, a toggle button for
    /// checkable entries or a drop-down button for entries with sub items.
    /// </summary>
    protected virtual FrameworkElement? CreateMenuElement(RibbonNodeModel entry, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        if (entry is not RibbonMenuItemModel item)
        {
            return entry is RibbonMenuSeparatorModel ? CreateMenuSeparatorElement(entry, bindings) : null;
        }

        // Structural changes swap the element kind.
        bindings.Watch(item, bindings.RequestRecreate, nameof(RibbonMenuItemModel.IsCheckable), nameof(RibbonMenuItemModel.GroupName));
        bindings.WatchCollection(item.Items, bindings.RequestRecreate);
        WatchMenuStructure(item.Items, bindings, bindings.RequestRecreate);
        if (item.IsCheckable)
        {
            var toggle = new RibbonToggleButton { HorizontalAlignment = HorizontalAlignment.Stretch, CanAddToQuickAccess = false };
            SetModel(toggle, item);
            BindMenuElement(toggle, item, bindings);
            bindings.OneWay(item, () => toggle.GroupName = item.GroupName, nameof(RibbonMenuItemModel.GroupName));
            bindings.TwoWay(item, nameof(RibbonMenuItemModel.IsChecked), () => toggle.IsChecked = item.IsChecked, toggle, ToggleButton.IsCheckedProperty, () => item.IsChecked = toggle.IsChecked == true);
            toggle.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Ribbon?.Metrics ?? RibbonMetrics.Comfortable, false, true));
            return toggle;
        }

        if (item.Items.Count > 0)
        {
            var dropDown = new RibbonDropDownButton { HorizontalAlignment = HorizontalAlignment.Stretch, CanAddToQuickAccess = false };
            SetModel(dropDown, item);
            BindMenuElement(dropDown, item, bindings);
            var menu = new MenuFlyout { Placement = FlyoutPlacementMode.RightEdgeAlignedTop };
            foreach (var child in CreateMenuItems(item.Items, bindings))
            {
                menu.Items.Add(child);
            }

            dropDown.Flyout = menu;
            dropDown.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Ribbon?.Metrics ?? RibbonMetrics.Comfortable, false, true));
            return dropDown;
        }

        return CreateMenuButton(item, bindings);
    }

    private static FrameworkElement CreateMenuSeparatorElement(RibbonNodeModel separator, RibbonModelBindings bindings)
    {
        var line = new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Margin = new Thickness(4) };
        RibbonTheme.SetThemeBrush(line, Microsoft.UI.Xaml.Shapes.Shape.FillProperty, "RibbonSeparatorBrush");
        bindings.OneWay(separator, () => line.Visibility = separator.IsVisible ? Visibility.Visible : Visibility.Collapsed, nameof(RibbonNodeModel.IsVisible));
        return line;
    }

    /// <summary>Creates a menu-like button (rich flyouts, gallery footers).</summary>
    protected virtual RibbonButton? CreateMenuButton(RibbonNodeModel entry, RibbonModelBindings bindings)
    {
        if (entry is not RibbonMenuItemModel item)
        {
            return null;
        }

        var button = new RibbonButton { HorizontalAlignment = HorizontalAlignment.Stretch, CanAddToQuickAccess = false };
        SetModel(button, item);
        BindMenuElement(button, item, bindings);
        button.ApplyLayout(new RibbonItemLayout(RibbonItemSize.Medium, Ribbon?.Metrics ?? RibbonMetrics.Comfortable, false, true));
        return button;
    }

    private void BindMenuElement(FrameworkElement element, RibbonMenuItemModel item, RibbonModelBindings bindings)
    {
        bindings.OneWay(item, () =>
        {
            if (element is IRibbonItemEditable editable)
            {
                editable.Id = item.Id;
                editable.Label = item.Label;
                editable.Icon = item.Icon;
                editable.LargeIcon = item.LargeIcon;
                editable.Shortcut = item.Shortcut;
                editable.KeyTip = item.KeyTip;
                editable.ScreenTip = (object?)item.ScreenTip ?? item.Description;
                editable.CanAddToQuickAccess = false;
            }

            element.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            if (element is Control control)
            {
                control.IsEnabled = item.IsEnabled;
            }
        });
        if (element is IRibbonCommandSource source && item.Items.Count == 0)
        {
            BindCommands(bindings, item, () =>
            {
                source.Command = item.Command ?? ResolveCommand(item.CommandId);
                source.CommandParameter = item.CommandParameter;
            }, nameof(RibbonMenuItemModel.Command), nameof(RibbonMenuItemModel.CommandId), nameof(RibbonMenuItemModel.CommandParameter));
        }

        WatchScreenTip(bindings, item, () => RibbonItemHelper.UpdateToolTip(element));
    }

    /// <summary>Creates a backstage.</summary>
    public virtual RibbonBackstage CreateBackstage(RibbonBackstageModel model, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(bindings);
        var backstage = new RibbonBackstage();
        bindings.OneWay(model, () => backstage.Title = model.Title, nameof(RibbonBackstageModel.Title));
        var updatingItems = false;
        RibbonBackstageItem? Find(RibbonBackstageItemModel? item) => item is null ? null : backstage.Items.FirstOrDefault(i => ReferenceEquals(GetModel(i), item));
        void ApplySelection()
        {
            if (Find(model.SelectedItem) is { } selected)
            {
                backstage.SelectedItem = selected;
                return;
            }

            // The selected page is gone (or none was chosen): let the backstage pick the first page and report it.
            backstage.EnsureSelection();
            var current = backstage.SelectedItem is { } item ? GetModel(item) as RibbonBackstageItemModel : null;
            if (!ReferenceEquals(model.SelectedItem, current))
            {
                model.SelectedItem = current;
            }
        }

        bindings.Items(
            model.Items,
            backstage.Items,
            CreateBackstageItem,
            changing: () => updatingItems = true,
            changed: () =>
            {
                updatingItems = false;
                ApplySelection();
            });
        bindings.OneWay(model, ApplySelection, nameof(RibbonBackstageModel.SelectedItem));
        var token = backstage.RegisterPropertyChangedCallback(RibbonBackstage.SelectedItemProperty, (_, _) =>
        {
            if (updatingItems || bindings.IsSuspended)
            {
                return;
            }

            if (backstage.SelectedItem is { } selected && GetModel(selected) is RibbonBackstageItemModel m && !ReferenceEquals(model.SelectedItem, m))
            {
                model.SelectedItem = m;
            }
        });
        bindings.Add(() => backstage.UnregisterPropertyChangedCallback(RibbonBackstage.SelectedItemProperty, token));
        return backstage;
    }

    /// <summary>Creates a backstage navigation item.</summary>
    protected virtual RibbonBackstageItem CreateBackstageItem(RibbonBackstageItemModel model, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(bindings);
        var item = new RibbonBackstageItem();
        SetModel(item, model);
        bindings.OneWay(model, () =>
        {
            item.Id = model.Id;
            item.Header = model.Label;
            item.Icon = model.Icon;
            item.KeyTip = model.KeyTip;
            item.IsEnabled = model.IsEnabled;
            item.IsVisible = model.IsVisible;
            item.ScreenTip = model.ScreenTip is null ? model.Description : RibbonScreenTipService.Create(model.Label, model.ScreenTip, null);
            item.Placement = model.Placement;
            item.ClosesBackstage = model.ClosesBackstage;
            item.HasSeparatorBefore = model.HasSeparatorBefore;
            item.CommandParameter = model.CommandParameter;
            switch (model.Content)
            {
                case UIElement element:
                    item.ContentTemplate = null;
                    item.Content = element;
                    break;
                case { } content:
                    item.Content = content;
                    item.ContentTemplate = ContentTemplateSelector?.SelectTemplate(content);
                    break;
                default:
                    item.Content = null;
                    item.ContentTemplate = null;
                    break;
            }
        });
        WatchScreenTip(bindings, model, () => item.ScreenTip = RibbonScreenTipService.Create(model.Label, model.ScreenTip, null));
        BindCommands(bindings, model, () => item.Command = model.Command ?? ResolveCommand(model.CommandId), nameof(RibbonBackstageItemModel.Command), nameof(RibbonBackstageItemModel.CommandId));
        return item;
    }

    /// <summary>Creates a contextual tab group.</summary>
    /// <remarks><see cref="RibbonNodeModel.KeyTip"/> and <see cref="RibbonNodeModel.IsEnabled"/> are not supported by contextual groups.</remarks>
    public virtual RibbonContextualTabGroup CreateContextualGroup(RibbonContextualGroupModel model, RibbonModelBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(bindings);
        var group = new RibbonContextualTabGroup();
        SetModel(group, model);
        bindings.OneWay(model, () =>
        {
            group.Id = model.Id;
            group.Header = model.Label;
            group.Color = model.Color.ToColor();
            group.Activation = model.Activation;
            group.KeyTip = model.KeyTip;
            group.IsEnabled = model.IsEnabled;
        }, nameof(RibbonNodeModel.Id), nameof(RibbonNodeModel.Label), nameof(RibbonContextualGroupModel.Color), nameof(RibbonContextualGroupModel.Activation), nameof(RibbonNodeModel.KeyTip), nameof(RibbonNodeModel.IsEnabled));
        bindings.TwoWay(model, nameof(RibbonNodeModel.IsVisible), () => group.IsVisible = model.IsVisible, group, RibbonContextualTabGroup.IsVisibleProperty, () => model.IsVisible = group.IsVisible);
        return group;
    }

    /// <summary>Executes a command when possible.</summary>
    protected static void Execute(ICommand? command, object? parameter)
    {
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }

    /// <summary>
    /// Calls <paramref name="onChanged"/> when the content of the object returned by <paramref name="inner"/> (e.g. a
    /// <see cref="Model.RibbonScreenTip"/>) changes; follows replacements of that object through <paramref name="property"/>.
    /// </summary>
    protected static void WatchInner(RibbonModelBindings bindings, INotifyPropertyChanged model, Func<INotifyPropertyChanged?> inner, string property, Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(onChanged);
        RibbonModelBindings? scope = null;
        INotifyPropertyChanged? watched = null;
        bindings.OneWay(model, () =>
        {
            var current = inner();
            if (scope is not null && ReferenceEquals(current, watched))
            {
                return;
            }

            scope?.Dispose();
            scope = bindings.Child();
            watched = current;
            if (current is not null)
            {
                scope.Watch(current, onChanged);
            }
        }, property);
    }

    private static void WatchScreenTip(RibbonModelBindings bindings, RibbonNodeModel model, Action onChanged)
        => WatchInner(bindings, model, () => model.ScreenTip, nameof(RibbonNodeModel.ScreenTip), onChanged);

    private static void BindKeywords(DependencyObject element, RibbonNodeModel model, RibbonModelBindings bindings)
        => bindings.Collection(model.Keywords, () =>
        {
            if (model.Keywords.Count > 0)
            {
                RibbonSearch.SetKeywords(element, string.Join(",", model.Keywords));
            }
            else
            {
                element.ClearValue(RibbonSearch.KeywordsProperty);
            }
        });

    private static void ApplyAutomationId(FrameworkElement element, string? automationId, ref bool explicitValue)
    {
        if (automationId is { Length: > 0 })
        {
            AutomationProperties.SetAutomationId(element, automationId);
            explicitValue = true;
        }
        else if (explicitValue)
        {
            explicitValue = false;
            element.ClearValue(AutomationProperties.AutomationIdProperty);
            RibbonItemHelper.UpdateAutomation(element);
        }
    }

    private void OnStructureChanged() => Ribbon?.OnModelElementsChanged();

    /// <summary>Removes an element from its current parent so it can be hosted elsewhere.</summary>
    protected static void DetachFromParent(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var parent = VisualTreeHelper.GetParent(element) ?? (element as FrameworkElement)?.Parent;
        switch (parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Border border when ReferenceEquals(border.Child, element):
                border.Child = null;
                break;
            case ContentPresenter presenter when ReferenceEquals(presenter.Content, element):
                presenter.Content = null;
                break;
            case ContentControl control when ReferenceEquals(control.Content, element):
                control.Content = null;
                break;
        }
    }

    private static void ApplyFlyoutStyle(Flyout flyout)
    {
        if (Application.Current.Resources.TryGetValue("RibbonFlyoutPresenterStyle", out var style))
        {
            flyout.FlyoutPresenterStyle = (Style)style;
        }
    }

    /// <summary>Translates the parameter of a command (galleries, colour pickers) and forwards to the wrapped command.</summary>
    private sealed class ParameterCommand(ICommand inner, Func<object?, object?> convert) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => inner.CanExecuteChanged += value;
            remove => inner.CanExecuteChanged -= value;
        }

        public bool CanExecute(object? parameter) => inner.CanExecute(convert(parameter));

        public void Execute(object? parameter) => inner.Execute(convert(parameter));
    }

    private sealed class ChangeToken : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Raise() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
