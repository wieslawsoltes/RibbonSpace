using System.Globalization;
using System.Text;

namespace RibbonSpace.KeyTips;

/// <summary>Request for a KeyTip.</summary>
/// <param name="Label">Label used to derive a KeyTip.</param>
/// <param name="Explicit">Explicit KeyTip (kept when valid and unique).</param>
public readonly record struct RibbonKeyTipRequest(string? Label, string? Explicit = null);

/// <summary>
/// Assigns unique, prefix-free KeyTips (e.g. "H", "N", "FP", "1") to a scope such as the tab row or the
/// commands of one tab. Explicit KeyTips win; generated tips prefer the first letter of the label, then the first
/// letters of its words, then other letters of the label, then two-letter combinations, then digits.
/// </summary>
public static class RibbonKeyTipAssigner
{
    /// <summary>Assigns KeyTips. The result has one entry per request (never null, never duplicated).</summary>
    /// <param name="requests">Requests in visual order.</param>
    /// <param name="reserved">KeyTips already used in the scope (e.g. "F" for the File button).</param>
    public static IReadOnlyList<string> Assign(IReadOnlyList<RibbonKeyTipRequest> requests, IEnumerable<string>? reserved = null)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var used = new HashSet<string>(reserved?.Select(Normalize) ?? [], StringComparer.Ordinal);
        var result = new string?[requests.Count];

        // Explicit KeyTips first, in order; conflicting explicit tips fall back to generation.
        for (var i = 0; i < requests.Count; i++)
        {
            var tip = Normalize(requests[i].Explicit);
            if (tip.Length > 0 && IsAvailable(tip, used))
            {
                used.Add(tip);
                result[i] = tip;
            }
        }

        // Single-character tips are capped so enough first characters stay free to prefix the two-character
        // tips of the remaining items (each free first character offers 36 two-character tips). Without the cap a
        // large scope would hand out every letter and leave no prefix-free tips for the rest.
        var pending = result.Count(r => r is null);
        var freeSingles = SingleAlphabet.Count(c => IsAvailable(c.ToString(), used));
        var freePrefixes = freeSingles + (IsAvailable("0", used) ? 1 : 0);
        var singleBudget = pending <= freeSingles
            ? pending
            : Math.Clamp(((PairAlphabet.Length * freePrefixes) - pending) / (PairAlphabet.Length - 1), 0, freeSingles);

        for (var i = 0; i < requests.Count && singleBudget > 0; i++)
        {
            if (result[i] is not null)
            {
                continue;
            }

            foreach (var candidate in SingleCandidates(requests[i].Label))
            {
                if (IsAvailable(candidate, used))
                {
                    used.Add(candidate);
                    result[i] = candidate;
                    singleBudget--;
                    break;
                }
            }
        }

        // Items whose labels offered no free letter still get a one-key digit while the budget allows.
        for (var i = 0; i < requests.Count && singleBudget > 0; i++)
        {
            if (result[i] is null && FirstAvailable(Digits, used) is { } digit)
            {
                used.Add(digit);
                result[i] = digit;
                singleBudget--;
            }
        }

        // Remaining items get two-character tips whose first character is not a single-character tip.
        for (var i = 0; i < requests.Count; i++)
        {
            if (result[i] is not null)
            {
                continue;
            }

            result[i] = FirstAvailable(DoubleCandidates(requests[i].Label), used) ?? Fallback(used);
            used.Add(result[i]!);
        }

        return result!;
    }

    /// <summary>Normalizes a KeyTip (upper case, letters and digits only).</summary>
    public static string Normalize(string? keyTip)
    {
        if (string.IsNullOrWhiteSpace(keyTip))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var c in keyTip.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static bool IsAvailable(string tip, HashSet<string> used)
    {
        foreach (var existing in used)
        {
            if (existing.StartsWith(tip, StringComparison.Ordinal) || tip.StartsWith(existing, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string Letters(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        var normalized = label.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c) && c < 128)
            {
                builder.Append(char.ToUpperInvariant(c));
            }
            else if (char.IsWhiteSpace(c) || c is '&' or '-' or '/' or '(' or ')')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SingleCandidates(string? label)
    {
        var letters = Letters(label);
        var words = letters.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            yield return word[0].ToString();
        }

        foreach (var c in letters.Replace(" ", string.Empty, StringComparison.Ordinal))
        {
            yield return c.ToString();
        }
    }

    private static IEnumerable<string> DoubleCandidates(string? label)
    {
        var letters = Letters(label);
        var words = letters.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            yield return $"{words[0][0]}{words[1][0]}";
        }

        var compact = letters.Replace(" ", string.Empty, StringComparison.Ordinal);
        for (var i = 0; i < compact.Length; i++)
        {
            for (var j = i + 1; j < compact.Length; j++)
            {
                yield return $"{compact[i]}{compact[j]}";
            }
        }

        for (var i = 0; i < compact.Length; i++)
        {
            for (var d = 1; d <= 9; d++)
            {
                yield return $"{compact[i]}{d}";
            }
        }
    }

    private const string SingleAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    private static readonly string[] Digits = ["1", "2", "3", "4", "5", "6", "7", "8", "9"];
    private const string PairAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private static string? FirstAvailable(IEnumerable<string> candidates, HashSet<string> used)
    {
        foreach (var candidate in candidates)
        {
            if (IsAvailable(candidate, used))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string Fallback(HashSet<string> used)
    {
        // Breadth-first over ever longer tips; always terminates because the used set is finite.
        IEnumerable<string> level = ["0", .. SingleAlphabet.Select(c => c.ToString())];
        while (true)
        {
            var next = new List<string>();
            foreach (var prefix in level)
            {
                foreach (var c in PairAlphabet)
                {
                    var tip = prefix + c;
                    if (IsAvailable(tip, used))
                    {
                        return tip;
                    }

                    next.Add(tip);
                }
            }

            level = next.Where(t => !used.Contains(t));
        }
    }

    /// <summary>
    /// Numeric KeyTips for the Quick Access Toolbar: 1..9, then 09, 08, ..., 01, then 0A..0Z (Office convention).
    /// Longer toolbars continue with 001..009, 00A..00Z, 0001..., so every item gets a prefix-free tip.
    /// </summary>
    public static IReadOnlyList<string> AssignQuickAccess(int count)
    {
        count = Math.Max(0, count);
        var tips = new List<string>(count);
        for (var i = 1; i <= Math.Min(9, count); i++)
        {
            tips.Add(i.ToString(CultureInfo.InvariantCulture));
        }

        var prefix = "0";
        while (tips.Count < count)
        {
            for (var i = 9; i >= 1 && tips.Count < count; i--)
            {
                tips.Add(prefix + i.ToString(CultureInfo.InvariantCulture));
            }

            for (var c = 'A'; c <= 'Z' && tips.Count < count; c++)
            {
                tips.Add(prefix + c);
            }

            prefix += "0";
        }

        return tips;
    }
}
