using RibbonSpace.Commands;
using RibbonSpace.Layout;
using RibbonSpace.Model;
using RibbonSpace.Theming;
using I = RibbonSpace.Model.RibbonIcons;

namespace RibbonSpace.Demo.ViewModels;

/// <summary>Excel-like ribbon defined 100% in a view model (no XAML), bound to <c>Ribbon.Model</c>.</summary>
public sealed class ExcelViewModel : ObservableObject
{
    private string _status = "Ready";
    private string _selectedCell = "A1";

    public ExcelViewModel()
    {
        Commands = new RibbonCommandCatalog();
        Commands.Executed += (_, e) => Status = $"{e.Id}{(e.Parameter is null ? string.Empty : $" → {e.Parameter}")}";
        foreach (var (id, label, shortcut) in new[]
        {
            ("paste", "Paste", "Ctrl+V"), ("cut", "Cut", "Ctrl+X"), ("copy", "Copy", "Ctrl+C"), ("formatPainter", "Format Painter", ""),
            ("insertCells", "Insert", ""), ("deleteCells", "Delete", ""), ("formatCells", "Format Cells", "Ctrl+1"),
            ("autoSum", "AutoSum", "Alt+="), ("fill", "Fill", ""), ("clear", "Clear", ""), ("sortFilter", "Sort & Filter", ""), ("findSelect", "Find & Select", "Ctrl+F"),
            ("pivotTable", "PivotTable", ""), ("table", "Table", "Ctrl+T"), ("recommendedCharts", "Recommended Charts", ""),
            ("insertFunction", "Insert Function", "Shift+F3"), ("calculateNow", "Calculate Now", "F9"), ("refreshAll", "Refresh All", "Ctrl+Alt+F5"),
            ("freezePanes", "Freeze Panes", ""), ("newWindow", "New Window", ""), ("increaseDecimal", "Increase Decimal", ""), ("decreaseDecimal", "Decrease Decimal", ""),
            ("percent", "Percent Style", "Ctrl+Shift+%"), ("comma", "Comma Style", ""), ("wrapText", "Wrap Text", ""), ("mergeCenter", "Merge & Center", ""),
            ("chartStyles", "Change Colors", ""), ("switchRowColumn", "Switch Row/Column", ""), ("selectData", "Select Data", ""), ("changeChartType", "Change Chart Type", ""),
        })
        {
            Commands.Register(id, label, p => Status = $"{label}{(p is null ? string.Empty : $" ({p})")}", shortcut: shortcut.Length == 0 ? null : shortcut, category: "Excel");
        }

        Commands.Register("formatCellsLauncher", "Format Cells dialog", _ => Status = "Format Cells dialog");

        Bold = new RibbonToggleButtonModel("bold", "Bold", I.Bold) { Shortcut = "Ctrl+B", KeyTip = "1" };
        Italic = new RibbonToggleButtonModel("italic", "Italic", I.Italic) { Shortcut = "Ctrl+I", KeyTip = "2" };
        Underline = new RibbonToggleButtonModel("underline", "Underline", I.Underline) { Shortcut = "Ctrl+U", KeyTip = "3" };
        FillColor = new RibbonColorPickerModel("fillColor", "Fill Color", I.Fill) { SelectedColor = RibbonColor.Parse("#FFFF00"), ShowNoColor = true, ShowAutomatic = false, KeyTip = "H" };
        FontColor = new RibbonColorPickerModel("fontColor", "Font Color", RibbonIcons.FontColor) { SelectedColor = RibbonColor.Parse("#C00000"), KeyTip = "FC" };
        FontFamily = new RibbonComboBoxModel("fontFamily", "Font", ["Aptos Narrow", "Arial", "Calibri", "Cambria", "Consolas", "Segoe UI", "Times New Roman", "Verdana"]) { Text = "Aptos Narrow", IsEditable = true, InputWidth = 128, PreviewFontFamily = true, KeyTip = "FF" };
        FontSize = new RibbonComboBoxModel("fontSize", "Font Size", [8d, 9d, 10d, 11d, 12d, 14d, 16d, 18d, 20d, 24d, 28d, 36d, 48d, 72d]) { Text = "11", IsEditable = true, InputWidth = 46, KeyTip = "FS" };
        NumberFormat = new RibbonComboBoxModel("numberFormat", "Number Format", ["General", "Number", "Currency", "Accounting", "Short Date", "Long Date", "Time", "Percentage", "Fraction", "Scientific", "Text"]) { Text = "General", InputWidth = 110, KeyTip = "N" };
        Align = new[]
        {
            new RibbonToggleButtonModel("alignLeft", "Align Left", I.AlignLeft) { GroupName = "halign", IsChecked = true },
            new RibbonToggleButtonModel("alignCenter", "Center", I.AlignCenter) { GroupName = "halign" },
            new RibbonToggleButtonModel("alignRight", "Align Right", I.AlignRight) { GroupName = "halign" },
        };
        ChartTools = new RibbonContextualGroupModel("chart", "Chart Tools") { Color = RibbonColor.Parse("#107C41"), Activation = RibbonContextualActivation.SelectOnShow };
        ToggleChartCommand = new RibbonRelayCommand(() =>
        {
            ChartTools.IsVisible = !ChartTools.IsVisible;
            Status = ChartTools.IsVisible ? "Chart selected" : "Chart deselected";
        });
        Ribbon = BuildRibbon();
    }

