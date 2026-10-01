# Migrating the Space apps

Each Space app hand-rolled a small ribbon or toolbar. This table maps their APIs to RibbonSpace.

| Existing API | RibbonSpace |
|---|---|
| `RibbonBar.AddTab(name, factory, contextual)` (TextSpace) | `RibbonTab` (+ `ContextualGroupId`) or `RibbonTabModel`; tabs are no longer rebuilt on selection |
| `RibbonBar.InsertGroup(tab, index, factory)` | `tab.Groups.Insert(index, group)` or `RibbonModelMerger` |
| `SetTabVisible(name, bool)` | `RibbonTab.IsTabVisible` or `SetActiveContextualGroups(...)` |
| `RibbonButton(glyph, label, action, large, showLabel, dropdown)` | `RibbonButton` / `RibbonDropDownButton` (`Size`, `Label`, `Icon`, `Command` or `Click`) |
| `OfficeButton.IsSelected` used as a toggle | `RibbonToggleButton.IsChecked` (+ `GroupName` for alignment groups) |
| `OfficeComboField` (font, size, indent fields) | `RibbonFontComboBox`, `RibbonFontSizeComboBox`, `RibbonComboBox`, `RibbonSpinner` |
| `ColorPalette(...).AsFlyout()` | `RibbonColorPicker` / `RibbonColorPalette` |
| `StyleGallery` | `RibbonGallery` + `RibbonGalleryItem` (`PreviewText`, `PreviewForeground`, …) |
| `TablePicker` | `RibbonGridPicker` |
| `RibbonGroup(title, launch)` / `CadRibbonPanel(title, launch)` | `RibbonGroup` (`DialogLauncherCommand` / `DialogLauncherClick`) |
| `CadRibbonButton(command, invoke, alternatives)` | `RibbonSplitButton` with a `MenuFlyout` (+ `FollowLastChoice`) |
| NoteSpace `RibbonDefinitions` records | `RibbonModel` / `RibbonTabModel` / `RibbonGroupModel` / `RibbonButtonModel` (object initializers) |
| GridSpace `WorkbookRibbon.Create()` + `CommandRequested(id)` | `RibbonModel` + `Ribbon.ItemInvoked` (`e.ItemId`) or `RibbonCommandCatalog` |
| DataSpace `SetCommandEnabled(id, bool)` | `Ribbon.SetCommandEnabled(id, bool)` / `catalog.SetEnabled` |
| `ToggleCollapsed()` / `IsCollapsed` | `ToggleMinimized()` / `IsMinimized` / `VisibilityMode` |
| `RibbonScroller` | built in (`RibbonScrollPanel`), plus adaptive resizing before scrolling |
| Title bars with Save / Undo / Redo | `RibbonTitleBar` + `RibbonQuickAccessToolBar` |
| Hand-written search dialogs (`CommandSearchAsync`, GridSpace search) | `RibbonSearchBox`, `Ribbon.Search`, `RibbonCommandPalette` |
| `CadMenuBar`, `Studio.Menu`, `CommandMenuBar` | `RibbonMenuBar` |
| Tool palettes, options bars, rails (ImageSpace, ArtSpace, VideoSpace, LightSpace, CodeSpace) | `RibbonToolBar` (`Orientation`, `Columns`), `RibbonContextualToolBar` |
| Hard-coded `OfficeTheme` / `OfficePalette` brushes | `RibbonTheme.ApplyPalette(...)`, brush keys, Light / Dark / HighContrast |
| `SKCanvasElement` path icons | `Icon="M…"` path data, or `RibbonIcon.Path(...)` |
| Automation ids `Command-{id}`, `RibbonTab{Name}` | `Id` → `AutomationId`; tab headers `RibbonTab_{id}` (set `AutomationProperties.AutomationId` explicitly to keep old ids) |

## Typical migration (GridSpace)

```csharp
// Before: records + SetTabs + CommandRequested
ribbon.SetTabs(WorkbookRibbon.Create());
ribbon.CommandRequested += RunCommand;

// After: RibbonModel + ItemInvoked (same string ids)
Ribbon.Model = WorkbookRibbonModel.Create();          // RibbonTabModel/RibbonGroupModel/RibbonButtonModel with the same ids
Ribbon.ItemInvoked += (_, e) => RunCommand(e.ItemId!);
Ribbon.Model.SetActiveContextualGroups(chartSelected ? ["chart"] : []);  // instead of rebuilding tabs
```
