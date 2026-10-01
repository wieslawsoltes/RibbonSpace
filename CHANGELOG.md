# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- `RibbonSpace.Core`:
  - MVVM ribbon model (tabs, groups, 18 item kinds, menus, galleries, colour pickers, backstage, contextual groups,
    QAT, tab-row items)
  - command catalog, relay and async commands, key gestures
  - adaptive layout, item arrangement and simplified overflow algorithms
  - KeyTip assignment and navigation
  - ranked command search
  - versioned, source-generated state JSON and customization model
  - Office colour palettes with shade generation, and application accent palettes (Word, Excel, PowerPoint, ...)
  - localization (en, de, fr, es, pl)
  - model merging
- `RibbonSpace.Uno` ribbon:
  - `Ribbon` with classic and simplified layouts, adaptive group resizing, collapsed-group popups and scroll
    fallback
  - visibility modes (always, tabs only with popup, full screen)
  - density and custom metrics
  - contextual tab groups
  - tab-row items
  - application button (backstage or application menu)
  - display-options menu and context menu
  - KeyTips, including menu entries of drop-down and split buttons, with focus handling and Alt / F10 toggling
  - shortcut routing limited to reachable items
  - keyboard navigation of the tab row (roving tab stop, arrows, Home / End, Down into commands)
  - Selection / SelectionItem / ExpandCollapse automation patterns
  - state and customization
  - MVVM `Model` binding
- `RibbonSpace.Uno` controls:
  - items: buttons, drop-down and split buttons, toggles with radio groups, check boxes, combo boxes (font and size
    presets), spinners, text boxes, sliders, labels, separators, button groups, stack panels, segmented controls
  - `RibbonGallery` with live preview and an expanded popup
  - `RibbonColorPicker` / `RibbonColorPalette` / `RibbonColorDialog`
  - `RibbonGridPicker`
  - `RibbonScreenTip`
- Chrome and toolbars:
  - Quick Access Toolbar with linked copies and customize menu
  - `RibbonBackstage`
  - `RibbonSearchBox`, `RibbonCommandPalette`
  - `RibbonTitleBar`, `RibbonStatusBar`, `RibbonZoomControl`
  - `RibbonToolBar` (horizontal, vertical, columns, overflow), `RibbonContextualToolBar`, `RibbonMenuBar`
  - `RibbonCustomizeDialog`
- Theming: Light / Dark / HighContrast (system colours) theme resources with live palette and chrome switching;
  `RibbonTheme.GetBrush` / `SetThemeBrush` for theme-aware code-built visuals.
- XAML without namespace boilerplate: RibbonSpace types are registered in Uno's global XAML namespace (`<Ribbon>`,
  no prefix or declaration in Uno.Sdk projects) and in the XML namespace `https://github.com/wieslawsoltes/RibbonSpace`
  (`RibbonXmlns`) with the implicit `rs` prefix; `using:` remains for Windows App SDK heads.
- Persistence of recent searches, QAT defaults and reset (`DefaultQuickAccessItemIds`, `ResetQuickAccess`).
- Tooling and samples:
  - automation peers
  - gallery sample (Word, Excel, PowerPoint, toolbars, settings)
  - runtime UI test runner
  - screenshot automation
  - CI, Pages and Trusted Publishing release workflows
