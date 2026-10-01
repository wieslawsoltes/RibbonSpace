using System.Collections.Concurrent;
using System.Windows.Input;
using RibbonSpace.Model;

namespace RibbonSpace.Commands;

/// <summary>Metadata of a command registered in a <see cref="RibbonCommandCatalog"/>.</summary>
public sealed class RibbonCommandDescriptor : ObservableObject
{
    private string _label;
    private RibbonIcon? _icon;
    private string? _description;
    private string? _shortcut;
    private string? _category;
    private bool _isEnabled = true;
    private bool? _isChecked;
    private ICommand? _command;

    /// <summary>Creates a descriptor.</summary>
    public RibbonCommandDescriptor(string id, string label, ICommand? command = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label;
        _command = command;
    }

    /// <summary>Unique command id (e.g. "home.paste" or "PASTE").</summary>
    public string Id { get; }

    /// <summary>Display label.</summary>
    public string Label { get => _label; set => SetProperty(ref _label, value); }

    /// <summary>Icon.</summary>
    public RibbonIcon? Icon { get => _icon; set => SetProperty(ref _icon, value); }

    /// <summary>Description used for ScreenTips and command search.</summary>
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    /// <summary>Shortcut, e.g. "Ctrl+B".</summary>
    public string? Shortcut { get => _shortcut; set => SetProperty(ref _shortcut, value); }

    /// <summary>Category shown in customization UI ("File", "Home", "Popular Commands", ...).</summary>
    public string? Category { get => _category; set => SetProperty(ref _category, value); }

    /// <summary>Search keywords / aliases.</summary>
    public IList<string> Keywords { get; } = new List<string>();

    /// <summary>Enabled state pushed to every ribbon item bound to this id.</summary>
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

    /// <summary>Checked state pushed to every toggle item bound to this id (<c>null</c> = not a toggle).</summary>
    public bool? IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }

    /// <summary>The command implementation.</summary>
    public ICommand? Command { get => _command; set => SetProperty(ref _command, value); }
}

/// <summary>
/// Central, id-based registry of commands shared by ribbons, toolbars, menus, search and shortcuts.
/// Items reference commands through <c>CommandId</c>; state (enabled / checked) is pushed by id.
/// </summary>
public class RibbonCommandCatalog
{
    private readonly ConcurrentDictionary<string, RibbonCommandDescriptor> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _order = [];
    private readonly Lock _gate = new();

    /// <summary>Raised when a command is registered or removed.</summary>
    public event EventHandler<string>? CommandsChanged;

    /// <summary>Raised when any registered descriptor's state changes (id, property name).</summary>
    public event EventHandler<RibbonCommandStateChangedEventArgs>? CommandStateChanged;

    /// <summary>Raised when a command is executed through <see cref="Execute"/>.</summary>
    public event EventHandler<RibbonCommandExecutedEventArgs>? Executed;

    /// <summary>All registered commands, in registration order.</summary>
    public IReadOnlyCollection<RibbonCommandDescriptor> Commands
    {
        get
        {
            lock (_gate)
            {
                return _order.Select(id => _commands[id]).ToArray();
            }
        }
    }

    /// <summary>Registers (or replaces) a command. A replaced command keeps its position.</summary>
    public RibbonCommandDescriptor Register(RibbonCommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        lock (_gate)
        {
            if (_commands.TryGetValue(descriptor.Id, out var previous))
            {
                previous.PropertyChanged -= OnDescriptorChanged;
            }
            else
            {
                _order.Add(descriptor.Id);
            }

            _commands[descriptor.Id] = descriptor;
            descriptor.PropertyChanged += OnDescriptorChanged;
        }

        CommandsChanged?.Invoke(this, descriptor.Id);
        return descriptor;
    }

    /// <summary>Registers a command from delegates.</summary>
    public RibbonCommandDescriptor Register(string id, string label, Action<object?> execute, Func<object?, bool>? canExecute = null, RibbonIcon? icon = null, string? shortcut = null, string? description = null, string? category = null)
        => Register(new RibbonCommandDescriptor(id, label, new RibbonRelayCommand(execute, canExecute))
        {
            Icon = icon,
            Shortcut = shortcut,
            Description = description,
            Category = category,
        });

    /// <summary>Registers an existing <see cref="ICommand"/>.</summary>
    public RibbonCommandDescriptor Register(string id, string label, ICommand command, RibbonIcon? icon = null, string? shortcut = null, string? description = null, string? category = null)
        => Register(new RibbonCommandDescriptor(id, label, command)
        {
            Icon = icon,
            Shortcut = shortcut,
            Description = description,
            Category = category,
        });

    /// <summary>Removes a command.</summary>
    public bool Unregister(string id)
    {
        RibbonCommandDescriptor? removed;
        lock (_gate)
        {
            if (!_commands.TryRemove(id, out removed))
            {
                return false;
            }

            _order.RemoveAll(o => string.Equals(o, id, StringComparison.OrdinalIgnoreCase));
            removed.PropertyChanged -= OnDescriptorChanged;
        }

        CommandsChanged?.Invoke(this, id);
        return true;
    }

    /// <summary>Finds a command.</summary>
    public RibbonCommandDescriptor? Find(string? id) => id is not null && _commands.TryGetValue(id, out var d) ? d : null;

    /// <summary>Resolves the <see cref="ICommand"/> for an id.</summary>
    public bool TryResolve(string? id, out ICommand? command)
    {
        command = Find(id)?.Command;
        return command is not null;
    }

    /// <summary>Returns whether a command can execute (unknown ids return false).</summary>
    public bool CanExecute(string id, object? parameter = null)
    {
        var d = Find(id);
        return d is { IsEnabled: true, Command: not null } && d.Command.CanExecute(parameter);
    }

    /// <summary>Executes a command by id. Returns false when unknown or disabled.</summary>
    public bool Execute(string id, object? parameter = null)
    {
        if (!CanExecute(id, parameter))
        {
            return false;
        }

        Find(id)!.Command!.Execute(parameter);
        Executed?.Invoke(this, new RibbonCommandExecutedEventArgs(id, parameter));
        return true;
    }

    /// <summary>Sets the enabled state of a command.</summary>
    public void SetEnabled(string id, bool enabled)
    {
        if (Find(id) is { } d) d.IsEnabled = enabled;
    }

    /// <summary>Sets the checked state of a command.</summary>
    public void SetChecked(string id, bool? isChecked)
    {
        if (Find(id) is { } d) d.IsChecked = isChecked;
    }

    private void OnDescriptorChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is RibbonCommandDescriptor d)
        {
            CommandStateChanged?.Invoke(this, new RibbonCommandStateChangedEventArgs(d.Id, e.PropertyName));
        }
    }
}

/// <summary>Arguments of <see cref="RibbonCommandCatalog.CommandStateChanged"/>.</summary>
public sealed class RibbonCommandStateChangedEventArgs(string id, string? propertyName) : EventArgs
{
    /// <summary>Command id.</summary>
    public string Id { get; } = id;

    /// <summary>Changed property.</summary>
    public string? PropertyName { get; } = propertyName;
}

/// <summary>Arguments of <see cref="RibbonCommandCatalog.Executed"/>.</summary>
public sealed class RibbonCommandExecutedEventArgs(string id, object? parameter) : EventArgs
{
    /// <summary>Command id.</summary>
    public string Id { get; } = id;

    /// <summary>Command parameter.</summary>
    public object? Parameter { get; } = parameter;
}
