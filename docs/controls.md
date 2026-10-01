# Controls reference

Generated from the source XML documentation by `tools/generate-api-reference.py`.

## Common item properties

Every item control (`RibbonButton`, `RibbonToggleButton`, `RibbonCheckBox` and all `RibbonControlBase` items) implements `IRibbonItem` and has:

| Member | Type | Description |
|---|---|---|
| `Id` | `string?` | Stable id used for persistence, the Quick Access Toolbar, customization and automation (falls back to Name / CommandId). |
| `Label` | `string?` | Label. |
| `Icon` | `object?` | Icon: glyph, path data, image URI, RibbonIcon, IconSource, IconElement or ImageSource. |
| `LargeIcon` | `object?` | Icon used in the large size (falls back to Icon). |
| `Size` | `RibbonItemSize` | Preferred (largest) size; also selects the default SizeDefinition. |
| `SizeDefinition` | `string?` | Adaptive sizes for the Large, Medium and Small group states, e.g. "Large, Medium, Small". |
| `CurrentSize` | `RibbonItemSize` | Size currently used (set by the hosting group, toolbar or QAT). |
| `IsSimplified` | `bool` | True when presented in the simplified line, a toolbar or the QAT. |
| `ActualShowLabel` | `bool` | Effective label visibility for the current layout. |
| `KeyTip` | `string?` | Explicit KeyTip; generated from the label when not set. |
| `ScreenTip` | `object?` | Tooltip: a string (description) or a RibbonScreenTip. |
| `CommandId` | `string?` | Id of a command registered in the ribbon command catalog. |
| `Shortcut` | `string?` | Keyboard shortcut shown in the ScreenTip, e.g. "Ctrl+B". |
| `SimplifiedVisibility` | `RibbonSimplifiedVisibility` | Behaviour in the simplified ribbon. |
| `SimplifiedLabel` | `RibbonSimplifiedLabel` | Label visibility in the simplified ribbon (Auto shows labels of items whose preferred size is Large). |
| `ShowLabel` | `bool` | Shows the label in the medium size. |
| `CanAddToQuickAccess` | `bool` | Allows "Add to Quick Access Toolbar". |
| `Metrics` | `RibbonMetrics` | Layout metrics in effect. |
| `EffectiveSizeDefinition` | `RibbonSizeDefinition` | Effective adaptive size definition. |
| `ShowLabelInSimplifiedMode` | `bool` | Whether the label is shown in the simplified line. |

Methods: `ApplyLayout(RibbonItemLayout)`, `IRibbonItem.CreateLinkedCopy()`, `IRibbonItem.CreateOverflowMenuItems()`, `IRibbonItem.OnKeyTip()`, `IRibbonItem.Invoke()`.

## Ribbon

*Base:* `Control`

Office-style ribbon: tab row with application (File) button, contextual tabs and tab-row commands, adaptive classic or simplified command area, Quick Access Toolbar, backstage, KeyTips, display options, customization and persistence. Use declaratively in XAML or bind `Model` for MVVM.

