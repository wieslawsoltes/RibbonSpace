using RibbonSpace.Layout;

namespace RibbonSpace.Core.Tests;

public class AdaptiveLayoutTests
{
    private static RibbonGroupLayoutInfo G(double l, double m, double s, double c, int order = 0, bool canCollapse = true)
        => new([l, m, s, c], order, canCollapse);

    [Fact]
    public void Everything_stays_large_when_it_fits()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40), G(100, 80, 60, 40)], 500);
        Assert.True(result.Fits);
        Assert.All(result.States, s => Assert.Equal(RibbonGroupState.Large, s));
        Assert.Equal(200, result.TotalWidth);
    }

    [Fact]
    public void Rightmost_group_reduces_first()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40), G(100, 80, 60, 40)], 185);
        Assert.Equal([RibbonGroupState.Large, RibbonGroupState.Medium], result.States);
        Assert.True(result.Fits);
    }

    [Fact]
    public void All_groups_reach_medium_before_any_reaches_small()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40), G(100, 80, 60, 40)], 145);
        Assert.Equal([RibbonGroupState.Medium, RibbonGroupState.Small], result.States);
    }

    [Fact]
    public void Reduction_order_overrides_position()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40, order: 5), G(100, 80, 60, 40)], 185);
        Assert.Equal([RibbonGroupState.Medium, RibbonGroupState.Large], result.States);
    }

    [Fact]
    public void Groups_collapse_last_and_respect_can_collapse()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40), G(100, 80, 60, 40, canCollapse: false)], 100);
        Assert.Equal([RibbonGroupState.Collapsed, RibbonGroupState.Small], result.States);
        Assert.True(result.Fits);
    }

    [Fact]
    public void Reports_when_nothing_fits()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40), G(100, 80, 60, 40)], 10);
        Assert.False(result.Fits);
        Assert.All(result.States, s => Assert.Equal(RibbonGroupState.Collapsed, s));
        Assert.Equal(80, result.TotalWidth);
    }

    [Fact]
    public void Skips_steps_that_do_not_reduce_width()
    {
        // Group 1 has no medium variant benefit; group 0 must shrink instead.
        var result = RibbonAdaptiveLayout.Compute([G(100, 70, 60, 40), G(100, 100, 100, 40)], 175);
        Assert.Equal(RibbonGroupState.Medium, result.States[0]);
        Assert.NotEqual(RibbonGroupState.Collapsed, result.States[1]);
        Assert.True(result.Fits);
    }

    [Fact]
    public void Spacing_is_included()
    {
        var result = RibbonAdaptiveLayout.Compute([G(100, 80, 60, 40), G(100, 80, 60, 40)], 205, spacing: 10);
        Assert.Equal(RibbonGroupState.Medium, result.States[1]);
        Assert.Equal(190, result.TotalWidth);
    }
}

public class GroupItemsArrangerTests
{
    [Fact]
    public void Large_items_take_full_columns_and_small_items_stack_in_threes()
    {
        var items = new[]
        {
            new RibbonArrangeItem(50, 66, true),
            new RibbonArrangeItem(60, 22, false),
            new RibbonArrangeItem(70, 22, false),
            new RibbonArrangeItem(40, 22, false),
            new RibbonArrangeItem(30, 22, false),
        };
        var result = RibbonGroupItemsArranger.Arrange(items, 66, 3, columnSpacing: 0);
        Assert.Equal(0, result.Items[0].Column);
        Assert.Equal(66, result.Items[0].Height);
        Assert.Equal(1, result.Items[1].Column);
        Assert.Equal(2, result.Items[3].Row);
        Assert.Equal(2, result.Items[4].Column);
        Assert.Equal(50, result.Items[1].X);
        Assert.Equal(120, result.Items[4].X); // 50 + max(60,70,40)
        Assert.Equal(150, result.Width);
    }

    [Fact]
    public void Large_item_breaks_small_column()
    {
        var items = new[]
        {
            new RibbonArrangeItem(40, 22, false),
            new RibbonArrangeItem(50, 66, true),
            new RibbonArrangeItem(40, 22, false),
        };
        var result = RibbonGroupItemsArranger.Arrange(items, 66, 3, 0);
        Assert.Equal([0, 1, 2], result.Items.Select(i => i.Column));
    }

