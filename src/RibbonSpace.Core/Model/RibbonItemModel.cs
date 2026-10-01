using System.Windows.Input;
using RibbonSpace.Layout;

namespace RibbonSpace.Model;

/// <summary>Base class of items placed in ribbon groups, toolbars, the Quick Access Toolbar or menus.</summary>
public abstract class RibbonItemModel : RibbonNodeModel
{
    private RibbonItemSize _size = RibbonItemSize.Medium;
    private RibbonSizeDefinition? _sizeDefinition;
    private RibbonSimplifiedVisibility _simplifiedVisibility;
    private bool? _showLabelInSimplified;
    private string? _commandId;
    private ICommand? _command;
    private object? _commandParameter;
    private string? _shortcut;
    private bool _canAddToQuickAccess = true;
    private bool _showLabel = true;

    /// <summary>Creates an item.</summary>
    protected RibbonItemModel(string? id, string? label)
        : base(id, label)
    {
    }

    /// <summary>Preferred (largest) size. Also selects the default <see cref="SizeDefinition"/>.</summary>
    public RibbonItemSize Size
    {
        get => _size;
        set
        {
            if (SetProperty(ref _size, value) && _sizeDefinition is null)
            {
                OnPropertyChanged(nameof(EffectiveSizeDefinition));
            }
        }
    }

    /// <summary>Explicit adaptive size definition; <c>null</c> derives it from <see cref="Size"/>.</summary>
    public RibbonSizeDefinition? SizeDefinition
    {
        get => _sizeDefinition;
        set
        {
            if (SetProperty(ref _sizeDefinition, value))
            {
                OnPropertyChanged(nameof(EffectiveSizeDefinition));
            }
        }
    }

    /// <summary>Effective size definition.</summary>
    public RibbonSizeDefinition EffectiveSizeDefinition => SizeDefinition ?? RibbonSizeDefinition.ForPreferredSize(Size);

    /// <summary>Simplified ribbon behaviour.</summary>
    public RibbonSimplifiedVisibility SimplifiedVisibility { get => _simplifiedVisibility; set => SetProperty(ref _simplifiedVisibility, value); }

    /// <summary>Shows the label next to the icon in simplified mode (default: true for items whose preferred size is Large).</summary>
    public bool? ShowLabelInSimplified { get => _showLabelInSimplified; set => SetProperty(ref _showLabelInSimplified, value); }

    /// <summary>Shows the label in Medium / Large sizes (set false for icon-only toolbar buttons).</summary>
    public bool ShowLabel { get => _showLabel; set => SetProperty(ref _showLabel, value); }

    /// <summary>Id of a command registered in the ribbon command catalog.</summary>
    public string? CommandId { get => _commandId; set => SetProperty(ref _commandId, value); }

    /// <summary>Command executed when the item is invoked.</summary>
    public ICommand? Command { get => _command; set => SetProperty(ref _command, value); }

    /// <summary>Parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter { get => _commandParameter; set => SetProperty(ref _commandParameter, value); }

    /// <summary>Displayed keyboard shortcut (e.g. "Ctrl+B"); also registered by shortcut managers.</summary>
    public string? Shortcut { get => _shortcut; set => SetProperty(ref _shortcut, value); }

    /// <summary>Allows "Add to Quick Access Toolbar" for this item.</summary>
    public bool CanAddToQuickAccess { get => _canAddToQuickAccess; set => SetProperty(ref _canAddToQuickAccess, value); }
}

/// <summary>Push button.</summary>
public class RibbonButtonModel : RibbonItemModel
{
    /// <summary>Creates a button.</summary>
    public RibbonButtonModel(string? id = null, string? label = null, RibbonIcon? icon = null, ICommand? command = null, RibbonItemSize size = RibbonItemSize.Medium)
        : base(id, label)
    {
        Icon = icon;
        Command = command;
        Size = size;
    }
}

/// <summary>Two-state toggle button; toggles sharing a <see cref="GroupName"/> behave like radio buttons.</summary>
public class RibbonToggleButtonModel : RibbonButtonModel
{
    private bool _isChecked;
    private string? _groupName;

    /// <summary>Creates a toggle button.</summary>
    public RibbonToggleButtonModel(string? id = null, string? label = null, RibbonIcon? icon = null, ICommand? command = null, RibbonItemSize size = RibbonItemSize.Small)
        : base(id, label, icon, command, size)
    {
    }

