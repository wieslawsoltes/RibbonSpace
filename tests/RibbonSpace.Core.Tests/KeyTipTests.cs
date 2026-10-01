using RibbonSpace.KeyTips;

namespace RibbonSpace.Core.Tests;

public class KeyTipTests
{
    private static IReadOnlyList<string> Assign(params string[] labels)
        => RibbonKeyTipAssigner.Assign(labels.Select(l => new RibbonKeyTipRequest(l)).ToArray());

    [Fact]
    public void Uses_first_letters_when_unique()
    {
        Assert.Equal(["H", "I", "D"], Assign("Home", "Insert", "Design"));
    }

    [Fact]
    public void Resolves_conflicts_and_is_prefix_free()
    {
        var tips = Assign("Paste", "Page Layout", "Print", "Picture", "Pen", "Paragraph");
        Assert.Equal(tips.Count, tips.Distinct().Count());
        foreach (var a in tips)
        {
            foreach (var b in tips)
            {
                if (!ReferenceEquals(a, b) && a != b)
                {
                    Assert.False(b.StartsWith(a, StringComparison.Ordinal), $"{a} is a prefix of {b}");
                }
            }
        }
    }

    [Fact]
    public void Explicit_keytips_win_and_reserved_are_avoided()
    {
        var tips = RibbonKeyTipAssigner.Assign([new("Home", "H"), new("Help"), new("File menu")], reserved: ["F"]);
        Assert.Equal("H", tips[0]);
        Assert.NotEqual("H", tips[1]);
        Assert.NotEqual("F", tips[2]);
        Assert.False(tips[2].StartsWith('F'));
    }

    [Fact]
    public void Empty_labels_get_fallback_tips()
    {
        var tips = Assign("", "", "");
        Assert.Equal(3, tips.Distinct().Count());
        Assert.All(tips, t => Assert.False(string.IsNullOrEmpty(t)));
    }

    [Fact]
    public void Many_items_get_unique_tips()
    {
        var tips = Assign(Enumerable.Range(0, 80).Select(i => "Bold").ToArray());
        Assert.Equal(80, tips.Distinct().Count());
    }

    [Fact]
    public void Diacritics_are_normalized()
    {
        Assert.Equal("E", Assign("Éditer")[0]);
    }

    [Fact]
    public void Quick_access_tips_follow_office_convention()
    {
        var tips = RibbonKeyTipAssigner.AssignQuickAccess(12);
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8", "9", "09", "08", "07"], tips);
    }

    [Fact]
    public void Scope_matches_multi_key_tips()
    {
        var scope = new RibbonKeyTipScope<string>();
        scope.Add("FP", "format painter");
        scope.Add("FF", "font");
        scope.Add("V", "paste");
        Assert.Equal(RibbonKeyTipMatch.Partial, scope.Process('f', out _));
        Assert.Equal(2, scope.Candidates.Count());
        Assert.Equal(RibbonKeyTipMatch.Complete, scope.Process('p', out var target));
        Assert.Equal("format painter", target);
        Assert.Equal(RibbonKeyTipMatch.None, scope.Process('x', out _));
        Assert.Equal(RibbonKeyTipMatch.Complete, scope.Process('v', out target));
        Assert.Equal("paste", target);
    }

    [Fact]
    public void Scope_backspace_and_navigator_stack()
    {
        var scope = new RibbonKeyTipScope<string>();
        scope.Add("AB", "x");
        scope.Process('a', out _);
        Assert.True(scope.Backspace());
        Assert.False(scope.Backspace());

        var navigator = new RibbonKeyTipNavigator<string>();
        navigator.Push(scope);
        navigator.Push(new RibbonKeyTipScope<string>());
        Assert.Equal(2, navigator.Depth);
        Assert.True(navigator.Pop());
        Assert.False(navigator.Pop());
        Assert.False(navigator.IsActive);
    }
}