| Member | Kind | Type | Description |
|---|---|---|---|
| `BackstageOpened` | event | `EventHandler?` | Raised after the backstage opened. |
| `BackstageClosed` | event | `EventHandler?` | Raised after the backstage closed. |
| `Backstage` | property | `RibbonBackstage?` | Backstage shown by the application (File) button. |
| `IsBackstageOpen` | property | `bool` | Opens / closes the backstage. |
| `SetCommandEnabled` | method | `void` | Enables or disables every item bound to a command id (persisted for items created later). |
| `SetCommandChecked` | method | `void` | Checks or unchecks every toggle item bound to a command id. |
| `FindItemsByCommand` | method | `IEnumerable<FrameworkElement>` | Items (including QAT copies) bound to a command id. |
| `SearchRequested` | event | `EventHandler?` | Raised when Alt+Q (or the search shortcut) is pressed; search boxes subscribe to focus themselves. |
| `KeyTipModeChanged` | event | `EventHandler<bool>?` | Raised when KeyTips are shown or hidden. |
| `IsKeyTipsEnabled` | property | `bool` | Enables KeyTips (Alt / F10). |
| `IsShortcutRoutingEnabled` | property | `bool` | Routes keyboard shortcuts declared on items (`Shortcut="Ctrl+B"`) to those items anywhere in the window. Plain keys are ignored while a text input has focus. |
| `IsKeyTipMode` | property | `bool` | True while KeyTips are shown. |
| `ActiveKeyTips` | property | `IReadOnlyList<RibbonKeyTip>` | Currently displayed KeyTip badges. |
| `InvalidateShortcuts` | method | `void` | Invalidates the shortcut routing cache (called automatically when tabs or items change). |
| `ShowKeyTips` | method | `void` | Shows the first KeyTip level (application button, QAT, tabs, tab-row items). |
| `CancelKeyTips` | method | `void` | Leaves KeyTip mode and closes every popup KeyTip navigation opened (Alt, F10, click elsewhere). |
| `HideKeyTips` | method | `void` | Hides KeyTips (popups opened by navigation stay open; see `CancelKeyTips`). |
| `ProcessKeyTipInput` | method | `bool` | Feeds one character to KeyTip mode (as if typed). Returns true when it matched a KeyTip (partially or completely). Useful for UI automation and tests. |
| `PopKeyTipLevel` | method | `bool` | Leaves the current KeyTip level (like Esc). Returns false when KeyTip mode ended. |
| `ContextMenuOpening` | event | `EventHandler<RibbonContextMenuEventArgs>?` | Raised to build a custom context menu (add or remove entries, or set Handled). |
| `CustomizeRequested` | event | `EventHandler<RibbonCustomizeRequestedEventArgs>?` | Raised when the user asks to customize the ribbon / QAT. Set Handled to show your own UI. |
| `IsContextMenuEnabled` | property | `bool` | Enables the right-click context menu (Add to QAT, Collapse, Customize...). |
| `CanCustomize` | property | `bool` | Allows customization commands (context menu, QAT "More Commands..."). |
| `ShowItemContextMenu` | method | `bool` | Shows the Office context menu for an item. Returns false when disabled. |
| `ShowCustomizeDialog` | method | `void` | Opens the customization UI (raises `CustomizeRequested`, then shows `RibbonCustomizeDialog`). |
| `BuildDisplayOptionsMenu` | method | `MenuFlyout` | Builds the Office "Ribbon display options" menu. |
| `Model` | property | `RibbonModel?` | MVVM definition of the ribbon. Tabs, groups, items, contextual groups, QAT, tab-row items and the backstage are generated from the model and kept in two-way sync (selection, display options, checked states, values...). XAML-declared tabs are kept in front of generated tabs. Collection changes are applied incrementally; model subscriptions are detached while the ribbon is unloaded (the elements are kept) and re-attached when it loads. |
| `GetModel` | method | `RibbonNodeModel?` | Returns the model an element was generated from. |
| `QuickAccessChanged` | event | `EventHandler?` | Raised when the QAT position, visibility or items change (hosts such as `RibbonTitleBar` relayout). |
| `QuickAccessToolBar` | property | `RibbonQuickAccessToolBar?` | The Quick Access Toolbar. |
| `QuickAccessPosition` | property | `RibbonQuickAccessPosition` | QAT position. |
| `IsQuickAccessVisible` | property | `bool` | QAT visibility. |
| `ShowQuickAccessLabels` | property | `bool` | QAT labels. |
| `IsQuickAccessHostedExternally` | property | `bool` | True when a `RibbonTitleBar` hosts the QAT above the ribbon. |
| `QuickAccessCandidateIds` | property | `ObservableCollection<string>` | Ids of commands offered in the QAT customize drop-down (Office "Customize Quick Access Toolbar"). |
| `DefaultQuickAccessItemIds` | property | `IReadOnlyList<string>?` | QAT item ids as declared by the application (captured when the ribbon first loads, before user state is applied). Used by "Reset" in the customize dialog and `ResetQuickAccess`. |
| `DefaultQuickAccessPosition` | property | `RibbonQuickAccessPosition` | Default QAT position (captured on first load). |
| `ResetQuickAccess` | method | `void` | Restores the application's default QAT items and position. |
| `FindQuickAccessCopy` | method | `FrameworkElement?` | Returns the QAT copy of an item, if any. |
| `IsInQuickAccess` | method | `bool` | True when an item (or its copy) is in the QAT. |
| `AddToQuickAccess` | method | `bool` | Adds a linked copy of an item to the QAT. Returns false when not possible. |
| `RemoveFromQuickAccess` | method | `bool` | Removes an item (or its linked copy) from the QAT. |
| `GetQuickAccessItemIds` | method | `IReadOnlyList<string>` | Ids of items in the QAT. |
| `BuildQuickAccessCustomizeMenu` | method | `MenuFlyout` | Builds the QAT customize drop-down. |
| `ToggleQuickAccessPosition` | method | `void` | Moves the QAT above / below the ribbon. |
| `SearchEngine` | property | `RibbonSearchEngine` | Command search engine ("Tell me" / Microsoft Search / command palette). |
| `AdditionalSearchEntries` | property | `ObservableCollection<RibbonSearchEntry>` | Extra search entries (help topics, recent documents, custom actions). Targets may be `Action`s. |
| `BuildSearchEntries` | method | `IReadOnlyList<RibbonSearchEntry>` | Builds search entries for every reachable item, backstage item, catalog command and additional entry. |
| `Search` | method | `IReadOnlyList<RibbonSearchResult>` | Searches commands. |
| `ExecuteSearchEntry` | method | `bool` | Executes a search result (invokes the item, opening its tab for inputs and galleries). |
| `Customization` | property | `RibbonCustomization` | Current user customization (read-only snapshot; use `ApplyCustomization` to change). |
| `GetState` | method | `RibbonState` | Captures the persistable state (selection, display options, QAT, customization). |
| `ApplyState` | method | `void` | Restores a state captured with `GetState`. |
| `SaveStateToJson` | method | `string` | Serializes the state to JSON. |
| `LoadStateFromJson` | method | `bool` | Restores state from JSON (ignored when invalid). |
| `ApplyCustomization` | method | `void` | Applies a customization (hidden / renamed / reordered tabs and groups, custom tabs and groups). |
| `ResetCustomization` | method | `void` | Removes all user customizations. |
| `CompactTabStripWidth` | property | `double` | Below this width tab-row items (Comments, Share...) hide their labels. |
| `SelectedTabChanged` | event | `EventHandler<RibbonTabChangedEventArgs>?` | Raised when the selected tab changes. |
| `ItemInvoked` | event | `EventHandler<RibbonItemInvokedEventArgs>?` | Raised when any item is invoked (buttons, menu items, galleries...). Ideal for string-id command routing. |
| `ApplicationButtonClick` | event | `EventHandler<RibbonHandledEventArgs>?` | Raised when the application (File) button is clicked. Set `Handled` to suppress the default action. |
| `StateChanged` | event | `EventHandler?` | Raised when the display mode, visibility mode, QAT or customization changes (for auto-saving state). |
| `Tabs` | property | `ObservableCollection<RibbonTab>` | Tabs (regular and contextual). |
| `ContextualGroups` | property | `ObservableCollection<RibbonContextualTabGroup>` | Contextual tab groups. |
| `TabStripItems` | property | `ObservableCollection<UIElement>` | Items at the right end of the tab row (Comments, Share, Editing mode...). |
| `SelectedTab` | property | `RibbonTab?` | Selected tab. |
| `SelectedTabId` | property | `string?` | Id of the selected tab (two-way bindable). |
| `DisplayMode` | property | `RibbonDisplayMode` | Classic or simplified layout. |
| `VisibilityMode` | property | `RibbonVisibilityMode` | Always show / tabs only / full screen. |
| `IsMinimized` | property | `bool` | Collapsed to the tab row (same as `TabsOnly`). |
| `Density` | property | `RibbonDensity` | Spacing density. |
| `CustomMetrics` | property | `RibbonMetrics?` | Fully custom metrics (overrides `Density`). |
| `IsApplicationButtonVisible` | property | `bool` | Shows the application (File) button. |
| `ApplicationButtonLabel` | property | `string?` | Label of the application button (defaults to the localized "File"). |
| `ApplicationButtonContent` | property | `object?` | Custom content of the application button (logo, text). |
| `ApplicationMenu` | property | `FlyoutBase?` | Menu shown by the application button when no backstage is set (classic application menu). |
| `ApplicationButtonKeyTip` | property | `string?` | KeyTip of the application button. |
| `TabStripStartContent` | property | `object?` | Content placed before the tabs. |
| `TabStripEndContent` | property | `object?` | Content placed at the right end of the tab row. |
| `IsDisplayOptionsButtonVisible` | property | `bool` | Shows the ribbon display options button. |
| `IsSimplifiedModeAvailable` | property | `bool` | Offers the simplified layout in the display options. |
| `IsAdaptiveLayoutEnabled` | property | `bool` | Enables adaptive group resizing. |
| `IsCollapsible` | property | `bool` | Allows collapsing (double-click on a tab, Ctrl+F1, display options). |
| `CommandCatalog` | property | `RibbonCommandCatalog?` | Command catalog resolving `CommandId` references. |
| `ContextualActivation` | property | `RibbonContextualActivation` | Default activation of contextual groups when they become visible. |
| `IsFullScreenRevealed` | property | `bool` | True while the ribbon is temporarily revealed in full-screen mode. |
| `VisibleTabs` | property | `IReadOnlyList<RibbonTab>` | Tabs currently shown in the tab row, in display order. |
| `TabHeaders` | property | `IReadOnlyList<RibbonTabHeader>` | Headers of the visible tabs. |
| `ApplicationButton` | property | `Button?` | The application (File) button. |
| `DisplayOptionsButton` | property | `Button?` | The display options button. |
| `IsMinimizedPopupOpen` | property | `bool` | True while the minimized ribbon shows its commands in a temporary popup. |
| `FindContextualGroup` | method | `RibbonContextualTabGroup?` | Finds a contextual group by id. |
| `SetActiveContextualGroups` | method | `void` | Shows exactly the given contextual groups and hides the others. |
| `FindTab` | method | `RibbonTab?` | Finds a tab by id (or header). |
| `SelectTab` | method | `bool` | Selects a tab by id. Returns false when unknown or hidden. |
| `ToggleMinimized` | method | `void` | Toggles between `AlwaysShow` and `TabsOnly`. |
| `OpenMinimizedPopup` | method | `void` | Opens the temporary command popup of a minimized ribbon. |
| `CloseMinimizedPopup` | method | `void` | Closes the temporary command popup of a minimized ribbon. |
| `InvokeApplicationButton` | method | `void` | Runs the application button action (backstage, application menu or the click event). |
| `GetAllItems` | method | `IEnumerable<FrameworkElement>` | All items of all tabs, the tab row and the QAT. |
| `FindItem` | method | `FrameworkElement?` | Finds an item by id. |
| `InvalidateLayout` | method | `void` | Forces the adaptive layout to be recomputed (e.g. after changing item content in code). |

