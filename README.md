<p align="center">
  <img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/build/icon.png" width="96" alt="RibbonSpace" />
</p>

<h1 align="center">RibbonSpace</h1>

<p align="center">
  <b>A modern, Office-style Ribbon for Uno Platform.</b><br/>
  Classic and simplified layouts, adaptive resizing, contextual tabs, backstage, Quick Access Toolbar, KeyTips,
  galleries with live preview, Office color pickers, ScreenTips, command search, customization, toolbars and full MVVM.
</p>

<p align="center">
  <a href="https://github.com/wieslawsoltes/RibbonSpace/actions/workflows/build.yml"><img src="https://github.com/wieslawsoltes/RibbonSpace/actions/workflows/build.yml/badge.svg" alt="Build" /></a>
  <a href="https://www.nuget.org/packages/RibbonSpace.Uno"><img src="https://img.shields.io/nuget/v/RibbonSpace.Uno.svg?label=RibbonSpace.Uno" alt="NuGet" /></a>
  <a href="https://www.nuget.org/packages/RibbonSpace.Core"><img src="https://img.shields.io/nuget/v/RibbonSpace.Core.svg?label=RibbonSpace.Core" alt="NuGet" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT" /></a>
  <a href="https://wieslawsoltes.github.io/RibbonSpace/"><img src="https://img.shields.io/badge/demo-WebAssembly-5B5FC7.svg" alt="Live demo" /></a>
</p>

![Word-style ribbon built with RibbonSpace](https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/word.png)

RibbonSpace is the shared command UI for the Uno "Space" applications (TextSpace, GridSpace, PresentationSpace, NoteSpace,
DataSpace, CadSpace, and ImageSpace, VectorSpace and the other toolbar-based apps). It was designed after studying all seventeen
of them and modern Word, Excel and PowerPoint ([research](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/research/feature-inventory.md)).

## Highlights

| | |
|---|---|
| **Adaptive layout** | Groups shrink Large → Medium → Small → Collapsed (popup), with `SizeDefinition`, `ReductionOrder`, row layouts (Font / Paragraph) and scrolling as the last resort. |
| **Simplified ribbon** | Office's single-line ribbon with a "More options" overflow of linked copies, plus per-item `SimplifiedVisibility`. |
| **Contextual tabs** | Coloured contextual tab groups ("Table Tools") with auto-select, and the previous tab restored when they hide. |
| **Backstage** | Full-window File view with pages, actions, footer items, an accent pane, animation and KeyTips. |
| **Quick Access Toolbar** | Add or remove any command (linked copies that stay in sync), customize menu, above / below / title bar, labels, persistence. |
| **KeyTips** | Alt / F10 with multi-level, multi-letter, collision-free tips that reach collapsed groups, launchers, overflow, backstage and the QAT. |
| **Command controls** | Buttons, split buttons (checkable, follow-last-choice), drop-downs, toggles and radio groups, check boxes, button groups, stacks. |
| **Inputs** | Editable combo boxes (font preview, values not in the list), spinners with units and drag-scrubbing, text boxes, sliders, segmented controls. |
| **Galleries** | In-ribbon galleries that resize with the group, expand into categorized, filterable popups, have footer commands and **live preview**. |
| **Colour and table pickers** | Office palette with theme shades, standard / recent / automatic / no colour and More Colors; Insert Table grid picker. |
| **Search** | Microsoft Search box (Alt+Q): fuzzy, acronym and keyword ranking over every command, plus a VS Code-style command palette. |
| **Title bar and status bar** | QAT, title, search and account; OS title-bar integration; status bar with Office zoom control. |
| **Toolbars** | Horizontal / vertical / two-column tool palettes with "⋯" overflow, tool option bars, a classic menu bar. |
| **MVVM** | Bind a `RibbonModel`. Everything is generated and **two-way synchronized**; string-id routing via `ItemInvoked`, `ICommand`, or a command catalog. |
| **Customization and state** | Office "Customize the Ribbon / QAT" dialog, hide / rename / reorder, custom tabs and groups, import / export, versioned AOT-safe JSON state, plugin merging. |
| **Theming** | Lookless templates; Light / Dark / HighContrast; 11 application palettes plus custom accents, applied live; Colorful or Neutral chrome; Compact / Comfortable / Touch density; fully custom metrics. |
| **Quality** | Accessibility peers and stable automation ids, localization (en, de, fr, es, pl), RTL, 91 unit tests and 64 runtime UI tests, trimming / AOT friendly. |

