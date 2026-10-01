# Testing

| Suite | What | How |
|---|---|---|
| `tests/RibbonSpace.Core.Tests` | Adaptive layout, item arrangement, simplified overflow, KeyTips, search ranking, gestures, catalog, models, merging, state JSON, colours and palettes, localization | `dotnet test tests/RibbonSpace.Core.Tests` |
| `tests/RibbonSpace.Uno.RuntimeTests` | Real controls in a real Uno window: resizing, collapse popups, simplified overflow, density, visibility modes, selection, contextual tabs, radio toggles, catalog commands, shortcuts, QAT linked copies, state round-trips, customization, KeyTip levels, search, backstage, MVVM two-way sync, inputs, galleries, colour picker, theming, localization, toolbars, title bar | `dotnet run --project tests/RibbonSpace.Uno.RuntimeTests -f net10.0-desktop` (on Linux CI: `xvfb-run -a …`) |

The runtime runner prints `PASS`/`FAIL` lines. It writes JUnit XML to the path in `RIBBONSPACE_TEST_RESULTS`,
exits with the number of failures, and accepts `RIBBONSPACE_TEST_FILTER=substring`.

## Screenshot automation

The gallery app can take screenshots for documentation and visual review:

```bash
RIBBONSPACE_CAPTURE=word.png RIBBONSPACE_PAGE=word RIBBONSPACE_WIDTH=1400 RIBBONSPACE_ACTION=keytips-home \
  dotnet run --project samples/RibbonSpace.Demo -f net10.0-desktop
```

`RIBBONSPACE_ACTION` accepts comma-separated steps: `keytips`, `keytips-home`, `backstage`, `simplified`, `classic`,
`overflow`, `minimized`, `minimized-popup`, `fullscreen`, `contextual`, `insert`, `design`, `layout`, `view`,
`gallery`, `colorpicker`, `paste`, `qat-below`, `search`, `palette`, `customize`, `customize-qat`, `add-qat`,
`collapsed-group`, `touch`, `compact`, `colorful` and `dark`, plus `page:<key>` (switch pages mid-run) and `surface-cad` (CAD surface style for the Office pages). With
`RIBBONSPACE_PAGE=cad`: `cad-slideout`, `cad-appmenu`, `cad-panelbuttons`, `cad-paneltitles`, `cad-float`,
`cad-tooltip`, `cad-layers`, `cad-contextual`, `cad-hatch`, `cad-circle`, `cad-blocks`, `cad-view`, `cad-3d`, `cad-light` and
`cad-cmd:<command>` (runs a command-line entry such as `cad-cmd:circle`). `RIBBONSPACE_THEME=Dark` switches to the dark theme. Open
popups (KeyTips, flyouts, backstage) are composited into the capture.