## RibbonTab

*Base:* `Control`

Ribbon tab: a header in the tab row and a set of groups.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Groups` | property | `ObservableCollection<RibbonGroup>` | Groups. |
| `Header` | property | `string?` | Header text. |
| `IsTabVisible` | property | `bool` | Visibility of the tab (independent of selection). |
| `ContextualGroupId` | property | `string?` | Id of the `RibbonContextualTabGroup` owning this tab. |
| `IsSelected` | property | `bool` | True for the selected tab. |
| `Ribbon` | property | `Ribbon?` | Owning ribbon. |
| `HeaderElement` | property | `RibbonTabHeader?` | The generated header in the tab row. |
| `ContextualGroup` | property | `RibbonContextualTabGroup?` | Contextual group. |
| `IsContextual` | property | `bool` | True for contextual tabs. |
| `GroupsPanel` | property | `RibbonGroupsPanel` | The adaptive groups panel. |
| `OverflowButton` | property | `RibbonButton` | The simplified overflow ("More options") button. |
| `IsAvailable` | property | `bool` | True when the tab should appear in the tab row. |
| `InvalidateGroupsLayout` | method | `void` | Invalidates the adaptive layout of the groups. |
| `ShowOverflow` | method | `void` | Opens the simplified overflow menu. |

## RibbonTabHeader

*Base:* `Button`

Header of a tab in the ribbon tab row.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Tab` | property | `RibbonTab` | The tab. |
| `IsSelected` | property | `bool` | Selected state. |
| `Select` | method | `void` | Selects the tab (UI Automation SelectionItem pattern, keyboard). |
| `ContextualBrush` | property | `Brush?` | Contextual accent. |
| `IsContextual` | property | `bool` | True for contextual tabs. |
| `ContextualHeader` | property | `string?` | Header of the contextual group ("Table Tools"). |

## RibbonContextualTabGroup

*Base:* `DependencyObject`

