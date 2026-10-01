using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using RibbonSpace.Search;

namespace RibbonSpace.Controls;

/// <summary>Attached search metadata for ribbon items.</summary>
public static class RibbonSearch
{
    /// <summary>Identifies the Keywords attached property (comma separated synonyms).</summary>
    public static readonly DependencyProperty KeywordsProperty = DependencyProperty.RegisterAttached("Keywords", typeof(string), typeof(RibbonSearch), new PropertyMetadata(null));

    /// <summary>Identifies the IsSearchable attached property (default true).</summary>
    public static readonly DependencyProperty IsSearchableProperty = DependencyProperty.RegisterAttached("IsSearchable", typeof(bool), typeof(RibbonSearch), new PropertyMetadata(true));

    /// <summary>Gets keywords.</summary>
    public static string? GetKeywords(DependencyObject element) => (string?)element.GetValue(KeywordsProperty);

    /// <summary>Sets keywords.</summary>
    public static void SetKeywords(DependencyObject element, string? value) => element.SetValue(KeywordsProperty, value);

    /// <summary>Gets whether the item appears in search.</summary>
    public static bool GetIsSearchable(DependencyObject element) => (bool)element.GetValue(IsSearchableProperty);

    /// <summary>Sets whether the item appears in search.</summary>
    public static void SetIsSearchable(DependencyObject element, bool value) => element.SetValue(IsSearchableProperty, value);
}

public partial class Ribbon
{
    /// <summary>Command search engine ("Tell me" / Microsoft Search / command palette).</summary>
    public RibbonSearchEngine SearchEngine { get; } = new();

    /// <summary>Extra search entries (help topics, recent documents, custom actions). Targets may be <see cref="Action"/>s.</summary>
    public ObservableCollection<RibbonSearchEntry> AdditionalSearchEntries { get; } = [];

    /// <summary>Builds search entries for every reachable item, backstage item, catalog command and additional entry.</summary>
    public IReadOnlyList<RibbonSearchEntry> BuildSearchEntries()
    {
        var entries = new List<RibbonSearchEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tab in Tabs.Where(t => t.IsAvailable))
        {
            foreach (var group in tab.Groups.Where(g => g.IsShown))
            {
                foreach (var element in group.GetAllItems())
                {
                    if (element is not IRibbonItemInternal item || string.IsNullOrWhiteSpace(item.Label) || !RibbonSearch.GetIsSearchable(element) || element.Visibility != Visibility.Visible)
                    {
                        continue;
                    }

                    var id = item.Id ?? $"{tab.EffectiveId}/{group.EffectiveId}/{item.Label}";
                    if (!seen.Add(id))
                    {
                        continue;
                    }

                    var description = item.ScreenTip switch
                    {
                        string s => s,
                        RibbonScreenTip st => st.Description,
                        Model.RibbonScreenTip m => m.Description,
                        _ => null,
                    };
                    entries.Add(new RibbonSearchEntry(
                        id,
                        item.Label!.Replace('\n', ' '),
                        $"{tab.Header} › {group.Header}",
                        description,
                        RibbonSearch.GetKeywords(element)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                        item.Shortcut,
                        element,
                        element is not Microsoft.UI.Xaml.Controls.Control { IsEnabled: false }));
                }
            }
        }

        if (Backstage is { } backstage)
        {
            foreach (var item in backstage.Items.Where(i => i.IsVisible && !string.IsNullOrEmpty(i.Header)))
            {
                var id = "backstage/" + (item.Id ?? item.Header);
                if (seen.Add(id))
                {
                    entries.Add(new RibbonSearchEntry(id, item.Header!, ApplicationButtonLabel ?? Localization.RibbonStrings.Current.File, Target: item, IsEnabled: item.IsEnabled));
                }
            }
        }

        if (CommandCatalog is { } catalog)
        {
            foreach (var command in catalog.Commands)
            {
                if (entries.Any(e => e.Target is IRibbonItem { CommandId: { } cid } && string.Equals(cid, command.Id, StringComparison.OrdinalIgnoreCase)) || !seen.Add("command/" + command.Id))
                {
                    continue;
                }

                entries.Add(new RibbonSearchEntry("command/" + command.Id, command.Label, command.Category, command.Description, command.Keywords.ToArray(), command.Shortcut, command, command.IsEnabled));
            }
        }

        foreach (var extra in AdditionalSearchEntries)
        {
            if (seen.Add(extra.Id))
            {
                entries.Add(extra);
            }
        }

        return entries;
    }

    /// <summary>Searches commands.</summary>
    public IReadOnlyList<RibbonSearchResult> Search(string? query, int maxResults = 12)
    {
        SearchEngine.SetEntries(BuildSearchEntries());
        return SearchEngine.Search(query, maxResults);
    }

    /// <summary>Executes a search result (invokes the item, opening its tab for inputs and galleries).</summary>
    public bool ExecuteSearchEntry(RibbonSearchEntry entry)
    {
        SearchEngine.MarkUsed(entry.Id);
        switch (entry.Target)
        {
            case RibbonBackstageItem backstageItem when Backstage is { } backstage:
                IsBackstageOpen = true;
                backstage.Invoke(backstageItem);
                return true;
            case Commands.RibbonCommandDescriptor descriptor:
                return CommandCatalog?.Execute(descriptor.Id) ?? false;
            case Action action:
                action();
                return true;
            case FrameworkElement element when element is IRibbonItem item:
                var needsUi = element is RibbonInputBase or RibbonGallery or RibbonDropDownButton || (element is RibbonButton { Flyout: not null });
                if (needsUi)
                {
                    var (tab, group) = FindLocation(element);
                    if (tab is not null)
                    {
                        SelectedTab = tab;
                        if (VisibilityMode == RibbonVisibilityMode.TabsOnly)
                        {
                            OpenMinimizedPopup();
                        }

                        if (group is { State: RibbonGroupState.Collapsed, IsSimplified: false })
                        {
                            group.OpenPopup();
                        }
                    }

                    DispatcherQueue?.TryEnqueue(() => item.OnKeyTip());
                    return true;
                }

                return item.Invoke();
            default:
                return false;
        }
    }
}
