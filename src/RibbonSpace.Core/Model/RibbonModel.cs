using System.Collections.ObjectModel;
using RibbonSpace.Commands;

namespace RibbonSpace.Model;

/// <summary>
/// Root of an MVVM ribbon definition. Bind it to <c>Ribbon.Model</c> in the Uno layer; every property is
/// observable and two-way synchronized with the control (selected tab, display mode, QAT, backstage...).
/// </summary>
public class RibbonModel : ObservableObject
{
    private string? _selectedTabId;
    private RibbonDisplayMode _displayMode;
    private RibbonVisibilityMode _visibilityMode;
    private RibbonDensity _density;
    private RibbonQuickAccessPosition _quickAccessPosition;
    private bool _isQuickAccessVisible = true;
    private bool _showQuickAccessLabels;
    private string? _applicationButtonLabel = "File";
    private bool _isApplicationButtonVisible = true;
    private string? _title;
    private RibbonCommandCatalog? _commandCatalog;

    /// <summary>Regular and contextual tabs, in display order.</summary>
    public ObservableCollection<RibbonTabModel> Tabs { get; } = [];

    /// <summary>Contextual tab groups.</summary>
    public ObservableCollection<RibbonContextualGroupModel> ContextualGroups { get; } = [];

    /// <summary>Items of the Quick Access Toolbar.</summary>
    public ObservableCollection<RibbonItemModel> QuickAccessItems { get; } = [];

    /// <summary>Commands offered in the QAT customize menu.</summary>
    public ObservableCollection<RibbonItemModel> QuickAccessCandidates { get; } = [];

    /// <summary>Items at the right end of the tab row (Comments, Share, Editing mode).</summary>
    public ObservableCollection<RibbonItemModel> TabStripItems { get; } = [];

    /// <summary>Backstage (File) view.</summary>
    public RibbonBackstageModel Backstage { get; } = new();

    /// <summary>Id of the selected tab.</summary>
    public string? SelectedTabId
    {
        get => _selectedTabId;
        set
        {
            if (SetProperty(ref _selectedTabId, value))
            {
                OnPropertyChanged(nameof(SelectedTab));
            }
        }
    }

    /// <summary>Classic or simplified layout.</summary>
    public RibbonDisplayMode DisplayMode { get => _displayMode; set => SetProperty(ref _displayMode, value); }

    /// <summary>Always show / tabs only / full screen.</summary>
    public RibbonVisibilityMode VisibilityMode { get => _visibilityMode; set => SetProperty(ref _visibilityMode, value); }

    /// <summary>Spacing density.</summary>
    public RibbonDensity Density { get => _density; set => SetProperty(ref _density, value); }

    /// <summary>QAT placement.</summary>
    public RibbonQuickAccessPosition QuickAccessPosition { get => _quickAccessPosition; set => SetProperty(ref _quickAccessPosition, value); }

    /// <summary>QAT visibility.</summary>
    public bool IsQuickAccessVisible { get => _isQuickAccessVisible; set => SetProperty(ref _isQuickAccessVisible, value); }

    /// <summary>Shows labels in the QAT (Office "Show command labels").</summary>
    public bool ShowQuickAccessLabels { get => _showQuickAccessLabels; set => SetProperty(ref _showQuickAccessLabels, value); }

    /// <summary>Label of the application (File) button.</summary>
    public string? ApplicationButtonLabel { get => _applicationButtonLabel; set => SetProperty(ref _applicationButtonLabel, value); }

    /// <summary>Shows the application (File) button.</summary>
    public bool IsApplicationButtonVisible { get => _isApplicationButtonVisible; set => SetProperty(ref _isApplicationButtonVisible, value); }

    /// <summary>Document / window title shown by <c>RibbonTitleBar</c>.</summary>
    public string? Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>Command catalog used to resolve <c>CommandId</c> references.</summary>
    public RibbonCommandCatalog? CommandCatalog { get => _commandCatalog; set => SetProperty(ref _commandCatalog, value); }

    /// <summary>The selected tab.</summary>
    public RibbonTabModel? SelectedTab
    {
        get => Tabs.FirstOrDefault(t => t.Id == SelectedTabId);
        set => SelectedTabId = value?.Id;
    }

    /// <summary>Finds a tab by id.</summary>
    public RibbonTabModel? FindTab(string id) => Tabs.FirstOrDefault(t => t.Id == id);

    /// <summary>Finds a contextual group by id.</summary>
    public RibbonContextualGroupModel? FindContextualGroup(string id) => ContextualGroups.FirstOrDefault(g => g.Id == id);

    /// <summary>Finds a group by id in any tab.</summary>
    public RibbonGroupModel? FindGroup(string id) => Tabs.SelectMany(t => t.Groups).FirstOrDefault(g => g.Id == id);

    /// <summary>Finds an item (recursively, including button groups and rows) by id.</summary>
    public RibbonItemModel? FindItem(string id) => EnumerateItems().FirstOrDefault(i => i.Id == id);

    /// <summary>Finds any node by id (tab, group, item, contextual group, backstage item).</summary>
    public RibbonNodeModel? FindNode(string id)
        => (RibbonNodeModel?)FindTab(id) ?? (RibbonNodeModel?)FindGroup(id) ?? (RibbonNodeModel?)FindItem(id)
        ?? (RibbonNodeModel?)FindContextualGroup(id) ?? Backstage.Items.FirstOrDefault(i => i.Id == id);

