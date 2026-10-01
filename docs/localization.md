# Localization

Built-in UI strings live in `RibbonStrings`. They cover menus, tooltips, the customize dialog, the colour palette,
search, KeyTips help and more. English, German, French, Spanish and Polish are included.

```csharp
RibbonStrings.Current = RibbonStrings.ForCulture(new CultureInfo("de-DE"));   // neutral-culture fallback, then English

RibbonStrings.Register("cs", new Dictionary<string, string>
{
    ["File"] = "Soubor",
    ["CollapseRibbon"] = "Sbalit pás karet",
});

RibbonStrings.Current.Override("File", "Home");          // rename the application button everywhere
```

`RibbonStrings.Keys` lists every key. Missing keys fall back to English. Menus are built on demand, so they always
use the current strings.

Your own labels (tabs, groups, items) are ordinary properties: bind them or use `x:Uid` / `.resw` resources.
