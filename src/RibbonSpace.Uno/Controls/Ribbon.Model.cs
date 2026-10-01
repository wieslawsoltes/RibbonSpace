using Microsoft.UI.Xaml;
using RibbonSpace.Commands;
using RibbonSpace.Controls.Mvvm;
using RibbonSpace.Model;

namespace RibbonSpace.Controls;

public partial class Ribbon
{
    /// <summary>Identifies <see cref="Model"/>.</summary>
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(nameof(Model), typeof(RibbonModel), typeof(Ribbon), new PropertyMetadata(null, (d, e) => ((Ribbon)d).OnModelChanged((RibbonModel?)e.OldValue, (RibbonModel?)e.NewValue)));

    private readonly List<FrameworkElement> _modelQuickAccessItems = [];
    private readonly Dictionary<FrameworkElement, RibbonModelBindings> _modelQuickAccessScopes = [];
    private RibbonModelBindings? _modelBindings;
    private RibbonElementFactory _itemFactory = new();
    private bool _syncingModel;
    private bool _modelElementsChangePending;
    private bool _modelUnloadPending;
    private bool _modelDetached;
    private long _modelCatalogToken = -1;
    private RibbonBackstage? _modelBackstage;
    private RibbonBackstage? _backstageBeforeModel;
    private RibbonCommandCatalog? _modelCatalog;
    private RibbonCommandCatalog? _catalogBeforeModel;

