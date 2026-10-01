# Commands and keyboard

RibbonSpace supports three command styles. You can mix them.

## 1. `ICommand`

Every command item exposes `Command` and `CommandParameter`. Toggles, check boxes and checkable split buttons also
pass their checked state to the owner. Combo boxes, spinners, galleries and colour pickers call `Command.Execute`
with their value.

## 2. Command catalog (`CommandId`)

```csharp
var catalog = new RibbonCommandCatalog();
catalog.Register("bold", "Bold", p => ToggleBold(), canExecute: _ => HasSelection, icon: RibbonIcons.Bold,
                 shortcut: "Ctrl+B", description: "Make your text bold.", category: "Home");
ribbon.CommandCatalog = catalog;               // or RibbonModel.CommandCatalog
```

```xml
<RibbonToggleButton Label="Bold" Icon="&#xE8DD;" CommandId="bold" />
```

- `catalog.SetEnabled("bold", false)` / `catalog.SetChecked("bold", true)` update every item bound to the id,
  including QAT copies. The same catalog can drive menus, toolbars and shortcuts.
- Catalog commands that aren't placed on the ribbon still show up in [search](search.md) and the command palette.
- `RibbonAsyncCommand` disables itself while running and reports failures through its `Failed` event.

## 3. String-id routing (`ItemInvoked`)

The existing Space apps route every command through one `ExecuteCommand(string id)` switch. `Ribbon.ItemInvoked`
supports that pattern directly:

```csharp
ribbon.ItemInvoked += (_, e) => ExecuteCommand(e.ItemId ?? e.CommandId, e.Parameter);
```

`e.Item` is always the original item, even when the user clicked a QAT copy or used the overflow menu, search or a
KeyTip. `e.Parameter` carries the checked state, the picked colour, the gallery value, combo text and so on.
`RibbonToolBar.ItemInvoked` works the same way for stand-alone toolbars.

## Command state by id without a catalog

```csharp
ribbon.SetCommandEnabled("undo", history.CanUndo);
ribbon.SetCommandChecked("bold", selection.IsBold);
```

The state is remembered, so items created later (lazy tabs, model regeneration) pick it up.

## Keyboard

| Key | Action |
|---|---|
| Alt, F10 | KeyTips ([details](keytips.md)) |
| Ctrl+F1 | Collapse / pin the ribbon |
| Alt+Q | Focus the search box (`RibbonSearchBox`, subscribed through `Ribbon.SearchRequested`) |
| Esc | Leave KeyTips / close the backstage, popups and drop-downs |
| Arrow keys | Move in toolbars, combo lists, galleries, grid pickers and segmented controls |
| Declared shortcuts | With `IsShortcutRoutingEnabled="True"`, `Shortcut="Ctrl+B"` invokes the item anywhere in the window |

Shortcut routing ignores plain keys while a text box has focus, and on macOS it treats ⌘ as Ctrl
(`RibbonKeyGesture.TreatMetaAsControl`). `RibbonKeyGesture.Parse("Ctrl+Shift+L")` parses and formats gestures,
including OEM keys like `Ctrl+]` and the macOS `⌘⇧L` style.
