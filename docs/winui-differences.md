# WinUI and Uno differences

This page tracks the differences between Uno Platform and WinUI 3 (Windows App SDK) that affect RibbonSpace. It also
tracks the issues found while porting the ribbon to WinUI. `RibbonSpace.WinUI` compiles the `RibbonSpace.Uno` sources,
so every entry is handled once, in the shared code or in `build/WinUI`, and both packages share the result. See
[WinUI 3](winui.md) for the port itself.

Status: **Fixed** (changed in the shared sources), **Handled** (handled by the WinUI build or the hosts), **Open**
(known, not resolved).

## XAML

| # | Difference | Symptom on WinUI | Resolution | Status |
|---|---|---|---|---|
| X1 | Uno resolves RibbonSpace types without a prefix (global XAML namespace), through `https://github.com/wieslawsoltes/RibbonSpace`, and with the undeclared `rs:` prefix (`XmlnsDefinition` / `XmlnsPrefix`). WinUI's XAML compiler only knows `using:` namespaces ([microsoft-ui-xaml#10616](https://github.com/microsoft/microsoft-ui-xaml/issues/10616)). | `WMC0001` unknown type errors | The build-time adapter (`build/WinUI/RibbonSpaceAdaptXaml.cs`) moves those elements, property elements and attached properties to `using:` namespaces, so the shared sample and test XAML stays unprefixed. Apps that use `RibbonSpace.WinUI` declare `xmlns:rs="using:RibbonSpace.Controls"`. The attributes are compiled only `#if !WINDOWS`. | Handled |
| X2 | Uno infers a XAML content property for collection properties named `Items`. WinUI needs `[ContentProperty]`. | `WMC0075: Missing Content Property definition for Element 'RibbonComboBox'` | `RibbonComboBox` declares `[ContentProperty(Name = nameof(Items))]`. | Fixed |
| X3 | Code generated for `x:Name` fields on WinUI warns when a name hides an inherited member. | `CS0108` for `FontSize`, `FontFamily` (Word page) and `Language` (Settings page) | Renamed to `FontSizeBox`, `FontFamilyBox` and `LanguageBox`. | Fixed |
| X4 | `dotnet build` runs WinUI's XAML compiler out of process, which reports no XAML diagnostics. | `XamlCompiler.exe exited with code 1` with no message | Build once with Visual Studio's MSBuild and `-p:UseXamlCompilerExecutable=false` to see the messages ([WinUI 3](winui.md#build-run-and-test)). | Handled |
| X5 | Type names inside attribute values (`TargetType`) resolve only through `using:` namespaces, on both platforms. | `Style TargetType="RibbonButton"` doesn't resolve | Declare `xmlns:rs="using:RibbonSpace.Controls"` in files that style RibbonSpace controls. The adapter keeps such declarations working. | Handled |

## Resources and styles

| # | Difference | Symptom on WinUI | Resolution | Status |
|---|---|---|---|---|
| R1 | WinUI rejects one `ResourceDictionary` instance under two `ThemeDictionaries` keys. Uno accepts it. | `COMException 0x800F1000` in `RibbonThemeResources..ctor`. From XAML: `Element is already the child of another element`. Every ribbon fails. | `Default` has a dictionary of its own that shares the `Light` brush instances, so live palette changes still reach both. | Fixed |
| R2 | A library's `Themes/Generic.xaml` resources are visible to application XAML on Uno. On WinUI they only supply default styles. | `Cannot find a Resource with the Name/Key RibbonWindowBackgroundBrush` when app XAML uses RibbonSpace brushes before any ribbon control is created | Merge `<RibbonThemeResources />` in App.xaml (as in the getting-started guide). The gallery now does. Controls still merge the resources themselves (`RibbonTheme.EnsureResources`). | Fixed |
| R3 | WinUI applies a style only to types its XAML type information knows. A subclass that is never used in library XAML is not known. | `Cannot apply a Style with TargetType 'RibbonSplitButton' to an object of type 'Control'` (`RibbonColorPicker`) | The base styles are keyed (`DefaultRibbonButtonStyle`, `DefaultRibbonSplitButtonStyle`, `DefaultRibbonComboBoxStyle`). Every control, including `RibbonDropDownButton`, `RibbonColorPicker`, `RibbonFontComboBox` and `RibbonFontSizeComboBox`, has its own `DefaultStyleKey` and an implicit style `BasedOn` the keyed one. App subclasses must do the same ([Theming](theming.md#re-templating)). | Fixed |
| R4 | Resource URIs carry the assembly name (`ms-appx:///RibbonSpace.Uno/Themes/...`), which differs per package. | Merged dictionaries and `Shared.xaml` not found | XAML: the adapter rewrites `ms-appx:///RibbonSpace.Uno/` to `ms-appx:///RibbonSpace.WinUI/`. Code: `RibbonThemeResources` builds the URI from its assembly name. | Fixed |
| R5 | Stale duplicates found during the port: `RibbonFontComboBox` and `RibbonFontSizeComboBox` had their own templates that never applied (their default style key was `RibbonComboBox`), and they lacked the `SelectionBoxTemplate` part. | None (dead code on both) | Removed. Both use the combo box template through `BasedOn`. | Fixed |

## Rendering and controls

| # | Difference | Symptom on WinUI | Resolution | Status |
|---|---|---|---|---|
| C1 | `FontIcon` falls back to a text font for characters missing from the symbol font on Uno, not on WinUI. Strings convert to glyph icons implicitly (`RibbonIcon` from `"%"`). | Empty boxes instead of `$`, `%`, `,`, `.0` (Excel Number group) | `RibbonIconPresenter` draws glyph icons without a font family as text when they aren't symbol-font code points. Covered by `ItemTests.Plain_character_glyph_icons_render_as_text`. | Fixed |
| C2 | Title bar integration: WinUI reports the caption button insets through `AppWindow.TitleBar` and needs passthrough regions for interactive content. Uno uses platform defaults. | Not applicable | `RibbonTitleBar` uses `AppWindow` insets and `InputNonClientPointerSource` passthrough regions `#if WINDOWS`. | Handled |
| C3 | Light-dismiss popups (collapsed groups, slide-outs, menus, pickers) close when the window loses activation. This is standard WinUI behaviour. | Runtime tests failed intermittently on a desktop where other apps took the foreground | The runtime test runner counts window deactivations and runs a test that failed after one once more (`RETRY`). On a quiet desktop all tests pass without retries. | Handled |
| C4 | Window activation events use different enum types: `CoreWindowActivationState` on Uno and `WindowActivationState` on WinUI. | Compile error in shared code | `#if HAS_UNO` in the test runner. | Handled |
| C5 | The CAD sample rasterizes menu icons with SkiaSharp. Uno's Skia renderer brings it in transitively; WinUI doesn't. | Missing SkiaSharp | The WinUI gallery references `SkiaSharp` (the same version Uno pins). | Handled |
| C6 | `RenderTargetBitmap` renders a `FlyoutPresenter` as an empty (0×0) bitmap on WinUI. Its template root and content render. | Gallery screenshots without flyout contents (color palette, simplified overflow, application menu). The flyouts themselves open and are themed correctly (checked by runtime tests and by the presenter's theme and background). | The screenshot automation (`Capture.cs`) falls back to the presenter's template root, then its content, and logs each composited popup (`[capture] popup …`). | Handled |
| C7 | The theme of a flyout opened from a themed subtree (CAD page in dark mode) must reach the presenter, not only its content. | None found: presenters report the target's theme (`Dark`, `#3B4453` for the CAD application menu) | Covered by the captures in both themes. | Checked |

## Build and packaging

| # | Difference | Symptom | Resolution | Status |
|---|---|---|---|---|
| B1 | Uno.Sdk's WinAppSDK target must be built with Visual Studio's MSBuild (`UNOB0008`). A plain WinUI library needs Visual Studio's AppxPackage PRI tasks unless it uses the Windows App SDK's MSIX tooling. | `MSB4062: Microsoft.Build.Packaging.Pri.Tasks.ExpandPriContent could not be loaded` | `EnableMsixTooling=true`, so `dotnet build` works without Visual Studio. | Handled |
| B2 | The Windows App SDK packs a library's XAML by item path (`AddXamlFilesToNugetPackage`), which assumes the XAML lives inside the project folder. | `NU5026` with a doubled path for linked and adapted XAML | `IncludeXamlFilesInNugetPackage=false`, and `RibbonSpacePackLayout` packs the layout under the `Link` path. | Handled |
| B3 | Plain WinUI library packages don't include the PRI index or the XBF layout, and NuGet drops build output with unknown extensions. | Package without themes: controls render untemplated in consuming apps | `RibbonSpacePackLayout` adds `RibbonSpace.WinUI.pri` and `RibbonSpace.WinUI/Themes/*.xbf` / `*.xaml`, and allows the `.xbf` and `.xaml` extensions. Verified with a separate app that consumes the package from a local feed. | Handled |
| B4 | WinUI apps need a runtime identifier, and solution builds need project platform mappings. | `-p:Platform=x64` on the solution built ARM64 apps | Apps default to the machine's architecture. `Platform=x64` / `ARM64` map to `win-x64` / `win-arm64`, and `RibbonSpace.WinUI.slnx` maps solution platforms to the app projects. | Handled |
| B5 | Unhandled XAML errors at startup end a WinUI app with a stowed exception (`0xC000027B`) and no managed stack in the event log. | Silent crash | Each WinUI app has a `Program.cs` that writes unhandled exceptions to standard error. | Handled |

## Platform issues

| # | Issue | Details | Status |
|---|---|---|---|
| P1 | Windows App SDK input fail-fast | One runtime test run on the ARM64 VM ended with `0xC0000409` in `Microsoft.InputStateManager.dll` (Windows App SDK 1.7.250909003). That happened while another app's UI test suite was running on the same desktop. It hasn't reproduced on a quiet desktop or in CI. | Open (watching) |

## Validation

| Environment | Builds | Runtime UI tests |
|---|---|---|
| Windows 11 ARM64 VM | Debug, Release; Any CPU (machine architecture), `x64` (emulated), `ARM64`. 0 warnings. | All pass on each build. Popup tests were flaky only while other apps' UI tests ran on the same desktop (C3). |
| GitHub Actions `windows-latest` (x64) | Release | All pass |
| macOS, Uno Skia desktop (shared sources) | Release | All pass |

The WinUI gallery was also captured on the VM page by page, in light and dark themes, with every screenshot action
(KeyTips, backstage, simplified and overflow, minimized, contextual tabs, galleries, colour picker, paste menu, QAT
below, search, command palette, Customize, collapsed groups, density, colourful chrome and the CAD panel states). The
captures were compared with the Uno gallery. Apart from C6 they match.