    [Fact]
    public void Two_row_layout_and_column_break()
    {
        var items = new[]
        {
            new RibbonArrangeItem(40, 30, false),
            new RibbonArrangeItem(40, 30, false),
            new RibbonArrangeItem(40, 30, false),
            new RibbonArrangeItem(40, 30, false, StartsNewColumn: true),
        };
        var result = RibbonGroupItemsArranger.Arrange(items, 60, 2, 0);
        Assert.Equal([0, 0, 1, 2], result.Items.Select(i => i.Column));
        Assert.Equal(30, result.Items[1].Y);
    }

    [Fact]
    public void Distribute_rows_centers_partial_columns()
    {
        var items = new[] { new RibbonArrangeItem(40, 22, false) };
        var stacked = RibbonGroupItemsArranger.Arrange(items, 66, 3);
        var distributed = RibbonGroupItemsArranger.Arrange(items, 66, 3, distributeRows: true);
        Assert.Equal(0, stacked.Items[0].Y);
        Assert.True(distributed.Items[0].Y > 0);
    }
}

public class SimplifiedLayoutTests
{
    private static RibbonSimplifiedGroup Group(params double[] widths)
        => new(widths.Select(w => new RibbonSimplifiedItem(w, RibbonSimplifiedVisibility.Auto)).ToArray());

    [Fact]
    public void Everything_in_line_when_it_fits()
    {
        var result = RibbonSimplifiedLayout.Compute([Group(30, 30), Group(30)], 200, 32);
        Assert.False(result.HasOverflow);
        Assert.All(result.InLine.SelectMany(g => g), Assert.True);
    }

    [Fact]
    public void Removes_from_the_end_of_the_rightmost_group()
    {
        var result = RibbonSimplifiedLayout.Compute([Group(30, 30), Group(30, 30)], 110, 20);
        Assert.True(result.HasOverflow);
        Assert.Equal([true, true], result.InLine[0]);
        Assert.Equal([true, false], result.InLine[1]);
        Assert.True(result.TotalWidth <= 110);
    }

    [Fact]
    public void Pinned_items_overflow_last_and_overflow_items_never_in_line()
    {
        var groups = new[]
        {
            new RibbonSimplifiedGroup([new(30, RibbonSimplifiedVisibility.Pinned), new(30, RibbonSimplifiedVisibility.Auto), new(30, RibbonSimplifiedVisibility.Overflow)]),
        };
        var result = RibbonSimplifiedLayout.Compute(groups, 60, 20);
        Assert.Equal([true, false, false], result.InLine[0]);
        Assert.True(result.HasOverflow);
    }

    [Fact]
    public void Hidden_items_are_neither_in_line_nor_counted()
    {
        var groups = new[] { new RibbonSimplifiedGroup([new(30, RibbonSimplifiedVisibility.Hidden), new(30, RibbonSimplifiedVisibility.Auto)]) };
        var result = RibbonSimplifiedLayout.Compute(groups, 100, 20);
        Assert.Equal([false, true], result.InLine[0]);
        Assert.False(result.HasOverflow);
        Assert.Equal(30, result.TotalWidth);
    }

    [Fact]
    public void Size_definition_parsing()
    {
        Assert.Equal(RibbonSizeDefinition.LargeMediumSmall, RibbonSizeDefinition.Parse("Large, Middle, Small"));
        Assert.Equal(RibbonSizeDefinition.AlwaysLarge, RibbonSizeDefinition.Parse("large"));
        Assert.Equal(new RibbonSizeDefinition(RibbonItemSize.Large, RibbonItemSize.Small, RibbonItemSize.Small), RibbonSizeDefinition.Parse("Large,Small"));
        Assert.False(RibbonSizeDefinition.TryParse("Huge", out _));
        Assert.Equal(RibbonItemSize.Medium, RibbonSizeDefinition.LargeMediumSmall.GetSize(RibbonGroupState.Medium));
        Assert.Equal(RibbonItemSize.Large, RibbonSizeDefinition.LargeMediumSmall.GetSize(RibbonGroupState.Collapsed));
    }
}
