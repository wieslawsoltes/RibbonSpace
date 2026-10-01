# Galleries and pickers

## RibbonGallery

```xml
<rs:RibbonGallery Id="styles" Label="Styles" Icon="&#xE790;" MaxColumns="6" MinColumns="3" Rows="1"
                  ItemWidth="76" ItemHeight="62" IsFilterEnabled="True"
                  Command="{x:Bind ViewModel.ApplyStyleCommand}" PreviewCommand="{x:Bind ViewModel.PreviewStyleCommand}">
  <rs:RibbonGalleryItem Label="Heading 1" Value="H1" PreviewText="AaBbCc" PreviewFontSize="17" PreviewForeground="#2F5496" Category="Headings" />
  <rs:RibbonGallery.FooterItems>
    <rs:RibbonButton Label="Clear Formatting" Icon="&#xE8E6;" />
  </rs:RibbonGallery.FooterItems>
</rs:RibbonGallery>
```

- **In the ribbon:** a window of `Rows` × columns with up/down row scrolling and a *More* button. Columns adapt to
  the group state (Large → `MaxColumns`, Medium → `MinColumns`); in the Small state the gallery becomes a
  drop-down button.
- **Expanded popup:**
  - opens in place over the inline gallery, with `DropDownColumns` columns
  - category headers (`Category` or `CategorySelector`)
  - an optional filter box
  - footer commands
- **Live preview:** `PreviewCommand` and the `ItemPreview` event receive the hovered value, then `null` when the
  pointer leaves.
- **Items:**
  - `RibbonGalleryItem` for text, colour or icon previews
  - any data object with `ItemTemplate`
  - `ItemsSource` for MVVM (`RibbonGalleryModel`)
- **Events:** `ItemClick`, `SelectedItem`, `Command`.

## Colour picker

```xml
<rs:RibbonColorPicker Label="Font Color" Icon="&#xE8D3;" SelectedColor="#C00000" ShowAutomatic="True" ShowNoColor="False" ShowMoreColors="True" IsSplit="True" />
```

- **Button:** a split button with a colour bar under the icon. Clicking the icon applies the current colour; the
  arrow opens the palette.
- **Palette** (`RibbonColorPalette`, also usable stand-alone):
  - Automatic
  - theme colours: 10 base colours plus 5 generated Office shades (lighter 80/60/40 %, darker 25/50 %, with special
    rules for near-white and near-black)
  - standard colours
  - recent colours
  - No Color
  - More Colors..., which opens `RibbonColorDialog` (HSL sliders, hex input, new/current preview)
- **Customizing:** `ThemeColors` and `StandardColors` accept your own lists; `SetColorName(color, "Blue, Accent 1")`
  names swatches for tooltips and screen readers (MVVM swatch names map automatically).
- **Keyboard:** arrow keys move between swatches, Enter / Space applies, Esc closes; the selected colour has a ring.
- **Events:** `ColorSelected`, and `ColorPreview` for live preview.
- **Command parameter:** the `Color`, or `RibbonColorPicker.NoColorParameter` for No Color. Pickers generated from
  a `RibbonColorPickerModel` pass a `RibbonColor?` instead (`null` = No Color).

## Grid picker (Insert Table)

```xml
<rs:RibbonDropDownButton Label="Table" Icon="&#xE80A;" Size="Large">
  <rs:RibbonDropDownButton.Flyout>
    <Flyout><rs:RibbonGridPicker Rows="8" Columns="10" Command="{x:Bind ViewModel.InsertTableCommand}" /></Flyout>
  </rs:RibbonDropDownButton.Flyout>
</rs:RibbonDropDownButton>
```

Hovering highlights cells and updates the caption ("3x4 Table"). Arrow keys and Enter work too. The command
receives a `RibbonGridSize`.

## Combo boxes

- **Behaviour:**
  - `IsEditable`: type any value, and Enter or losing focus commits it. Values that aren't in the list are kept,
    which fixes the common ComboBox limitation.
  - `IsFontPreview`: each entry renders in its own font.
  - `PlaceholderText` shows for mixed values (for example "*Varies*").
  - Item text comes from `ItemTextSelector` (preferred for trimmed / AOT apps), then `DisplayMemberPath`
    (evaluated with a data binding), then `RibbonNodeModel.Label` or `ToString()`. `ItemTemplate` customizes the list.
- **Keyboard:** Up/Down change the value, Alt+Down or F4 opens the list, Esc reverts.
- **Events:** `Committed` (`Item` + `Text`), `SelectionChanged`, and `Command` with the item or the text.
- **Presets:**
  - `RibbonFontComboBox` (common cross-platform fonts, editable, preview)
  - `RibbonFontSizeComboBox` (Office sizes 8–72)

## Spinner

```xml
<rs:RibbonSpinner Label="Before:" Value="6" Minimum="0" Maximum="1584" Increment="6" Unit="pt" Format="0" InputWidth="72" />
```

- **Input:** arrows (repeat buttons), Up/Down, PageUp/PageDown (×10), the mouse wheel, and dragging the label
  horizontally ("scrubbing", `IsScrubEnabled`).
- **Parsing:** units are stripped before parsing, and values are clamped to `Minimum`/`Maximum`.
- **Events:** `ValueChanged` (every change, including bindings) and `ValueCommitted` (user changes only: arrows,
  keys, wheel, typed text, end of a scrub). `Command` runs on `ValueCommitted`, so view-model updates never
  re-execute it. `RibbonSlider` follows the same rule.

## Segmented control

```xml
<rs:RibbonSegmentedControl SelectedIndex="0">
  <rs:RibbonSegment Label="Editing" Value="edit" />
  <rs:RibbonSegment Label="Color" Value="color" />
</rs:RibbonSegmentedControl>
```
