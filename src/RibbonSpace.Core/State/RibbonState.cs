namespace RibbonSpace.State;

/// <summary>
/// Persistable user state of a ribbon: selection, display options, Quick Access Toolbar and customizations.
/// Serialize with <see cref="RibbonStateSerializer"/> (AOT / trimming safe).
/// </summary>
public sealed class RibbonState
{
    /// <summary>Current schema version.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Schema version of the serialized data.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Selected tab id.</summary>
    public string? SelectedTabId { get; set; }

    /// <summary>Classic / simplified layout.</summary>
    public RibbonDisplayMode DisplayMode { get; set; }

    /// <summary>Ribbon display option.</summary>
    public RibbonVisibilityMode VisibilityMode { get; set; }

    /// <summary>Density.</summary>
    public RibbonDensity Density { get; set; }

    /// <summary>QAT placement.</summary>
    public RibbonQuickAccessPosition QuickAccessPosition { get; set; }

    /// <summary>QAT visibility.</summary>
    public bool IsQuickAccessVisible { get; set; } = true;

    /// <summary>QAT labels.</summary>
    public bool ShowQuickAccessLabels { get; set; }

    /// <summary>Minimize behaviour (tabs, panel titles, panel buttons or cycle).</summary>
    public RibbonMinimizeBehavior MinimizeBehavior { get; set; }

    /// <summary>Panel (group) titles shown.</summary>
    public bool ShowGroupCaptions { get; set; } = true;

    /// <summary>Panels floating outside the ribbon, with their positions. <c>null</c> keeps the current floats.</summary>
    public List<RibbonFloatingGroupState>? FloatingGroups { get; set; }

    /// <summary>Ids of items in the QAT, in order. <c>null</c> keeps the application defaults.</summary>
    public List<string>? QuickAccessItemIds { get; set; }

    /// <summary>User customizations of tabs and groups.</summary>
    public RibbonCustomization Customization { get; set; } = new();

    /// <summary>Ids of recently used search results (most recent first). <c>null</c> keeps the current list.</summary>
    public List<string>? RecentSearchIds { get; set; }

    /// <summary>Recently used custom colours as <c>#RRGGBB</c> / <c>#AARRGGBB</c>. <c>null</c> keeps the current list.</summary>
    public List<string>? RecentColors { get; set; }

    /// <summary>Replaces null collections and entries (hand-edited or older JSON) with empty values.</summary>
    public void Normalize()
    {
        Customization ??= new RibbonCustomization();
        Customization.Normalize();
        QuickAccessItemIds?.RemoveAll(string.IsNullOrEmpty);
        RecentSearchIds?.RemoveAll(string.IsNullOrEmpty);
        RecentColors?.RemoveAll(string.IsNullOrEmpty);
        FloatingGroups?.RemoveAll(g => g is null || string.IsNullOrEmpty(g.GroupId));
    }
}

/// <summary>User customization of the ribbon structure ("Customize the Ribbon").</summary>
public sealed class RibbonCustomization
{
    /// <summary>Tab ids in the user's order (unlisted tabs keep their relative order at the end).</summary>
    public List<string> TabOrder { get; set; } = [];

    /// <summary>
    /// Group order per tab (tab id → group ids in the user's order). Unlisted groups keep their relative order after
    /// the listed ones; user-created groups of a built-in tab follow its built-in groups.
    /// </summary>
    public Dictionary<string, List<string>> GroupOrder { get; set; } = [];

    /// <summary>Hidden tab ids.</summary>
    public List<string> HiddenTabIds { get; set; } = [];

    /// <summary>Hidden group ids.</summary>
    public List<string> HiddenGroupIds { get; set; } = [];

    /// <summary>Renamed nodes (id → label).</summary>
    public Dictionary<string, string> Labels { get; set; } = [];

    /// <summary>User-created tabs.</summary>
    public List<RibbonCustomTab> CustomTabs { get; set; } = [];

    /// <summary>User-created groups placed in built-in tabs.</summary>
    public List<RibbonCustomGroup> CustomGroups { get; set; } = [];

    /// <summary>Replaces null collections and entries (hand-edited or older JSON) with empty values.</summary>
    public void Normalize()
    {
        TabOrder ??= [];
        GroupOrder ??= [];
        foreach (var key in GroupOrder.Where(p => p.Value is null).Select(p => p.Key).ToArray())
        {
            GroupOrder.Remove(key);
        }

        foreach (var order in GroupOrder.Values)
        {
            order.RemoveAll(string.IsNullOrEmpty);
        }

        HiddenTabIds ??= [];
        HiddenGroupIds ??= [];
        Labels ??= [];
        CustomTabs ??= [];
        CustomGroups ??= [];
        TabOrder.RemoveAll(string.IsNullOrEmpty);
        HiddenTabIds.RemoveAll(string.IsNullOrEmpty);
        HiddenGroupIds.RemoveAll(string.IsNullOrEmpty);
        CustomTabs.RemoveAll(t => t is null || string.IsNullOrEmpty(t.Id));
        CustomGroups.RemoveAll(g => g is null || string.IsNullOrEmpty(g.Id));
        foreach (var tab in CustomTabs)
        {
            tab.Label ??= string.Empty;
            tab.Groups ??= [];
            tab.Groups.RemoveAll(g => g is null || string.IsNullOrEmpty(g.Id));
            tab.Groups.ForEach(g => g.Normalize());
        }

        CustomGroups.ForEach(g => g.Normalize());
    }

    /// <summary>True when nothing is customized.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => TabOrder.Count == 0 && GroupOrder.Count == 0 && HiddenTabIds.Count == 0 && HiddenGroupIds.Count == 0
        && Labels.Count == 0 && CustomTabs.Count == 0 && CustomGroups.Count == 0;
}

/// <summary>User-created tab.</summary>
public sealed class RibbonCustomTab
{
    /// <summary>Id (prefixed "custom." by convention).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Groups.</summary>
    public List<RibbonCustomGroup> Groups { get; set; } = [];
}

/// <summary>User-created group containing references to existing commands.</summary>
public sealed class RibbonCustomGroup
{
    /// <summary>Id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Tab hosting the group (for groups in built-in tabs).</summary>
    public string? TabId { get; set; }

    /// <summary>Ids of the items shown in the group (clones of existing ribbon items).</summary>
    public List<string> ItemIds { get; set; } = [];

    internal void Normalize()
    {
        Label ??= string.Empty;
        ItemIds ??= [];
        ItemIds.RemoveAll(string.IsNullOrEmpty);
    }
}

/// <summary>A panel (group) floating outside the ribbon.</summary>
public sealed class RibbonFloatingGroupState
{
    /// <summary>Group id.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Left position in window coordinates.</summary>
    public double X { get; set; }

    /// <summary>Top position in window coordinates.</summary>
    public double Y { get; set; }
}
