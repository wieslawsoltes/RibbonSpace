# Search and command palette

## Microsoft Search box

```xml
<rs:RibbonTitleBar Ribbon="{x:Bind Ribbon}" IsSearchVisible="True" />   <!-- built in -->
<rs:RibbonSearchBox Ribbon="{x:Bind Ribbon}" Width="320" />              <!-- or anywhere -->
```

- **Alt+Q** focuses the box. An empty query shows recently used commands.
- **Ranking:**
  - exact / prefix / word-start / substring matches
  - acronyms ("fp" → Format Painter)
  - fuzzy subsequences
  - case- and diacritic-insensitive, including letters without a Unicode decomposition (ł, ø, ß, æ)
  - keywords (`rs:RibbonSearch.Keywords="grid,table"`), paths ("Home › Font") and descriptions
  - recently used entries get a boost (persisted in `RibbonState.RecentSearchIds`)
  - disabled commands rank last
- **What is searched:**
  - every item of every available tab, including contextual tabs
  - backstage items
  - catalog commands that aren't on the ribbon
  - `Ribbon.AdditionalSearchEntries`
  - results added in `RibbonSearchBox.QuerySubmitting`, whose targets can be `Action`s
- **Execution:** buttons and toggles run directly. Inputs, galleries and drop-downs first select their tab (opening
  collapsed groups when needed), then focus or open the item.

```csharp
var results = ribbon.Search("font color");
ribbon.ExecuteSearchEntry(results[0].Entry);
```

The engine (`RibbonSearchEngine`) is UI-independent and lives in Core.

## Command palette

```csharp
RibbonCommandPalette.Show(ribbon);                                  // VS Code-style Ctrl+Shift+P
RibbonCommandPalette.Show(xamlRoot, entries, entry => Run(entry));   // apps without a ribbon (catalog-only)
```
