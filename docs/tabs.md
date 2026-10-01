# Tabs and contextual tabs

```xml
<RibbonTab Id="home" Header="Home" KeyTip="H" ScreenTip="Common commands" />
```

- **Selection:**
  - `SelectedTab` or `SelectedTabId`, both two-way bindable
  - `SelectTab("insert")`
  - the `SelectedTabChanged` event
- **Visibility:** `IsTabVisible` (tabs), `IsGroupVisible` (groups). Customization can also hide them.
- **Icons and tips:** `RibbonTab.Icon` shows an optional icon before the header (useful for icon-led apps; Office
  itself shows text only). `RibbonGroup.ScreenTip` appears on the button of a collapsed group and becomes the group's
  automation help text.
- **Keyboard:** the tab row is one Tab stop; Left / Right / Home / End switch tabs and Down enters the commands.
- **Tab row:**
  - The application (File) button: `IsApplicationButtonVisible`, `ApplicationButtonLabel`, `ApplicationButtonContent`,
    `ApplicationButtonKeyTip`, `ApplicationMenu`, `ApplicationButtonClick`.
  - `TabStripStartContent`, and `TabStripItems` + `TabStripEndContent` (Comments, Share, Editing mode). Their labels
    hide below `CompactTabStripWidth`.
  - The tab headers scroll with arrows when needed.

## Contextual tabs

```xml
<Ribbon.ContextualGroups>
  <RibbonContextualTabGroup Id="table" Header="Table Tools" Color="#0F7B6C" Activation="SelectOnShow" />
</Ribbon.ContextualGroups>
<RibbonTab Id="tableDesign" Header="Table Design" ContextualGroupId="table" />
<RibbonTab Id="tableLayout" Header="Layout" ContextualGroupId="table" />
```

```csharp
ribbon.SetActiveContextualGroups("table");   // show exactly these groups (selection changed)
ribbon.SetActiveContextualGroups();          // hide all
ribbon.FindContextualGroup("table")!.IsVisible = true;
```

- `KeyTip="J"` on the group gives its tabs Office-style prefixed KeyTips ("JT" Table Design, "JL" Layout) unless a
  tab has its own `KeyTip`. `IsEnabled="False"` keeps the tabs visible but disabled.
- Contextual tabs are placed after the regular tabs, with a coloured band, coloured text and a "Table Tools ›
  Table Design" tooltip.
- `Activation="SelectOnShow"` selects the first contextual tab when the group appears. You can also set this for all
  groups with `Ribbon.ContextualActivation`.
- When a contextual group hides while one of its tabs is selected, the previously selected regular tab is restored.
