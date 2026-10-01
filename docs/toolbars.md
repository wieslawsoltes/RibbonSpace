# Toolbars and chrome

RibbonSpace items also work outside the ribbon. That covers the command UIs of apps like Photoshop, Illustrator,
Premiere, VS Code and AutoCAD.

## RibbonToolBar

```xml
<!-- Horizontal command bar with "⋯" overflow (the overflow shows linked copies of the hidden items). -->
<rs:RibbonToolBar ShowLabels="False" ItemInvoked="OnToolInvoked">
  <rs:RibbonButton Label="Undo" Icon="&#xE7A7;" />
  <rs:RibbonSeparator />
  <rs:RibbonToggleButton Label="Snap" Icon="&#xE8AD;" IsChecked="True" />
</rs:RibbonToolBar>

<!-- Vertical two-column tool palette with radio tools and a tool split button. -->
<rs:RibbonToolBar Orientation="Vertical" Columns="2">
  <rs:RibbonToggleButton Label="Move (V)" Icon="&#xE7C2;" GroupName="tool" />
  <rs:RibbonToggleButton Label="Brush (B)" Icon="&#xE771;" GroupName="tool" IsChecked="True" />
  <rs:RibbonSplitButton Label="Rectangle" Icon="&#xE739;" Size="Small" FollowLastChoice="True">
    <MenuFlyout>
      <MenuFlyoutItem Text="Rectangle" Tag="&#xE739;" />
      <MenuFlyoutItem Text="Ellipse" Tag="&#xEA3A;" />
    </MenuFlyout>
  </rs:RibbonSplitButton>
</rs:RibbonToolBar>
```

- **Properties:**
  - `Orientation`, `Columns` (vertical palettes), `ShowLabels`, `IsOverflowEnabled`
  - `Density`
  - `CommandCatalog`
  - `Ribbon`, an optional link for the context menu and shared command state
- **Behaviour:**
  - arrow-key navigation
  - separators rotate automatically in vertical toolbars
  - with `FollowLastChoice`, the split button shows the last chosen tool (glyph in `Tag` or `Icon`) and exposes it as
    `LastChoice`; `Label` and `Icon` (and their bindings) stay untouched
- **Also serves as:** activity rails, like the VS Code / LightSpace panel launchers.

## RibbonContextualToolBar (tool option bars)

```xml
<rs:RibbonContextualToolBar ActiveContext="{x:Bind ViewModel.ActiveTool, Mode=OneWay}">
  <rs:RibbonToolBar rs:RibbonContextualToolBar.Context="brush"> … size, hardness, mode, opacity … </rs:RibbonToolBar>
  <rs:RibbonToolBar rs:RibbonContextualToolBar.Context="text"> … font, size, alignment … </rs:RibbonToolBar>
</rs:RibbonContextualToolBar>
```

Contents are created once and switched instantly, never rebuilt.

## RibbonMenuBar

```xml
<rs:RibbonMenuBar>
  <rs:RibbonMenuBarItem Header="File">
    <MenuFlyoutItem Text="Open..." KeyboardAcceleratorTextOverride="Ctrl+O" />
    <MenuFlyoutSubItem Text="Open Recent"> … </MenuFlyoutSubItem>
  </rs:RibbonMenuBarItem>
</rs:RibbonMenuBar>
```

- **Mouse:** hovering switches between open menus.
- **Keyboard:** Left / Right move between headers and Down opens the menu. `FocusFirst()` supports F10 activation;
  `OpenMenu("Edit")` opens a menu by header.
- **Dynamic state:** the `Opening` event lets you refresh enabled and checked states before a menu opens.

## RibbonTitleBar

```xml
<rs:RibbonTitleBar Ribbon="{x:Bind Ribbon}" Title="Report" Subtitle="• Saved" AppIcon="&#xE8A5;" IsSearchVisible="True">
  <rs:RibbonTitleBar.StartContent><ToggleSwitch OnContent="AutoSave" OffContent="AutoSave" /></rs:RibbonTitleBar.StartContent>
  <rs:RibbonTitleBar.EndContent><!-- account, share, … --></rs:RibbonTitleBar.EndContent>
</rs:RibbonTitleBar>
```

- **Contents:**
  - the ribbon's QAT (when it is placed above the ribbon)
  - a centred title
  - a Microsoft Search box (Alt+Q)
- **Adaptive:** the search box hides below 720 px and the title below 480 px.
- **Window integration:** `titleBar.AttachToWindow(window)` extends content into the OS title bar where the
  platform supports it and reserves space for the caption buttons (`CaptionButtonsInset`).
- **Colourful chrome:** `RibbonTheme.ApplyChromeStyle(RibbonChromeStyle.Colorful)` paints the title bar and tab row
  in the accent colour.

## RibbonStatusBar and RibbonZoomControl

```xml
<rs:RibbonStatusBar>
  <TextBlock Text="Page 1 of 3" />
  <rs:RibbonStatusBar.EndItems>
    <rs:RibbonToggleButton Label="Print Layout" Icon="&#xE7C3;" GroupName="view" IsChecked="True" />
    <rs:RibbonZoomControl Value="100" Minimum="10" Maximum="500" ZoomDialogRequested="OnZoomDialog" />
  </rs:RibbonStatusBar.EndItems>
</rs:RibbonStatusBar>
```