    /// <summary>Checked state.</summary>
    public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }

    /// <summary>Mutual exclusion group (e.g. "align" for Left / Center / Right).</summary>
    public string? GroupName { get => _groupName; set => SetProperty(ref _groupName, value); }
}

/// <summary>Button with a drop-down menu (no primary action).</summary>
public class RibbonDropDownButtonModel : RibbonItemModel
{
    private object? _dropDownContent;

    /// <summary>Creates a drop-down button.</summary>
    public RibbonDropDownButtonModel(string? id = null, string? label = null, RibbonIcon? icon = null, RibbonItemSize size = RibbonItemSize.Medium)
        : base(id, label)
    {
        Icon = icon;
        Size = size;
    }

    /// <summary>Menu entries (<see cref="RibbonMenuItemModel"/>, separators, headers, galleries, color palettes, grid pickers...).</summary>
    public System.Collections.ObjectModel.ObservableCollection<RibbonNodeModel> MenuItems { get; } = [];

    /// <summary>Custom drop-down content (view model or element) shown instead of / above the menu.</summary>
    public object? DropDownContent { get => _dropDownContent; set => SetProperty(ref _dropDownContent, value); }
}

/// <summary>Button with a primary action and a drop-down part (Office Paste, Bullets, Shapes).</summary>
public class RibbonSplitButtonModel : RibbonDropDownButtonModel
{
    private bool _isCheckable;
    private bool _isChecked;
    private bool _followLastChoice;

    /// <summary>Creates a split button.</summary>
    public RibbonSplitButtonModel(string? id = null, string? label = null, RibbonIcon? icon = null, ICommand? command = null, RibbonItemSize size = RibbonItemSize.Large)
        : base(id, label, icon, size)
    {
        Command = command;
    }

    /// <summary>The primary part toggles (Bullets / Numbering).</summary>
    public bool IsCheckable { get => _isCheckable; set => SetProperty(ref _isCheckable, value); }

    /// <summary>Checked state when <see cref="IsCheckable"/>.</summary>
    public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }

    /// <summary>When a menu item is chosen, the primary part adopts its icon, label and command (tool split buttons).</summary>
    public bool FollowLastChoice { get => _followLastChoice; set => SetProperty(ref _followLastChoice, value); }
}

/// <summary>Check box.</summary>
public class RibbonCheckBoxModel : RibbonItemModel
{
    private bool? _isChecked = false;
    private bool _isThreeState;

    /// <summary>Creates a check box.</summary>
    public RibbonCheckBoxModel(string? id = null, string? label = null, bool isChecked = false)
        : base(id, label)
    {
        _isChecked = isChecked;
        Size = RibbonItemSize.Medium;
    }

    /// <summary>Checked state.</summary>
    public bool? IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }

    /// <summary>Supports the indeterminate state.</summary>
    public bool IsThreeState { get => _isThreeState; set => SetProperty(ref _isThreeState, value); }
}

/// <summary>Editable or read-only combo box (font family, font size, number format, zoom...).</summary>
public class RibbonComboBoxModel : RibbonItemModel
{
    private object? _selectedItem;
    private string? _text;
    private bool _isEditable;
    private double _inputWidth = 120;
    private string? _placeholder;
    private string? _displayMemberPath;
    private bool _previewFontFamily;
    private double _maxDropDownHeight = 420;

    /// <summary>Creates a combo box.</summary>
    public RibbonComboBoxModel(string? id = null, string? label = null, IEnumerable<object>? items = null)
        : base(id, label)
    {
        Size = RibbonItemSize.Small;
        if (items is not null)
        {
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
    }

    /// <summary>Available values.</summary>
    public System.Collections.ObjectModel.ObservableCollection<object> Items { get; } = [];

    /// <summary>Selected value.</summary>
    public object? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

    /// <summary>Text of the editable box (committed on Enter / focus loss / pick).</summary>
    public string? Text { get => _text; set => SetProperty(ref _text, value); }

    /// <summary>Allows typing arbitrary values (font size "11.5", zoom "137%").</summary>
    public bool IsEditable { get => _isEditable; set => SetProperty(ref _isEditable, value); }

    /// <summary>Width of the input box.</summary>
    public double InputWidth { get => _inputWidth; set => SetProperty(ref _inputWidth, value); }

    /// <summary>Placeholder shown for empty / mixed values (e.g. "*Varies*").</summary>
    public string? Placeholder { get => _placeholder; set => SetProperty(ref _placeholder, value); }

    /// <summary>Property path used to display items.</summary>
    public string? DisplayMemberPath { get => _displayMemberPath; set => SetProperty(ref _displayMemberPath, value); }

    /// <summary>Renders each entry in its own font (font family pickers).</summary>
    public bool PreviewFontFamily { get => _previewFontFamily; set => SetProperty(ref _previewFontFamily, value); }

    /// <summary>Maximum drop-down height.</summary>
    public double MaxDropDownHeight { get => _maxDropDownHeight; set => SetProperty(ref _maxDropDownHeight, value); }
}

/// <summary>Numeric spinner with units (indent, spacing, width, rotation).</summary>
public class RibbonSpinnerModel : RibbonItemModel
{
    private double _value;
    private double _minimum;
    private double _maximum = 100;
    private double _increment = 1;
    private string _format = "0.##";
    private string? _unit;
    private double _inputWidth = 64;

