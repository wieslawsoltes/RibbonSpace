namespace RibbonSpace.Layout;

/// <summary>Measured widths of a group in each <see cref="RibbonGroupState"/>.</summary>
/// <param name="Widths">Widths indexed by <see cref="RibbonGroupState"/> (Large, Medium, Small, Collapsed).</param>
/// <param name="ReductionOrder">Groups with a higher value reduce first.</param>
/// <param name="CanCollapse">Whether the Collapsed state may be used.</param>
public sealed record RibbonGroupLayoutInfo(IReadOnlyList<double> Widths, int ReductionOrder = 0, bool CanCollapse = true)
{
    /// <summary>Width for a state. Missing states (fewer than four widths) report <see cref="double.NaN"/> (state unavailable).</summary>
    public double GetWidth(RibbonGroupState state)
        => Widths is not null && (int)state < Widths.Count ? Widths[(int)state] : double.NaN;
}

/// <summary>How <see cref="RibbonAdaptiveLayout"/> shrinks groups when the tab does not fit.</summary>
public enum RibbonReductionStrategy
{
    /// <summary>
    /// Office: every group goes to Medium, then every group to Small, then groups collapse (higher
    /// <see cref="RibbonGroupLayoutInfo.ReductionOrder"/> first within each step).
    /// </summary>
    Stepwise,
    /// <summary>
    /// AutoCAD-like: the group with the highest reduction order (ties: rightmost) shrinks all the way to Collapsed before
    /// the next one starts, so important panels stay large as long as possible.
    /// </summary>
    GroupByGroup,
}

/// <summary>Result of <c>RibbonAdaptiveLayout.Compute</c>.</summary>
/// <param name="States">Chosen state per group.</param>
/// <param name="TotalWidth">Resulting total width (including spacing).</param>
/// <param name="Fits">Whether the result fits in the available width.</param>
public sealed record RibbonAdaptiveLayoutResult(IReadOnlyList<RibbonGroupState> States, double TotalWidth, bool Fits);

/// <summary>
/// Office-style adaptive group reduction. All groups start Large; while the tab does not fit, groups are reduced one
/// step at a time. Every group is first reduced to Medium, then to Small, then collapsed. Within a step, groups with
/// a higher <see cref="RibbonGroupLayoutInfo.ReductionOrder"/> go first, and ties go right-to-left (the rightmost
/// groups shrink first, as in Microsoft Office). Steps that would not make a group narrower are skipped.
/// </summary>
public static class RibbonAdaptiveLayout
{
    /// <summary>Computes the group states for the available width.</summary>
    /// <param name="groups">Groups in display order.</param>
    /// <param name="availableWidth">Available width.</param>
    /// <param name="spacing">Spacing between groups.</param>
    public static RibbonAdaptiveLayoutResult Compute(IReadOnlyList<RibbonGroupLayoutInfo> groups, double availableWidth, double spacing = 0)
        => Compute(groups, availableWidth, spacing, RibbonReductionStrategy.Stepwise);

    /// <summary>Computes the group states for the available width with a reduction strategy.</summary>
    public static RibbonAdaptiveLayoutResult Compute(IReadOnlyList<RibbonGroupLayoutInfo> groups, double availableWidth, double spacing, RibbonReductionStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (strategy == RibbonReductionStrategy.GroupByGroup)
        {
            return ComputeGroupByGroup(groups, availableWidth, spacing);
        }

        var states = new RibbonGroupState[groups.Count];
        var total = Total(groups, states, spacing);
        if (total <= availableWidth || groups.Count == 0)
        {
            return new RibbonAdaptiveLayoutResult(states, total, true);
        }

        var order = Enumerable.Range(0, groups.Count)
            .OrderByDescending(i => groups[i].ReductionOrder)
            .ThenByDescending(i => i)
            .ToArray();

        foreach (var targetState in new[] { RibbonGroupState.Medium, RibbonGroupState.Small, RibbonGroupState.Collapsed })
        {
            foreach (var index in order)
            {
                var info = groups[index];
                if (targetState == RibbonGroupState.Collapsed && !info.CanCollapse)
                {
                    continue;
                }

                var current = info.GetWidth(states[index]);
                var next = info.GetWidth(targetState);
                if (double.IsNaN(next) || next >= current - 0.5)
                {
                    // Keep the previous state but still remember that we "passed" this state so the next,
                    // narrower state can be applied.
                    if (!double.IsNaN(next) && targetState != RibbonGroupState.Collapsed && next <= current)
                    {
                        total -= current - next;
                        states[index] = targetState;
                    }

                    continue;
                }

                total -= current - next;
                states[index] = targetState;
                if (total <= availableWidth)
                {
                    return new RibbonAdaptiveLayoutResult(states, total, true);
                }
            }
        }

        return new RibbonAdaptiveLayoutResult(states, Total(groups, states, spacing), false);
    }

    private static RibbonAdaptiveLayoutResult ComputeGroupByGroup(IReadOnlyList<RibbonGroupLayoutInfo> groups, double availableWidth, double spacing)
    {
        var states = new RibbonGroupState[groups.Count];
        var total = Total(groups, states, spacing);
        if (total <= availableWidth || groups.Count == 0)
        {
            return new RibbonAdaptiveLayoutResult(states, total, true);
        }

        var order = Enumerable.Range(0, groups.Count)
            .OrderByDescending(i => groups[i].ReductionOrder)
            .ThenByDescending(i => i)
            .ToArray();
        foreach (var index in order)
        {
            var info = groups[index];
            foreach (var targetState in new[] { RibbonGroupState.Medium, RibbonGroupState.Small, RibbonGroupState.Collapsed })
            {
                if (targetState == RibbonGroupState.Collapsed && !info.CanCollapse)
                {
                    continue;
                }

                var current = info.GetWidth(states[index]);
                var next = info.GetWidth(targetState);
                if (double.IsNaN(next) || next > current)
                {
                    continue;
                }

                total -= current - next;
                states[index] = targetState;
                if (total <= availableWidth)
                {
                    return new RibbonAdaptiveLayoutResult(states, total, true);
                }
            }
        }

        return new RibbonAdaptiveLayoutResult(states, Total(groups, states, spacing), false);
    }

    private static double Total(IReadOnlyList<RibbonGroupLayoutInfo> groups, IReadOnlyList<RibbonGroupState> states, double spacing)
    {
        var total = 0d;
        for (var i = 0; i < groups.Count; i++)
        {
            total += groups[i].GetWidth(states[i]);
        }

        return total + (Math.Max(0, groups.Count - 1) * spacing);
    }
}
