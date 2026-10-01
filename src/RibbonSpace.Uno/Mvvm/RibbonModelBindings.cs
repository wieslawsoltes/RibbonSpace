using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace RibbonSpace.Controls.Mvvm;

/// <summary>
/// Reflection-free, AOT / trimming safe synchronization between observable models and elements.
/// Every subscription is released by <see cref="Dispose"/>; <see cref="Suspend"/> / <see cref="Resume"/> detach and
/// re-attach the model-side subscriptions (so a long-lived view model does not keep an unloaded tree alive) without
/// recreating the elements.
/// </summary>
public sealed class RibbonModelBindings : IDisposable
{
    private readonly List<Entry> _entries = [];
    private RibbonModelBindings? _parent;
    private DispatcherQueue? _dispatcher;
    private bool _disposed;
    private bool _suspended;

    /// <summary>Creates a binding scope.</summary>
    public RibbonModelBindings()
    {
    }

    /// <summary>Creates a binding scope marshalling model changes raised on other threads to <paramref name="dispatcher"/>.</summary>
    public RibbonModelBindings(DispatcherQueue? dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Dispatcher of the owning element. Model changes raised on another thread are marshalled to it.
    /// Child scopes inherit the dispatcher of their parent.
    /// </summary>
    public DispatcherQueue? Dispatcher
    {
        get => _dispatcher ?? _parent?.Dispatcher;
        set => _dispatcher = value;
    }

    /// <summary>True after <see cref="Dispose"/>.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>True while model subscriptions are detached (this scope or a parent is suspended).</summary>
    public bool IsSuspended => _suspended || _parent?.IsSuspended == true;

    /// <summary>Applies <paramref name="apply"/> now and whenever one of <paramref name="properties"/> changes.</summary>
    public void OneWay(INotifyPropertyChanged model, Action apply, params string[] properties)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(apply);
        if (_disposed)
        {
            return;
        }

        apply();
        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (properties.Length == 0 || string.IsNullOrEmpty(e.PropertyName) || properties.Contains(e.PropertyName))
            {
                Run(apply);
            }
        }

