using RibbonSpace.Search;

namespace RibbonSpace.Core.Tests;

public class SearchTests
{
    private static RibbonSearchEngine Engine()
    {
        var engine = new RibbonSearchEngine();
        engine.SetEntries(
        [
            new("paste", "Paste", "Home › Clipboard", Keywords: ["clipboard"]),
            new("format-painter", "Format Painter", "Home › Clipboard"),
            new("bold", "Bold", "Home › Font", "Make your text bold.", Shortcut: "Ctrl+B"),
            new("font-color", "Font Color", "Home › Font"),
            new("insert-table", "Table", "Insert › Tables", Keywords: ["grid", "tabulka"]),
            new("pivot", "PivotTable", "Insert › Tables", IsEnabled: false),
            new("page-color", "Page Color", "Design › Page Background"),
        ]);
        return engine;
    }

    [Fact]
    public void Exact_label_ranks_first()
    {
        var results = Engine().Search("bold");
        Assert.Equal("bold", results[0].Entry.Id);
    }

    [Fact]
    public void Multi_term_queries_require_all_terms()
    {
        var results = Engine().Search("font color");
        Assert.Equal("font-color", results[0].Entry.Id);
        Assert.DoesNotContain(results, r => r.Entry.Id == "paste");
    }

    [Fact]
    public void Keywords_path_and_acronyms_match()
    {
        var engine = Engine();
        Assert.Contains(engine.Search("grid"), r => r.Entry.Id == "insert-table");
        Assert.Contains(engine.Search("clipboard"), r => r.Entry.Id == "format-painter");
        Assert.Equal("format-painter", engine.Search("fp")[0].Entry.Id);
    }

    [Fact]
    public void Fuzzy_and_diacritic_insensitive()
    {
        var engine = Engine();
        Assert.Contains(engine.Search("pste"), r => r.Entry.Id == "paste");
        Assert.Contains(engine.Search("PÁSTE"), r => r.Entry.Id == "paste");
    }

    [Fact]
    public void Disabled_entries_rank_lower_and_recent_entries_rank_higher()
    {
        var engine = Engine();
        var results = engine.Search("table");
        Assert.Equal("insert-table", results[0].Entry.Id);
        engine.MarkUsed("page-color");
        Assert.Equal("page-color", engine.Search("")[0].Entry.Id);
        Assert.Equal("page-color", engine.Search("color")[0].Entry.Id);
    }

    [Fact]
    public void No_match_returns_empty()
    {
        Assert.Empty(Engine().Search("zzzzqqq"));
    }

    [Fact]
    public void Normalize_strips_punctuation()
    {
        Assert.Equal("sort filter", RibbonSearchEngine.Normalize("Sort & Filter!"));
    }
}