    /// <summary>Enumerates every item of every tab (recursively), plus tab strip items.</summary>
    public IEnumerable<RibbonItemModel> EnumerateItems()
    {
        foreach (var tab in Tabs)
        {
            foreach (var group in tab.Groups)
            {
                foreach (var item in Flatten(group.Items))
                {
                    yield return item;
                }
            }
        }

        foreach (var item in Flatten(TabStripItems))
        {
            yield return item;
        }
    }

    /// <summary>Enumerates items with their tab and group (used by search and customization).</summary>
    public IEnumerable<(RibbonTabModel Tab, RibbonGroupModel Group, RibbonItemModel Item)> EnumerateItemsWithPath()
    {
        foreach (var tab in Tabs)
        {
            foreach (var group in tab.Groups)
            {
                foreach (var item in Flatten(group.Items))
                {
                    yield return (tab, group, item);
                }
            }
        }
    }

    /// <summary>Flattens nested containers (button groups, rows).</summary>
    public static IEnumerable<RibbonItemModel> Flatten(IEnumerable<RibbonItemModel> items)
    {
        foreach (var item in items)
        {
            yield return item;
            if (item is RibbonButtonGroupModel container)
            {
                foreach (var child in Flatten(container.Items))
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>Items referencing a command id.</summary>
    public IEnumerable<RibbonItemModel> FindItemsByCommand(string commandId)
        => EnumerateItems().Concat(Flatten(QuickAccessItems)).Concat(Flatten(TabStripItems))
            .Where(i => string.Equals(i.CommandId, commandId, StringComparison.OrdinalIgnoreCase)).Distinct();

    /// <summary>
    /// Every node referencing a command id: items, menu entries (drop-downs, split buttons, gallery and colour picker
    /// menus, nested sub-menus) and backstage items.
    /// </summary>
    public IEnumerable<RibbonNodeModel> FindNodesByCommand(string commandId)
    {
        var items = EnumerateItems().Concat(Flatten(QuickAccessItems)).Concat(Flatten(TabStripItems)).Distinct().ToList();
        IEnumerable<RibbonNodeModel> nodes = items;
        nodes = nodes.Concat(items.SelectMany(MenuEntries));
        nodes = nodes.Concat(Backstage.Items);
        return nodes.Distinct().Where(n => string.Equals(CommandIdOf(n), commandId, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<RibbonNodeModel> MenuEntries(RibbonItemModel item)
    {
        var roots = item switch
        {
            RibbonDropDownButtonModel dropDown => dropDown.MenuItems,
            RibbonGalleryModel gallery => gallery.MenuItems,
            _ => null,
        };
        return roots is null ? [] : FlattenMenu(roots);
    }

    private static IEnumerable<RibbonNodeModel> FlattenMenu(IEnumerable<RibbonNodeModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node is RibbonMenuItemModel { Items.Count: > 0 } menu)
            {
                foreach (var child in FlattenMenu(menu.Items))
                {
                    yield return child;
                }
            }
        }
    }

    private static string? CommandIdOf(RibbonNodeModel node) => node switch
    {
        RibbonItemModel item => item.CommandId,
        RibbonMenuItemModel menu => menu.CommandId,
        RibbonBackstageItemModel backstage => backstage.CommandId,
        _ => null,
    };

    /// <summary>Sets enabled state of all items, menu entries and backstage items using a command id.</summary>
    public void SetCommandEnabled(string commandId, bool enabled)
    {
        foreach (var node in FindNodesByCommand(commandId))
        {
            node.IsEnabled = enabled;
        }
    }

    /// <summary>Sets checked state of all toggle / check items and checkable menu entries using a command id.</summary>
    public void SetCommandChecked(string commandId, bool isChecked)
    {
        foreach (var node in FindNodesByCommand(commandId))
        {
            switch (node)
            {
                case RibbonToggleButtonModel toggle:
                    toggle.IsChecked = isChecked;
                    break;
                case RibbonSplitButtonModel split:
                    split.IsChecked = isChecked;
                    break;
                case RibbonCheckBoxModel check:
                    check.IsChecked = isChecked;
                    break;
                case RibbonMenuItemModel menu:
                    menu.IsChecked = isChecked;
                    break;
            }
        }
    }

    /// <summary>Shows or hides a contextual group. Returns false when unknown.</summary>
    public bool SetContextualGroupVisible(string id, bool visible)
    {
        var group = FindContextualGroup(id);
        if (group is null)
        {
            return false;
        }

        group.IsVisible = visible;
        return true;
    }

    /// <summary>Shows exactly the given contextual groups and hides the others (selection-driven context switching).</summary>
    public void SetActiveContextualGroups(params string[] ids)
    {
        foreach (var group in ContextualGroups)
        {
            group.IsVisible = ids.Contains(group.Id, StringComparer.Ordinal);
        }
    }

    /// <summary>Tabs that are currently visible (regular visible tabs and tabs of visible contextual groups).</summary>
    public IEnumerable<RibbonTabModel> VisibleTabs
        => Tabs.Where(t => t.IsVisible && (t.ContextualGroupId is null || FindContextualGroup(t.ContextualGroupId)?.IsVisible == true));
}
