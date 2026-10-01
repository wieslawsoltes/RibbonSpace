# Accessibility

- **Automation peers:**

  | Element | Role |
  |---|---|
  | `Ribbon` | Tab, with the Selection pattern (selected tab) and ExpandCollapse (minimized ribbon) |
  | `RibbonTabHeader` | TabItem, with the SelectionItem pattern (screen readers announce "selected") |
  | `RibbonGroup` | Group, named by its header |
  | `RibbonQuickAccessToolBar`, `RibbonToolBar` | ToolBar |
  | Buttons, toggles, check boxes | the platform peers |

- **Names and ids:**
  - `AutomationProperties.Name` comes from `Label`.
  - `AutomationId` comes from `Id` (tab headers use `RibbonTab_{id}`, backstage buttons `Backstage_{id}`).
  - `AcceleratorKey` comes from `Shortcut`, and `AccessKey` from `KeyTip`.
  - Stable ids keep existing UI tests working.
- **Keyboard:**
  - KeyTips (Alt / F10) reach every command, including collapsed groups, dialog launchers, overflow, the backstage
    and the QAT.
  - The tab row is a single Tab stop (roving focus): **Left / Right / Home / End** switch tabs (mirrored in
    right-to-left layouts) and **Down** moves into the selected tab's commands.
  - Tab, arrow-key navigation in toolbars, lists, galleries, grid pickers and segmented controls.
  - Esc closes popups (collapsed groups, "Show tabs only" popup, backstage, menus).
  - KeyTip mode takes keyboard focus, so typed KeyTip letters never reach a focused text box or document, and gives
    focus back when the mode ends.
  - Ctrl+F1 collapses the ribbon and Alt+Q focuses search.
- **High contrast:**
  - A dedicated `HighContrast` theme dictionary is included.
  - Checked and hover states use borders as well as fills.
  - Disabled items use a distinct colour.
- **ScreenTips:** titles, shortcuts and descriptions are exposed as tooltips.
- **Motion:** only the backstage entrance animation moves, and it is short (≤ 220 ms).
