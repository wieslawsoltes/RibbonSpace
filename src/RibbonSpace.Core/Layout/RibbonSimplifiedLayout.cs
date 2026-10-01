namespace RibbonSpace.Layout;

/// <summary>Item of a simplified (single-line) ribbon.</summary>
/// <param name="Width">Width in the line.</param>
/// <param name="Visibility">Simplified behaviour.</param>
public readonly record struct RibbonSimplifiedItem(double Width, RibbonSimplifiedVisibility Visibility);

/// <summary>Group of a simplified ribbon.</summary>
/// <param name="Items">Items.</param>
/// <param name="ReductionOrder">Groups with a higher value overflow first.</param>
public sealed record RibbonSimplifiedGroup(IReadOnlyList<RibbonSimplifiedItem> Items, int ReductionOrder = 0);

/// <summary>Result of <see cref="RibbonSimplifiedLayout.Compute"/>.</summary>
/// <param name="InLine">Per group, per item: true when shown in the line, false when moved to the overflow menu.</param>
/// <param name="HasOverflow">True when at least one item is in the overflow menu.</param>
/// <param name="TotalWidth">Width of the line (including the overflow button when shown).</param>
public sealed record RibbonSimplifiedLayoutResult(IReadOnlyList<IReadOnlyList<bool>> InLine, bool HasOverflow, double TotalWidth);

/// <summary>
/// Simplified ribbon overflow: items are removed from the end of the groups (by reduction order, then right-to-left)
/// into the "More options" menu until the line fits. <see cref="RibbonSimplifiedVisibility.Pinned"/> items go last.
/// </summary>
public static class RibbonSimplifiedLayout
{
    /// <summary>Computes which items stay in the line.</summary>
    public static RibbonSimplifiedLayoutResult Compute(IReadOnlyList<RibbonSimplifiedGroup> groups, double availableWidth, double overflowButtonWidth, double groupSpacing = 0)
    {
        var inLine = groups.Select(g => g.Items.Select(i => i.Visibility is RibbonSimplifiedVisibility.Auto or RibbonSimplifiedVisibility.Pinned).ToArray()).ToArray();
        var hasOverflow = groups.Any(g => g.Items.Any(i => i.Visibility == RibbonSimplifiedVisibility.Overflow));

        double Width()
        {
            var total = 0d;
            var visibleGroups = 0;
            for (var g = 0; g < groups.Count; g++)
            {
                var any = false;
                for (var i = 0; i < groups[g].Items.Count; i++)
                {
                    if (inLine[g][i])
                    {
                        total += groups[g].Items[i].Width;
                        any = true;
                    }
                }

                if (any)
                {
                    visibleGroups++;
                }
            }

            return total + (Math.Max(0, visibleGroups - 1) * groupSpacing) + (hasOverflow ? overflowButtonWidth : 0);
        }

        var groupOrder = Enumerable.Range(0, groups.Count).OrderByDescending(g => groups[g].ReductionOrder).ThenByDescending(g => g).ToArray();
        foreach (var pass in new[] { RibbonSimplifiedVisibility.Auto, RibbonSimplifiedVisibility.Pinned })
        {
            foreach (var g in groupOrder)
            {
                for (var i = groups[g].Items.Count - 1; i >= 0; i--)
                {
                    if (Width() <= availableWidth)
                    {
                        return new RibbonSimplifiedLayoutResult(inLine, hasOverflow, Width());
                    }

                    if (inLine[g][i] && groups[g].Items[i].Visibility == pass)
                    {
                        inLine[g][i] = false;
                        hasOverflow = true;
                    }
                }
            }
        }

        return new RibbonSimplifiedLayoutResult(inLine, hasOverflow, Width());
    }
}