        AddEntry(new Entry(() => model.PropertyChanged += Handler, () => model.PropertyChanged -= Handler, apply, null));
    }

    /// <summary>
    /// Two-way synchronization of a model property with a dependency property of an element.
    /// </summary>
    public void TwoWay(INotifyPropertyChanged model, string property, Action toElement, DependencyObject element, DependencyProperty dp, Action toModel)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (_disposed)
        {
            return;
        }

        var syncing = false;
        void ToElement()
        {
            if (syncing) return;
            syncing = true;
            try { toElement(); }
            finally { syncing = false; }
        }

        OneWay(model, ToElement, property);
        var token = element.RegisterPropertyChangedCallback(dp, (_, _) =>
        {
            if (syncing || IsSuspended || _disposed) return;
            syncing = true;
            try { toModel(); }
            finally { syncing = false; }
        });
        Add(() => element.UnregisterPropertyChangedCallback(dp, token));
    }

    /// <summary>Runs <paramref name="onChanged"/> now and whenever the collection changes.</summary>
    public void Collection(INotifyCollectionChanged collection, Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(onChanged);
        if (_disposed)
        {
            return;
        }

        onChanged();
        void Handler(object? sender, NotifyCollectionChangedEventArgs e) => Run(onChanged);
        AddEntry(new Entry(() => collection.CollectionChanged += Handler, () => collection.CollectionChanged -= Handler, onChanged, null));
    }

    /// <summary>
    /// Runs <paramref name="onChanged"/> whenever one of <paramref name="properties"/> changes (not immediately; also
    /// when the scope is resumed).
    /// </summary>
    public void Watch(INotifyPropertyChanged model, Action onChanged, params string[] properties)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(onChanged);
        if (_disposed)
        {
            return;
        }

        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (properties.Length == 0 || string.IsNullOrEmpty(e.PropertyName) || properties.Contains(e.PropertyName))
            {
                Run(onChanged);
            }
        }

        AddEntry(new Entry(() => model.PropertyChanged += Handler, () => model.PropertyChanged -= Handler, onChanged, null));
    }

    /// <summary>Runs <paramref name="onChanged"/> whenever the collection changes (not immediately; also when the scope is resumed).</summary>
    public void WatchCollection(INotifyCollectionChanged collection, Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(onChanged);
        if (_disposed)
        {
            return;
        }

        void Handler(object? sender, NotifyCollectionChangedEventArgs e) => Run(onChanged);
        AddEntry(new Entry(() => collection.CollectionChanged += Handler, () => collection.CollectionChanged -= Handler, onChanged, null));
    }

    /// <summary>
    /// Asks the collection mirror that created the element of this scope (see <see cref="Items{TModel, TElement}"/>) to
    /// recreate it, e.g. when a model change requires another element type. No-op for other scopes.
    /// </summary>
    public void RequestRecreate()
    {
        if (!_disposed)
        {
            _recreate?.Invoke();
        }
    }

    private Action? _recreate;

    internal void SetRecreateHandler(Action? handler) => _recreate = handler;

    /// <summary>
    /// Mirrors <paramref name="source"/> into <paramref name="target"/> incrementally: elements of models that stay in the
    /// collection are kept (with their bindings), new models get an element created by <paramref name="create"/> in a
    /// dedicated child scope, and elements of removed models are removed, their scope disposed and their linked copies
    /// unlinked. Elements of <paramref name="target"/> that were not generated (XAML items) are left untouched.
    /// </summary>
    /// <param name="source">Model collection.</param>
    /// <param name="target">Element collection.</param>
    /// <param name="create">Creates the element of a model (may return <c>null</c> for unsupported models).</param>
    /// <param name="removeOnDispose">Removes the generated elements from <paramref name="target"/> when the scope is disposed.</param>
    /// <param name="changing">Called before the target is updated.</param>
    /// <param name="changed">Called after the target was updated.</param>
    /// <returns>The mirror (exposes the generated elements).</returns>
    public RibbonCollectionMirror<TModel, TElement> Items<TModel, TElement>(
        ObservableCollection<TModel> source,
        IList<TElement> target,
        Func<TModel, RibbonModelBindings, TElement?> create,
        bool removeOnDispose = false,
        Action? changing = null,
        Action? changed = null)
        where TModel : class
        where TElement : class
    {
        var mirror = new RibbonCollectionMirror<TModel, TElement>(this, source, target, create, removeOnDispose, changing, changed);
        if (_disposed)
        {
            return mirror;
        }

        mirror.Reconcile();
        void Handler(object? sender, NotifyCollectionChangedEventArgs e) => Run(mirror.Reconcile);

        // The per-element scopes are children of this scope, so they are suspended / resumed with it.
        AddEntry(new Entry(() => source.CollectionChanged += Handler, () => source.CollectionChanged -= Handler, mirror.Reconcile, mirror.Dispose));
        return mirror;
    }

    /// <summary>Adds a custom cleanup action.</summary>
    public void Add(Action dispose)
    {
        ArgumentNullException.ThrowIfNull(dispose);
        if (_disposed)
        {
            dispose();
            return;
        }

        _entries.Add(new Entry(null, null, null, dispose) { Attached = true });
    }

    /// <summary>
    /// Adds a child scope suspended, resumed and disposed with this one. Disposing the child removes it from this scope.
    /// </summary>
    public RibbonModelBindings Child()
    {
        var child = new RibbonModelBindings { _parent = this };
        if (_disposed)
        {
            child._disposed = true;
            return child;
        }

        // A child created while this scope is suspended starts suspended and is resumed with it.
        var suspended = IsSuspended;
        child._suspended = suspended;
        var entry = new Entry(child.Resume, child.Suspend, null, child.Dispose) { Attached = !suspended };
        child._detachFromParent = () => _entries.Remove(entry);
        _entries.Add(entry);
        return child;
    }

    private Action? _detachFromParent;

    /// <summary>Detaches every model subscription (elements keep their current values).</summary>
    public void Suspend()
    {
        if (_disposed || _suspended)
        {
            return;
        }

        _suspended = true;
        foreach (var entry in _entries.ToArray())
        {
            entry.Detach();
        }
    }

    /// <summary>Re-attaches the model subscriptions suspended by <see cref="Suspend"/> and re-applies the model values.</summary>
    public void Resume()
    {
        if (_disposed || !_suspended)
        {
            return;
        }

        _suspended = false;
        foreach (var entry in _entries.ToArray())
        {
            if (!_disposed && _entries.Contains(entry))
            {
                entry.Attach();
            }
        }
    }

    private void AddEntry(Entry entry)
    {
        _entries.Add(entry);
        if (!IsSuspended)
        {
            entry.Subscribe();
        }
    }

    /// <summary>Runs an action on the dispatcher of the owning element (directly when already on its thread).</summary>
    internal void Run(Action action)
    {
        if (_disposed)
        {
            return;
        }

        var dispatcher = Dispatcher;
        if (dispatcher is not null)
        {
            if (dispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                dispatcher.TryEnqueue(() =>
                {
                    if (!_disposed && !IsSuspended)
                    {
                        action();
                    }
                });
            }

            return;
        }

        if (DispatcherQueue.GetForCurrentThread() is null && App is { } fallback)
        {
            fallback.TryEnqueue(() =>
            {
                if (!_disposed && !IsSuspended)
                {
                    action();
                }
            });
            return;
        }

        action();
    }

    /// <summary>
    /// Places <paramref name="elements"/> contiguously and in order in <paramref name="target"/>, starting where the first
    /// of them already is (or at the end). Other elements of the target are kept.
    /// </summary>
    internal static void ArrangeContiguous<T>(IList<T> target, IReadOnlyList<T> elements)
        where T : class
    {
        if (elements.Count == 0)
        {
            return;
        }

        var start = elements.Select(e => target.IndexOf(e)).Where(i => i >= 0).DefaultIfEmpty(target.Count).Min();
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var desired = Math.Min(start + i, target.Count);
            var current = target.IndexOf(element);
            if (current == desired)
            {
                continue;
            }

            if (current < 0)
            {
                target.Insert(desired, element);
            }
            else if (target is ObservableCollection<T> observable)
            {
                observable.Move(current, Math.Min(desired, target.Count - 1));
            }
            else
            {
                target.RemoveAt(current);
                target.Insert(Math.Min(desired, target.Count), element);
            }
        }
    }

    private static DispatcherQueue? App { get; set; }

    /// <summary>
    /// Sets the fallback dispatcher used to marshal model changes raised on background threads by scopes without a
    /// <see cref="Dispatcher"/> (prefer the per-scope dispatcher; the ribbon sets it to its own dispatcher).
    /// </summary>
    public static void SetDispatcher(DispatcherQueue? dispatcher) => App = dispatcher;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var entries = _entries.ToArray();
        _entries.Clear();
        foreach (var entry in entries)
        {
            entry.Dispose();
        }

        _detachFromParent?.Invoke();
        _detachFromParent = null;
        _parent = null;
    }

    private sealed class Entry(Action? subscribe, Action? unsubscribe, Action? refresh, Action? dispose)
    {
        public bool Attached { get; set; }

        public void Subscribe()
        {
            if (!Attached)
            {
                Attached = true;
                subscribe?.Invoke();
            }
        }

        public void Attach()
        {
            if (Attached && subscribe is not null)
            {
                return;
            }

            Attached = true;
            subscribe?.Invoke();
            refresh?.Invoke();
        }

        public void Detach()
        {
            if (unsubscribe is not null && Attached)
            {
                Attached = false;
                unsubscribe();
            }
        }

        public void Dispose()
        {
            if (unsubscribe is not null && Attached)
            {
                Attached = false;
                unsubscribe();
            }

            dispose?.Invoke();
        }
    }
}

