using Microsoft.UI.Xaml;
using RibbonSpace.State;

namespace RibbonSpace.Controls;

public partial class Ribbon
{
    private RibbonCustomization _customization = new();
    private readonly List<RibbonTab> _customTabs = [];
    private readonly List<(RibbonTab Tab, RibbonGroup Group)> _customGroups = [];
    private bool _applyingCustomization;

    /// <summary>Current user customization (read-only snapshot; use <see cref="ApplyCustomization"/> to change).</summary>
    public RibbonCustomization Customization => _customization;

    /// <summary>Captures the persistable state (selection, display options, QAT, customization).</summary>
    public RibbonState GetState() => new()
    {
        SelectedTabId = SelectedTab?.EffectiveId,
        DisplayMode = DisplayMode,
        VisibilityMode = VisibilityMode,
        Density = Density,
        QuickAccessPosition = QuickAccessPosition,
        IsQuickAccessVisible = IsQuickAccessVisible,
        ShowQuickAccessLabels = ShowQuickAccessLabels,
        QuickAccessItemIds = GetQuickAccessItemIds().ToList(),
        Customization = Clone(_customization),
        RecentSearchIds = SearchEngine.Recent.ToList(),
    };

    /// <summary>Restores a state captured with <see cref="GetState"/>.</summary>
    public void ApplyState(RibbonState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Normalize();
        ApplyCustomization(state.Customization);
        if (state.RecentSearchIds is { } recent)
        {
            SearchEngine.SetRecent(recent);
        }

        DisplayMode = state.DisplayMode;
        VisibilityMode = state.VisibilityMode;
        Density = state.Density;
        QuickAccessPosition = state.QuickAccessPosition;
        IsQuickAccessVisible = state.IsQuickAccessVisible;
        ShowQuickAccessLabels = state.ShowQuickAccessLabels;
        if (state.QuickAccessItemIds is { } ids && QuickAccessToolBar is { } qat)
        {
            // Reorder in place: copies that stay keep their links, only dropped copies are removed (and unlinked).
            var existing = qat.Items.OfType<FrameworkElement>().ToList();
            var desired = new List<UIElement>();
            foreach (var id in ids)
            {
                var own = existing.FirstOrDefault(i => (i is IRibbonItem ri && ri.Id == id) || ((RibbonItemHelper.GetSourceItem(i) as IRibbonItem)?.Id == id));
                var element = own ?? (FindItem(id) is { } item && !IsInQuickAccess(item) ? CreateQuickAccessCopy(item) : null);
                if (element is not null && !desired.Contains(element))
                {
                    desired.Add(element);
                }
            }

            foreach (var item in qat.Items.Where(i => !desired.Contains(i)).ToArray())
            {
                qat.Items.Remove(item);
            }

            for (var i = 0; i < desired.Count; i++)
            {
                var index = qat.Items.IndexOf(desired[i]);
                if (index < 0)
                {
                    qat.Items.Insert(i, desired[i]);
                }
                else if (index != i)
                {
                    qat.Items.Move(index, i);
                }
            }
        }

        if (state.SelectedTabId is { } tabId)
        {
            SelectTab(tabId);
        }
    }

    /// <summary>Serializes the state to JSON.</summary>
    public string SaveStateToJson() => RibbonStateSerializer.Serialize(GetState());

    /// <summary>Restores state from JSON (ignored when invalid).</summary>
    public bool LoadStateFromJson(string? json)
    {
        var state = RibbonStateSerializer.Deserialize(json);
        if (state is null)
        {
            return false;
        }

        ApplyState(state);
        return true;
    }