Contextual tab group ("Table Tools") shown for a matching selection.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Header` | property | `string?` | Header ("Table Tools"). |
| `Color` | property | `Color` | Accent color. |
| `IsVisible` | property | `bool` | Visibility. |
| `IsEnabled` | property | `bool` | Enables the group's tab headers (disabled headers stay visible but cannot be selected). |
| `Activation` | property | `RibbonContextualActivation` | Selection behaviour when shown. |

## RibbonGroup

*Base:* `Control`

Ribbon group ("Clipboard", "Font"): arranges items, shows the caption and dialog launcher, adapts its size (Large → Medium → Small → Collapsed) and presents itself in the simplified line.

| Member | Kind | Type | Description |
|---|---|---|---|
| `DialogLauncherClick` | event | `RoutedEventHandler?` | Raised when the dialog launcher is clicked. |
| `Items` | property | `ObservableCollection<UIElement>` | Items. |
| `Header` | property | `string?` | Caption. |
| `DialogLauncherCommand` | property | `ICommand?` | Dialog launcher command (shows the launcher). |
| `DialogLauncherCommandParameter` | property | `object?` | Dialog launcher command parameter. |
| `IsDialogLauncherVisible` | property | `bool` | Shows the dialog launcher even without a command (handle `DialogLauncherClick`). |
| `DialogLauncherScreenTip` | property | `object?` | ScreenTip of the dialog launcher. |
| `ReductionOrder` | property | `int` | Groups with a higher value shrink first. |
| `CanCollapse` | property | `bool` | Allows collapsing into a single button. |
| `ItemsLayout` | property | `RibbonGroupItemsLayout` | Item arrangement. |
| `RowCount` | property | `int` | Rows per column (2 or 3). |
| `State` | property | `RibbonGroupState` | Current adaptive state (set by the ribbon). |
| `IsGroupVisible` | property | `bool` | Visibility of the group (use instead of Visibility; customization may also hide groups). |
| `Tab` | property | `RibbonTab?` | Owning tab. |
| `Ribbon` | property | `Ribbon?` | Owning ribbon. |
| `LayoutVersion` | property | `int` | Increments whenever cached measurements must be recomputed. |
| `IsPopupOpen` | property | `bool` | True when the collapsed popup is open. |
| `CollapsedButton` | property | `RibbonButton?` | The collapsed-state button (KeyTips, tests). |
| `DialogLauncher` | property | `Button?` | The dialog launcher button. |
| `ItemsPanel` | property | `RibbonGroupItemsPanel` | The items panel. |
| `OpenDialogLauncher` | method | `void` | Invokes the dialog launcher. |
| `GetOverflowItems` | method | `IEnumerable<UIElement>` | Items that are currently not shown in the simplified line (moved to the overflow menu). |
| `OpenPopup` | method | `void` | Opens the popup of a collapsed group. |
| `ClosePopup` | method | `void` | Closes the popup of a collapsed group. |
| `GetAllItems` | method | `IEnumerable<FrameworkElement>` | All items including nested ones. |

## RibbonButton

*Base:* `Button`

Ribbon push button (large, medium, small and simplified presentations).

| Member | Kind | Type | Description |
|---|---|---|---|
| `ShowChevron` | property | `bool` | Shows a drop-down chevron. |
| `IsChromeButton` | property | `bool` | Internal chrome button (collapsed group, overflow): clicks are not reported as commands. |
| `PerformClick` | method | `void` | Raises Click and executes the command. |

## RibbonDropDownButton

*Base:* `RibbonButton`

Button that opens a drop-down (menu or any flyout content).

## RibbonSplitButton

*Base:* `RibbonControlBase`

Split button: a primary action plus a drop-down part (Office Paste, Bullets, Shapes, Font Color). Large items stack the parts vertically; medium / small items place the arrow to the right.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Click` | event | `RoutedEventHandler?` | Raised when the primary part is clicked. |
| `Flyout` | property | `FlyoutBase?` | Drop-down flyout (MenuFlyout or Flyout with any content). |
| `IsCheckable` | property | `bool` | The primary part toggles (Bullets, Numbering). |
| `IsChecked` | property | `bool?` | Checked state. |
| `FollowLastChoice` | property | `bool` | When a menu item is chosen, the primary part adopts its icon, label and action (tool split buttons). The chosen item is exposed as `LastChoice`; `Label` and `Icon` are not modified. |
| `LastChoice` | property | `MenuFlyoutItem?` | Menu item chosen last (`FollowLastChoice`); the primary part shows and invokes it. |
| `ColorBar` | property | `Microsoft.UI.Xaml.Media.Brush?` | Color bar under the icon (Font Color, Highlight). |
| `IsDropDownOpen` | property | `bool` | True while the drop-down is open. |
| `OpenDropDown` | method | `void` | Opens the drop-down. |
| `CloseDropDown` | method | `void` | Closes the drop-down. |

## RibbonToggleButton

*Base:* `ToggleButton`

Toggle button; toggles sharing a `GroupName` behave like radio buttons.

| Member | Kind | Type | Description |
|---|---|---|---|
| `GroupName` | property | `string?` | Radio group name (scope: the owning group, toolbar or ribbon). |
| `ShowChevron` | property | `bool` | Shows a drop-down chevron. |

## RibbonCheckBox

*Base:* `CheckBox`

Compact ribbon check box.

## RibbonComboBox

*Base:* `RibbonInputBase`

Editable / read-only ribbon combo box (font family, font size, number format, zoom).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Committed` | event | `EventHandler<RibbonComboBoxCommittedEventArgs>?` | Raised after the user picked an item or committed text. |
| `SelectionChanged` | event | `EventHandler<SelectionChangedEventArgs>?` | Raised when the selected item changes. |
| `Items` | property | `ObservableCollection<object>` | Items (in addition to `ItemsSource`). |
| `ItemsSource` | property | `object?` | Items source (overrides `Items`). |
| `SelectedItem` | property | `object?` | Selected item. |
| `Text` | property | `string?` | Text of the input. |
| `IsEditable` | property | `bool` | Allows typing arbitrary values. |
| `ItemTemplate` | property | `DataTemplate?` | Item template of the drop-down. |
| `MaxDropDownHeight` | property | `double` | Maximum drop-down height. |
| `IsFontPreview` | property | `bool` | Renders each entry in its own font (font pickers). |
| `ItemTextSelector` | property | `Func<object?, string>?` | Converts items to text (defaults to `DisplayMemberPath`, then ToString / RibbonNodeModel label). Preferred over `DisplayMemberPath` in trimmed / AOT apps: it needs no binding metadata. |
| `DisplayMemberPath` | property | `string?` | Property path of the item shown as its text (resolved with a data binding, like WinUI's ComboBox). |
| `IsDropDownOpen` | property | `bool` | True while the drop-down is open. |
| `EffectiveItems` | property | `IReadOnlyList<object>` | Effective items. |
| `GetItemText` | method | `string` | Text for an item. |
| `Commit` | method | `void` | Commits an item (selects it, executes the command, raises Committed). |
| `CommitText` | method | `void` | Commits free text (editable combo boxes). Matching items are selected. |
| `OpenDropDown` | method | `void` | Opens the drop-down. |
| `CloseDropDown` | method | `void` | Closes the drop-down. |

## RibbonFontComboBox

*Base:* `RibbonComboBox`

Font family picker with preview (Office "Font").

| Member | Kind | Type | Description |
|---|---|---|---|
| `DefaultFonts` | property | `IReadOnlyList<string>` | Common cross-platform font families. |

## RibbonFontSizeComboBox

*Base:* `RibbonComboBox`

Font size picker (Office "Font Size").

| Member | Kind | Type | Description |
|---|---|---|---|
| `DefaultSizes` | property | `IReadOnlyList<double>` | Office font sizes. |

## RibbonSpinner

*Base:* `RibbonInputBase`

Numeric spinner with units, arrow keys, wheel and label scrubbing.

| Member | Kind | Type | Description |
|---|---|---|---|
| `ValueChanged` | event | `EventHandler<RibbonValueChangedEventArgs>?` | Raised when the value changes (user or programmatic). |
| `ValueCommitted` | event | `EventHandler<RibbonValueChangedEventArgs>?` | Raised when the user commits a value (arrows, keys, wheel, typed text, end of a label scrub); this is when the command executes. Programmatic / bound changes only raise `ValueChanged`. |
| `Value` | property | `double` | Value (kept within `Minimum` and `Maximum`). |
| `Minimum` | property | `double` | Minimum. |
| `Maximum` | property | `double` | Maximum. |
| `Increment` | property | `double` | Step for arrows, keys and wheel (PageUp / PageDown use 10x). |
| `Format` | property | `string` | Numeric format. |
| `Unit` | property | `string?` | Unit suffix ("pt", "cm", "\"", "°", "%"). |
| `IsScrubEnabled` | property | `bool` | Allows dragging the label horizontally to change the value. |
| `FormatValue` | method | `string` | Formatted value. |
| `CommitText` | method | `void` | Parses the text (unit suffix optional) and commits the value. |

## RibbonTextBox

*Base:* `RibbonInputBase`

Single-line text input (Enter executes the command with the text).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Submitted` | event | `EventHandler<string>?` | Raised when Enter is pressed. |
| `Text` | property | `string` | Text. |

