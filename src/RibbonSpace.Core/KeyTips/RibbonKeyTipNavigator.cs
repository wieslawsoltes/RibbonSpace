namespace RibbonSpace.KeyTips;

/// <summary>Result of <see cref="RibbonKeyTipScope{T}.Process"/>.</summary>
public enum RibbonKeyTipMatch
{
    /// <summary>No KeyTip starts with the typed keys plus this key; the key is ignored and the typed prefix is kept (Office behaviour).</summary>
    None,
    /// <summary>The typed keys are a prefix of one or more KeyTips; wait for more keys.</summary>
    Partial,
    /// <summary>A KeyTip was fully typed.</summary>
    Complete,
}

/// <summary>
/// One level of KeyTips (e.g. the tab row, or the commands of a tab) with multi-key prefix matching.
/// </summary>
/// <typeparam name="T">Target type (UI element or model).</typeparam>
public sealed class RibbonKeyTipScope<T>
    where T : class
{
    private readonly List<(string Tip, T Target)> _entries = [];
    private string _typed = string.Empty;

    /// <summary>Entries of the scope.</summary>
    public IReadOnlyList<(string Tip, T Target)> Entries => _entries;

    /// <summary>Keys typed so far.</summary>
    public string Typed => _typed;

    /// <summary>Adds an entry. Empty tips (nothing left after normalization) can never be typed and are rejected.</summary>
    public void Add(string tip, T target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var normalized = RibbonKeyTipAssigner.Normalize(tip);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A KeyTip needs at least one letter or digit.", nameof(tip));
        }

        _entries.Add((normalized, target));
    }

    /// <summary>Entries still reachable with the typed prefix.</summary>
    public IEnumerable<(string Tip, T Target)> Candidates
        => _entries.Where(e => e.Tip.StartsWith(_typed, StringComparison.Ordinal));

    /// <summary>Processes one key.</summary>
    public RibbonKeyTipMatch Process(char key, out T? target)
    {
        target = null;
        var next = _typed + char.ToUpperInvariant(key);
        var matches = _entries.Where(e => e.Tip.StartsWith(next, StringComparison.Ordinal)).ToArray();
        if (matches.Length == 0)
        {
            return RibbonKeyTipMatch.None;
        }

        var exact = matches.FirstOrDefault(m => m.Tip == next);
        if (exact.Target is not null)
        {
            _typed = string.Empty;
            target = exact.Target;
            return RibbonKeyTipMatch.Complete;
        }

        _typed = next;
        return RibbonKeyTipMatch.Partial;
    }

    /// <summary>Removes the last typed key. Returns false when nothing was typed.</summary>
    public bool Backspace()
    {
        if (_typed.Length == 0)
        {
            return false;
        }

        _typed = _typed[..^1];
        return true;
    }

    /// <summary>Clears typed input.</summary>
    public void Reset() => _typed = string.Empty;
}

/// <summary>Stack of KeyTip scopes (Office KeyTip mode: Alt → tab → group popup → menu...).</summary>
/// <typeparam name="T">Target type.</typeparam>
public sealed class RibbonKeyTipNavigator<T>
    where T : class
{
    private readonly Stack<RibbonKeyTipScope<T>> _scopes = new();

    /// <summary>True while KeyTips are shown.</summary>
    public bool IsActive => _scopes.Count > 0;

    /// <summary>Current scope.</summary>
    public RibbonKeyTipScope<T>? Current => _scopes.Count > 0 ? _scopes.Peek() : null;

    /// <summary>Depth of the navigation (1 = top level).</summary>
    public int Depth => _scopes.Count;

    /// <summary>Enters a new scope.</summary>
    public void Push(RibbonKeyTipScope<T> scope) => _scopes.Push(scope);

    /// <summary>Leaves the current scope; returns false when KeyTip mode ends.</summary>
    public bool Pop()
    {
        if (_scopes.Count > 0)
        {
            _scopes.Pop();
        }

        return _scopes.Count > 0;
    }

    /// <summary>Ends KeyTip mode.</summary>
    public void Clear() => _scopes.Clear();
}