    /// <summary>Applies a customization (hidden / renamed / reordered tabs and groups, custom tabs and groups).</summary>
    public void ApplyCustomization(RibbonCustomization customization)
    {
        ArgumentNullException.ThrowIfNull(customization);
        customization.Normalize();
        var selectedId = SelectedTab?.EffectiveId;
        _applyingCustomization = true;
        try
        {
            RemoveCustomElements();
            _customization = Clone(customization);
            foreach (var tab in Tabs)
            {
                RestoreOriginal(tab);
            }

            foreach (var custom in _customization.CustomTabs)
            {
                var tab = new RibbonTab { Id = custom.Id, Header = custom.Label, IsCustom = true };
                foreach (var group in custom.Groups)
                {
                    tab.Groups.Add(CreateCustomGroup(group));
                }

                _customTabs.Add(tab);
                Tabs.Add(tab);
            }

            foreach (var group in _customization.CustomGroups)
            {
                if (group.TabId is { } tabId && FindTab(tabId) is { } host)
                {
                    var element = CreateCustomGroup(group);
                    host.Groups.Add(element);
                    _customGroups.Add((host, element));
                }
            }

            foreach (var tab in Tabs)
            {
                ApplyCustomizationToTab(tab);
            }
        }
        finally
        {
            _applyingCustomization = false;
        }

        OnTabsChanged();
        // Custom tabs are recreated as new instances: keep the user's selection by id.
        if (selectedId is not null && SelectedTab?.EffectiveId != selectedId)
        {
            SelectTab(selectedId);
        }

        RaiseStateChanged();
    }

    /// <summary>Removes all user customizations.</summary>
    public void ResetCustomization() => ApplyCustomization(new RibbonCustomization());

    private RibbonGroup CreateCustomGroup(RibbonCustomGroup definition)
    {
        var group = new RibbonGroup { Id = definition.Id, Header = definition.Label, IsCustom = true };
        foreach (var id in definition.ItemIds)
        {
            if (FindItem(id) is IRibbonItem item && item.CreateLinkedCopy() is { } copy)
            {
                RibbonItemHelper.SetSourceItem(copy, (FrameworkElement)item);
                group.Items.Add(copy);
            }
        }

        return group;
    }

    private void RemoveCustomElements()
    {
        foreach (var tab in _customTabs)
        {
            Tabs.Remove(tab);
            foreach (var item in tab.Groups.SelectMany(g => g.Items))
            {
                RibbonItemHelper.UnlinkTree(item);
            }
        }

        foreach (var (tab, group) in _customGroups)
        {
            tab.Groups.Remove(group);
            foreach (var item in group.Items)
            {
                RibbonItemHelper.UnlinkTree(item);
            }
        }

        _customTabs.Clear();
        _customGroups.Clear();
    }

    private static void RestoreOriginal(RibbonTab tab)
    {
        if (tab.OriginalHeader is not null)
        {
            tab.Header = tab.OriginalHeader;
        }

        tab.HiddenByCustomization = false;
        foreach (var group in tab.Groups)
        {
            if (group.OriginalHeader is not null)
            {
                group.Header = group.OriginalHeader;
            }

            group.HiddenByCustomization = false;
        }
    }

    private void ApplyCustomizationToTab(RibbonTab tab)
    {
        var c = _customization;
        if (tab.EffectiveId is { } tabId)
        {
            if (c.Labels.TryGetValue(tabId, out var label))
            {
                tab.OriginalHeader ??= tab.Header;
                tab.Header = label;
            }

            if (tab.HiddenByCustomization != c.HiddenTabIds.Contains(tabId))
            {
                tab.HiddenByCustomization = c.HiddenTabIds.Contains(tabId);
            }

            tab.GroupOrder = c.GroupOrder.TryGetValue(tabId, out var groupOrder) && groupOrder.Count > 0 ? groupOrder.ToArray() : null;
        }

        foreach (var group in tab.Groups)
        {
            if (group.EffectiveId is not { } groupId)
            {
                continue;
            }

            if (c.Labels.TryGetValue(groupId, out var groupLabel))
            {
                group.OriginalHeader ??= group.Header;
                group.Header = groupLabel;
            }

            group.HiddenByCustomization = c.HiddenGroupIds.Contains(groupId);
        }

        if (!_applyingCustomization)
        {
            OnTabsChanged();
        }
    }

    private List<RibbonTab> ApplyTabOrder(List<RibbonTab> tabs)
    {
        var order = _customization.TabOrder;
        if (order.Count == 0)
        {
            return tabs;
        }

        return tabs
            .Select((tab, index) => (tab, index, rank: order.IndexOf(tab.EffectiveId ?? string.Empty)))
            .OrderBy(t => t.tab.IsContextual ? 1 : 0)
            .ThenBy(t => t.rank < 0 ? order.Count + t.index : t.rank)
            .Select(t => t.tab)
            .ToList();
    }

    private static RibbonCustomization Clone(RibbonCustomization customization)
        => RibbonStateSerializer.Deserialize(RibbonStateSerializer.Serialize(new RibbonState { Customization = customization }))?.Customization ?? new RibbonCustomization();
}