## RibbonSlider

*Base:* `RibbonInputBase`

Slider item (zoom, opacity, brush size).

| Member | Kind | Type | Description |
|---|---|---|---|
| `ValueChanged` | event | `EventHandler<double>?` | Raised when the value changes (user or programmatic). |
| `ValueCommitted` | event | `EventHandler<double>?` | Raised when the user changes the value with the slider (drag, track click, keys); this is when the command executes. Programmatic / bound changes only raise `ValueChanged`. |
| `Value` | property | `double` | Value. |
| `Minimum` | property | `double` | Minimum. |
| `Maximum` | property | `double` | Maximum. |
| `StepFrequency` | property | `double` | Step. |

## RibbonGallery

*Base:* `RibbonControlBase`

In-ribbon gallery (Styles, Shape Styles, Themes, Transitions): shows a row-scrolled window of previews, adapts its column count to the group state, collapses into a drop-down button, and expands into a categorized, filterable popup with footer commands. Supports live preview.

| Member | Kind | Type | Description |
|---|---|---|---|
| `ItemClick` | event | `EventHandler<RibbonGalleryItemEventArgs>?` | Raised when an item is picked. |
| `ItemPreview` | event | `EventHandler<RibbonGalleryItemEventArgs>?` | Raised while hovering items (live preview); the item is null when the preview ends. |
| `Items` | property | `ObservableCollection<object>` | Items (used when `ItemsSource` is not set). |
| `FooterItems` | property | `ObservableCollection<UIElement>` | Footer entries of the expanded popup (e.g. RibbonButtons "Clear Formatting", "Apply Styles..."). |
| `ItemsSource` | property | `object?` | Items source. |
| `SelectedItem` | property | `object?` | Selected item. |
| `ItemTemplate` | property | `DataTemplate?` | Template for data items. |
| `ItemWidth` | property | `double` | Item width. |
| `ItemHeight` | property | `double` | Item height. |
| `MinColumns` | property | `int` | Columns in the Medium group state. |
| `MaxColumns` | property | `int` | Columns in the Large group state. |
| `Rows` | property | `int` | Visible rows in the ribbon. |
| `DropDownColumns` | property | `int` | Columns of the expanded popup (0 = max(MaxColumns, 5)). |
| `PreviewCommand` | property | `ICommand?` | Live preview command (item value while hovering, then null). |
| `IsFilterEnabled` | property | `bool` | Shows a filter box in the popup. |
| `ShowItemLabels` | property | `bool` | Shows labels in default item visuals. |
| `IsDropDownOnly` | property | `bool` | Always presented as a drop-down button (never inline). |
| `MaxDropDownHeight` | property | `double` | Maximum popup height. |
| `CategorySelector` | property | `Func<object, string?>?` | Selects a category for data items (defaults to RibbonGalleryItem.Category / model Category). |
| `EffectiveItems` | property | `IReadOnlyList<object>` | Effective items. |
| `IsInline` | property | `bool` | True when shown inline in the ribbon (not as a button). |
| `IsDropDownOpen` | property | `bool` | True while the popup is open. |
| `FirstRow` | property | `int` | First visible row of the inline window. |
| `Columns` | property | `int` | Columns currently shown inline. |
| `ScrollRows` | method | `void` | Scrolls the inline window by rows. |
| `Pick` | method | `void` | Picks an item (selects, executes the command, raises ItemClick, closes the popup). |
| `OpenDropDown` | method | `void` | Opens the expanded popup. |
| `CloseDropDown` | method | `void` | Closes the popup. |

## RibbonGalleryItem

*Base:* `DependencyObject`