    /// <summary>Creates a spinner.</summary>
    public RibbonSpinnerModel(string? id = null, string? label = null, double value = 0)
        : base(id, label)
    {
        _value = value;
        Size = RibbonItemSize.Medium;
    }

    /// <summary>Current value.</summary>
    public double Value { get => _value; set => SetProperty(ref _value, value); }

    /// <summary>Minimum value.</summary>
    public double Minimum { get => _minimum; set => SetProperty(ref _minimum, value); }

    /// <summary>Maximum value.</summary>
    public double Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }

    /// <summary>Step used by the arrows, arrow keys and mouse wheel.</summary>
    public double Increment { get => _increment; set => SetProperty(ref _increment, value); }

    /// <summary>.NET numeric format string.</summary>
    public string Format { get => _format; set => SetProperty(ref _format, value); }

    /// <summary>Unit suffix ("pt", "cm", "\"", "°", "%").</summary>
    public string? Unit { get => _unit; set => SetProperty(ref _unit, value); }

    /// <summary>Width of the input box.</summary>
    public double InputWidth { get => _inputWidth; set => SetProperty(ref _inputWidth, value); }
}

/// <summary>Single-line text input.</summary>
public class RibbonTextBoxModel : RibbonItemModel
{
    private string? _text;
    private string? _placeholder;
    private double _inputWidth = 140;

    /// <summary>Creates a text box.</summary>
    public RibbonTextBoxModel(string? id = null, string? label = null)
        : base(id, label)
    {
        Size = RibbonItemSize.Medium;
    }

    /// <summary>Text (Enter executes <see cref="RibbonItemModel.Command"/> with the text).</summary>
    public string? Text { get => _text; set => SetProperty(ref _text, value); }

    /// <summary>Placeholder text.</summary>
    public string? Placeholder { get => _placeholder; set => SetProperty(ref _placeholder, value); }

    /// <summary>Width of the input.</summary>
    public double InputWidth { get => _inputWidth; set => SetProperty(ref _inputWidth, value); }
}

/// <summary>Slider (zoom, opacity, brush size).</summary>
public class RibbonSliderModel : RibbonItemModel
{
    private double _value;
    private double _minimum;
    private double _maximum = 100;
    private double _stepFrequency = 1;
    private double _sliderWidth = 120;

    /// <summary>Creates a slider.</summary>
    public RibbonSliderModel(string? id = null, string? label = null)
        : base(id, label)
    {
        Size = RibbonItemSize.Medium;
    }

    /// <summary>Value.</summary>
    public double Value { get => _value; set => SetProperty(ref _value, value); }

    /// <summary>Minimum.</summary>
    public double Minimum { get => _minimum; set => SetProperty(ref _minimum, value); }

    /// <summary>Maximum.</summary>
    public double Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }

    /// <summary>Step.</summary>
    public double StepFrequency { get => _stepFrequency; set => SetProperty(ref _stepFrequency, value); }

    /// <summary>Width of the track.</summary>
    public double SliderWidth { get => _sliderWidth; set => SetProperty(ref _sliderWidth, value); }
}

/// <summary>Static text label.</summary>
public class RibbonLabelModel : RibbonItemModel
{
    /// <summary>Creates a label.</summary>
    public RibbonLabelModel(string? id = null, string? label = null)
        : base(id, label)
    {
        CanAddToQuickAccess = false;
    }
}

