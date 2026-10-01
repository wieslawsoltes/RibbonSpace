# Getting started

## Install

```bash
dotnet add package RibbonSpace.Uno
```

`RibbonSpace.Uno` depends on `RibbonSpace.Core`, which is a plain .NET library of models and algorithms with no UI
dependency. You can reference Core from view-model projects that must not depend on Uno.

The package targets Uno Platform 6 (Uno.Sdk 6.7+) and .NET 10:

| Target | Notes |
|---|---|
| `net10.0-desktop` | Windows (Win32), macOS, Linux (X11 / framebuffer), Skia renderer |
| `net10.0-browserwasm` | WebAssembly |
| `net10.0-ios`, `net10.0-android` | Mobile (published packages include these targets) |
| `net10.0-windows10.0.26100` | WinAppSDK (packages built on Windows) |
| `net10.0` | Reference assembly for other heads |

No extra setup is needed. The theme resources merge themselves into `Application.Resources` the first time a
RibbonSpace control is created. To control the merge order, merge them explicitly:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
      <rs:RibbonThemeResources xmlns:rs="using:RibbonSpace.Controls" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

## Your first ribbon

```xml
<Page xmlns:rs="using:RibbonSpace.Controls" Background="{ThemeResource RibbonWindowBackgroundBrush}">
  <Grid RowDefinitions="Auto,Auto,*">
    <rs:RibbonTitleBar Title="Document1" AppIcon="&#xE8A5;" Ribbon="{x:Bind Ribbon}" />
    <rs:Ribbon x:Name="Ribbon" Grid.Row="1">
      <rs:Ribbon.QuickAccessToolBar>
        <rs:RibbonQuickAccessToolBar>
          <rs:RibbonButton Id="save" Label="Save" Icon="&#xE74E;" Command="{x:Bind ViewModel.SaveCommand}" />
        </rs:RibbonQuickAccessToolBar>
      </rs:Ribbon.QuickAccessToolBar>

      <rs:RibbonTab Id="home" Header="Home">
        <rs:RibbonGroup Id="clipboard" Header="Clipboard" DialogLauncherCommand="{x:Bind ViewModel.ClipboardCommand}">
          <rs:RibbonSplitButton Id="paste" Label="Paste" Icon="&#xE77F;" SizeDefinition="Large" Command="{x:Bind ViewModel.PasteCommand}">
            <MenuFlyout>
              <MenuFlyoutItem Text="Keep Text Only" />
            </MenuFlyout>
          </rs:RibbonSplitButton>
          <rs:RibbonButton Id="cut" Label="Cut" Icon="&#xE8C6;" Shortcut="Ctrl+X" />
          <rs:RibbonButton Id="copy" Label="Copy" Icon="&#xE8C8;" Shortcut="Ctrl+C" />
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
      <rs:RibbonTab Id="insert" Header="Insert" />
    </rs:Ribbon>
  </Grid>
</Page>
```

You now have:
- adaptive resizing
- KeyTips (Alt / F10)
- Ctrl+F1 to collapse
- the display-options menu (full screen, tabs only, simplified)
- a right-click context menu (add to the QAT, customize)
- ScreenTips
- light / dark themes

Search comes from the title bar.

Prefer view models? Bind a `RibbonModel` instead; see [MVVM](mvvm.md).

## Run the gallery

```bash
dotnet run --project samples/RibbonSpace.Demo -f net10.0-desktop
```

The gallery has Word (XAML), Excel (100% MVVM), PowerPoint (simplified ribbon), a toolbars and palettes page, and a
settings page (themes, palettes, density, language, state JSON, event log).

For WebAssembly:

```bash
dotnet publish samples/RibbonSpace.Demo -f net10.0-browserwasm -c Release -o artifacts/browser
```
