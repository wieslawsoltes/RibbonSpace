using System.Globalization;
using System.Text;

namespace RibbonSpace.Search;

/// <summary>Searchable command ("Tell me" / Microsoft Search / command palette entry).</summary>
/// <param name="Id">Unique id.</param>
/// <param name="Label">Label.</param>
/// <param name="Path">Location, e.g. "Home › Font".</param>
/// <param name="Description">Description.</param>
/// <param name="Keywords">Keywords and synonyms.</param>
/// <param name="Shortcut">Shortcut text.</param>
/// <param name="Target">Payload (UI element, model or delegate).</param>
/// <param name="IsEnabled">Disabled entries are ranked last.</param>
public sealed record RibbonSearchEntry(
    string Id,
    string Label,
    string? Path = null,
    string? Description = null,
    IReadOnlyList<string>? Keywords = null,
    string? Shortcut = null,
    object? Target = null,
    bool IsEnabled = true)
{
    // Get-only (not init) properties: the WinUI XAML compiler generates setters for public settable properties of
    // types reachable from XAML metadata, which do not compile against init accessors (CS8852).

    /// <summary>Unique id.</summary>
    public string Id { get; } = Id;

    /// <summary>Label.</summary>
    public string Label { get; } = Label;

    /// <summary>Location, e.g. "Home › Font".</summary>
    public string? Path { get; } = Path;

    /// <summary>Description.</summary>
    public string? Description { get; } = Description;

    /// <summary>Keywords and synonyms.</summary>
    public IReadOnlyList<string>? Keywords { get; } = Keywords;

    /// <summary>Shortcut text.</summary>
    public string? Shortcut { get; } = Shortcut;

    /// <summary>Payload (UI element, model or delegate).</summary>
    public object? Target { get; } = Target;

    /// <summary>Disabled entries are ranked last.</summary>
    public bool IsEnabled { get; } = IsEnabled;
}

/// <summary>A ranked search result.</summary>
/// <param name="Entry">Entry.</param>
/// <param name="Score">Score (higher is better).</param>
public sealed record RibbonSearchResult(RibbonSearchEntry Entry, double Score)
{
    /// <summary>Entry.</summary>
    public RibbonSearchEntry Entry { get; } = Entry;

    /// <summary>Score (higher is better).</summary>
    public double Score { get; } = Score;
}

/// <summary>
/// Ranked, diacritic- and case-insensitive command search. Every query term must match the label, keywords, path
/// or description (prefix, word-start, substring or fuzzy subsequence). Label matches rank highest.
/// </summary>
public sealed class RibbonSearchEngine
{
    private readonly List<RibbonSearchEntry> _entries = [];
    private readonly List<string> _recent = [];

    /// <summary>Maximum number of remembered recent ids.</summary>
    public int MaxRecent { get; set; } = 8;

    /// <summary>Indexed entries.</summary>
    public IReadOnlyList<RibbonSearchEntry> Entries => _entries;

    /// <summary>Ids of recently executed entries (most recent first).</summary>
    public IReadOnlyList<string> Recent => _recent;

    /// <summary>Replaces the index.</summary>
    public void SetEntries(IEnumerable<RibbonSearchEntry> entries)
    {
        _entries.Clear();
        foreach (var entry in entries)
        {
            if (_entries.All(e => e.Id != entry.Id))
            {
                _entries.Add(entry);
            }
        }
    }

    /// <summary>Adds an entry (an existing entry with the same id is replaced).</summary>
    public void Add(RibbonSearchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var index = _entries.FindIndex(e => e.Id == entry.Id);
        if (index >= 0)
        {
            _entries[index] = entry;
        }
        else
        {
            _entries.Add(entry);
        }
    }

    /// <summary>Records an executed entry (boosts it in future searches).</summary>
    public void MarkUsed(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        _recent.Remove(id);
        _recent.Insert(0, id);
        TrimRecent();
    }

    /// <summary>Replaces the recent list (e.g. restored from <c>RibbonState.RecentSearchIds</c>).</summary>
    public void SetRecent(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        _recent.Clear();
        foreach (var id in ids)
        {
            if (!string.IsNullOrEmpty(id) && !_recent.Contains(id))
            {
                _recent.Add(id);
            }
        }

        TrimRecent();
    }

    private void TrimRecent()
    {
        while (_recent.Count > Math.Max(0, MaxRecent))
        {
            _recent.RemoveAt(_recent.Count - 1);
        }
    }

