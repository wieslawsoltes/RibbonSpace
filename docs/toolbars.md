# Toolbars and chrome

RibbonSpace items also work outside the ribbon. That covers the command UIs of apps like Photoshop, Illustrator,
Premiere, VS Code and AutoCAD.

## RibbonToolBar

```xml
<!-- Horizontal command bar with "⋯" overflow (the overflow shows linked copies of the hidden items). -->
<RibbonToolBar ShowLabels="False" ItemInvoked="OnToolInvoked">
  <RibbonButton Label="Undo" Icon="&#xE7A7;" />
  <RibbonSeparator />
  <RibbonToggleButton Label="Snap" Icon="&#xE8AD;" IsChecked="True" />
</RibbonToolBar>

<!-- Vertical two-column tool palette with radio tools and a tool split button. -->
<RibbonToolBar Orientation="Vertical" Columns="2">
  <RibbonToggleButton Label="Move (V)" Icon="&#xE7C2;" GroupName="tool" />
  <RibbonToggleButton Label="Brush (B)" Icon="&#xE771;" GroupName="tool" IsChecked="True" />
  <RibbonSplitButton Label="Rectangle" Icon="&#xE739;" Size="Small" FollowLastChoice="True">
    <MenuFlyout>
      <MenuFlyoutItem Text="Rectangle" Tag="&#xE739;" />
      <MenuFlyoutItem Text="Ellipse" Tag="&#xEA3A;" />
    </MenuFlyout>
  </RibbonSplitButton>
</RibbonToolBar>
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
<RibbonContextualToolBar ActiveContext="{x:Bind ViewModel.ActiveTool, Mode=OneWay}">
  <RibbonToolBar RibbonContextualToolBar.Context="brush"> … size, hardness, mode, opacity … </RibbonToolBar>
  <RibbonToolBar RibbonContextualToolBar.Context="text"> … font, size, alignment … </RibbonToolBar>
</RibbonContextualToolBar>
```

Contents are created once and switched instantly, never rebuilt.

## RibbonMenuBar

```xml
<RibbonMenuBar>
  <RibbonMenuBarItem Header="File">
    <MenuFlyoutItem Text="Open..." KeyboardAcceleratorTextOverride="Ctrl+O" />
    <MenuFlyoutSubItem Text="Open Recent"> … </MenuFlyoutSubItem>
  </RibbonMenuBarItem>
</RibbonMenuBar>
```

- **Mouse:** hovering switches between open menus.
- **Keyboard:** Left / Right move between headers and Down opens the menu. `FocusFirst()` supports F10 activation;
  `OpenMenu("Edit")` opens a menu by header.
- **Dynamic state:** the `Opening` event lets you refresh enabled and checked states before a menu opens.

## RibbonTitleBar

```xml
<RibbonTitleBar Ribbon="{x:Bind Ribbon}" Title="Report" Subtitle="• Saved" AppIcon="&#xE8A5;" IsSearchVisible="True">
  <RibbonTitleBar.StartContent><ToggleSwitch OnContent="AutoSave" OffContent="AutoSave" /></RibbonTitleBar.StartContent>
  <RibbonTitleBar.EndContent><!-- account, share, … --></RibbonTitleBar.EndContent>
</RibbonTitleBar>
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
- **Application button:** `IsAppIconMenuEnabled="True"` turns `AppIcon` into a button that opens the ribbon's
  application menu or backstage, as in AutoCAD; handle `AppIconClick` to replace the default action.

## RibbonStatusBar and RibbonZoomControl

```xml
<RibbonStatusBar>
  <TextBlock Text="Page 1 of 3" />
  <RibbonStatusBar.EndItems>
    <RibbonToggleButton Label="Print Layout" Icon="&#xE7C3;" GroupName="view" IsChecked="True" />
    <RibbonZoomControl Value="100" Minimum="10" Maximum="500" ZoomDialogRequested="OnZoomDialog" />
  </RibbonStatusBar.EndItems>
</RibbonStatusBar>
```

Status bar items are small buttons with labels by default. `ShowLabels="False"` makes them icon-only, for example CAD
drafting toggles such as Grid, Snap and Ortho; an item with `ShowLabel="False"` is always icon-only. Items take part in
command resolution and `ItemInvoked` like ribbon items (set `Ribbon` or `CommandCatalog`).