    /// <summary>
    /// MVVM definition of the ribbon. Tabs, groups, items, contextual groups, QAT, tab-row items and the backstage are
    /// generated from the model and kept in two-way sync (selection, display options, checked states, values...).
    /// XAML-declared tabs are kept in front of generated tabs. Collection changes are applied incrementally; model
    /// subscriptions are detached while the ribbon is unloaded (the elements are kept) and re-attached when it loads.
    /// </summary>
    public RibbonModel? Model { get => (RibbonModel?)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    /// <summary>Factory creating elements from models (override to add custom item types).</summary>
    public RibbonElementFactory ItemFactory
    {
        get => _itemFactory;
        set
        {
            _itemFactory = value ?? throw new ArgumentNullException(nameof(value));
            var model = Model;
            OnModelChanged(model, model);
        }
    }

    /// <summary>Returns the model an element was generated from.</summary>
    public static RibbonNodeModel? GetModel(DependencyObject element) => RibbonElementFactory.GetModel(element);

    /// <summary>Re-attaches the model subscriptions (call from <c>OnLoaded</c>; safe before a model is set).</summary>
    internal void OnModelLoaded()
    {
        _modelUnloadPending = false;
        _modelDetached = false;
        if (_modelBindings is { } bindings)
        {
            bindings.Dispatcher ??= DispatcherQueue;
            bindings.Resume();
        }
    }

    /// <summary>
    /// Detaches the model subscriptions so a long-lived model does not keep the unloaded ribbon alive (call from
    /// <c>OnUnloaded</c>; safe before a model is set). Deferred so a re-parenting Unloaded / Loaded pair keeps them.
    /// </summary>
    internal void OnModelUnloaded()
    {
        _modelUnloadPending = true;
        void Detach()
        {
            if (!_modelUnloadPending || IsLoaded)
            {
                return;
            }

            _modelUnloadPending = false;
            _modelDetached = true;
            _modelBindings?.Suspend();
        }

        if (DispatcherQueue?.TryEnqueue(Detach) != true)
        {
            Detach();
        }
    }

    /// <summary>
    /// Called by the element factory after generated elements were added, removed or replaced: re-links QAT copies
    /// whose source element was replaced and refreshes shortcuts (coalesced).
    /// </summary>
    internal void OnModelElementsChanged()
    {
        if (_modelElementsChangePending)
        {
            return;
        }

        void Apply()
        {
            _modelElementsChangePending = false;
            if (Model is { } model && _modelBindings is { IsDisposed: false } bindings)
            {
                SyncModelQuickAccess(model, bindings);
            }

            InvalidateShortcuts();
        }

        _modelElementsChangePending = true;
        if (DispatcherQueue?.TryEnqueue(Apply) != true)
        {
            Apply();
        }
    }

    private void OnModelChanged(RibbonModel? oldModel, RibbonModel? newModel)
    {
        // Disposing the scope removes the generated tabs, contextual groups and tab-row items (XAML ones are kept).
        _modelBindings?.Dispose();
        _modelBindings = null;
        RemoveModelQuickAccessItems();
        ReleaseModelBackstage();
        ReleaseModelCatalog();
        if (newModel is null)
        {
            return;
        }

        RibbonModelBindings.SetDispatcher(DispatcherQueue);
        _itemFactory.Ribbon = this;
        if (_modelCatalogToken < 0)
        {
            // Generated elements resolve CommandId references through the catalog: re-resolve when it changes.
            _modelCatalogToken = RegisterPropertyChangedCallback(CommandCatalogProperty, (_, _) => _itemFactory.InvalidateCommands());
        }

        var bindings = _modelBindings = new RibbonModelBindings(DispatcherQueue);
        bindings.OneWay(newModel, () => ApplyModelCatalog(newModel), nameof(RibbonModel.CommandCatalog));
        bindings.Items(newModel.ContextualGroups, ContextualGroups, _itemFactory.CreateContextualGroup, removeOnDispose: true);
        string? selectedId = null;
        bindings.Items(
            newModel.Tabs,
            Tabs,
            _itemFactory.CreateTab,
            removeOnDispose: true,
            changing: () => selectedId = newModel.SelectedTabId ?? SelectedTab?.EffectiveId,
            changed: () =>
            {
                if (selectedId is not null && SelectedTab?.EffectiveId != selectedId)
                {
                    SelectTab(selectedId);
                }

                OnModelElementsChanged();
            });
        bindings.Items<RibbonItemModel, UIElement>(newModel.TabStripItems, TabStripItems, _itemFactory.CreateItem, removeOnDispose: true, changed: OnModelElementsChanged);
        bindings.Collection(newModel.QuickAccessItems, () => SyncModelQuickAccess(newModel, bindings));
        bindings.Collection(newModel.QuickAccessCandidates, () =>
        {
            QuickAccessCandidateIds.Clear();
            foreach (var candidate in newModel.QuickAccessCandidates)
            {
                QuickAccessCandidateIds.Add(candidate.Id);
            }
        });

        // The generated backstage replaces a XAML backstage only while the model defines items.
        var backstage = _modelBackstage = _itemFactory.CreateBackstage(newModel.Backstage, bindings);
        bindings.Collection(newModel.Backstage.Items, () => ApplyModelBackstage(newModel, backstage));
        bindings.OneWay(newModel.Backstage, () =>
        {
            if (IsBackstageOpen != newModel.Backstage.IsOpen)
            {
                IsBackstageOpen = newModel.Backstage.IsOpen;
            }
        }, nameof(RibbonBackstageModel.IsOpen));

        bindings.OneWay(newModel, () =>
        {
            _syncingModel = true;
            try
            {
                if (newModel.SelectedTabId is { } id && SelectedTab?.EffectiveId != id)
                {
                    SelectTab(id);
                }

                DisplayMode = newModel.DisplayMode;
                VisibilityMode = newModel.VisibilityMode;
                Density = newModel.Density;
                QuickAccessPosition = newModel.QuickAccessPosition;
                IsQuickAccessVisible = newModel.IsQuickAccessVisible;
                ShowQuickAccessLabels = newModel.ShowQuickAccessLabels;
                ApplicationButtonLabel = newModel.ApplicationButtonLabel;
                IsApplicationButtonVisible = newModel.IsApplicationButtonVisible;
            }
            finally
            {
                _syncingModel = false;
            }
        },
        nameof(RibbonModel.SelectedTabId), nameof(RibbonModel.DisplayMode), nameof(RibbonModel.VisibilityMode), nameof(RibbonModel.Density),
        nameof(RibbonModel.QuickAccessPosition), nameof(RibbonModel.IsQuickAccessVisible), nameof(RibbonModel.ShowQuickAccessLabels),
        nameof(RibbonModel.ApplicationButtonLabel), nameof(RibbonModel.IsApplicationButtonVisible));

        if (newModel.SelectedTabId is null && SelectedTab is not null)
        {
            newModel.SelectedTabId = SelectedTab.EffectiveId;
        }

        InvalidateShortcuts();
        if (_modelDetached && !IsLoaded)
        {
            // Assigned while unloaded after having been loaded: stay detached until loaded again.
            bindings.Suspend();
        }
    }

    private void ApplyModelCatalog(RibbonModel model)
    {
        if (model.CommandCatalog is { } catalog)
        {
            if (_modelCatalog is null)
            {
                _catalogBeforeModel = CommandCatalog;
            }

            _modelCatalog = catalog;
            CommandCatalog = catalog;
        }
        else
        {
            ReleaseModelCatalog();
        }
    }

    private void ReleaseModelCatalog()
    {
        if (_modelCatalog is null)
        {
            return;
        }

        // Only restore when the catalog is still the one the model applied (the application may have replaced it).
        if (ReferenceEquals(CommandCatalog, _modelCatalog))
        {
            CommandCatalog = _catalogBeforeModel;
        }

        _modelCatalog = null;
        _catalogBeforeModel = null;
    }

    private void ApplyModelBackstage(RibbonModel model, RibbonBackstage backstage)
    {
        if (model.Backstage.Items.Count > 0)
        {
            if (!ReferenceEquals(Backstage, backstage))
            {
                _backstageBeforeModel = Backstage;
                Backstage = backstage;
            }
        }
        else if (ReferenceEquals(Backstage, backstage))
        {
            Backstage = _backstageBeforeModel;
            _backstageBeforeModel = null;
        }
    }

    private void ReleaseModelBackstage()
    {
        if (_modelBackstage is not null && ReferenceEquals(Backstage, _modelBackstage))
        {
            // Never clears a XAML / application backstage: only the generated one is replaced by what was there before.
            Backstage = _backstageBeforeModel;
        }

        _modelBackstage = null;
        _backstageBeforeModel = null;
    }

    private void RemoveModelQuickAccessItems()
    {
        _syncingModel = true;
        try
        {
            foreach (var item in _modelQuickAccessItems)
            {
                QuickAccessToolBar?.Items.Remove(item);
            }
        }
        finally
        {
            _syncingModel = false;
        }

        foreach (var scope in _modelQuickAccessScopes.Values)
        {
            scope.Dispose();
        }

        _modelQuickAccessItems.Clear();
        _modelQuickAccessScopes.Clear();
    }

    /// <summary>
    /// Applies <see cref="RibbonModel.QuickAccessItems"/> to the QAT incrementally: copies whose source element is
    /// unchanged are kept, copies of replaced elements are re-created, items without a ribbon element are generated.
    /// </summary>
    private void SyncModelQuickAccess(RibbonModel model, RibbonModelBindings bindings)
    {
        if (_syncingModel || QuickAccessToolBar is not { } qat)
        {
            return;
        }

        _syncingModel = true;
        try
        {
            var sources = GetAllItems().Where(i => RibbonItemHelper.GetSourceItem(i) is null && GetModel(i) is not null).ToList();
            var available = _modelQuickAccessItems.Where(qat.Items.Contains).ToList();
            var desired = new List<FrameworkElement>();
            foreach (var itemModel in model.QuickAccessItems)
            {
                var source = sources.FirstOrDefault(i => ReferenceEquals(GetModel(i), itemModel));
                var existing = available.FirstOrDefault(e => IsQuickAccessCopyOf(e, itemModel, source));
                if (existing is not null)
                {
                    available.Remove(existing);
                    desired.Add(existing);
                    continue;
                }

                FrameworkElement? element = null;
                if (source is IRibbonItem ri && ri.CreateLinkedCopy() is { } copy)
                {
                    RibbonItemHelper.SetSourceItem(copy, source);
                    element = copy;
                }
                else if (source is null)
                {
                    var scope = bindings.Child();
                    element = _itemFactory.CreateItem(itemModel, scope);
                    if (element is null)
                    {
                        scope.Dispose();
                    }
                    else
                    {
                        _modelQuickAccessScopes[element] = scope;
                    }
                }

                if (element is not null && !desired.Contains(element))
                {
                    RibbonItemHelper.SetOwner(element, this);
                    desired.Add(element);
                }
            }

            foreach (var stale in available)
            {
                // The QAT unlinks copies that leave it.
                qat.Items.Remove(stale);
                DisposeQuickAccessScope(stale);
            }

            RibbonModelBindings.ArrangeContiguous<UIElement>(qat.Items, desired);
            _modelQuickAccessItems.Clear();
            _modelQuickAccessItems.AddRange(desired);
        }
        finally
        {
            _syncingModel = false;
        }
    }

    private static bool IsQuickAccessCopyOf(FrameworkElement element, RibbonItemModel model, FrameworkElement? source)
    {
        var linkedSource = RibbonItemHelper.GetSourceItem(element);
        return linkedSource is not null
            ? source is not null && ReferenceEquals(linkedSource, source)
            : source is null && ReferenceEquals(GetModel(element), model);
    }

    private void DisposeQuickAccessScope(FrameworkElement element)
    {
        if (_modelQuickAccessScopes.Remove(element, out var scope))
        {
            scope.Dispose();
        }
    }

    private void OnModelSelectedTabChanged(RibbonTab? tab)
    {
        if (Model is { } model && !_syncingModel && tab is not null)
        {
            model.SelectedTabId = tab.EffectiveId;
        }
    }

    private void OnModelDisplayModeChanged()
    {
        if (Model is { } model && !_syncingModel)
        {
            model.DisplayMode = DisplayMode;
            model.Density = Density;
        }
    }

    private void OnModelVisibilityModeChanged()
    {
        if (Model is { } model && !_syncingModel)
        {
            model.VisibilityMode = VisibilityMode;
        }
    }

    private void OnModelQuickAccessChanged()
    {
        if (Model is { } model && !_syncingModel)
        {
            model.QuickAccessPosition = QuickAccessPosition;
            model.IsQuickAccessVisible = IsQuickAccessVisible;
            model.ShowQuickAccessLabels = ShowQuickAccessLabels;
        }
    }

    private void OnModelBackstageChanged(bool open)
    {
        if (Model is { } model && model.Backstage.IsOpen != open)
        {
            model.Backstage.IsOpen = open;
        }
    }

    private void SyncQuickAccessToModel()
    {
        if (Model is not { } model || _syncingModel || QuickAccessToolBar is not { } qat)
        {
            return;
        }

        _syncingModel = true;
        try
        {
            var models = qat.Items.OfType<FrameworkElement>()
                .Select(i => GetModel(RibbonItemHelper.GetSourceItem(i) ?? i) as RibbonItemModel)
                .OfType<RibbonItemModel>()
                .ToList();
            if (!models.SequenceEqual(model.QuickAccessItems))
            {
                model.QuickAccessItems.Clear();
                foreach (var m in models)
                {
                    model.QuickAccessItems.Add(m);
                }
            }

            _modelQuickAccessItems.Clear();
            _modelQuickAccessItems.AddRange(qat.Items.OfType<FrameworkElement>().Where(i => GetModel(RibbonItemHelper.GetSourceItem(i) ?? i) is not null));
            foreach (var element in _modelQuickAccessScopes.Keys.Where(e => !qat.Items.Contains(e)).ToList())
            {
                DisposeQuickAccessScope(element);
            }
        }
        finally
        {
            _syncingModel = false;
        }
    }
}
