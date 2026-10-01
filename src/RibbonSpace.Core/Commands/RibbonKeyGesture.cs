using System.Text;

namespace RibbonSpace.Commands;

/// <summary>Keyboard modifiers of a <see cref="RibbonKeyGesture"/>.</summary>
[Flags]
public enum RibbonModifierKeys
{
    /// <summary>No modifier.</summary>
    None = 0,
    /// <summary>Control (Command on macOS when <see cref="RibbonKeyGesture.TreatMetaAsControl"/> is enabled).</summary>
    Control = 1,
    /// <summary>Shift.</summary>
    Shift = 2,
    /// <summary>Alt / Option.</summary>
    Alt = 4,
    /// <summary>Windows / Command key.</summary>
    Meta = 8,
}

/// <summary>
/// Parsed keyboard shortcut such as "Ctrl+Shift+L", "F5", "Alt+Down" or "Ctrl+]". Keys use
/// the Windows <c>VirtualKey</c> names so the Uno layer can map them directly.
/// </summary>
public readonly record struct RibbonKeyGesture(RibbonModifierKeys Modifiers, string Key)
{
    /// <summary>When true (default) the macOS Command key satisfies Control in gesture matching.</summary>
    public static bool TreatMetaAsControl { get; set; } = true;

    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Esc"] = "Escape", ["Del"] = "Delete", ["Ins"] = "Insert", ["Return"] = "Enter",
        ["PgUp"] = "PageUp", ["PgDn"] = "PageDown", ["Plus"] = "Add", ["+"] = "Add", ["-"] = "Subtract",
        ["Minus"] = "Subtract", ["="] = "Add", ["Spacebar"] = "Space", ["Backspace"] = "Back", ["*"] = "Multiply",
        ["Up"] = "Up", ["Down"] = "Down", ["Left"] = "Left", ["Right"] = "Right",
        ["["] = "OemOpenBracket", ["]"] = "OemCloseBracket", [","] = "OemComma", ["."] = "OemPeriod",
        ["/"] = "OemQuestion", [";"] = "OemSemicolon", ["'"] = "OemQuote", ["\\"] = "OemBackslash", ["`"] = "OemTilde",
    };

    private static readonly Dictionary<string, string> CanonicalKeys = new[]
    {
        "Escape", "Delete", "Insert", "Enter", "PageUp", "PageDown", "Home", "End", "Tab", "Back", "Space", "Up", "Down",
        "Left", "Right", "Add", "Subtract", "Multiply", "Divide", "Decimal", "Pause", "Print", "Snapshot", "Scroll",
        "Apps", "Help", "Clear", "Select", "Execute", "Sleep",
    }.ToDictionary(k => k, k => k, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Add"] = "+", ["Subtract"] = "-", ["OemOpenBracket"] = "[", ["OemCloseBracket"] = "]", ["OemComma"] = ",",
        ["OemPeriod"] = ".", ["OemQuestion"] = "/", ["OemSemicolon"] = ";", ["OemQuote"] = "'", ["OemBackslash"] = "\\",
        ["OemTilde"] = "`", ["Escape"] = "Esc", ["Delete"] = "Del", ["PageUp"] = "PgUp", ["PageDown"] = "PgDn",
    };

    /// <summary>Parses a gesture. Separators: '+' (the '+' key itself may be written as "Plus").</summary>
    public static RibbonKeyGesture Parse(string text)
    {
        if (!TryParse(text, out var gesture))
        {
            throw new FormatException($"'{text}' is not a valid key gesture.");
        }

        return gesture;
    }

    /// <summary>Tries to parse a gesture.</summary>
    public static bool TryParse(string? text, out RibbonKeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var parts = new List<string>();
        var current = new StringBuilder();
        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            // '+' separates parts unless nothing precedes it, in which case it is the '+' key itself ("Ctrl + +").
            if (c == '+' && current.ToString().Trim().Length > 0)
            {
                parts.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString().Trim());
        }

        var modifiers = RibbonModifierKeys.None;
        string? key = null;
        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                return false;
            }

            switch (part.ToUpperInvariant())
            {
                case "CTRL" or "CONTROL" or "STRG":
                    modifiers |= RibbonModifierKeys.Control;
                    break;
                case "SHIFT":
                    modifiers |= RibbonModifierKeys.Shift;
                    break;
                case "ALT" or "OPTION" or "OPT":
                    modifiers |= RibbonModifierKeys.Alt;
                    break;
                case "WIN" or "META" or "CMD" or "COMMAND":
                    modifiers |= RibbonModifierKeys.Meta;
                    break;
                default:
                    if (key is not null)
                    {
                        return false;
                    }

                    key = NormalizeKey(part);
                    break;
            }
        }

        if (key is null)
        {
            return false;
        }

        gesture = new RibbonKeyGesture(modifiers, key);
        return true;
    }

    private static string NormalizeKey(string key)
    {
        key = key.Trim();
        if (key.Length == 0)
        {
            return string.Empty;
        }

        if (KeyAliases.TryGetValue(key, out var alias))
        {
            return alias;
        }

        if (CanonicalKeys.TryGetValue(key, out var canonical))
        {
            return canonical;
        }

        if (key.Length is 2 or 3 && (key[0] is 'F' or 'f') && int.TryParse(key.AsSpan(1), out var function) && function is >= 1 and <= 24)
        {
            return "F" + function.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (key.Length == 1 && char.IsLetter(key[0]))
        {
            return key.ToUpperInvariant();
        }

        if (key.Length == 1 && char.IsDigit(key[0]))
        {
            return "Number" + key;
        }

        return char.ToUpperInvariant(key[0]) + key[1..];
    }

    /// <summary>Returns true when the pressed key and modifiers match this gesture.</summary>
    public bool Matches(string key, RibbonModifierKeys modifiers)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(Key))
        {
            return false;
        }

        if (!string.Equals(NormalizeKey(key), Key, StringComparison.OrdinalIgnoreCase)
            && !(Key.StartsWith("Number", StringComparison.Ordinal) && string.Equals(key, "NumberPad" + Key[^1], StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (TreatMetaAsControl && modifiers.HasFlag(RibbonModifierKeys.Meta) && !Modifiers.HasFlag(RibbonModifierKeys.Meta))
        {
            modifiers = (modifiers & ~RibbonModifierKeys.Meta) | RibbonModifierKeys.Control;
        }

        return modifiers == Modifiers;
    }

    /// <summary>Human readable text, e.g. "Ctrl+Shift+L".</summary>
    public override string ToString() => ToDisplayString(false);

    /// <summary>Formats the gesture; with <paramref name="macStyle"/> uses ⌘ ⌥ ⇧ ⌃ symbols.</summary>
    public string ToDisplayString(bool macStyle)
    {
        var parts = new List<string>();
        if (macStyle)
        {
            var controlIsCommand = TreatMetaAsControl && !Modifiers.HasFlag(RibbonModifierKeys.Meta);
            if (Modifiers.HasFlag(RibbonModifierKeys.Control)) parts.Add(controlIsCommand ? "⌘" : "⌃");
            if (Modifiers.HasFlag(RibbonModifierKeys.Alt)) parts.Add("⌥");
            if (Modifiers.HasFlag(RibbonModifierKeys.Shift)) parts.Add("⇧");
            if (Modifiers.HasFlag(RibbonModifierKeys.Meta)) parts.Add("⌘");
            return string.Concat(parts) + KeyDisplay(Key);
        }

        if (Modifiers.HasFlag(RibbonModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(RibbonModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(RibbonModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(RibbonModifierKeys.Meta)) parts.Add("Win");
        parts.Add(KeyDisplay(Key));
        return string.Join("+", parts);
    }

    private static string KeyDisplay(string key)
    {
        if (DisplayNames.TryGetValue(key, out var display))
        {
            return display;
        }

        return key.StartsWith("Number", StringComparison.Ordinal) && key.Length == 7 ? key[^1..] : key;
    }
}