<table>
<tr>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/excel.png" alt="Excel style MVVM ribbon" /><br/><sub>Excel: 100% MVVM (<code>RibbonModel</code>)</sub></td>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/ppt.png" alt="PowerPoint simplified ribbon" /><br/><sub>PowerPoint: simplified ribbon</sub></td>
</tr>
<tr>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/keytips-home.png" alt="KeyTips" /><br/><sub>KeyTips (Alt → H)</sub></td>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/gallery.png" alt="Gallery" /><br/><sub>Expanded gallery with categories, filter and footer</sub></td>
</tr>
<tr>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/backstage.png" alt="Backstage" /><br/><sub>Backstage</sub></td>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/customize.png" alt="Customize dialog" /><br/><sub>Customize the Ribbon</sub></td>
</tr>
<tr>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/color.png" alt="Color picker" /><br/><sub>Office colour picker</sub></td>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/search.png" alt="Search" /><br/><sub>Command search (Alt+Q)</sub></td>
</tr>
<tr>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/dark.png" alt="Dark theme" /><br/><sub>Dark theme</sub></td>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/colorful.png" alt="Colorful chrome" /><br/><sub>Colorful chrome, PowerPoint palette</sub></td>
</tr>
<tr>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/contextual.png" alt="Contextual tabs" /><br/><sub>Contextual tabs (Table Tools)</sub></td>
<td><img src="https://raw.githubusercontent.com/wieslawsoltes/RibbonSpace/main/docs/images/tools.png" alt="Toolbars" /><br/><sub>Menu bar, option bar, tool palette, rail, floating command bar</sub></td>
</tr>
</table>

## Install

```bash
dotnet add package RibbonSpace.Uno
```

| Package | Contents |
|---|---|
| `RibbonSpace.Uno` | Controls for Uno Platform 6: desktop (Windows, macOS, Linux), WebAssembly, iOS, Android, WinAppSDK |
| `RibbonSpace.Core` | UI-agnostic models, commands, layout algorithms, KeyTips, search, state, palettes, strings (`net10.0`, `net9.0`) |

## Quick start (XAML)

```xml
<Grid xmlns:rs="using:RibbonSpace.Controls" RowDefinitions="Auto,Auto,*">
  <rs:RibbonTitleBar Title="Report" AppIcon="&#xE8A5;" Ribbon="{x:Bind Ribbon}" />
  <rs:Ribbon x:Name="Ribbon" Grid.Row="1" ItemInvoked="OnItemInvoked">
    <rs:RibbonTab Id="home" Header="Home">
      <rs:RibbonGroup Id="clipboard" Header="Clipboard" IsDialogLauncherVisible="True">
        <rs:RibbonSplitButton Id="paste" Label="Paste" Icon="&#xE77F;" SizeDefinition="Large" Shortcut="Ctrl+V">
          <MenuFlyout><MenuFlyoutItem Text="Keep Text Only" /></MenuFlyout>
        </rs:RibbonSplitButton>
        <rs:RibbonButton Id="cut" Label="Cut" Icon="&#xE8C6;" Command="{x:Bind ViewModel.CutCommand}" />
        <rs:RibbonButton Id="copy" Label="Copy" Icon="&#xE8C8;" />
      </rs:RibbonGroup>
      <rs:RibbonGroup Id="font" Header="Font" ItemsLayout="Rows" RowCount="2">
        <rs:RibbonStackPanel>
          <rs:RibbonFontComboBox Text="Aptos" />
          <rs:RibbonFontSizeComboBox Text="11" />
        </rs:RibbonStackPanel>
        <rs:RibbonStackPanel>
          <rs:RibbonButtonGroup>
            <rs:RibbonToggleButton Id="bold" Label="Bold" Icon="&#xE8DD;" Shortcut="Ctrl+B" IsChecked="{x:Bind ViewModel.IsBold, Mode=TwoWay}" />
            <rs:RibbonToggleButton Id="italic" Label="Italic" Icon="&#xE8DB;" Shortcut="Ctrl+I" />
          </rs:RibbonButtonGroup>
          <rs:RibbonColorPicker Id="fontColor" Label="Font Color" />
        </rs:RibbonStackPanel>
      </rs:RibbonGroup>
    </rs:RibbonTab>
  </rs:Ribbon>
</Grid>
```

