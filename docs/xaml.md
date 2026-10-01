# XAML usage

Add the namespace `xmlns:rs="using:RibbonSpace.Controls"`.

```xml
<rs:Ribbon x:Name="Ribbon"
           DisplayMode="Classic"
           VisibilityMode="AlwaysShow"
           Density="Comfortable"
           QuickAccessPosition="AboveRibbon"
           IsShortcutRoutingEnabled="True"
           ItemInvoked="OnItemInvoked">
  <rs:Ribbon.QuickAccessToolBar> <rs:RibbonQuickAccessToolBar> … </rs:RibbonQuickAccessToolBar> </rs:Ribbon.QuickAccessToolBar>
  <rs:Ribbon.Backstage> <rs:RibbonBackstage> … </rs:RibbonBackstage> </rs:Ribbon.Backstage>
  <rs:Ribbon.ContextualGroups>
    <rs:RibbonContextualTabGroup Id="table" Header="Table Tools" Color="#0F7B6C" Activation="SelectOnShow" />
  </rs:Ribbon.ContextualGroups>
  <rs:Ribbon.TabStripItems>
    <rs:RibbonButton Label="Comments" Icon="&#xE90A;" SimplifiedLabel="Show" />
  </rs:Ribbon.TabStripItems>

  <rs:RibbonTab Id="home" Header="Home" KeyTip="H">
    <rs:RibbonGroup Id="clipboard" Header="Clipboard" Icon="&#xE77F;" IsDialogLauncherVisible="True"> … </rs:RibbonGroup>
  </rs:RibbonTab>
  <rs:RibbonTab Id="tableDesign" Header="Table Design" ContextualGroupId="table"> … </rs:RibbonTab>
</rs:Ribbon>
```

## Items

| Element | Purpose |
|---|---|
| `RibbonButton` | Push button (large / medium / small / simplified). `Flyout` turns it into a drop-down. |
| `RibbonDropDownButton` | Button with a chevron that opens its `Flyout` (`MenuFlyout` or `Flyout` with any content). |
| `RibbonSplitButton` | Primary action and drop-down part. `IsCheckable` / `IsChecked` for Bullets, `FollowLastChoice` for tool alternatives. |
| `RibbonToggleButton` | Toggle. `GroupName` makes a radio group (scope: ribbon, toolbar or group). |
| `RibbonCheckBox` | Compact check box. |
| `RibbonComboBox`, `RibbonFontComboBox`, `RibbonFontSizeComboBox` | Editable or read-only combo boxes. |
| `RibbonSpinner` | Numeric input with unit, format, arrows, wheel and label scrubbing. |
| `RibbonTextBox`, `RibbonSlider`, `RibbonLabel`, `RibbonSeparator` | Text input, slider, static text, separator. |
| `RibbonGallery` + `RibbonGalleryItem` | In-ribbon gallery with an expanded popup. |
| `RibbonColorPicker` | Office colour split button. |
| `RibbonGridPicker` | Table size picker (usually inside a drop-down's `Flyout`). |
| `RibbonSegmentedControl` + `RibbonSegment` | Single-selection segments. |
| `RibbonButtonGroup` | Joined row of small icon buttons. |
| `RibbonStackPanel` | Row (or column) container. Use it in groups with `ItemsLayout="Rows"`. |
| Any `UIElement` | Arbitrary content is laid out as a small row item, or as a full-height item if it is tall. |

Common item properties: `Id`, `Label`, `Icon`, `LargeIcon`, `Size`, `SizeDefinition`, `KeyTip`, `ScreenTip`,
`Shortcut`, `CommandId`, `Command`, `CommandParameter`, `SimplifiedVisibility`, `SimplifiedLabel`, `ShowLabel`,
`CanAddToQuickAccess`, plus the attached `rs:RibbonSearch.Keywords`.

## Icons

`Icon` accepts any of the following:
- a glyph (`"&#xE8DD;"`)
- SVG / XAML path data (`"M3 4h18v2H3z …"`)
- an image URI (`ms-appx:///Assets/paste.png`, `.svg`)
- a short text (`"Aa"`, `"¶"`, `"$"`)
- a Core `RibbonIcon` (glyph / path / image / text with an optional fixed colour)
- an `IconSource` (`FontIconSource`, `PathIconSource`, `BitmapIconSource`, `SymbolIconSource`, `ImageIconSource`)
- an `IconElement`
- an `ImageSource`

`RibbonIcons` (Core) contains about 200 Office command glyphs. The few glyphs missing from the Uno Fluent font are
provided as vector paths.

## ScreenTips

```xml
<rs:RibbonButton Label="Paste" Shortcut="Ctrl+V" ScreenTip="Add content from the Clipboard." />
<rs:RibbonButton Label="Paste">
  <rs:RibbonButton.ScreenTip>
    <rs:RibbonScreenTip Title="Paste (Ctrl+V)" Description="…" HelpText="Tell me more" Image="ms-appx:///Assets/paste-help.png" />
  </rs:RibbonButton.ScreenTip>
</rs:RibbonButton>
```

A string becomes the description. The title defaults to the label, and the shortcut is shown next to the title.