    public RibbonModel Ribbon { get; }

    public RibbonCommandCatalog Commands { get; }

    public RibbonToggleButtonModel Bold { get; }

    public RibbonToggleButtonModel Italic { get; }

    public RibbonToggleButtonModel Underline { get; }

    public RibbonColorPickerModel FillColor { get; }

    public RibbonColorPickerModel FontColor { get; }

    public RibbonComboBoxModel FontFamily { get; }

    public RibbonComboBoxModel FontSize { get; }

    public RibbonComboBoxModel NumberFormat { get; }

    public RibbonToggleButtonModel[] Align { get; }

    public RibbonContextualGroupModel ChartTools { get; }

    public RibbonRelayCommand ToggleChartCommand { get; }

    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public string SelectedCell { get => _selectedCell; set => SetProperty(ref _selectedCell, value); }

    private static RibbonMenuItemModel Menu(string id, string label, RibbonIcon? icon = null, string? commandId = null) => new(id, label, icon) { CommandId = commandId ?? id };

    private RibbonModel BuildRibbon()
    {
        var model = new RibbonModel { CommandCatalog = Commands, ApplicationButtonLabel = "File", Title = "Budget 2026.xlsx" };
        model.ContextualGroups.Add(ChartTools);

        var home = new RibbonTabModel("home", "Home") { KeyTip = "H" };
        var clipboard = new RibbonGroupModel("clipboard", "Clipboard", I.Paste) { DialogLauncherCommandId = "formatCellsLauncher", ReductionOrder = -1 };
        var paste = new RibbonSplitButtonModel("paste", "Paste", I.Paste) { CommandId = "paste", SizeDefinition = RibbonSizeDefinition.AlwaysLarge, KeyTip = "V", Shortcut = "Ctrl+V" };
        paste.MenuItems.Add(new RibbonMenuHeaderModel("Paste"));
        paste.MenuItems.Add(Menu("pasteValues", "Values", I.Paste, "paste"));
        paste.MenuItems.Add(Menu("pasteFormulas", "Formulas", I.Function, "paste"));
        paste.MenuItems.Add(Menu("pasteTranspose", "Transpose", I.Rotate, "paste"));
        paste.MenuItems.Add(new RibbonMenuSeparatorModel());
        paste.MenuItems.Add(Menu("pasteSpecial", "Paste Special...", null, "paste"));
        clipboard.Items.Add(paste);
        clipboard.Items.Add(new RibbonButtonModel("cut", "Cut", I.Cut) { CommandId = "cut", KeyTip = "X" });
        clipboard.Items.Add(new RibbonButtonModel("copy", "Copy", I.Copy) { CommandId = "copy", KeyTip = "C" });
        clipboard.Items.Add(new RibbonButtonModel("formatPainter", "Format Painter", I.FormatPainter) { CommandId = "formatPainter", KeyTip = "FP" });
        home.Groups.Add(clipboard);

        var font = new RibbonGroupModel("font", "Font", I.Font) { ItemsLayout = RibbonGroupItemsLayout.Rows, RowCount = 2, DialogLauncherCommandId = "formatCellsLauncher" };
        font.Items.Add(new RibbonRowModel("fontRow1", FontFamily, FontSize, new RibbonButtonGroupModel("fontSizeButtons",
            new RibbonButtonModel("growFont", "Increase Font Size", I.FontIncrease),
            new RibbonButtonModel("shrinkFont", "Decrease Font Size", I.FontDecrease))));
        var borders = new RibbonSplitButtonModel("borders", "Borders", I.Borders, size: RibbonItemSize.Small) { KeyTip = "B" };
        foreach (var b in new[] { "Bottom Border", "Top Border", "Left Border", "Right Border", "No Border", "All Borders", "Outside Borders", "Thick Box Border" })
        {
            borders.MenuItems.Add(new RibbonMenuItemModel("border." + b, b, I.Borders));
        }

        font.Items.Add(new RibbonRowModel("fontRow2", new RibbonButtonGroupModel("fontStyles", Bold, Italic, Underline), borders, FillColor, FontColor));
        home.Groups.Add(font);

        var alignment = new RibbonGroupModel("alignment", "Alignment", I.AlignCenter) { DialogLauncherCommandId = "formatCellsLauncher" };
        alignment.Items.Add(new RibbonButtonGroupModel("halignGroup", Align));
        alignment.Items.Add(new RibbonToggleButtonModel("wrapText", "Wrap Text", I.Refresh, size: RibbonItemSize.Medium) { CommandId = "wrapText" });
        alignment.Items.Add(new RibbonSplitButtonModel("mergeCenter", "Merge & Center", I.Merge, size: RibbonItemSize.Medium) { CommandId = "mergeCenter" });
        home.Groups.Add(alignment);

        var number = new RibbonGroupModel("number", "Number", I.Calculator) { ItemsLayout = RibbonGroupItemsLayout.Rows, RowCount = 2, DialogLauncherCommandId = "formatCellsLauncher" };
        number.Items.Add(new RibbonRowModel("numberRow1", NumberFormat));
        number.Items.Add(new RibbonRowModel("numberRow2", new RibbonButtonGroupModel("numberButtons",
            new RibbonButtonModel("currency", "Accounting Number Format", "$") { CommandId = "comma" },
            new RibbonButtonModel("percent", "Percent Style", "%") { CommandId = "percent" },
            new RibbonButtonModel("comma", "Comma Style", ",") { CommandId = "comma" },
            new RibbonButtonModel("increaseDecimal", "Increase Decimal", ".0") { CommandId = "increaseDecimal" },
            new RibbonButtonModel("decreaseDecimal", "Decrease Decimal", ".00") { CommandId = "decreaseDecimal" })));
        home.Groups.Add(number);

        var styles = new RibbonGroupModel("styles", "Styles", I.Palette) { ReductionOrder = 1 };
        var conditional = new RibbonDropDownButtonModel("conditionalFormatting", "Conditional Formatting", I.Filter, RibbonItemSize.Large) { KeyTip = "L" };
        foreach (var entry in new[] { "Highlight Cells Rules", "Top/Bottom Rules", "Data Bars", "Color Scales", "Icon Sets" })
        {
            var sub = new RibbonMenuItemModel("cf." + entry, entry);
            sub.Items.Add(new RibbonMenuItemModel("cf." + entry + ".1", "Greater Than..."));
            sub.Items.Add(new RibbonMenuItemModel("cf." + entry + ".2", "Less Than..."));
            sub.Items.Add(new RibbonMenuItemModel("cf." + entry + ".3", "Between..."));
            conditional.MenuItems.Add(sub);
        }

        conditional.MenuItems.Add(new RibbonMenuSeparatorModel());
        conditional.MenuItems.Add(new RibbonMenuItemModel("cf.new", "New Rule..."));
        conditional.MenuItems.Add(new RibbonMenuItemModel("cf.manage", "Manage Rules..."));
        styles.Items.Add(conditional);
        var cellStyles = new RibbonGalleryModel("cellStyles", "Cell Styles") { Icon = I.Palette, MaxColumns = 4, MinColumns = 2, ItemWidth = 84, ItemHeight = 30, ShowLabels = false, DropDownColumns = 6 };
        foreach (var (label, category) in new[] { ("Normal", "Good, Bad and Neutral"), ("Bad", "Good, Bad and Neutral"), ("Good", "Good, Bad and Neutral"), ("Neutral", "Good, Bad and Neutral"), ("Calculation", "Data and Model"), ("Check Cell", "Data and Model"), ("Input", "Data and Model"), ("Output", "Data and Model"), ("Title", "Titles and Headings"), ("Heading 1", "Titles and Headings"), ("Heading 2", "Titles and Headings"), ("Total", "Titles and Headings") })
        {
            cellStyles.Items.Add(new RibbonGalleryItemModel("cs." + label, label, null, category) { Content = label });
        }

        cellStyles.MenuItems.Add(new RibbonMenuItemModel("cs.new", "New Cell Style...", I.Add));
        cellStyles.MenuItems.Add(new RibbonMenuItemModel("cs.merge", "Merge Styles...", I.Merge));
        cellStyles.Rows = 2;
        styles.Items.Add(cellStyles);
        home.Groups.Add(styles);

        var cells = new RibbonGroupModel("cells", "Cells", I.Table);
        cells.Items.Add(new RibbonSplitButtonModel("insertCells", "Insert", I.InsertRow, size: RibbonItemSize.Medium) { CommandId = "insertCells" });
        cells.Items.Add(new RibbonSplitButtonModel("deleteCells", "Delete", I.DeleteRow, size: RibbonItemSize.Medium) { CommandId = "deleteCells" });
        cells.Items.Add(new RibbonDropDownButtonModel("formatCells", "Format", I.Table, RibbonItemSize.Medium));
        home.Groups.Add(cells);

        var editing = new RibbonGroupModel("editing", "Editing", I.Sum);
        editing.Items.Add(new RibbonSplitButtonModel("autoSum", "AutoSum", I.Sum, size: RibbonItemSize.Medium) { CommandId = "autoSum" });
        editing.Items.Add(new RibbonDropDownButtonModel("fill", "Fill", I.ChevronDown, RibbonItemSize.Medium));
        editing.Items.Add(new RibbonDropDownButtonModel("clear", "Clear", I.Clear, RibbonItemSize.Medium));
        editing.Items.Add(new RibbonDropDownButtonModel("sortFilter", "Sort & Filter", I.Filter, RibbonItemSize.Large));
        editing.Items.Add(new RibbonDropDownButtonModel("findSelect", "Find & Select", I.Find, RibbonItemSize.Large));
        home.Groups.Add(editing);
        model.Tabs.Add(home);

        var insert = new RibbonTabModel("insert", "Insert") { KeyTip = "N" };
        var tables = new RibbonGroupModel("tables", "Tables", I.Table);
        tables.Items.Add(new RibbonSplitButtonModel("pivotTable", "PivotTable", I.Pivot) { CommandId = "pivotTable" });
        tables.Items.Add(new RibbonButtonModel("recommendedPivot", "Recommended PivotTables", I.Pivot, size: RibbonItemSize.Large));
        var tableButton = new RibbonDropDownButtonModel("table", "Table", I.Table, RibbonItemSize.Large);
        tableButton.MenuItems.Add(new RibbonGridPickerModel("tablePicker", "Insert Table", new RibbonRelayCommand<RibbonGridSize>(s => Status = $"Inserted {s} table")));
        tables.Items.Add(tableButton);
        insert.Groups.Add(tables);
        var charts = new RibbonGroupModel("charts", "Charts", I.Chart) { DialogLauncherCommandId = "recommendedCharts" };
        charts.Items.Add(new RibbonButtonModel("recommendedCharts", "Recommended Charts", I.Chart, size: RibbonItemSize.Large) { CommandId = "recommendedCharts" });
        foreach (var chart in new[] { "Column", "Line", "Pie", "Bar", "Area", "Scatter" })
        {
            charts.Items.Add(new RibbonDropDownButtonModel("chart." + chart, chart, I.Chart, RibbonItemSize.Small));
        }

        charts.Items.Add(new RibbonButtonModel("selectChart", "Select sample chart", I.Chart, ToggleChartCommand, RibbonItemSize.Large));
        insert.Groups.Add(charts);
        model.Tabs.Add(insert);

        var formulas = new RibbonTabModel("formulas", "Formulas") { KeyTip = "M" };
        var library = new RibbonGroupModel("functionLibrary", "Function Library", I.Function);
        library.Items.Add(new RibbonButtonModel("insertFunction", "Insert Function", I.Function, size: RibbonItemSize.Large) { CommandId = "insertFunction" });
        foreach (var f in new[] { "AutoSum", "Recently Used", "Financial", "Logical", "Text", "Date & Time", "Lookup & Reference", "Math & Trig" })
        {
            library.Items.Add(new RibbonDropDownButtonModel("fx." + f, f, I.Function, RibbonItemSize.Large));
        }

        formulas.Groups.Add(library);
        var calculation = new RibbonGroupModel("calculation", "Calculation", I.Calculator);
        calculation.Items.Add(new RibbonButtonModel("calculateNow", "Calculate Now", I.Calculator) { CommandId = "calculateNow" });
        calculation.Items.Add(new RibbonButtonModel("calculateSheet", "Calculate Sheet", I.Calculator));
        formulas.Groups.Add(calculation);
        model.Tabs.Add(formulas);

        var data = new RibbonTabModel("data", "Data") { KeyTip = "A" };
        var queries = new RibbonGroupModel("queries", "Queries & Connections", I.Database);
        queries.Items.Add(new RibbonSplitButtonModel("refreshAll", "Refresh All", I.Refresh) { CommandId = "refreshAll" });
        data.Groups.Add(queries);
        var sortFilter = new RibbonGroupModel("sortFilterGroup", "Sort & Filter", I.Filter);
        sortFilter.Items.Add(new RibbonButtonModel("sortAZ", "Sort A to Z", I.SortAscending));
        sortFilter.Items.Add(new RibbonButtonModel("sortZA", "Sort Z to A", I.SortDescending));
        sortFilter.Items.Add(new RibbonButtonModel("sortDialog", "Sort", I.Sort, size: RibbonItemSize.Large));
        sortFilter.Items.Add(new RibbonToggleButtonModel("filter", "Filter", I.Filter, size: RibbonItemSize.Large) { Shortcut = "Ctrl+Shift+L" });
        data.Groups.Add(sortFilter);
        model.Tabs.Add(data);

        var view = new RibbonTabModel("view", "View") { KeyTip = "W" };
        var show = new RibbonGroupModel("show", "Show", I.View);
        show.Items.Add(new RibbonCheckBoxModel("gridlines", "Gridlines", true));
        show.Items.Add(new RibbonCheckBoxModel("formulaBar", "Formula Bar", true));
        show.Items.Add(new RibbonCheckBoxModel("headings", "Headings", true));
        view.Groups.Add(show);
        var zoom = new RibbonGroupModel("zoom", "Zoom", I.Zoom);
        zoom.Items.Add(new RibbonButtonModel("zoomDialog", "Zoom", I.Zoom, size: RibbonItemSize.Large));
        zoom.Items.Add(new RibbonButtonModel("zoom100", "100%", I.Zoom, size: RibbonItemSize.Large));
        zoom.Items.Add(new RibbonSpinnerModel("zoomValue", "Zoom:", 100) { Minimum = 10, Maximum = 400, Increment = 10, Unit = "%", Format = "0" });
        view.Groups.Add(zoom);
        var window = new RibbonGroupModel("window", "Window", I.Window);
        window.Items.Add(new RibbonButtonModel("newWindow", "New Window", I.Window, size: RibbonItemSize.Large) { CommandId = "newWindow" });
        window.Items.Add(new RibbonDropDownButtonModel("freezePanes", "Freeze Panes", I.Freeze, RibbonItemSize.Large));
        view.Groups.Add(window);
        model.Tabs.Add(view);

        var chartDesign = new RibbonTabModel("chartDesign", "Chart Design") { ContextualGroupId = "chart", KeyTip = "JC" };
        var chartLayouts = new RibbonGroupModel("chartLayouts", "Chart Layouts", I.Layout);
        chartLayouts.Items.Add(new RibbonDropDownButtonModel("addElement", "Add Chart Element", I.Add, RibbonItemSize.Large));
        chartLayouts.Items.Add(new RibbonDropDownButtonModel("quickLayout", "Quick Layout", I.Layout, RibbonItemSize.Large));
        chartDesign.Groups.Add(chartLayouts);
        var chartStyles = new RibbonGroupModel("chartStylesGroup", "Chart Styles", I.Palette) { ReductionOrder = 1 };
        chartStyles.Items.Add(new RibbonDropDownButtonModel("chartColors", "Change Colors", I.Palette, RibbonItemSize.Large));
        var styleGallery = new RibbonGalleryModel("chartStyleGallery", "Chart Styles") { MaxColumns = 6, MinColumns = 3, ItemWidth = 58, ItemHeight = 52, ShowLabels = false };
        for (var i = 1; i <= 14; i++)
        {
            styleGallery.Items.Add(new RibbonGalleryItemModel($"chartStyle{i}", $"Style {i}", I.Chart));
        }

        chartStyles.Items.Add(styleGallery);
        chartDesign.Groups.Add(chartStyles);
        var chartData = new RibbonGroupModel("chartData", "Data", I.Database);
        chartData.Items.Add(new RibbonButtonModel("switchRowColumn", "Switch Row/Column", I.Rotate, size: RibbonItemSize.Large) { CommandId = "switchRowColumn" });
        chartData.Items.Add(new RibbonButtonModel("selectData", "Select Data", I.Table, size: RibbonItemSize.Large) { CommandId = "selectData" });
        chartDesign.Groups.Add(chartData);
        var type = new RibbonGroupModel("type", "Type", I.Chart);
        type.Items.Add(new RibbonButtonModel("changeChartType", "Change Chart Type", I.Chart, size: RibbonItemSize.Large) { CommandId = "changeChartType" });
        chartDesign.Groups.Add(type);
        model.Tabs.Add(chartDesign);

        model.QuickAccessItems.Add(new RibbonButtonModel("save", "Save", I.Save) { Shortcut = "Ctrl+S", Command = new RibbonRelayCommand(() => Status = "Saved") });
        model.QuickAccessItems.Add(new RibbonButtonModel("undo", "Undo", I.Undo) { Shortcut = "Ctrl+Z", Command = new RibbonRelayCommand(() => Status = "Undo") });
        model.QuickAccessItems.Add(new RibbonButtonModel("redo", "Redo", I.Redo) { Shortcut = "Ctrl+Y", Command = new RibbonRelayCommand(() => Status = "Redo") });
        model.QuickAccessCandidates.Add(paste);
        model.QuickAccessCandidates.Add(Bold);

        model.TabStripItems.Add(new RibbonButtonModel("comments", "Comments", I.Comment) { ShowLabelInSimplified = true });
        model.TabStripItems.Add(new RibbonSplitButtonModel("share", "Share", I.Share, size: RibbonItemSize.Small) { ShowLabelInSimplified = true });

        model.Backstage.Title = "Excel";
        model.Backstage.Items.Add(new RibbonBackstageItemModel("home", "Home", I.Home, "Start a new workbook or open a recent one."));
        model.Backstage.Items.Add(new RibbonBackstageItemModel("new", "New", I.New, "Blank workbook · Budget · Invoice · Calendar"));
        model.Backstage.Items.Add(new RibbonBackstageItemModel("open", "Open", I.Open, "Recent workbooks"));
        model.Backstage.Items.Add(new RibbonBackstageItemModel("save", "Save", I.Save) { Command = new RibbonRelayCommand(() => Status = "Saved from backstage"), HasSeparatorBefore = true });
        model.Backstage.Items.Add(new RibbonBackstageItemModel("export", "Export", I.Export, "Create a PDF or change the file type."));
        model.Backstage.Items.Add(new RibbonBackstageItemModel("options", "Options", I.Settings, "Excel options") { Placement = RibbonBackstagePlacement.Bottom });
        return model;
    }
}
