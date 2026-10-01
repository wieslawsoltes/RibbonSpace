# KeyTips

Press **Alt** (or **F10**) to show KeyTips for the File button, the QAT items (`1`, `2`, …), the tabs and the
tab-row items. Typing a tab's KeyTip selects the tab and shows KeyTips for its commands, including:
- collapsed-group buttons
- dialog launchers
- items inside button groups and rows
- the simplified ⋯ button

What activating a KeyTip does:

| Target | Action |
|---|---|
| Button, toggle, check box | invoke |
| Drop-down, split button (menu) | open the menu and show KeyTips for its entries |
| Gallery, colour picker, rich drop-down | open the drop-down |
| Combo box, spinner, text box | focus the input |
| Collapsed group | open its popup and show KeyTips for its items |
| File | open the backstage and show KeyTips for its navigation |

Keys:
- **Backspace** removes the last typed letter.
- **Esc** goes back one level (closing popups opened by KeyTips).
- **Alt** or **F10** again, or clicking anywhere, ends KeyTip mode and closes the popups KeyTips opened.
- While KeyTips are shown, keyboard focus is in the ribbon; it returns to the previously focused element when the
  mode ends (unless the invoked command moved it, e.g. into an input).

## Assignment

Explicit KeyTips (`KeyTip="FP"`) win. Everything else is generated from labels by `RibbonKeyTipAssigner`:
1. first letters of the label, then other letters
2. two-letter combinations
3. digits

In large scopes (more than 35 commands) single-character tips are rationed so that enough first characters remain
free to prefix two-character tips; the QAT continues `1`…`9`, `09`…`01`, `0A`…`0Z`, then `009`…, so every
command stays reachable. The result is always unique and prefix-free, so "F" and "FP" never coexist. Diacritics are normalized. Groups use
`RibbonGroup.KeyTip` for their collapsed button, and the File button uses `ApplicationButtonKeyTip` (default `F`).

## Automation API

```csharp
ribbon.ShowKeyTips();
ribbon.CurrentKeyTips;          // (KeyTip, Target) of the current level
ribbon.ProcessKeyTipInput('H'); // type a key
ribbon.PopKeyTipLevel();        // Esc
ribbon.CancelKeyTips();         // Alt / click elsewhere: leave KeyTip mode and close KeyTip popups
ribbon.HideKeyTips();           // hide badges only
ribbon.KeyTipModeChanged += …;
```

Disable KeyTips with `IsKeyTipsEnabled="False"`.