/// <summary>Incremental mirror of a model collection into an element collection (see <see cref="RibbonModelBindings.Items{TModel, TElement}"/>).</summary>
/// <typeparam name="TModel">Model type.</typeparam>
/// <typeparam name="TElement">Element type.</typeparam>
public sealed class RibbonCollectionMirror<TModel, TElement>
    where TModel : class
    where TElement : class
{
    private readonly RibbonModelBindings _owner;
    private readonly ObservableCollection<TModel> _source;
    private readonly IList<TElement> _target;
    private readonly Func<TModel, RibbonModelBindings, TElement?> _create;
    private readonly bool _removeOnDispose;
    private readonly Action? _changing;
    private readonly Action? _changed;
    private readonly List<(TModel Model, TElement? Element, RibbonModelBindings Scope)> _entries = [];
    private bool _disposed;
    private bool _reconciling;

    internal RibbonCollectionMirror(RibbonModelBindings owner, ObservableCollection<TModel> source, IList<TElement> target, Func<TModel, RibbonModelBindings, TElement?> create, bool removeOnDispose, Action? changing, Action? changed)
    {
        _owner = owner;
        _source = source;
        _target = target;
        _create = create;
        _removeOnDispose = removeOnDispose;
        _changing = changing;
        _changed = changed;
    }

    /// <summary>Generated elements, in model order.</summary>
    public IEnumerable<TElement> Elements => _entries.Where(e => e.Element is not null).Select(e => e.Element!).ToArray();

    /// <summary>Returns the element generated for a model.</summary>
    public TElement? FindElement(TModel model) => _entries.FirstOrDefault(e => ReferenceEquals(e.Model, model)).Element;

    /// <summary>Re-synchronizes the target with the source (keeps the elements of models that are still present).</summary>
    public void Reconcile()
    {
        if (_disposed || _reconciling)
        {
            return;
        }

        _reconciling = true;
        try
        {
            _changing?.Invoke();
            var previous = _entries.ToList();
            var next = new List<(TModel Model, TElement? Element, RibbonModelBindings Scope)>(_source.Count);
            foreach (var model in _source.ToArray())
            {
                var index = previous.FindIndex(e => ReferenceEquals(e.Model, model));
                if (index >= 0)
                {
                    next.Add(previous[index]);
                    previous.RemoveAt(index);
                    continue;
                }

                next.Add(Create(model));
            }

            foreach (var removed in previous)
            {
                Discard(removed, true);
            }

            _entries.Clear();
            _entries.AddRange(next);
            Arrange();
            _changed?.Invoke();
        }
        finally
        {
            _reconciling = false;
        }
    }

    private (TModel Model, TElement? Element, RibbonModelBindings Scope) Create(TModel model)
    {
        var scope = _owner.Child();
        scope.SetRecreateHandler(() => _owner.Run(() => Recreate(model, scope)));
        try
        {
            return (model, _create(model, scope), scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private void Recreate(TModel model, RibbonModelBindings scope)
    {
        var index = _entries.FindIndex(e => ReferenceEquals(e.Model, model) && ReferenceEquals(e.Scope, scope));
        if (_disposed || _reconciling || index < 0)
        {
            return;
        }

        _reconciling = true;
        try
        {
            _changing?.Invoke();
            var old = _entries[index];
            var position = old.Element is null ? -1 : _target.IndexOf(old.Element);
            Discard(old, true);
            var created = Create(model);
            _entries[index] = created;
            if (created.Element is not null && position >= 0)
            {
                _target.Insert(Math.Min(position, _target.Count), created.Element);
            }

            Arrange();
            _changed?.Invoke();
        }
        finally
        {
            _reconciling = false;
        }
    }

    private void Arrange() => RibbonModelBindings.ArrangeContiguous(_target, _entries.Where(e => e.Element is not null).Select(e => e.Element!).ToList());

    private void Discard((TModel Model, TElement? Element, RibbonModelBindings Scope) entry, bool remove)
    {
        entry.Scope.Dispose();
        if (entry.Element is null)
        {
            return;
        }

        if (remove)
        {
            _target.Remove(entry.Element);
        }

        if (entry.Element is DependencyObject element)
        {
            RibbonItemHelper.UnlinkTree(element);
        }
    }

    internal void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var entry in _entries.ToArray())
        {
            Discard(entry, _removeOnDispose);
        }

        _entries.Clear();
    }
}