/// <summary>Vertical separator between items.</summary>
public class RibbonSeparatorModel : RibbonItemModel
{
    /// <summary>Creates a separator.</summary>
    public RibbonSeparatorModel()
        : base(null, null)
    {
        CanAddToQuickAccess = false;
    }
}

/// <summary>Joined row of small buttons (Bold / Italic / Underline, alignment buttons).</summary>
public class RibbonButtonGroupModel : RibbonItemModel
{
    /// <summary>Creates a button group.</summary>
    public RibbonButtonGroupModel(string? id = null, params RibbonItemModel[] items)
        : base(id, null)
    {
        Size = RibbonItemSize.Small;
        CanAddToQuickAccess = false;
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    /// <summary>Child items.</summary>
    public System.Collections.ObjectModel.ObservableCollection<RibbonItemModel> Items { get; } = [];
}

/// <summary>Horizontal row container used by groups with <see cref="RibbonGroupItemsLayout.Rows"/>.</summary>
public class RibbonRowModel : RibbonButtonGroupModel
{
    /// <summary>Creates a row.</summary>
    public RibbonRowModel(string? id = null, params RibbonItemModel[] items)
        : base(id, items)
    {
    }
}

/// <summary>Grid size picked from a <see cref="RibbonGridPickerModel"/>.</summary>
/// <param name="Rows">Rows.</param>
/// <param name="Columns">Columns.</param>
public readonly record struct RibbonGridSize(int Rows, int Columns)
{
    /// <inheritdoc />
    public override string ToString() => $"{Columns}x{Rows}";
}

/// <summary>Hover grid picker (Insert Table).</summary>
public class RibbonGridPickerModel : RibbonItemModel
{
    private int _rows = 8;
    private int _columns = 10;

    /// <summary>Creates a grid picker. The command receives a <see cref="RibbonGridSize"/>.</summary>
    public RibbonGridPickerModel(string? id = null, string? label = null, ICommand? command = null)
        : base(id, label)
    {
        Command = command;
    }

    /// <summary>Number of rows.</summary>
    public int Rows { get => _rows; set => SetProperty(ref _rows, Math.Max(1, value)); }

    /// <summary>Number of columns.</summary>
    public int Columns { get => _columns; set => SetProperty(ref _columns, Math.Max(1, value)); }
}

/// <summary>Segment of a <see cref="RibbonSegmentedModel"/>.</summary>
public class RibbonSegmentModel : RibbonNodeModel
{
    private object? _value;

    /// <summary>Creates a segment.</summary>
    public RibbonSegmentModel(string? id = null, string? label = null, RibbonIcon? icon = null, object? value = null)
        : base(id, label)
    {
        Icon = icon;
        _value = value ?? id;
    }

    /// <summary>Value represented by the segment.</summary>
    public object? Value { get => _value; set => SetProperty(ref _value, value); }
}

/// <summary>Segmented control / workspace switcher (single selection).</summary>
public class RibbonSegmentedModel : RibbonItemModel
{
    private RibbonSegmentModel? _selectedSegment;

    /// <summary>Creates a segmented control.</summary>
    public RibbonSegmentedModel(string? id = null, string? label = null)
        : base(id, label)
    {
        CanAddToQuickAccess = false;
    }

    /// <summary>Segments.</summary>
    public System.Collections.ObjectModel.ObservableCollection<RibbonSegmentModel> Segments { get; } = [];

    /// <summary>Selected segment; the command is executed with its value when it changes.</summary>
    public RibbonSegmentModel? SelectedSegment { get => _selectedSegment; set => SetProperty(ref _selectedSegment, value); }
}

/// <summary>Arbitrary content hosted in a group (the Uno layer renders it with a template or as an element).</summary>
public class RibbonCustomItemModel : RibbonItemModel
{
    private object? _content;
    private string? _templateKey;

    /// <summary>Creates a custom item.</summary>
    public RibbonCustomItemModel(string? id = null, object? content = null)
        : base(id, null)
    {
        _content = content;
        CanAddToQuickAccess = false;
    }

    /// <summary>Content (view model or UI element).</summary>
    public object? Content { get => _content; set => SetProperty(ref _content, value); }

    /// <summary>Resource key of a DataTemplate used to render <see cref="Content"/>.</summary>
    public string? TemplateKey { get => _templateKey; set => SetProperty(ref _templateKey, value); }
}
