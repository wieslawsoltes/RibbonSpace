namespace RibbonSpace.Model;

/// <summary>
/// Merges ribbon models (plugins, add-ins, MDI child documents) by node id and reverts the merge.
/// Tabs merge into tabs, groups into groups and items into groups. <see cref="RibbonNodeModel.MergeAction"/>
/// and <see cref="RibbonNodeModel.Order"/> control the result.
/// </summary>
public sealed class RibbonModelMerger
{
    private readonly List<Action> _undo = [];

    /// <summary>Merges <paramref name="source"/> into <paramref name="target"/>. Call <see cref="Unmerge"/> to revert.</summary>
    public static RibbonModelMerger Merge(RibbonModel target, RibbonModel source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        var merger = new RibbonModelMerger();
        merger.MergeCore(target, source);
        return merger;
    }

    /// <summary>Reverts every change made by the merge (in reverse order).</summary>
    public void Unmerge()
    {
        for (var i = _undo.Count - 1; i >= 0; i--)
        {
            _undo[i]();
        }

        _undo.Clear();
    }

    private void MergeCore(RibbonModel target, RibbonModel source)
    {
        MergeList(target.ContextualGroups, source.ContextualGroups, null);
        MergeList(target.Tabs, source.Tabs, (existing, tab) => MergeList(existing.Groups, tab.Groups, (g, sg) => MergeItems(g.Items, sg.Items)));
        MergeItems(target.QuickAccessItems, source.QuickAccessItems);
        MergeItems(target.TabStripItems, source.TabStripItems);
        MergeList(target.Backstage.Items, source.Backstage.Items, null);
        foreach (var candidate in source.QuickAccessCandidates.ToArray())
        {
            if (target.QuickAccessCandidates.All(c => c.Id != candidate.Id))
            {
                Insert(target.QuickAccessCandidates, candidate);
            }
        }

        // Plugin commands must resolve in the host: copy descriptors the host does not define yet.
        if (source.CommandCatalog is { } sourceCatalog && !ReferenceEquals(sourceCatalog, target.CommandCatalog))
        {
            if (target.CommandCatalog is null)
            {
                target.CommandCatalog = sourceCatalog;
                _undo.Add(() =>
                {
                    if (ReferenceEquals(target.CommandCatalog, sourceCatalog))
                    {
                        target.CommandCatalog = null;
                    }
                });
            }
            else
            {
                var catalog = target.CommandCatalog;
                foreach (var descriptor in sourceCatalog.Commands)
                {
                    if (catalog.Find(descriptor.Id) is null)
                    {
                        catalog.Register(descriptor);
                        _undo.Add(() =>
                        {
                            if (ReferenceEquals(catalog.Find(descriptor.Id), descriptor))
                            {
                                catalog.Unregister(descriptor.Id);
                            }
                        });
                    }
                }
            }
        }
    }

    /// <summary>Merges a list of nodes by id honouring <see cref="RibbonNodeModel.MergeAction"/>.</summary>
    private void MergeList<T>(IList<T> target, IEnumerable<T> source, Action<T, T>? mergeChildren)
        where T : RibbonNodeModel
    {
        foreach (var node in source.ToArray())
        {
            var existing = string.IsNullOrEmpty(node.Id) ? null : target.FirstOrDefault(n => n.Id == node.Id);
            switch (node.MergeAction)
            {
                case RibbonMergeAction.Remove:
                    if (existing is not null)
                    {
                        Remove(target, existing);
                    }

                    break;
                case RibbonMergeAction.Replace when existing is not null:
                    Replace(target, existing, node);
                    break;
                case RibbonMergeAction.Merge when existing is not null:
                    mergeChildren?.Invoke(existing, node);
                    break;
                default:
                    // Add, or Merge / Replace without an existing node.
                    Insert(target, node);
                    break;
            }
        }
    }

    /// <summary>Items merge like other nodes and also recurse into containers (button groups, rows, drop-down menus).</summary>
    private void MergeItems(IList<RibbonItemModel> target, IEnumerable<RibbonItemModel> source)
        => MergeList(target, source, (existing, item) =>
        {
            switch ((existing, item))
            {
                case (RibbonButtonGroupModel targetGroup, RibbonButtonGroupModel sourceGroup):
                    MergeItems(targetGroup.Items, sourceGroup.Items);
                    break;
                case (RibbonDropDownButtonModel targetMenu, RibbonDropDownButtonModel sourceMenu):
                    MergeList(targetMenu.MenuItems, sourceMenu.MenuItems, null);
                    break;
                case (RibbonGalleryModel targetGallery, RibbonGalleryModel sourceGallery):
                    MergeList(targetGallery.Items, sourceGallery.Items, null);
                    MergeList(targetGallery.MenuItems, sourceGallery.MenuItems, null);
                    break;
            }
        });

    private void Insert<T>(IList<T> list, T node)
        where T : class
    {
        var order = (node as RibbonNodeModel)?.Order ?? 0;
        var index = list.Count;
        if (order != 0)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (((list[i] as RibbonNodeModel)?.Order ?? 0) > order)
                {
                    index = i;
                    break;
                }
            }
        }

        list.Insert(index, node);
        _undo.Add(() => list.Remove(node));
    }

    private void Remove<T>(IList<T> list, T node)
    {
        var index = list.IndexOf(node);
        if (index < 0)
        {
            return;
        }

        // Remember the neighbour so the node returns to the right place even if other merges changed the list.
        var previous = index > 0 ? list[index - 1] : default;
        list.RemoveAt(index);
        _undo.Add(() =>
        {
            var anchor = previous is null ? -1 : list.IndexOf(previous);
            list.Insert(anchor >= 0 ? anchor + 1 : Math.Min(index, list.Count), node);
        });
    }

    private void Replace<T>(IList<T> list, T existing, T replacement)
    {
        var index = list.IndexOf(existing);
        list[index] = replacement;
        _undo.Add(() =>
        {
            var current = list.IndexOf(replacement);
            if (current >= 0)
            {
                list[current] = existing;
            }
        });
    }
}
