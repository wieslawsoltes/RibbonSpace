using System.Collections.ObjectModel;
using System.Windows.Input;
using RibbonSpace.Theming;

namespace RibbonSpace.Model;

/// <summary>Ribbon group ("Clipboard", "Font", "Paragraph").</summary>
public class RibbonGroupModel : RibbonNodeModel
{
    private RibbonGroupItemsLayout _itemsLayout;
    private int _rowCount = 3;
    private ICommand? _dialogLauncherCommand;
    private string? _dialogLauncherCommandId;
    private RibbonScreenTip? _dialogLauncherScreenTip;
    private int _reductionOrder;
    private bool _canCollapse = true;
    private RibbonSimplifiedVisibility _simplifiedVisibility;

    /// <summary>Creates a group.</summary>
    public RibbonGroupModel(string? id = null, string? label = null, RibbonIcon? icon = null)
        : base(id, label)
    {
        Icon = icon;
    }

    /// <summary>Items.</summary>
    public ObservableCollection<RibbonItemModel> Items { get; } = [];

    /// <summary>Arrangement of items.</summary>
    public RibbonGroupItemsLayout ItemsLayout { get => _itemsLayout; set => SetProperty(ref _itemsLayout, value); }

    /// <summary>Rows of stacked medium / small items (2 or 3).</summary>
    public int RowCount { get => _rowCount; set => SetProperty(ref _rowCount, Math.Clamp(value, 1, 3)); }

    /// <summary>Command of the dialog launcher (the small arrow in the caption). <c>null</c> hides the launcher.</summary>
    public ICommand? DialogLauncherCommand { get => _dialogLauncherCommand; set => SetProperty(ref _dialogLauncherCommand, value); }

    /// <summary>Catalog id of the dialog launcher command.</summary>
    public string? DialogLauncherCommandId { get => _dialogLauncherCommandId; set => SetProperty(ref _dialogLauncherCommandId, value); }

    /// <summary>ScreenTip of the dialog launcher.</summary>
    public RibbonScreenTip? DialogLauncherScreenTip { get => _dialogLauncherScreenTip; set => SetProperty(ref _dialogLauncherScreenTip, value); }

    /// <summary>Groups with a higher value shrink first when space runs out (ties shrink right-to-left).</summary>
    public int ReductionOrder { get => _reductionOrder; set => SetProperty(ref _reductionOrder, value); }

    /// <summary>Allows the group to collapse into a single drop-down button.</summary>
    public bool CanCollapse { get => _canCollapse; set => SetProperty(ref _canCollapse, value); }

    /// <summary>Simplified ribbon behaviour of the whole group.</summary>
    public RibbonSimplifiedVisibility SimplifiedVisibility { get => _simplifiedVisibility; set => SetProperty(ref _simplifiedVisibility, value); }
}

/// <summary>Ribbon tab ("Home", "Insert").</summary>
public class RibbonTabModel : RibbonNodeModel
{
    private string? _contextualGroupId;

    /// <summary>Creates a tab.</summary>
    public RibbonTabModel(string? id = null, string? label = null)
        : base(id, label)
    {
    }

    /// <summary>Groups.</summary>
    public ObservableCollection<RibbonGroupModel> Groups { get; } = [];

    /// <summary>Id of the <see cref="RibbonContextualGroupModel"/> owning this tab; <c>null</c> for regular tabs.</summary>
    public string? ContextualGroupId
    {
        get => _contextualGroupId;
        set
        {
            if (SetProperty(ref _contextualGroupId, value))
            {
                OnPropertyChanged(nameof(IsContextual));
            }
        }
    }

    /// <summary>True when the tab belongs to a contextual group.</summary>
    public bool IsContextual => ContextualGroupId is not null;
}

/// <summary>Contextual tab group ("Table Tools", "Picture Tools") shown only for a matching selection.</summary>
public class RibbonContextualGroupModel : RibbonNodeModel
{
    private RibbonColor _color = RibbonColor.Parse("#0F6CBD");
    private RibbonContextualActivation _activation = RibbonContextualActivation.None;

    /// <summary>Creates a contextual group (hidden by default).</summary>
    public RibbonContextualGroupModel(string? id = null, string? label = null)
        : base(id, label)
    {
        IsVisible = false;
    }

    /// <summary>Accent color of the tabs.</summary>
    public RibbonColor Color { get => _color; set => SetProperty(ref _color, value); }

    /// <summary>Selection behaviour when shown.</summary>
    public RibbonContextualActivation Activation { get => _activation; set => SetProperty(ref _activation, value); }
}

/// <summary>Navigation entry of the backstage view (File menu).</summary>
public class RibbonBackstageItemModel : RibbonNodeModel
{
    private object? _content;
    private string? _commandId;
    private ICommand? _command;
    private object? _commandParameter;
    private RibbonBackstagePlacement _placement;
    private bool _closesBackstage = true;
    private bool _hasSeparatorBefore;

    /// <summary>Creates a backstage item. Items with <see cref="Content"/> are pages; items with a command are actions.</summary>
    public RibbonBackstageItemModel(string? id = null, string? label = null, RibbonIcon? icon = null, object? content = null)
        : base(id, label)
    {
        Icon = icon;
        _content = content;
    }

    /// <summary>Page content (view model or element).</summary>
    public object? Content { get => _content; set => SetProperty(ref _content, value); }

    /// <summary>Catalog command id for action items (Save, Close).</summary>
    public string? CommandId { get => _commandId; set => SetProperty(ref _commandId, value); }

    /// <summary>Command for action items.</summary>
    public ICommand? Command { get => _command; set => SetProperty(ref _command, value); }

    /// <summary>Command parameter.</summary>
    public object? CommandParameter { get => _commandParameter; set => SetProperty(ref _commandParameter, value); }

    /// <summary>Top list or footer list.</summary>
    public RibbonBackstagePlacement Placement { get => _placement; set => SetProperty(ref _placement, value); }

    /// <summary>Action items close the backstage after executing.</summary>
    public bool ClosesBackstage { get => _closesBackstage; set => SetProperty(ref _closesBackstage, value); }

    /// <summary>Draws a separator above the item.</summary>
    public bool HasSeparatorBefore { get => _hasSeparatorBefore; set => SetProperty(ref _hasSeparatorBefore, value); }
}

/// <summary>Backstage (File) view.</summary>
public class RibbonBackstageModel : ObservableObject
{
    private RibbonBackstageItemModel? _selectedItem;
    private bool _isOpen;
    private string? _title;

    /// <summary>Navigation items.</summary>
    public ObservableCollection<RibbonBackstageItemModel> Items { get; } = [];

    /// <summary>Selected page.</summary>
    public RibbonBackstageItemModel? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

    /// <summary>Open state.</summary>
    public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

    /// <summary>Optional title shown at the top of the navigation pane.</summary>
    public string? Title { get => _title; set => SetProperty(ref _title, value); }
}