Item of a `RibbonGallery` with a text / color preview (styles, themes, shape styles).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Category` | property | `string?` | Category of the expanded gallery. |
| `Value` | property | `object?` | Value passed to the command. |
| `PreviewText` | property | `string?` | Text preview ("AaBbCcDd"). |
| `PreviewForeground` | property | `Brush?` | Text preview brush. |
| `PreviewBackground` | property | `Brush?` | Preview background (swatches, themes). |
| `PreviewFontFamily` | property | `FontFamily?` | Text preview font. |
| `PreviewFontSize` | property | `double` | Text preview size. |
| `PreviewFontWeight` | property | `FontWeight` | Text preview weight. |
| `PreviewFontStyle` | property | `FontStyle` | Text preview style. |

## RibbonColorPicker

*Base:* `RibbonSplitButton`

Office color picker split button (Font Color, Highlight, Shape Fill) with a color bar under the icon.

| Member | Kind | Type | Description |
|---|---|---|---|
| `NoColorParameter` | property | `object` | Parameter passed to the command for "No Color". |
| `ColorSelected` | event | `EventHandler<Color?>?` | Raised when a color is picked. |
| `ColorPreview` | event | `EventHandler<Color?>?` | Raised while hovering swatches (live preview). |
| `SelectedColor` | property | `Color?` | Selected color (null = No Color). |
| `IsSplit` | property | `bool` | Click applies the current color (true) or opens the palette (false). |
| `Palette` | property | `RibbonColorPalette` | The palette. |
| `ShowAutomatic` | property | `bool` | Shows "Automatic". |
| `ShowNoColor` | property | `bool` | Shows "No Color". |
| `ShowMoreColors` | property | `bool` | Shows "More Colors...". |
| `AutomaticColor` | property | `Color` | Automatic color. |

## RibbonColorPalette

*Base:* `Control`

Office color palette: Automatic, theme colors with generated shades, standard colors, recent colors, No Color and More Colors. Usable standalone (toolbars, dialogs) or inside `RibbonColorPicker`.

| Member | Kind | Type | Description |
|---|---|---|---|
| `ColorSelected` | event | `EventHandler<Color?>?` | Raised when a color is picked (null = No Color). |
| `ColorPreview` | event | `EventHandler<Color?>?` | Raised while hovering swatches (live preview, null when leaving). |
| `MoreColorsRequested` | event | `EventHandler?` | Raised when "More Colors..." is clicked. |
| `SelectedColor` | property | `Color?` | Selected color. |
| `ShowAutomatic` | property | `bool` | Shows "Automatic". |
| `AutomaticColor` | property | `Color` | Automatic color. |
| `ShowNoColor` | property | `bool` | Shows "No Color". |
| `ShowMoreColors` | property | `bool` | Shows "More Colors...". |
| `ShowThemeColors` | property | `bool` | Shows the theme grid. |
| `ThemeColors` | property | `ObservableCollection<Color>` | Theme base colors (empty = Office theme). |
| `StandardColors` | property | `ObservableCollection<Color>` | Standard colors (empty = Office standard colors). |
| `ColorNames` | property | `IReadOnlyDictionary<Color, string>` | Names shown in swatch tooltips and announced by screen readers (e.g. "Blue, Accent 1"). Colours without a name use their hex value. |
| `SetColorName` | method | `void` | Sets (or clears with `null`) the display name of a colour. |
| `RecentColors` | property | `ObservableCollection<Color>` | Recent colors (maintained automatically, max 10). |
| `EndPreview` | method | `void` | Ends a running live preview (raises `ColorPreview` with null). |
| `FocusSelected` | method | `bool` | Focuses the selected swatch (or the first one) for keyboard use. |
| `Select` | method | `void` | Selects a color (adds it to recent colors) and raises `ColorSelected`. |

## RibbonGridPicker

*Base:* `Control`

Hover grid picker (Office "Insert Table"): the command receives a `RibbonGridSize`.

| Member | Kind | Type | Description |
|---|---|---|---|
| `SizePicked` | event | `EventHandler<RibbonGridSize>?` | Raised when a size is picked (click, Enter or Space), before the command executes; the hosting popup / flyout is closed afterwards. |
| `Rows` | property | `int` | Rows. |
| `Columns` | property | `int` | Columns. |
| `Command` | property | `ICommand?` | Command receiving the picked `RibbonGridSize`. |
| `HighlightedSize` | property | `RibbonGridSize` | Currently highlighted size. |
| `Highlight` | method | `void` | Highlights a size. |
| `Pick` | method | `void` | Picks a size. |

## RibbonSegmentedControl

*Base:* `RibbonControlBase`

Segmented control / workspace switcher (single selection, arrow-key navigation).

| Member | Kind | Type | Description |
|---|---|---|---|
| `SelectionChanged` | event | `EventHandler<RibbonSegment?>?` | Raised when the selection changes (user or programmatic); the command only runs for user selections. |
| `Segments` | property | `ObservableCollection<RibbonSegment>` | Segments. |
| `SelectedIndex` | property | `int` | Selected index. |
| `SelectedSegment` | property | `RibbonSegment?` | Selected segment. |

## RibbonSegment

*Base:* `DependencyObject`

Segment of a `RibbonSegmentedControl`.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Value` | property | `object?` | Value. |

## RibbonButtonGroup

*Base:* `RibbonItemsContainer`

Joined row of small buttons (Bold / Italic / Underline, alignment).

## RibbonStackPanel

*Base:* `RibbonItemsContainer`

Stack of items acting as one item: a horizontal row (used by groups with `Rows`) or a vertical column.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Orientation` | property | `Orientation` | Orientation. |
| `Spacing` | property | `double` | Spacing between children. |

## RibbonSeparator

*Base:* `RibbonControlBase`

Vertical separator between items (horizontal inside vertical toolbars and menus).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Orientation` | property | `Orientation` | Orientation of the line. |

## RibbonLabel

*Base:* `RibbonControlBase`

Static text item.

## RibbonQuickAccessToolBar

*Base:* `Control`

Quick Access Toolbar: small command buttons plus the customize drop-down.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Items` | property | `ObservableCollection<UIElement>` | Items (usually small buttons; linked copies of ribbon items are added by "Add to Quick Access Toolbar"). |
| `ShowCustomizeButton` | property | `bool` | Shows the customize drop-down. |
| `ShowLabels` | property | `bool` | Shows labels next to icons (Office "Show command labels"). |
| `Ribbon` | property | `Ribbon?` | Owning ribbon. |
| `CustomizeButton` | property | `Button?` | The customize button. |

## RibbonBackstage

*Base:* `Control`

Backstage (File) view: full-window navigation pane with pages (Home, New, Open, Info...), actions (Save, Close) and footer items (Account, Options). Hosted by `Backstage` or used standalone.

| Member | Kind | Type | Description |
|---|---|---|---|
| `CloseRequested` | event | `EventHandler?` | Raised when the back button or Esc asks to close. |
| `ItemInvoked` | event | `EventHandler<RibbonBackstageItem>?` | Raised when an item is invoked (page selected or action executed). |
| `Items` | property | `ObservableCollection<RibbonBackstageItem>` | Items. |
| `SelectedItem` | property | `RibbonBackstageItem?` | Selected page. |
| `Title` | property | `string?` | Title shown at the top of the navigation pane (application name). |
| `NavigationPaneWidth` | property | `double` | Width of the navigation pane. |
| `ShowPageTitle` | property | `bool` | Shows the selected page header as a large title. |
| `PaneHeader` | property | `object?` | Custom content above the navigation items (logo, account). |
| `PageTitle` | property | `string?` | Title of the selected page (template use). |
| `BackButton` | property | `Button?` | The back button. |
| `FocusSelectedItem` | method | `bool` | Moves keyboard focus to the selected navigation item (or the first available one). |
| `EnsureSelection` | method | `void` | Selects the first page when nothing (or a removed / hidden item) is selected. |
| `Invoke` | method | `void` | Selects a page or executes an action item. |
| `AnimateContent` | method | `void` | Plays the entrance animation of the content area. |

## RibbonBackstageItem

*Base:* `DependencyObject`

Navigation entry of a `RibbonBackstage`: a page (Content) or an action (Command).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Header` | property | `string?` | Header. |
| `Content` | property | `object?` | Page content (element or data with `ContentTemplate`). Items without content are actions. |
| `ContentTemplate` | property | `DataTemplate?` | Template for non-element content. |
| `Command` | property | `ICommand?` | Action command (Save, Close). |
| `CommandParameter` | property | `object?` | Command parameter. |
| `Placement` | property | `RibbonBackstagePlacement` | Main list or footer list. |
| `HasSeparatorBefore` | property | `bool` | Draws a separator above the item. |
| `ClosesBackstage` | property | `bool` | Actions close the backstage after executing. |
| `IsEnabled` | property | `bool` | Enabled state. |
| `IsVisible` | property | `bool` | Shows the item in the navigation pane. |
| `IsPage` | property | `bool` | True for page items. |
| `NavigationButton` | property | `Button?` | Navigation button generated for this item. |

