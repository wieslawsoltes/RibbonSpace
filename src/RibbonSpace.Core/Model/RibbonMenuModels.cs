using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RibbonSpace.Model;

/// <summary>Menu entry of drop-down / split buttons, backstage lists and context menus.</summary>
public class RibbonMenuItemModel : RibbonNodeModel
{
    private string? _commandId;
    private ICommand? _command;
    private object? _commandParameter;
    private bool _isCheckable;
    private bool _isChecked;
    private string? _groupName;
    private string? _shortcut;

    /// <summary>Creates a menu item.</summary>
    public RibbonMenuItemModel(string? id = null, string? label = null, RibbonIcon? icon = null, ICommand? command = null)
        : base(id, label)
    {
        Icon = icon;
        _command = command;
    }

    /// <summary>Command id resolved through the ribbon command catalog.</summary>
    public string? CommandId { get => _commandId; set => SetProperty(ref _commandId, value); }

    /// <summary>Command.</summary>
    public ICommand? Command { get => _command; set => SetProperty(ref _command, value); }

    /// <summary>Command parameter.</summary>
    public object? CommandParameter { get => _commandParameter; set => SetProperty(ref _commandParameter, value); }

    /// <summary>Renders as a check (or radio, with <see cref="GroupName"/>) item.</summary>
    public bool IsCheckable { get => _isCheckable; set => SetProperty(ref _isCheckable, value); }

    /// <summary>Checked state.</summary>
    public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }

    /// <summary>Radio group name.</summary>
    public string? GroupName { get => _groupName; set => SetProperty(ref _groupName, value); }

    /// <summary>Shortcut text shown right-aligned.</summary>
    public string? Shortcut { get => _shortcut; set => SetProperty(ref _shortcut, value); }

    /// <summary>Sub menu entries.</summary>
    public ObservableCollection<RibbonNodeModel> Items { get; } = [];
}

/// <summary>Menu separator.</summary>
public sealed class RibbonMenuSeparatorModel : RibbonNodeModel
{
    /// <summary>Creates a separator.</summary>
    public RibbonMenuSeparatorModel()
        : base(null, null)
    {
    }
}

/// <summary>Non-interactive menu section header ("Paste Options:", "Recently Used Shapes").</summary>
public sealed class RibbonMenuHeaderModel : RibbonNodeModel
{
    /// <summary>Creates a header.</summary>
    public RibbonMenuHeaderModel(string label)
        : base(null, label)
    {
    }
}
