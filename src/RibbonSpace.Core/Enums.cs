namespace RibbonSpace;

/// <summary>Visual size of a ribbon item.</summary>
public enum RibbonItemSize
{
    /// <summary>Tall item: large icon above a (possibly two-line) label; occupies the full group height.</summary>
    Large,
    /// <summary>Row item: small icon followed by its label; stacked up to three per column.</summary>
    Medium,
    /// <summary>Icon-only row item; stacked up to three per column.</summary>
    Small,
}

/// <summary>Adaptive size state of a ribbon group, from the widest to the narrowest.</summary>
public enum RibbonGroupState
{
    /// <summary>Items use their largest size.</summary>
    Large = 0,
    /// <summary>Items use their medium size.</summary>
    Medium = 1,
    /// <summary>Items use their smallest size.</summary>
    Small = 2,
    /// <summary>The group is shown as a single drop-down button that opens the full group in a popup.</summary>
    Collapsed = 3,
}

/// <summary>Overall ribbon layout.</summary>
public enum RibbonDisplayMode
{
    /// <summary>Classic three-row ribbon with group captions (Office "Classic ribbon").</summary>
    Classic,
    /// <summary>Single-line ribbon with an overflow menu (Office "Simplified ribbon").</summary>
    Simplified,
}

/// <summary>Ribbon visibility, mirroring the Office "Ribbon display options" menu.</summary>
public enum RibbonVisibilityMode
{
    /// <summary>Tabs and commands are always shown.</summary>
    AlwaysShow,
    /// <summary>Only tabs are shown; selecting a tab shows the commands in a temporary overlay.</summary>
    TabsOnly,
    /// <summary>The ribbon is hidden until revealed from the top edge (Office "Full-screen mode").</summary>
    FullScreen,
    /// <summary>Tabs plus one button per panel (group); clicking a button opens the panel (AutoCAD "Minimize to Panel Buttons").</summary>
    PanelButtons,
    /// <summary>Tabs plus the panel (group) titles; clicking a title opens the panel (AutoCAD "Minimize to Panel Titles").</summary>
    PanelTitles,
}

/// <summary>What the ribbon's minimize button / <c>ToggleMinimized</c> does (AutoCAD minimize behaviour).</summary>
public enum RibbonMinimizeBehavior
{
    /// <summary>Toggle between the full ribbon and tabs only (Office behaviour).</summary>
    Tabs,
    /// <summary>Toggle between the full ribbon and panel titles.</summary>
    PanelTitles,
    /// <summary>Toggle between the full ribbon and panel buttons.</summary>
    PanelButtons,
    /// <summary>Cycle full ribbon → panel buttons → panel titles → tabs → full ribbon (AutoCAD "Cycle Through All").</summary>
    CycleAll,
}

/// <summary>Placement of the Quick Access Toolbar.</summary>
public enum RibbonQuickAccessPosition
{
    /// <summary>Above the ribbon, next to the title / File button.</summary>
    AboveRibbon,
    /// <summary>Below the ribbon command area.</summary>
    BelowRibbon,
}

/// <summary>Spacing density (Office "Touch/Mouse mode").</summary>
public enum RibbonDensity
{
    /// <summary>Default desktop mouse density.</summary>
    Comfortable,
    /// <summary>Tighter spacing for dense professional tools.</summary>
    Compact,
    /// <summary>Larger targets for touch input.</summary>
    Touch,
}

/// <summary>How an item participates in the simplified (single-line) ribbon.</summary>
public enum RibbonSimplifiedVisibility
{
    /// <summary>Shown in the line while space permits, then moved to the overflow menu.</summary>
    Auto,
    /// <summary>Always shown in the line (moved to overflow only as a last resort).</summary>
    Pinned,
    /// <summary>Always shown in the overflow menu.</summary>
    Overflow,
    /// <summary>Never shown in simplified mode.</summary>
    Hidden,
}

/// <summary>How the items of a classic group are arranged.</summary>
public enum RibbonGroupItemsLayout
{
    /// <summary>Large items take a full column; medium/small items stack in columns (Office default).</summary>
    Columns,
    /// <summary>Each child is a horizontal row; rows stack vertically (Office Font / Paragraph groups).</summary>
    Rows,
}

/// <summary>How a contextual tab group reacts when it becomes visible.</summary>
public enum RibbonContextualActivation
{
    /// <summary>Visible tabs are not selected automatically.</summary>
    None,
    /// <summary>The first tab of a group is selected when the group becomes visible.</summary>
    SelectOnShow,
}

/// <summary>Merge behaviour of a node when models are merged (plugins, MDI child documents).</summary>
public enum RibbonMergeAction
{
    /// <summary>Add the node, or merge its children when a node with the same id exists.</summary>
    Merge,
    /// <summary>Replace an existing node with the same id.</summary>
    Replace,
    /// <summary>Remove the existing node with the same id.</summary>
    Remove,
    /// <summary>Always add the node, even when a node with the same id exists.</summary>
    Add,
}

/// <summary>Placement of a backstage navigation item.</summary>
public enum RibbonBackstagePlacement
{
    /// <summary>Main list at the top of the navigation pane.</summary>
    Top,
    /// <summary>Footer list at the bottom of the navigation pane (Account, Feedback, Options).</summary>
    Bottom,
}

/// <summary>Orientation of a toolbar.</summary>
public enum RibbonToolBarOrientation
{
    /// <summary>Horizontal toolbar (command bar, options bar).</summary>
    Horizontal,
    /// <summary>Vertical toolbar (tool palette / rail).</summary>
    Vertical,
}