## RibbonScreenTip

*Base:* `Control`

Rich tooltip (Office "ScreenTip"): bold title with shortcut, description, image and help footer.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Title` | property | `string?` | Title (defaults to the item label). |
| `Description` | property | `string?` | Description. |
| `Image` | property | `object?` | Illustration (any icon description). |
| `HelpText` | property | `string?` | Footer ("Tell me more"). |
| `DisabledReason` | property | `string?` | Why the command is disabled. |
| `TitleText` | property | `string` | Formatted title including the shortcut (template use). |

## RibbonKeyTip

*Base:* `Control`

KeyTip badge shown in KeyTip mode.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Text` | property | `string` | KeyTip text. |
| `Target` | property | `FrameworkElement?` | Target element. |
| `GetKeyTip` | method | `string?` | Gets the attached KeyTip. |
| `SetKeyTip` | method | `void` | Sets the attached KeyTip. |

## RibbonSearchBox

*Base:* `Control`

Microsoft Search style command search box ("Search (Alt+Q)") for the title bar or tab row. Searches the linked `Ribbon` (all tabs, contextual tabs, backstage, catalog) and executes the chosen command. When the box is hidden (narrow title bars), Alt+Q opens the `RibbonCommandPalette` instead.

| Member | Kind | Type | Description |
|---|---|---|---|
| `QuerySubmitting` | event | `EventHandler<RibbonSearchQueryEventArgs>?` | Raised to let the application add results (help, documents, people...) for the current query. |
| `ResultExecuted` | event | `EventHandler<RibbonSearchEntry>?` | Raised after a result was executed. |
| `Ribbon` | property | `Ribbon?` | Ribbon searched and driven by this box. |
| `PlaceholderText` | property | `string?` | Placeholder (defaults to "Search (Alt+Q)"). |
| `MaxResults` | property | `int` | Maximum results. |
| `IsResultsOpen` | property | `bool` | True while results are shown. |
| `FocusSearch` | method | `void` | Focuses the box and shows recent commands. |
| `Close` | method | `void` | Closes the results. |

## RibbonTitleBar

*Base:* `Control`

Office-style title bar: application icon, Quick Access Toolbar (of the linked ribbon), document title, centered command search and end content (account, share, window buttons area).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Ribbon` | property | `Ribbon?` | Linked ribbon (its QAT is hosted here when placed above the ribbon; search drives it). |
| `Title` | property | `string?` | Document / window title (falls back to `Title` of the linked ribbon's model). |
| `DisplayTitle` | property | `string?` | Title shown by the template: `Title`, or the title of the linked ribbon's model. |
| `Subtitle` | property | `string?` | Secondary text next to the title ("Saved", "Editing"). |
| `AppIcon` | property | `object?` | Application icon. |
| `StartContent` | property | `object?` | Content after the icon (AutoSave toggle...). |
| `EndContent` | property | `object?` | Content at the right (account, share, comments). |
| `IsSearchVisible` | property | `bool` | Shows the command search box. |
| `CaptionButtonsInset` | property | `Thickness` | Space reserved for OS caption buttons (set automatically by `AttachToWindow`). |
| `SearchWidth` | property | `double` | Width of the search box. |
| `SearchBox` | property | `RibbonSearchBox?` | The search box. |
| `AttachToWindow` | method | `void` | Extends the window content into the OS title bar and uses the empty area of this control as drag region (where supported); interactive parts (QAT, search, start / end content) keep receiving input. Reserves space for the caption buttons (system insets on Windows; left on macOS, right elsewhere; mirrored for RTL). |

## RibbonStatusBar

*Base:* `Control`

Status bar with start and end zones (page count, word count, view buttons, zoom).

| Member | Kind | Type | Description |
|---|---|---|---|
| `ItemInvoked` | event | `EventHandler<RibbonItemInvokedEventArgs>?` | Raised when an item is invoked. |
| `Items` | property | `System.Collections.ObjectModel.ObservableCollection<UIElement>` | Items at the start (left). |
| `EndItems` | property | `System.Collections.ObjectModel.ObservableCollection<UIElement>` | Items at the end (right). |
| `CommandCatalog` | property | `RibbonCommandCatalog?` | Command catalog for `CommandId` resolution (falls back to the ribbon's). |
| `Ribbon` | property | `Ribbon?` | Optional ribbon (shared command catalog, item invoked notifications). |

## RibbonZoomControl

*Base:* `RibbonControlBase`

Office status bar zoom control ([-] slider [+] 100%). The command is executed only for user changes (buttons, slider), never when `Value` is set in code.

| Member | Kind | Type | Description |
|---|---|---|---|
| `ZoomDialogRequested` | event | `EventHandler?` | Raised when the percentage button is clicked (show a Zoom dialog). |
| `ValueChanged` | event | `EventHandler<double>?` | Raised when the value changes (user or code). |
| `Value` | property | `double` | Zoom percentage (clamped to `Minimum` .. `Maximum`). |
| `Minimum` | property | `double` | Minimum. |
| `Maximum` | property | `double` | Maximum. |
| `Step` | property | `double` | Step of the +/- buttons. |
| `ValueText` | property | `string` | Formatted value (template use). |

## RibbonToolBar

*Base:* `Control`

Stand-alone command toolbar built from ribbon items: horizontal command bars, tool option bars, vertical tool palettes (1 or 2 columns) and activity rails. Items that do not fit move into a "⋯" overflow menu.

| Member | Kind | Type | Description |
|---|---|---|---|
| `ItemInvoked` | event | `EventHandler<RibbonItemInvokedEventArgs>?` | Raised when an item is invoked. |
| `Items` | property | `ObservableCollection<UIElement>` | Items. |
| `Orientation` | property | `Orientation` | Orientation. |
| `Columns` | property | `int` | Columns of vertical toolbars (tool palettes: 1 or 2). |
| `ShowLabels` | property | `bool` | Shows labels next to icons. |
| `IsOverflowEnabled` | property | `bool` | Moves items that do not fit into the overflow menu (otherwise they are clipped). |
| `Density` | property | `RibbonDensity` | Density. |
| `CommandCatalog` | property | `RibbonCommandCatalog?` | Command catalog for `CommandId` resolution. |
| `Ribbon` | property | `Ribbon?` | Optional ribbon (context menu, shared command state). |
| `OverflowItems` | property | `IEnumerable<UIElement>` | Items currently in the overflow menu. |
| `ShowOverflow` | method | `void` | Shows the overflow menu. |

## RibbonContextualToolBar

*Base:* `Control`

Hosts several toolbars (or any content) and shows the one matching `ActiveContext` — Photoshop / Illustrator tool option bars, selection-dependent editor toolbars.

| Member | Kind | Type | Description |
|---|---|---|---|
| `ContextChanged` | event | `EventHandler<string?>?` | Raised after the active context changed. |
| `Items` | property | `ObservableCollection<UIElement>` | Contextual contents (set RibbonContextualToolBar.Context on each). |
| `ActiveContext` | property | `string?` | Active context key (tool, selection kind, view). |
| `GetContext` | method | `string?` | Gets the context key of an element. |
| `SetContext` | method | `void` | Sets the context key of an element. |

## RibbonMenuBar

*Base:* `Control`

Classic menu bar (File, Edit, View...) with hover switching and keyboard navigation.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Items` | property | `ObservableCollection<RibbonMenuBarItem>` | Menus. |
| `IsMenuOpen` | property | `bool` | True while a menu is open. |
| `FocusFirst` | method | `void` | Focuses the first menu header (F10 / Alt activation). |
| `OpenMenu` | method | `bool` | Opens a menu by header. |

