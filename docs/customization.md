# Customization and state

## State

```csharp
string json = ribbon.SaveStateToJson();         // RibbonState: selected tab, display & visibility mode, density,
ribbon.LoadStateFromJson(json);                 // QAT position / visibility / labels / items, customization

RibbonState state = ribbon.GetState();
await RibbonStateSerializer.SaveAsync(state, path);
ribbon.ApplyState(await RibbonStateSerializer.LoadAsync(path) ?? new RibbonState());

ribbon.StateChanged += (_, _) => Save(ribbon.SaveStateToJson());   // autosave
```

The JSON is versioned (`SchemaVersion`), camel-cased and uses string enums. It is written with
`System.Text.Json` source generation, so it is AOT and trimming safe. Invalid or newer documents are rejected
(`null`) rather than throwing.

## Customization

```csharp
ribbon.ApplyCustomization(new RibbonCustomization
{
    TabOrder = ["insert", "home"],
    GroupOrder = { ["home"] = ["editing", "clipboard"] },   // per-tab group order (unlisted groups follow)
    HiddenTabIds = ["mailings"],
    HiddenGroupIds = ["voice"],
    Labels = { ["home"] = "Start" },
    CustomTabs = [new RibbonCustomTab { Id = "custom.tab", Label = "My Tools",
        Groups = [new RibbonCustomGroup { Id = "custom.g", Label = "Favorites", ItemIds = ["bold", "paste"] }] }],
    CustomGroups = [new RibbonCustomGroup { Id = "custom.home", Label = "Mine", TabId = "home", ItemIds = ["find"] }],
});
ribbon.ResetCustomization();
```

Custom groups contain linked copies of existing items.

## Customize dialog

`ribbon.ShowCustomizeDialog(RibbonCustomizePage.Ribbon)` (or `RibbonCustomizePage.QuickAccessToolbar`) opens Office-style "Customize the
Ribbon" and "Quick Access Toolbar" pages:
- command list with search
- tab and group tree with visibility check boxes
- Add / Remove
- Move Up / Down
- New Tab / New Group
- Rename
- Reset
- Import/Export (JSON)

The dialog works on a copy and applies it on OK. The same dialog is reachable from the context menu and from the QAT
menu (*More Commands...*). To show your own UI instead, handle `CustomizeRequested` and set `Handled = true`. Use
`CanCustomize="False"` to hide the entry points.

## Context menu

Right-clicking an item shows:
- Add to / Remove from Quick Access Toolbar
- Customize Quick Access Toolbar...
- Show QAT Below / Above the Ribbon
- Customize the Ribbon...
- Collapse / Pin the Ribbon

Extend it with `ContextMenuOpening` (add items, or set `Handled`), or disable it with `IsContextMenuEnabled`.
