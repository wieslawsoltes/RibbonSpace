using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RibbonSpace.Model;

/// <summary>Item of a <see cref="RibbonGalleryModel"/>.</summary>
public class RibbonGalleryItemModel : RibbonNodeModel
{
    private string? _category;
    private object? _content;
    private object? _value;

    /// <summary>Creates a gallery item.</summary>
    public RibbonGalleryItemModel(string? id = null, string? label = null, RibbonIcon? icon = null, string? category = null, object? value = null)
        : base(id, label)
    {
        Icon = icon;
        _category = category;
        _value = value;
    }

    /// <summary>Category used to group items in the expanded gallery.</summary>
    public string? Category { get => _category; set => SetProperty(ref _category, value); }

    /// <summary>Preview content (view model or element), rendered with the gallery item template.</summary>
    public object? Content { get => _content; set => SetProperty(ref _content, value); }

    /// <summary>Value passed to the gallery command.</summary>
    public object? Value { get => _value; set => SetProperty(ref _value, value); }
}

/// <summary>
/// In-ribbon gallery (Styles, Shape Styles, Themes, Transitions, Chart Styles) that can expand into a
/// categorized, filterable popup with footer menu commands. Supports live preview through <see cref="PreviewCommand"/>.
/// </summary>
public class RibbonGalleryModel : RibbonItemModel
{
    private RibbonGalleryItemModel? _selectedItem;
    private ICommand? _previewCommand;
    private int _minColumns = 3;
    private int _maxColumns = 7;
    private int _dropDownColumns = 7;
    private double _itemWidth = 72;
    private double _itemHeight = 60;
    private bool _isFilterEnabled;
    private bool _showLabels = true;
    private int _rows = 1;

    /// <summary>Creates a gallery.</summary>
    public RibbonGalleryModel(string? id = null, string? label = null)
        : base(id, label)
    {
        Size = RibbonItemSize.Large;
    }

    /// <summary>Items.</summary>
    public ObservableCollection<RibbonGalleryItemModel> Items { get; } = [];

    /// <summary>Footer menu entries of the expanded gallery ("Clear Formatting", "Apply Styles...").</summary>
    public ObservableCollection<RibbonNodeModel> MenuItems { get; } = [];

    /// <summary>Selected item; the <see cref="RibbonItemModel.Command"/> receives the item's <c>Value</c> (or the item).</summary>
    public RibbonGalleryItemModel? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

    /// <summary>Live preview command: executed with the hovered item's value, then with <c>null</c> when the pointer leaves.</summary>
    public ICommand? PreviewCommand { get => _previewCommand; set => SetProperty(ref _previewCommand, value); }

    /// <summary>Columns shown in the ribbon in the Medium group state.</summary>
    public int MinColumns { get => _minColumns; set => SetProperty(ref _minColumns, Math.Max(1, value)); }

    /// <summary>Columns shown in the ribbon in the Large group state.</summary>
    public int MaxColumns { get => _maxColumns; set => SetProperty(ref _maxColumns, Math.Max(1, value)); }

    /// <summary>Columns of the expanded popup.</summary>
    public int DropDownColumns { get => _dropDownColumns; set => SetProperty(ref _dropDownColumns, Math.Max(1, value)); }

    /// <summary>Item width.</summary>
    public double ItemWidth { get => _itemWidth; set => SetProperty(ref _itemWidth, value); }

    /// <summary>Item height.</summary>
    public double ItemHeight { get => _itemHeight; set => SetProperty(ref _itemHeight, value); }

    /// <summary>Shows a filter box in the expanded popup.</summary>
    public bool IsFilterEnabled { get => _isFilterEnabled; set => SetProperty(ref _isFilterEnabled, value); }

    /// <summary>Visible rows in the ribbon.</summary>
    public int Rows { get => _rows; set => SetProperty(ref _rows, Math.Max(1, value)); }

    /// <summary>Shows item labels under the previews.</summary>
    public bool ShowLabels { get => _showLabels; set => SetProperty(ref _showLabels, value); }
}

/// <summary>Office color picker (Font Color, Highlight, Shape Fill): automatic, theme grid, standard, recent, more colors.</summary>
public class RibbonColorPickerModel : RibbonItemModel
{
    private Theming.RibbonColor? _selectedColor = Theming.RibbonColor.Parse("#FF0000");
    private bool _showAutomatic = true;
    private bool _showNoColor;
    private bool _showMoreColors = true;
    private bool _isSplit = true;
    private Theming.RibbonColor _automaticColor = Theming.RibbonColor.Black;

    /// <summary>Creates a color picker.</summary>
    public RibbonColorPickerModel(string? id = null, string? label = null, RibbonIcon? icon = null, ICommand? command = null)
        : base(id, label)
    {
        Icon = icon;
        Command = command;
        Size = RibbonItemSize.Small;
    }

    /// <summary>Selected color (<c>null</c> = "No Color"). The command receives the color.</summary>
    public Theming.RibbonColor? SelectedColor { get => _selectedColor; set => SetProperty(ref _selectedColor, value); }

    /// <summary>Shows the "Automatic" entry.</summary>
    public bool ShowAutomatic { get => _showAutomatic; set => SetProperty(ref _showAutomatic, value); }

    /// <summary>Automatic color.</summary>
    public Theming.RibbonColor AutomaticColor { get => _automaticColor; set => SetProperty(ref _automaticColor, value); }

    /// <summary>Shows "No Color".</summary>
    public bool ShowNoColor { get => _showNoColor; set => SetProperty(ref _showNoColor, value); }

    /// <summary>Shows "More Colors...".</summary>
    public bool ShowMoreColors { get => _showMoreColors; set => SetProperty(ref _showMoreColors, value); }

    /// <summary>Split button (click applies the current color) vs plain drop-down.</summary>
    public bool IsSplit { get => _isSplit; set => SetProperty(ref _isSplit, value); }

    /// <summary>Theme base colors (shades are generated); empty uses the Office theme.</summary>
    public ObservableCollection<Theming.RibbonColorSwatch> ThemeColors { get; } = [];

    /// <summary>Standard colors; empty uses the Office standard colors.</summary>
    public ObservableCollection<Theming.RibbonColorSwatch> StandardColors { get; } = [];

    /// <summary>Recently used colors (maintained automatically).</summary>
    public ObservableCollection<Theming.RibbonColor> RecentColors { get; } = [];
}
