using System.Collections.ObjectModel;

namespace RibbonSpace.Model;

/// <summary>Base class of every ribbon model node (tab, group, item, menu item, backstage item).</summary>
public abstract class RibbonNodeModel : ObservableObject
{
    private string _id;
    private string? _label;
    private RibbonIcon? _icon;
    private RibbonIcon? _largeIcon;
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private string? _keyTip;
    private RibbonScreenTip? _screenTip;
    private string? _description;
    private int _order;
    private object? _tag;
    private string? _automationId;
    private RibbonMergeAction _mergeAction;

    /// <summary>Creates a node.</summary>
    protected RibbonNodeModel(string? id = null, string? label = null)
    {
        _id = id ?? Guid.NewGuid().ToString("N");
        _label = label;
    }

    /// <summary>Stable identifier used for persistence, merging, KeyTips, QAT and automation.</summary>
    public string Id { get => _id; set => SetProperty(ref _id, value); }

    /// <summary>Display label.</summary>
    public string? Label { get => _label; set => SetProperty(ref _label, value); }

    /// <summary>Icon used for small / medium presentations.</summary>
    public RibbonIcon? Icon { get => _icon; set => SetProperty(ref _icon, value); }

    /// <summary>Icon used for large presentations; falls back to <see cref="Icon"/>.</summary>
    public RibbonIcon? LargeIcon { get => _largeIcon; set => SetProperty(ref _largeIcon, value); }

    /// <summary>Visibility of the node.</summary>
    public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

    /// <summary>Enabled state of the node (commands also contribute via CanExecute).</summary>
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

    /// <summary>Explicit KeyTip; generated from the label when <c>null</c>.</summary>
    public string? KeyTip { get => _keyTip; set => SetProperty(ref _keyTip, value); }

    /// <summary>Rich tooltip.</summary>
    public RibbonScreenTip? ScreenTip { get => _screenTip; set => SetProperty(ref _screenTip, value); }

    /// <summary>Description used by ScreenTips and command search.</summary>
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    /// <summary>Sort order used when merging models.</summary>
    public int Order { get => _order; set => SetProperty(ref _order, value); }

    /// <summary>Arbitrary user data.</summary>
    public object? Tag { get => _tag; set => SetProperty(ref _tag, value); }

    /// <summary>Automation id; defaults to <see cref="Id"/>.</summary>
    public string? AutomationId { get => _automationId; set => SetProperty(ref _automationId, value); }

    /// <summary>Behaviour when this node is merged into another model.</summary>
    public RibbonMergeAction MergeAction { get => _mergeAction; set => SetProperty(ref _mergeAction, value); }

    /// <summary>Additional search keywords / synonyms.</summary>
    public ObservableCollection<string> Keywords { get; } = [];

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name}({Id}, {Label})";
}
