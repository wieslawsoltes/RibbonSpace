# Feature research: the Space apps and Microsoft 365

RibbonSpace was extracted from, and designed to replace, the command UI of seventeen Uno Platform applications:
[VectorSpace](https://github.com/wieslawsoltes/VectorSpace), [TextSpace](https://github.com/wieslawsoltes/TextSpace),
[GridSpace](https://github.com/wieslawsoltes/GridSpace), [PdfSpace](https://github.com/wieslawsoltes/PdfSpace),
[ImageSpace](https://github.com/wieslawsoltes/ImageSpace), [LightSpace](https://github.com/wieslawsoltes/LightSpace),
[ArtSpace](https://github.com/wieslawsoltes/ArtSpace), [VideoSpace](https://github.com/wieslawsoltes/VideoSpace),
[EffectsSpace](https://github.com/wieslawsoltes/EffectsSpace), [LabSpace](https://github.com/wieslawsoltes/LabSpace),
[CodeSpace](https://github.com/wieslawsoltes/CodeSpace), [PresentationSpace](https://github.com/wieslawsoltes/PresentationSpace),
[DataSpace](https://github.com/wieslawsoltes/DataSpace), [NoteSpace](https://github.com/wieslawsoltes/NoteSpace),
[CadSpace](https://github.com/wieslawsoltes/CadSpace), [GitSpace](https://github.com/wieslawsoltes/GitSpace) and
[ControlSpace](https://github.com/wieslawsoltes/ControlSpace). The feature set was then completed against Word,
Excel and PowerPoint for Microsoft 365, and against two earlier Avalonia ribbons by the same author (RibbonControl and Ribbon).

## 1. What the apps had

Six apps have a ribbon: TextSpace, NoteSpace, GridSpace, DataSpace, PresentationSpace and CadSpace.
Each one implemented it independently, in C# code, usually in fewer than 150 lines.

| App | Ribbon implementation | Notable features | Missing |
|---|---|---|---|
| TextSpace (Word) | `RibbonBar`, `RibbonGroup`, `RibbonButton`, `StyleGallery`, `OfficeComboField`, `ColorPalette`, `TablePicker` | 10 tabs, 5 contextual tabs (Table Design/Layout, Picture/Shape Format, Equation), dialog launchers, inline style gallery, colour flyouts, table picker, backstage, in-app title bar with QAT | KeyTips, adaptive resizing, split buttons, real toggles, dark theme, `ICommand` |
| NoteSpace (OneNote) | Data-driven records `RibbonTab/Group/Command` | Declarative definitions, compact 3-row stacks, collapse state persisted, dark theme record | Dropdowns, galleries, combos, toggles, contextual tabs |
| GridSpace (Excel) | Records + `RibbonControl` | Command catalog, formula bar, contextual Chart Design / PivotTable Analyze | Contextual tabs rebuild the whole ribbon and forget the previous tab; search skipped analytics commands |
| DataSpace (Access) | `OfficeRibbon`, `OfficeCommandCatalog` | Tab ids, command enabled state by id (`SetCommandEnabled`), Ctrl+F1 collapse | Contextual tabs, dropdowns, checked state |
| PresentationSpace (PowerPoint) | `RibbonControl`, `RibbonScroller`, `CompactToggleSwitch` | 13 tabs, theme gallery with previews, colour flyouts, overflow scroll arrows without oscillation, title bar breakpoints | Contextual tabs, KeyTips, group shrinking |
| CadSpace (AutoCAD) | `CadRibbon`, `CadRibbonButton` (split), property selectors, `CadMenuBar`, `CadApplicationBar` | Split buttons with alternatives, selection-driven contextual tabs, application menu, customizable QAT, classic menu bar, command line and command search, dark theme | Persisted QAT, KeyTips, adaptive layout |

The other eleven apps use toolbars instead of a ribbon:
- ImageSpace and ArtSpace have Photoshop/Illustrator-style menu bars, tool palettes and options bars.
- VideoSpace and EffectsSpace have workspace switchers and transport bars.
- LightSpace, CodeSpace and ControlSpace have activity rails.
- GitSpace and PdfSpace have segmented selectors, floating palettes and "⋯" menus.
- VectorSpace and LabSpace have command palettes and runtime-inserted toolbars.

Problems common to all of them:
- Tabs are rebuilt on every switch.
- State is synchronized by hand.
- Selected looks are faked with background colours.
- Hover is implemented manually.
- Themes are hard-coded light (or dark) brushes.
- Width breakpoints are scattered through the code.
- No app has a real overflow menu, KeyTips or adaptive group resizing.

## 2. Requirements distilled

| # | Requirement | Source | RibbonSpace |
|---|---|---|---|
| 1 | Tabs with ids, KeyTips, visibility | all ribbons | `RibbonTab` (`Id`, `Header`, `KeyTip`, `IsTabVisible`) |
| 2 | Groups with captions and dialog launchers | TextSpace, CadSpace | `RibbonGroup` (`DialogLauncherCommand`, `IsDialogLauncherVisible`, `DialogLauncherClick`) |
| 3 | Large / medium / small buttons, 3-row stacks, row layouts | all | `RibbonButton` + `RibbonGroupItemsPanel` (`ItemsLayout=Columns/Rows`, `RowCount`) |
| 4 | Adaptive resizing instead of scrolling | missing everywhere | `RibbonGroupsPanel` + `RibbonAdaptiveLayout` (Large → Medium → Small → Collapsed, `ReductionOrder`, `SizeDefinition`) |
| 5 | Scroll arrows as last resort (no oscillation) | PresentationSpace | `RibbonScrollPanel` (measures with real width, clips, arrows, wheel) |
| 6 | Real toggle buttons and radio groups | faked everywhere | `RibbonToggleButton` (`GroupName`) |
| 7 | Split buttons (Paste, Bullets, tool alternatives) | CadSpace, VectorSpace | `RibbonSplitButton` (`IsCheckable`, `FollowLastChoice`) |
| 8 | Drop-down menus | all | `RibbonDropDownButton` + any `Flyout` / `MenuFlyout` |
| 9 | Editable combo boxes (font, size, number format, layers) | TextSpace, PresentationSpace, CadSpace | `RibbonComboBox`, `RibbonFontComboBox`, `RibbonFontSizeComboBox` (non-listed values supported) |
| 10 | Spinners / numeric fields with units, drag-scrubbing | TextSpace layout fields, VectorSpace | `RibbonSpinner` (`Unit`, `Format`, arrows, wheel, label scrubbing) |
| 11 | Colour pickers (theme grid, standard, no colour, custom) | TextSpace, PresentationSpace, GridSpace | `RibbonColorPicker` / `RibbonColorPalette` / `RibbonColorDialog` |
| 12 | Galleries with live preview | TextSpace styles, PresentationSpace themes | `RibbonGallery` (inline window, row scrolling, expanded categorized filterable popup, `PreviewCommand`, footer items) |
| 13 | Table grid picker | TextSpace | `RibbonGridPicker` |
| 14 | Check boxes | TextSpace, PresentationSpace | `RibbonCheckBox` |
| 15 | Contextual tabs with colour, auto-select, restoring the previous tab | TextSpace, GridSpace, CadSpace | `RibbonContextualTabGroup` (`Color`, `Activation=SelectOnShow`), `SetActiveContextualGroups`, previous-tab restore |
| 16 | File button → backstage or application menu | all | `Ribbon.Backstage` (`RibbonBackstage`, pages, actions, footer) or `ApplicationMenu` |
| 17 | Quick Access Toolbar, customizable and persisted | CadSpace, all title bars | `RibbonQuickAccessToolBar`, "Add to QAT", customize menu, above/below, labels, persisted ids |
| 18 | Title bar with QAT, title, search, account | TextSpace, PresentationSpace, GridSpace, NoteSpace | `RibbonTitleBar` (adaptive, window integration) |
| 19 | Command search ("Tell me", Alt+Q) | TextSpace, GridSpace, PresentationSpace, CadSpace | `RibbonSearchBox`, `Ribbon.Search`, `RibbonSearchEngine` (fuzzy, acronyms, recents) |
| 20 | Command palette | CodeSpace, VectorSpace, LabSpace | `RibbonCommandPalette` |
| 21 | Collapse / pin / display options / full screen | DataSpace, CadSpace, PresentationSpace | `VisibilityMode` (AlwaysShow / TabsOnly popup / FullScreen), Ctrl+F1, double-click, display-options menu |
| 22 | Single-line simplified ribbon with overflow | Office 365 | `DisplayMode=Simplified`, `RibbonSimplifiedLayout`, "More options" linked-copy overflow |
| 23 | KeyTips | missing everywhere | Alt / F10, multi-level, multi-letter, collision-free generation, popups, backstage |
| 24 | String-id command routing (existing apps) and MVVM `ICommand` | all | `ItemInvoked` (id + parameter), `CommandId` + `RibbonCommandCatalog`, `ICommand` everywhere |
| 25 | Command state by id (enabled / checked) | DataSpace | `Ribbon.SetCommandEnabled/Checked`, catalog descriptors |
| 26 | Keyboard shortcuts | all | `Shortcut="Ctrl+B"` + `IsShortcutRoutingEnabled`, `RibbonKeyGesture` |
| 27 | Dark theme, per-app accent, runtime switching without rebuilding | GitSpace rebuilds its whole UI | `RibbonThemeResources` (in-place brush mutation), 11 palettes, Colorful / Neutral chrome, HighContrast |
| 28 | Density (compact CAD, touch) | CadSpace, LightSpace | `Density` (Comfortable, Compact, Touch) + `CustomMetrics` |
| 29 | Toolbars (horizontal, vertical, 2-column palettes) with overflow | ImageSpace, ArtSpace, VectorSpace, PdfSpace | `RibbonToolBar` (`Orientation`, `Columns`, "⋯" overflow) |
| 30 | Tool options / contextual bars | ImageSpace, ArtSpace, ControlSpace | `RibbonContextualToolBar` |
| 31 | Classic menu bar | ImageSpace, VideoSpace, CadSpace, LabSpace | `RibbonMenuBar` (hover switching, arrow keys) |
| 32 | Workspace / mode switchers, segmented controls | VideoSpace, EffectsSpace, GitSpace | `RibbonSegmentedControl` |
| 33 | Status bar with zoom | Office, CadSpace, PdfSpace | `RibbonStatusBar`, `RibbonZoomControl` |
| 34 | Stable automation ids and names | GridSpace, DataSpace and CadSpace Playwright tests | `Id` → `AutomationId`, automation peers (Tab, TabItem, Group, ToolBar) |
| 35 | Customization and persistence | CadSpace, Office | `RibbonCustomization`, `RibbonState`, `RibbonCustomizeDialog`, AOT-safe JSON |
| 36 | Plugins / merging | RibbonControl (Avalonia) | `RibbonModelMerger.Merge/Unmerge` |
| 37 | Localization | missing everywhere | `RibbonStrings` (en, de, fr, es, pl; extensible) |
| 38 | Vector icons independent of platform fonts | CadSpace, GridSpace (Skia icons) | `RibbonIconPresenter`: glyph, path data, image, `RibbonIcon`, `IconSource`, text |

## 3. Microsoft 365 features and their equivalents

| Microsoft 365 | RibbonSpace |
|---|---|
| Rounded, floating command bar card on a neutral canvas (2023 visual refresh) | Default template (`RibbonCommandBarCornerRadius`, neutral chrome) |
| Colourful title bar theme | `RibbonTheme.ApplyChromeStyle(RibbonChromeStyle.Colorful)` |
| Dark Grey / Black themes | Dark theme dictionary |
| Ribbon display options (Full-screen, Show tabs only, Always show) | Display-options button and menu |
| Classic ↔ Simplified ribbon switch | `DisplayMode`, display-options menu |
| Group scaling policies | `SizeDefinition`, `ReductionOrder`, `CanCollapse`, `RowCount` |
| Contextual tab sets | `RibbonContextualTabGroup` |
| KeyTips (Alt), Ctrl+F1, Alt+Q | Built in |
| Customize the Ribbon / Quick Access Toolbar dialogs, import/export | `RibbonCustomizeDialog`, JSON state |
| ScreenTips with title, shortcut, description, help | `RibbonScreenTip` |
| Live preview in galleries | `RibbonGallery.PreviewCommand` / `ItemPreview` |
| Font colour picker with theme shades | `RibbonColorPalette` (Office shade generation) |
| Microsoft Search box | `RibbonSearchBox` |
| Touch / mouse mode | `Density` |

## 4. Lessons from the earlier Avalonia ribbons

- **Kept:** the layered MVVM model (definitions → observable models → elements), id-based merging, the versioned runtime state, the command catalog, the KeyTip collision resolver, the per-group size definitions, simplified mode, rich ScreenTips, reduced-motion awareness and the touch density.
- **Avoided:**
  - "Primitive enum" god-classes.
  - Hard-coded fallback widths. RibbonSpace measures groups in every state and caches the results.
  - Reflection-based JSON. RibbonSpace uses source-generated serialization.
  - Static managers everywhere.
  - Tabs-only KeyTips and tabs-only customization.
  - Thin automation peers.
  - Missing localization.
