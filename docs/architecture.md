# Architecture

```
RibbonSpace.Core  (net10.0 / net9.0, no UI dependency, AOT & trimming safe)
├── Model/        RibbonModel, tabs, groups, items, menus, galleries, backstage, contextual groups, icons, ScreenTips
├── Commands/     RibbonRelayCommand, RibbonAsyncCommand, RibbonCommandCatalog, RibbonKeyGesture
├── Layout/       RibbonAdaptiveLayout, RibbonGroupItemsArranger, RibbonSimplifiedLayout, RibbonSizeDefinition
├── KeyTips/      RibbonKeyTipAssigner, RibbonKeyTipScope / Navigator
├── Search/       RibbonSearchEngine
├── State/        RibbonState, RibbonCustomization, RibbonStateSerializer (source-generated JSON)
├── Theming/      RibbonColor, RibbonColorPalette (Office shades), RibbonThemePalette presets
└── Localization/ RibbonStrings

RibbonSpace.Uno   (Uno Platform 6, all targets)
├── Controls/     Ribbon, RibbonTab, RibbonGroup, items, QAT, backstage, search, title/status bars, toolbars, menu bar
├── Primitives/   RibbonItemContent, RibbonIconPresenter, panels (groups, items, scroll, toolbar, uniform grid), metrics
├── Mvvm/         RibbonElementFactory, RibbonModelBindings (reflection-free two-way sync)
├── Dialogs/      RibbonCustomizeDialog
├── Theming/      RibbonThemeResources (Light / Dark / HighContrast), RibbonTheme API
└── Themes/       Generic.xaml, Shared.xaml, Items.xaml, Ribbon.xaml, Inputs.xaml, Backstage.xaml, Chrome.xaml
```

## Design principles

- **Lookless controls.** Every visual is a `ControlTemplate` in `Themes/*.xaml`. Colours come from `ThemeResource`
  brush keys, so an app can re-template or re-colour any control.
- **Two usage styles, one element tree.** XAML-declared elements and MVVM-generated elements are the same controls,
  so every feature works in both.
- **UI-agnostic algorithms.** Adaptive reduction, item arrangement, simplified overflow, KeyTip assignment, search
  ranking, colour shades and state serialization live in Core and are covered by unit tests.
- **No hard-coded widths.** Groups are measured in every state (Large, Medium, Small, Collapsed). The widths are
  cached and invalidated only when content, metrics or presentation change.
- **Linked copies.** The QAT, custom groups and overflow menus show *linked copies* of real items (`CreateLinkedCopy`).
  State is synchronized both ways, and invoking a copy runs the original. That way Click handlers, commands and
  `ItemInvoked` fire exactly once.
- **Everything by id.** Tabs, groups and items have stable ids. Automation ids, persistence, customization, QAT
  restore, command state and `ItemInvoked` routing all use them.
- **No reflection in hot paths.** MVVM sync uses `INotifyPropertyChanged` subscriptions and
  `RegisterPropertyChangedCallback`, and JSON uses source generation.

## Layout pipeline

```
Ribbon ─ tab row (application button, RibbonScrollPanel[tab headers], tab-row items, end content)
       ─ command bar card
            RibbonTab → RibbonScrollPanel → RibbonGroupsPanel
                           │ classic:    measure each group per state → RibbonAdaptiveLayout.Compute → ApplyState
                           │ simplified: measure item widths → RibbonSimplifiedLayout.Compute → in-line mask + overflow
                           └ RibbonGroup → RibbonGroupItemsPanel (columns / rows / single line) → items
       ─ QAT hosts (above / below), full-screen reveal bar
```

Each item implements `IRibbonItem.ApplyLayout(RibbonItemLayout)`. The layout carries the size (Large, Medium or
Small), the metrics, whether the item is in the simplified line, a label override and the group state. The shared
`RibbonItemContent` panel draws icon, label and chevron for every size. Large labels are split optimally over two
lines, as in Office.

## Popups

Collapsed groups, the minimized ribbon, galleries, combo boxes, search results, KeyTips and the backstage use
`Popup`s placed with `RibbonPopupPlacement`, which keeps them inside the window. Collapsed groups and the minimized
ribbon *move* their existing elements into the popup and move them back afterwards, so element state is never
duplicated.
