# XAML usage

## XAML namespaces

RibbonSpace registers `RibbonSpace.Controls`, `RibbonSpace.Controls.Primitives` and `RibbonSpace.Controls.Mvvm` in
Uno's **global XAML namespace**, so with Uno.Sdk (implicit XAML namespaces are on by default) its types work without
any prefix or declaration, exactly like built-in controls:

```xml
<Page>
  <Grid RowDefinitions="Auto,*">
    <RibbonTitleBar Title="Report" Ribbon="{x:Bind Ribbon}" />
    <Ribbon x:Name="Ribbon" Grid.Row="1">
      <RibbonTab Header="Home">
        <RibbonGroup Header="Clipboard">
          <RibbonButton Label="Cut" Icon="&#xE8C6;" RibbonKeyTip.KeyTip="X" />
        </RibbonGroup>
      </RibbonTab>
    </Ribbon>
  </Grid>
</Page>
```

Elements, property elements (`<Ribbon.QuickAccessToolBar>`) and attached properties (`RibbonKeyTip.KeyTip`,
`RibbonSearch.Keywords`, `RibbonContextualToolBar.Context`) all resolve unprefixed. The same types are also mapped to
the XML namespace `https://github.com/wieslawsoltes/RibbonSpace` (`RibbonXmlns.Uri`) with the default prefix `rs`:

| Form | XAML | Works with |
|---|---|---|
| No prefix (recommended) | `<Ribbon>`, nothing to declare | Uno.Sdk projects (implicit XAML namespaces) |
| Implicit prefix | `<rs:Ribbon>`, nothing to declare | Uno.Sdk projects; use it to disambiguate from another library's type with the same name |
| XML namespace URI | `xmlns:rs="https://github.com/wieslawsoltes/RibbonSpace"` | every Uno target (Skia desktop, WebAssembly, iOS, Android) |
| CLR namespace | `xmlns:rs="using:RibbonSpace.Controls"` (`using:RibbonSpace.Controls.Primitives` for primitives) | everywhere, including **Windows App SDK** heads |

Two limitations, both from the XAML compilers rather than RibbonSpace:
- **Type names inside attribute values** (`Style TargetType`, `ControlTemplate TargetType`) only resolve through a
  `using:` namespace. Declare `xmlns:rs="using:RibbonSpace.Controls"` in files that style RibbonSpace controls:
  `<Style TargetType="rs:RibbonButton">`.
- **Windows App SDK heads** (`net10.0-windows…`) use WinUI's own XAML compiler, which supports neither the global
  namespace nor custom XML namespaces yet ([microsoft-ui-xaml#10616](https://github.com/microsoft/microsoft-ui-xaml/issues/10616)).
  XAML compiled for such a head keeps `xmlns:rs="using:RibbonSpace.Controls"` and `rs:` prefixes. The Uno Skia
  desktop head (`net10.0-desktop`) runs on Windows too and supports every form.

WinUI types always win over a RibbonSpace type with the same name (RibbonSpace types all start with `Ribbon`). To turn
the global namespace off for an app, set `<UnoEnableImplicitXamlNamespaces>false</UnoEnableImplicitXamlNamespaces>`
and declare a prefix.

## Ribbon

```xml
<Ribbon x:Name="Ribbon"
           DisplayMode="Classic"
           VisibilityMode="AlwaysShow"
           Density="Comfortable"
           QuickAccessPosition="AboveRibbon"
           IsShortcutRoutingEnabled="True"
           ItemInvoked="OnItemInvoked">
  <Ribbon.QuickAccessToolBar> <RibbonQuickAccessToolBar> … </RibbonQuickAccessToolBar> </Ribbon.QuickAccessToolBar>
  <Ribbon.Backstage> <RibbonBackstage> … </RibbonBackstage> </Ribbon.Backstage>
  <Ribbon.ContextualGroups>
    <RibbonContextualTabGroup Id="table" Header="Table Tools" Color="#0F7B6C" Activation="SelectOnShow" />
  </Ribbon.ContextualGroups>
  <Ribbon.TabStripItems>
    <RibbonButton Label="Comments" Icon="&#xE90A;" SimplifiedLabel="Show" />
  </Ribbon.TabStripItems>

  <RibbonTab Id="home" Header="Home" KeyTip="H">
    <RibbonGroup Id="clipboard" Header="Clipboard" Icon="&#xE77F;" IsDialogLauncherVisible="True"> … </RibbonGroup>
  </RibbonTab>
  <RibbonTab Id="tableDesign" Header="Table Design" ContextualGroupId="table"> … </RibbonTab>
</Ribbon>
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
`CanAddToQuickAccess`, plus the attached `RibbonSearch.Keywords`.

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
<RibbonButton Label="Paste" Shortcut="Ctrl+V" ScreenTip="Add content from the Clipboard." />
<RibbonButton Label="Paste">
  <RibbonButton.ScreenTip>
    <RibbonScreenTip Title="Paste (Ctrl+V)" Description="…" HelpText="Tell me more" Image="ms-appx:///Assets/paste-help.png" />
  </RibbonButton.ScreenTip>
</RibbonButton>
```

A string becomes the description. The title defaults to the label, and the shortcut is shown next to the title.