## Quick start (MVVM)

```csharp
var ribbon = new RibbonModel { CommandCatalog = catalog };
ribbon.Tabs.Add(new RibbonTabModel("home", "Home")
{
    Groups =
    {
        new RibbonGroupModel("clipboard", "Clipboard", RibbonIcons.Paste)
        {
            Items =
            {
                new RibbonSplitButtonModel("paste", "Paste", RibbonIcons.Paste) { CommandId = "paste" },
                new RibbonToggleButtonModel("bold", "Bold", RibbonIcons.Bold) { Shortcut = "Ctrl+B" },
                new RibbonColorPickerModel("fontColor", "Font Color", RibbonIcons.FontColor),
            },
        },
    },
});
```

```xml
<rs:Ribbon Model="{x:Bind ViewModel.Ribbon}" />
```

Checked states, values, selection, display modes, contextual groups, the QAT and the backstage stay in two-way sync.
See [MVVM](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/mvvm.md).

## Documentation

[Getting started](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/getting-started.md) · [Architecture](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/architecture.md) · [XAML](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/xaml.md) ·
[MVVM](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/mvvm.md) · [Commands and keyboard](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/commands.md) · [Controls reference](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/controls.md) ·
[Layout](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/layout.md) · [Tabs](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/tabs.md) · [QAT](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/quick-access-toolbar.md) ·
[Backstage](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/backstage.md) · [KeyTips](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/keytips.md) · [Search](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/search.md) ·
[Galleries and pickers](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/galleries.md) · [Toolbars and chrome](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/toolbars.md) ·
[Theming](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/theming.md) · [Localization](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/localization.md) · [Customization and state](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/customization.md) ·
[Accessibility](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/accessibility.md) · [Migration](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/migration.md) · [Testing](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/testing.md) ·
[Releasing](https://github.com/wieslawsoltes/RibbonSpace/blob/main/docs/releasing.md)

## Gallery app

```bash
dotnet run --project samples/RibbonSpace.Demo -f net10.0-desktop
```

It has Word (XAML), Excel (MVVM), PowerPoint (simplified), toolbars and palettes, and settings (theme, palette,
chrome, density, layout, language, state JSON, event log). It also runs in the browser:
[wieslawsoltes.github.io/RibbonSpace](https://wieslawsoltes.github.io/RibbonSpace/).

## Build and test

```bash
dotnet build RibbonSpace.slnx
dotnet test tests/RibbonSpace.Core.Tests
dotnet run --project tests/RibbonSpace.Uno.RuntimeTests -f net10.0-desktop   # runtime UI tests in a real window
```

Requirements: .NET 10 SDK, and Uno.Sdk 6.7 (resolved from `global.json`). Building for WebAssembly needs the
`wasm-tools` workload, and building every target needs `android` and `ios` too (`-p:RibbonSpaceAllTargets=true`).
On Windows that also adds the WinAppSDK target, which must be built with Visual Studio's `msbuild /restore`
rather than `dotnet build`.

## License

[MIT](https://github.com/wieslawsoltes/RibbonSpace/blob/main/LICENSE) © Wiesław Šoltés