## RibbonMenuBarItem

*Base:* `DependencyObject`

Top-level entry of a `RibbonMenuBar`.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Header` | property | `string?` | Header ("File", "Edit"). |
| `Items` | property | `ObservableCollection<MenuFlyoutItemBase>` | Menu entries (changes apply to the menu immediately). |
| `Opening` | event | `EventHandler?` | Raised before the menu opens (refresh enabled / checked states). |

## RibbonTheme

Runtime theming API for RibbonSpace (palette, accent, chrome style, light / dark).

| Member | Kind | Type | Description |
|---|---|---|---|
| `Changed` | event | `EventHandler?` | Raised after the palette or chrome style changed. |
| `Resources` | property | `RibbonThemeResources` | Theme resources merged into the application. |
| `Palette` | property | `RibbonThemePalette` | Current palette. |
| `ChromeStyle` | property | `RibbonChromeStyle` | Current chrome style. |
| `EnsureResources` | method | `RibbonThemeResources` | Makes sure `RibbonThemeResources` is merged into `Application.Current.Resources` (reuses an instance merged in App.xaml). Called automatically by RibbonSpace controls. |
| `ApplyPalette` | method | `void` | Applies a palette (e.g. `Excel`) keeping the chrome style. |
| `ApplyAccent` | method | `void` | Applies a custom accent color. |
| `ApplyChromeStyle` | method | `void` | Switches between accent-colored ("Colorful") and neutral chrome. |
| `Apply` | method | `void` | Applies palette and chrome style. |
| `SetBrushColor` | method | `void` | Overrides a single brush for a theme ("Light", "Dark", "HighContrast"). |
| `SetTheme` | method | `void` | Sets Light / Dark / Default on an element subtree (typically the window content). |
| `GetBrush` | method | `Brush?` | Resolves a RibbonSpace brush for the theme of  (its `ActualTheme`, or High Contrast when enabled). Unlike `Application.Current.Resources[key]` this honours `SetTheme` / `RequestedTheme` on a subtree or popup. Unknown keys fall back to the application resources. |
| `ToColor` | method | `Color` | Converts a Core color. |
| `ToRibbonColor` | method | `RibbonColor` | Converts to a Core color. |

## RibbonThemeResources

*Base:* `ResourceDictionary`

Theme resources (Light, Dark, HighContrast) used by every RibbonSpace control. Merged automatically into `Application.Current.Resources` by `EnsureResources`; you may also merge it explicitly in App.xaml (`&lt;RibbonThemeResources /&gt;`). Brush instances are mutated in place when the palette changes, so accent changes apply live without re-templating.

| Member | Kind | Type | Description |
|---|---|---|---|
| `BrushKeys` | property | `IReadOnlyList<string>` | All brush keys defined by RibbonSpace. |
| `Palette` | property | `RibbonThemePalette` | Current palette. |
| `ChromeStyle` | property | `RibbonChromeStyle` | Current chrome style. |
| `GetBrush` | method | `SolidColorBrush?` | Returns the brush instance of a key for a theme ("Light", "Dark", "HighContrast"). |
| `SetColor` | method | `void` | Overrides one brush color for one theme (in place). |
| `Apply` | method | `void` | Applies a palette and chrome style (in place; all controls update immediately). |

## RibbonCustomizeDialog

*Base:* `ContentDialog`

Office "Customize the Ribbon" / "Quick Access Toolbar" dialog: show / hide / rename / reorder tabs and groups, create custom tabs and groups with existing commands, edit the QAT, import / export and reset.

| Member | Kind | Type | Description |
|---|---|---|---|
| `WorkingState` | property | `RibbonState` | Working copy of the state (applied when OK is pressed). |
| `InvalidImportMessage` | property | `string?` | Overrides the message shown when imported customization text is not valid (default: `RibbonStrings.InvalidImport`). |

## RibbonCommandPalette

Command palette (VS Code "Ctrl+Shift+P" / Figma "Ctrl+K"): a centered, modal fuzzy command search. Tab cycles inside the palette and focus returns to the previously focused element when it closes.

| Member | Kind | Type | Description |
|---|---|---|---|
| `Show` | method | `void` | Shows the palette for a ribbon. |

