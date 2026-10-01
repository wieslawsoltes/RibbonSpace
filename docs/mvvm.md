# MVVM

Build the ribbon in a view model with `RibbonSpace.Core` and bind it:

```xml
<Ribbon Model="{x:Bind ViewModel.Ribbon}" />
```

```csharp
using RibbonSpace;
using RibbonSpace.Commands;
using RibbonSpace.Model;
using I = RibbonSpace.Model.RibbonIcons;

public sealed class EditorViewModel
{
    public RibbonCommandCatalog Commands { get; } = new();
    public RibbonToggleButtonModel Bold { get; } = new("bold", "Bold", I.Bold) { Shortcut = "Ctrl+B", KeyTip = "1" };
    public RibbonContextualGroupModel TableTools { get; } = new("table", "Table Tools") { Activation = RibbonContextualActivation.SelectOnShow };
    public RibbonModel Ribbon { get; }

    public EditorViewModel()
    {
        Commands.Register("paste", "Paste", _ => Paste(), shortcut: "Ctrl+V", icon: I.Paste);
        Ribbon = new RibbonModel { CommandCatalog = Commands };
        Ribbon.ContextualGroups.Add(TableTools);
        Ribbon.Tabs.Add(new RibbonTabModel("home", "Home")
        {
            Groups =
            {
                new RibbonGroupModel("clipboard", "Clipboard", I.Paste)
                {
                    Items =
                    {
                        new RibbonSplitButtonModel("paste", "Paste", I.Paste) { CommandId = "paste", SizeDefinition = RibbonSpace.Layout.RibbonSizeDefinition.AlwaysLarge },
                        new RibbonButtonModel("cut", "Cut", I.Cut, new RibbonRelayCommand(Cut)),
                    },
                },
                new RibbonGroupModel("font", "Font")
                {
                    ItemsLayout = RibbonGroupItemsLayout.Rows, RowCount = 2,
                    Items =
                    {
                        new RibbonRowModel("r1", new RibbonComboBoxModel("font", "Font", ["Aptos", "Arial"]) { IsEditable = true, PreviewFontFamily = true }),
                        new RibbonRowModel("r2", new RibbonButtonGroupModel("styles", Bold), new RibbonColorPickerModel("color", "Font Color")),
                    },
                },
            },
        });
        Ribbon.Tabs.Add(new RibbonTabModel("tableDesign", "Table Design") { ContextualGroupId = "table" });
    }

    public void OnSelectionChanged(bool isTable) => TableTools.IsVisible = isTable;
}
```

## What is synchronized

| Model | Element | Direction |
|---|---|---|
| Labels, icons, sizes, KeyTips, ScreenTips, shortcuts, visibility, enabled, command, parameter | all items | model → element |
| `RibbonToggleButtonModel.IsChecked`, `RibbonSplitButtonModel.IsChecked`, `RibbonCheckBoxModel.IsChecked` | toggles | two-way |
| `RibbonComboBoxModel.SelectedItem` / `Text` | combo boxes | two-way |
| `RibbonSpinnerModel.Value`, `RibbonSliderModel.Value`, `RibbonTextBoxModel.Text` | inputs | two-way |
| `RibbonColorPickerModel.SelectedColor` (+ `RecentColors`) | colour pickers | two-way |
| `RibbonGalleryModel.SelectedItem` | galleries | two-way |
| `RibbonSegmentedModel.SelectedSegment` | segmented controls | two-way |
| `RibbonContextualGroupModel.IsVisible` | contextual groups | two-way |
| `RibbonModel.SelectedTabId`, `DisplayMode`, `VisibilityMode`, `Density`, QAT options | ribbon | two-way |
| `RibbonModel.QuickAccessItems` | QAT (also "Add to QAT" in the UI) | two-way |
| `RibbonBackstageModel.IsOpen` / `SelectedItem` | backstage | two-way |
| `Tabs`, `Groups`, `Items`, `MenuItems`, `ContextualGroups`, `TabStripItems`, `Backstage.Items` | collections | model → element (regenerated) |

Model changes raised on background threads are marshalled to the UI thread.

## Menus and rich drop-downs

`RibbonDropDownButtonModel.MenuItems` / `RibbonSplitButtonModel.MenuItems` accept:
- `RibbonMenuItemModel`, which can be checkable (`GroupName` makes a radio item), carry a shortcut, or have sub-items in `Items`
- `RibbonMenuSeparatorModel`
- `RibbonMenuHeaderModel`
- `RibbonGridPickerModel`, `RibbonColorPickerModel` and any other item model

If the entries are plain menu items the factory creates a `MenuFlyout`; otherwise it creates a rich `Flyout`.
`DropDownContent` adds custom content, either an element or a view model rendered with `ContentTemplateSelector`.

## Custom content and factories

```csharp
new RibbonCustomItemModel("zoom", new ZoomViewModel()) { TemplateKey = "ZoomTemplate" }; // DataTemplate from resources
ribbon.ItemFactory = new MyFactory();   // derive from RibbonElementFactory and override CreateItem / CreateButton / …
ribbon.ItemFactory.ContentTemplateSelector = new MySelector();
```

`Ribbon.GetModel(element)` returns the model an element was generated from.

## Merging (plugins, MDI)

```csharp
var merge = RibbonModelMerger.Merge(shellModel, pluginModel); // by id; MergeAction = Merge / Add / Replace / Remove; Order
merge.Unmerge();                                               // revert
```