    /// <summary>Recently used entries.</summary>
    public IEnumerable<RibbonSearchEntry> GetRecentEntries()
        => _recent.Select(id => _entries.FirstOrDefault(e => e.Id == id)).OfType<RibbonSearchEntry>();

    /// <summary>Searches. An empty query returns recent entries.</summary>
    public IReadOnlyList<RibbonSearchResult> Search(string? query, int maxResults = 12)
    {
        var terms = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0)
        {
            return GetRecentEntries().Select((e, i) => new RibbonSearchResult(e, 100 - i)).Take(maxResults).ToArray();
        }

        var results = new List<RibbonSearchResult>();
        foreach (var entry in _entries)
        {
            var score = ScoreEntry(entry, terms);
            if (score <= 0)
            {
                continue;
            }

            var recentIndex = _recent.IndexOf(entry.Id);
            if (recentIndex >= 0)
            {
                score += 5 - Math.Min(4, recentIndex * 0.5);
            }

            if (!entry.IsEnabled)
            {
                score *= 0.5;
            }

            results.Add(new RibbonSearchResult(entry, score));
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Entry.Label.Length)
            .ThenBy(r => r.Entry.Label, StringComparer.CurrentCultureIgnoreCase)
            .Take(maxResults)
            .ToArray();
    }

    /// <summary>Scores an entry for already normalized terms (0 = no match).</summary>
    public static double ScoreEntry(RibbonSearchEntry entry, IReadOnlyList<string> terms)
    {
        var label = Normalize(entry.Label);
        var keywords = entry.Keywords?.Select(Normalize).ToArray() ?? [];
        var path = Normalize(entry.Path);
        var description = Normalize(entry.Description);
        if (terms.Count == 0)
        {
            return 0;
        }

        var total = 0d;
        foreach (var term in terms)
        {
            var best = Math.Max(
                ScoreText(label, term, 1.0),
                Math.Max(
                    keywords.Length == 0 ? 0 : keywords.Max(k => ScoreText(k, term, 0.8)),
                    Math.Max(ScoreText(path, term, 0.45), ScoreText(description, term, 0.35))));
            if (best <= 0)
            {
                return 0;
            }

            total += best;
        }

        if (label == string.Join(' ', terms))
        {
            total += 50;
        }

        return total / terms.Count;
    }

    private static double ScoreText(string text, string term, double weight)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        if (text == term)
        {
            return 100 * weight;
        }

        if (text.StartsWith(term, StringComparison.Ordinal))
        {
            return 85 * weight;
        }

        var index = text.IndexOf(" " + term, StringComparison.Ordinal);
        if (index >= 0)
        {
            return 70 * weight;
        }

        if (text.Contains(term, StringComparison.Ordinal))
        {
            return 50 * weight;
        }

        // Acronym ("fp" -> "format painter").
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (term.Length >= 2 && words.Length >= term.Length && string.Concat(words.Select(w => w[0])).StartsWith(term, StringComparison.Ordinal))
        {
            return 60 * weight;
        }

        // Fuzzy subsequence, only for terms of 3+ characters.
        if (term.Length >= 3)
        {
            var ti = 0;
            var gaps = 0;
            var last = -1;
            for (var i = 0; i < text.Length && ti < term.Length; i++)
            {
                if (text[i] == term[ti])
                {
                    if (last >= 0 && i - last > 1)
                    {
                        gaps++;
                    }

                    last = i;
                    ti++;
                }
            }

            if (ti == term.Length && gaps <= Math.Max(1, term.Length / 2))
            {
                return (30 - (gaps * 5)) * weight;
            }
        }

        return 0;
    }

    /// <summary>Lower-cases, removes diacritics and punctuation.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastSpace = true;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                var lower = char.ToLowerInvariant(c);
                // Letters without a Unicode decomposition (Polish ł, Nordic ø, German ß, ...).
                switch (lower)
                {
                    case 'ł': builder.Append('l'); break;
                    case 'ø': builder.Append('o'); break;
                    case 'đ': builder.Append('d'); break;
                    case 'ħ': builder.Append('h'); break;
                    case 'ı': builder.Append('i'); break;
                    case 'æ': builder.Append("ae"); break;
                    case 'œ': builder.Append("oe"); break;
                    case 'ß': builder.Append("ss"); break;
                    case 'þ': builder.Append("th"); break;
                    default: builder.Append(lower); break;
                }

                lastSpace = false;
            }
            else if (!lastSpace)
            {
                builder.Append(' ');
                lastSpace = true;
            }
        }

        return builder.ToString().Trim();
    }
}
