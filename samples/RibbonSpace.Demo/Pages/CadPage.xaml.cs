using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Controls;
using RibbonSpace.Controls.Primitives;
using RibbonSpace.Demo.Cad;
using Windows.System;

namespace RibbonSpace.Demo.Pages;

/// <summary>
/// AutoCAD-class showcase ("Drafting &amp; Annotation" workspace) using the CAD theme style: application menu,
/// expanded (slide-out) panels, floating panels, minimize states, progressive tooltips, templated layer and property
/// combo boxes, tool split buttons, contextual tabs, a model space canvas, a command line and CAD status toggles.
/// </summary>
public sealed partial class CadPage : UserControl
{
    // Command names and prompts echoed in the command line (item id → COMMAND, prompt).
    private static readonly Dictionary<string, (string Command, string Prompt)> Commands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["line"] = ("LINE", "Specify first point:"),
        ["polyline"] = ("PLINE", "Specify start point:"),
        ["circle"] = ("CIRCLE", "Specify center point for circle or [3P/2P/Ttr (tan tan radius)]:"),
        ["arc"] = ("ARC", "Specify start point of arc or [Center]:"),
        ["rectangle"] = ("RECTANG", "Specify first corner point or [Chamfer/Elevation/Fillet/Thickness/Width]:"),
        ["hatch"] = ("HATCH", "Pick internal point or [Select objects/Undo/seTtings]:"),
        ["ellipse"] = ("ELLIPSE", "Specify axis endpoint of ellipse or [Arc/Center]:"),
        ["spline"] = ("SPLINE", "Specify first point or [Method/Knots/Object]:"),
        ["constructionLine"] = ("XLINE", "Specify a point or [Hor/Ver/Ang/Bisect/Offset]:"),
        ["ray"] = ("RAY", "Specify start point:"),
        ["multiplePoints"] = ("POINT", "Specify a point:"),
        ["region"] = ("REGION", "Select objects:"),
        ["revisionCloud"] = ("REVCLOUD", "Specify first corner point or [Arc length/Object/Rectangular/Polygonal/Freehand/Style/Modify]:"),
        ["donut"] = ("DONUT", "Specify inside diameter of donut <0.5000>:"),
        ["move"] = ("MOVE", "Select objects:"),
        ["copy"] = ("COPY", "Select objects:"),
        ["stretch"] = ("STRETCH", "Select objects to stretch by crossing-window or crossing-polygon..."),
        ["rotate"] = ("ROTATE", "Select objects:"),
        ["mirror"] = ("MIRROR", "Select objects:"),
        ["scale"] = ("SCALE", "Select objects:"),
        ["trim"] = ("TRIM", "Select object to trim or shift-select to extend or [cuTting edges/Crossing/mOde/Project/eRase]:"),
        ["extend"] = ("EXTEND", "Select object to extend or shift-select to trim or [Boundary edges/Crossing/mOde/Project]:"),
        ["fillet"] = ("FILLET", "Select first object or [Undo/Polyline/Radius/Trim/Multiple]:"),
        ["chamfer"] = ("CHAMFER", "Select first line or [Undo/Polyline/Distance/Angle/Trim/mEthod/Multiple]:"),
        ["array"] = ("ARRAYRECT", "Select objects:"),
        ["erase"] = ("ERASE", "Select objects:"),
        ["explode"] = ("EXPLODE", "Select objects:"),
        ["offset"] = ("OFFSET", "Specify offset distance or [Through/Erase/Layer] <Through>:"),
        ["break"] = ("BREAK", "Select object:"),
        ["join"] = ("JOIN", "Select source object or multiple objects to join at once:"),
        ["text"] = ("MTEXT", "Specify first corner:"),
        ["dimension"] = ("DIMLINEAR", "Specify first extension line origin or <select object>:"),
        ["leader"] = ("MLEADER", "Specify leader arrowhead location or [leader Landing first/Content first/Options] <Options>:"),
        ["table"] = ("TABLE", "Specify insertion point:"),
        ["layerProperties"] = ("LAYER", "Layer Properties Manager opened."),
        ["insertBlock"] = ("INSERT", "Specify insertion point or [Basepoint/Scale/Rotate]:"),
        ["createBlock"] = ("BLOCK", "Enter block name or [?]:"),
        ["matchProperties"] = ("MATCHPROP", "Select source object:"),
        ["group"] = ("GROUP", "Select objects or [Name/Description]:"),
        ["measure"] = ("MEASUREGEOM", "Move cursor or [Distance/Radius/Angle/ARea/Volume/Quick/Mode/eXit] <eXit>:"),
        ["quickSelect"] = ("QSELECT", "Quick Select dialog opened."),
        ["selectAll"] = ("SELECTALL", "12 found"),
        ["idPoint"] = ("ID", "Specify point:"),
        ["paste"] = ("PASTECLIP", "Specify insertion point:"),
        ["copyClip"] = ("COPYCLIP", "Select objects:"),
        ["cutClip"] = ("CUTCLIP", "Select objects:"),
        ["plot"] = ("PLOT", "Plot - Model dialog opened."),
        ["qat.plot"] = ("PLOT", "Plot - Model dialog opened."),
        ["qat.save"] = ("QSAVE", "Drawing1.dwg saved."),
        ["qat.undo"] = ("U", "LINE"),
        ["qat.redo"] = ("MREDO", "Everything has been redone"),
        ["box"] = ("BOX", "Specify first corner or [Center]:"),
        ["extrude"] = ("EXTRUDE", "Select objects to extrude or [MOde]:"),
        ["purge"] = ("PURGE", "Purge dialog opened."),
        ["zoomExtents"] = ("ZOOM", "Specify corner of window, enter a scale factor (nX or nXP), or [All/Center/Dynamic/Extents/Previous/Scale/Window/Object] <real time>: _e"),
    };

    // Command-line aliases (typed names) → ribbon item ids.
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["l"] = "line", ["line"] = "line", ["pl"] = "polyline", ["pline"] = "polyline", ["c"] = "circle", ["circle"] = "circle",
        ["a"] = "arc", ["arc"] = "arc", ["rec"] = "rectangle", ["rectang"] = "rectangle", ["h"] = "hatch", ["hatch"] = "hatch",
        ["el"] = "ellipse", ["ellipse"] = "ellipse", ["spl"] = "spline", ["spline"] = "spline", ["xl"] = "constructionLine",
        ["xline"] = "constructionLine", ["ray"] = "ray", ["po"] = "multiplePoints", ["point"] = "multiplePoints", ["reg"] = "region",
        ["revcloud"] = "revisionCloud", ["do"] = "donut", ["donut"] = "donut", ["m"] = "move", ["move"] = "move", ["co"] = "copy",
        ["cp"] = "copy", ["copy"] = "copy", ["s"] = "stretch", ["stretch"] = "stretch", ["ro"] = "rotate", ["rotate"] = "rotate",
        ["mi"] = "mirror", ["mirror"] = "mirror", ["sc"] = "scale", ["scale"] = "scale", ["tr"] = "trim", ["trim"] = "trim",
        ["ex"] = "extend", ["extend"] = "extend", ["f"] = "fillet", ["fillet"] = "fillet", ["cha"] = "chamfer", ["chamfer"] = "chamfer",
        ["ar"] = "array", ["array"] = "array", ["arrayrect"] = "array", ["e"] = "erase", ["erase"] = "erase", ["x"] = "explode",
        ["explode"] = "explode", ["o"] = "offset", ["offset"] = "offset", ["br"] = "break", ["break"] = "break", ["j"] = "join",
        ["join"] = "join", ["t"] = "text", ["mt"] = "text", ["mtext"] = "text", ["dli"] = "dimension", ["dimlinear"] = "dimension",
        ["mld"] = "leader", ["mleader"] = "leader", ["tb"] = "table", ["table"] = "table", ["la"] = "layerProperties", ["layer"] = "layerProperties",
        ["i"] = "insertBlock", ["insert"] = "insertBlock", ["b"] = "createBlock", ["block"] = "createBlock", ["ma"] = "matchProperties",
        ["matchprop"] = "matchProperties", ["g"] = "group", ["group"] = "group", ["mea"] = "measure", ["measuregeom"] = "measure",
        ["qselect"] = "quickSelect", ["selectall"] = "selectAll", ["id"] = "idPoint", ["plot"] = "plot", ["print"] = "plot",
        ["pu"] = "purge", ["purge"] = "purge", ["box"] = "box", ["ext"] = "extrude", ["extrude"] = "extrude",
    };

    private readonly List<string> _history = [];
    private bool _initialized;

    public CadPage()
    {
        InitializeComponent();
        RibbonScreenTipService.ExtendedDelay = TimeSpan.FromSeconds(1.2);
        TitleBar.Ribbon = Ribbon;

        // Stroked, multi-colour CAD icons cannot be PathIcons: menus the ribbon builds (overflow, linked copies)
        // render them with the sample's Skia rasterizer for the CAD page theme.
        RibbonItemHelper.MenuIconConverter = icon => icon is RibbonSpace.Model.RibbonIcon { Kind: RibbonSpace.Model.RibbonIconKind.Path, ViewBoxSize: 32 } path
            ? CadMenuIcons.Create(path, _menuIconsDark)
            : null;
        AppMenu.Ribbon = Ribbon;
        StatusBar.Ribbon = Ribbon;

        LayerCombo.ItemsSource = CadLayer.Samples;
        LayerCombo.SelectedItem = CadLayer.Samples[1];
        LayerCombo.ItemTextSelector = item => (item as CadLayer)?.Name ?? string.Empty;
        ColorCombo.ItemsSource = CadColorOption.Samples;
        ColorCombo.SelectedItem = CadColorOption.Samples[0];
        TextColorCombo.ItemsSource = CadColorOption.Samples;
        TextColorCombo.SelectedItem = CadColorOption.Samples[0];
        HatchColorCombo.ItemsSource = CadColorOption.Samples;
        HatchColorCombo.SelectedItem = CadColorOption.Samples[0];
        LinetypeCombo.ItemsSource = CadLinetype.Samples;
        LinetypeCombo.SelectedItem = CadLinetype.Samples[0];
        LineweightCombo.ItemsSource = CadLineweight.Samples;
        LineweightCombo.SelectedItem = CadLineweight.Samples[0];
        foreach (var combo in new[] { ColorCombo, TextColorCombo, HatchColorCombo })
        {
            combo.ItemTextSelector = item => (item as CadColorOption)?.Name ?? string.Empty;
        }

        LinetypeCombo.ItemTextSelector = item => (item as CadLinetype)?.Name ?? string.Empty;
        LineweightCombo.ItemTextSelector = item => (item as CadLineweight)?.Name ?? string.Empty;
        LayerCombo.Committed += (_, e) => Echo($"Current layer: \"{(e.Item as CadLayer)?.Name}\"", true);

        // The title bar logo opens the application menu (IsAppIconMenuEnabled), as AutoCAD's top-left button.
        AppMenu.ItemInvoked += (_, e) => Echo($"Application menu: {e.Item.Label}", true);
        AppMenu.RecentItemInvoked += (_, item) => Echo($"Opening \"{item.Title}\"...", true);
        AppMenuExit.Click += (_, _) => Echo("EXIT is disabled in the showcase.", true);
        AppMenuOptions.Click += (_, _) => Echo("OPTIONS", true);
        if (Application.Current.Resources.TryGetValue("RibbonFlyoutPresenterStyle", out var presenter) && presenter is Style basedOn)
        {
            AppMenuFlyout.FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter))
            {
                BasedOn = basedOn,
                Setters =
                {
                    new Setter(Control.PaddingProperty, new Thickness(0)),
                    new Setter(Control.CornerRadiusProperty, new CornerRadius(2)),
                },
            };
        }

        Ribbon.ItemInvoked += OnItemInvoked;
        WorkspaceCombo.Committed += (_, e) => ApplyWorkspace(e.Text ?? string.Empty);
        BuildWorkspaceMenu();
        BuildMinimizeBehaviorMenu();
        BuildFileTabs();
        BuildLayoutTabs();

        // Viewport and interface toggles.
        UcsToggle.Click += (_, _) => UcsIcon.Visibility = Vis(UcsToggle.IsChecked);
        ViewCubeToggle.Click += (_, _) => ViewCube.Visibility = Vis(ViewCubeToggle.IsChecked);
        NavBarToggle.Click += (_, _) => NavBar.Visibility = Vis(NavBarToggle.IsChecked);
        CommandLineToggle.Click += (_, _) => CommandLine.Visibility = Vis(CommandLineToggle.IsChecked);
        CommandClose.Click += (_, _) =>
        {
            CommandLine.Visibility = Visibility.Collapsed;
            CommandLineToggle.IsChecked = false;
        };
        FileTabsToggle.Click += (_, _) => FileTabsRow.Visibility = Vis(FileTabsToggle.IsChecked);
        LayoutTabsToggle.Click += (_, _) => LayoutTabs.Visibility = Vis(LayoutTabsToggle.IsChecked);
        LightThemeToggle.Click += (_, _) => SetLightTheme(LightThemeToggle.IsChecked == true);
        GridToggle.Click += (_, _) => Canvas.IsGridVisible = GridToggle.IsChecked == true;
        LineweightToggle.Click += (_, _) =>
        {
            Canvas.ShowLineweights = LineweightToggle.IsChecked == true;
            Canvas.Redraw();
        };
        ModelToggle.Click += (_, _) => ModelToggle.Label = ModelToggle.IsChecked == true ? "MODEL" : "PAPER";
        CleanScreenToggle.Click += (_, _) => SetCleanScreen(CleanScreenToggle.IsChecked == true);
        NavPan.Click += (_, _) => Canvas.IsPanMode = NavPan.IsChecked == true;
        Canvas.CursorMoved += (_, p) => Coordinates.Text = string.Create(CultureInfo.InvariantCulture, $"{p.X:0.0000}, {p.Y:0.0000}, 0.0000");

        // Ribbon feature helpers (View tab → Ribbon panel).
        CycleButton.Click += (_, _) => Ribbon.ToggleMinimized();
        PanelTitlesToggle.Click += (_, _) => Ribbon.ShowGroupCaptions = PanelTitlesToggle.IsChecked == true;
        FloatLayersButton.Click += (_, _) => FloatLayersPanel();
        ReturnPanelsButton.Click += (_, _) => Ribbon.ReturnAllPanelsToRibbon();
        ShowTextEditorButton.Click += (_, _) => ShowTextEditor();
        ShowHatchButton.Click += (_, _) => ShowHatchCreation();
        CloseTextEditor.Click += (_, _) => CloseContextual("Text Editor");
        CloseHatch.Click += (_, _) => CloseContextual("Hatch Creation");
        Ribbon.RegisterPropertyChangedCallback(Ribbon.ShowGroupCaptionsProperty, (_, _) => PanelTitlesToggle.IsChecked = Ribbon.ShowGroupCaptions);

        CommandInput.KeyDown += OnCommandKeyDown;
        ActualThemeChanged += (_, _) => ApplyChrome();
        Loaded += OnLoaded;
        _history.Add("Command: _.OPEN \"Drawing1.dwg\"");
        _history.Add("Regenerating model.");
        _history.Add("Command:");
        UpdateHistory();
    }

    /// <summary>The ribbon (capture automation).</summary>
    public Ribbon CadRibbon => Ribbon;

    public RibbonGroup DrawPanel => DrawGroup;

    public RibbonGroup LayersPanel => LayersGroup;

    public RibbonComboBox LayerDropDown => LayerCombo;

    public RibbonApplicationMenu ApplicationMenu => AppMenu;

    public RibbonApplicationMenuItem SaveAsMenuItem => SaveAsItem;

    private static T? FindNamed<T>(DependencyObject root, string name)
        where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match && match.Name == name)
            {
                return match;
            }

            if (FindNamed<T>(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static Visibility Vis(bool? value) => value == true ? Visibility.Visible : Visibility.Collapsed;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyChrome();
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Canvas.SetCursor(new Windows.Foundation.Point(ActualWidth * 0.62, Math.Max(200, ActualHeight * 0.32)));
    }

    // ------------------------------------------------------------------ application menu

    public void ShowApplicationMenu()
    {
        Ribbon.InvokeApplicationButton(FindNamed<Button>(TitleBar, "PART_AppButton"));
    }

    // ------------------------------------------------------------------ commands

    private void OnItemInvoked(object? sender, RibbonItemInvokedEventArgs e)
    {
        var id = e.ItemId ?? e.CommandId ?? string.Empty;
        var choice = (e.Item as RibbonSplitButton)?.LastChoice?.Text ?? e.Parameter as string;
        switch (id)
        {
            case "text" when choice is null or "Multiline Text":
            case "annotateMText" when choice is null or "Multiline Text":
                ShowTextEditor();
                break;
            case "hatch" when choice is null or "Hatch":
            case "gradient":
                ShowHatchCreation();
                break;
            case "nav.zoomExtents":
            case "zoomExtents":
                Canvas.ZoomExtents();
                break;
        }

        if (id.StartsWith("status.", StringComparison.Ordinal) || id.StartsWith("nav.", StringComparison.Ordinal) && id != "nav.zoomExtents")
        {
            return;
        }

        if (Commands.TryGetValue(id, out var command))
        {
            var name = choice is not null && e.Item is RibbonSplitButton ? $"{command.Command} ({choice})" : command.Command;
            Echo($"Command: _{name}", false);
            Echo(command.Prompt, false);
        }
        else if (e.Item is IRibbonItem { Label: { Length: > 0 } label })
        {
            Echo($"Command: {label.Replace('\n', ' ').ToUpperInvariant().Replace(' ', '_')}", false);
        }
    }

    private void OnCommandKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CommandInput.Text = string.Empty;
            Echo("*Cancel*", false);
            e.Handled = true;
            return;
        }

        if (e.Key is not (VirtualKey.Enter or VirtualKey.Space) || string.IsNullOrWhiteSpace(CommandInput.Text))
        {
            return;
        }

        e.Handled = true;
        RunCommand(CommandInput.Text.Trim());
        CommandInput.Text = string.Empty;
    }

    /// <summary>Runs a typed command: aliases map to ribbon items, anything else goes through the ribbon search.</summary>
    public void RunCommand(string text)
    {
        if (Aliases.TryGetValue(text, out var id) && Ribbon.FindItem(id) is IRibbonItem item)
        {
            item.Invoke();
            return;
        }

        var results = Ribbon.Search(text, 1);
        if (results.Count > 0 && results[0].Score > 0)
        {
            Echo($"Command: {text.ToUpperInvariant()}", false);
            Ribbon.ExecuteSearchEntry(results[0].Entry);
            return;
        }

        Echo($"Unknown command \"{text.ToUpperInvariant()}\".  Press F1 for help.", false);
    }

    private void Echo(string line, bool asCommand)
    {
        // An empty "Command:" prompt is replaced by the next command, as in the AutoCAD command line.
        if (_history.Count > 0 && _history[^1] == "Command:")
        {
            _history.RemoveAt(_history.Count - 1);
        }

        _history.Add(asCommand ? "Command: " + line : line);
        while (_history.Count > 40)
        {
            _history.RemoveAt(0);
        }

        UpdateHistory();
        DemoSettings.Write("CAD · " + line);
    }

    private void UpdateHistory() => CommandHistory.Text = string.Join(Environment.NewLine, _history.TakeLast(3));

    // ------------------------------------------------------------------ contextual tabs

    public void ShowTextEditor()
    {
        Ribbon.SetActiveContextualGroups("textEditor");
        PlaceTextEditor();
        MTextLayer.Visibility = Visibility.Visible;
        Echo("Command: _MTEXT  Current text style:  \"Standard\"  Text height:  2.5  Annotative:  No", false);
    }

    public void ShowHatchCreation()
    {
        Ribbon.SetActiveContextualGroups("hatchCreation");
        Echo("Command: _HATCH", false);
        Echo("Pick internal point or [Select objects/Undo/seTtings]:", false);
    }

    private void PlaceTextEditor()
    {
        var topLeft = Canvas.ModelToScreen(3700, 4450);
        var bottomRight = Canvas.ModelToScreen(6300, 3750);
        Microsoft.UI.Xaml.Controls.Canvas.SetLeft(MTextEditor, topLeft.X);
        Microsoft.UI.Xaml.Controls.Canvas.SetTop(MTextEditor, topLeft.Y - 12);
        MTextEditor.Width = Math.Max(120, bottomRight.X - topLeft.X);
        MTextEditor.Height = Math.Max(40, bottomRight.Y - topLeft.Y + 12);
        MTextText.FontSize = Math.Clamp(250 * Canvas.ViewScale, 9, 18);
    }

    private void CloseContextual(string name)
    {
        MTextLayer.Visibility = Visibility.Collapsed;
        Ribbon.SetActiveContextualGroups();
        Ribbon.SelectTab("home");
        Echo($"{name} closed.", false);
    }

    // ------------------------------------------------------------------ workspaces

    private static readonly string[] Workspaces = ["Drafting & Annotation", "3D Basics", "3D Modeling"];

    public void ApplyWorkspace(string workspace)
    {
        string[] tabs = workspace switch
        {
            "3D Basics" => ["home", "visualize", "insert", "view", "manage", "output", "collaborate"],
            "3D Modeling" => ["home", "solid", "surface", "mesh", "visualize", "parametric", "insert", "annotate", "view", "manage", "output", "collaborate"],
            _ => ["home", "insert", "annotate", "parametric", "view", "manage", "output", "collaborate"],
        };
        foreach (var tab in Ribbon.Tabs.Where(t => !t.IsContextual))
        {
            tab.IsTabVisible = tabs.Contains(tab.Id);
        }

        if (WorkspaceCombo.Text != workspace)
        {
            WorkspaceCombo.Text = workspace;
        }

        Ribbon.SelectTab("home");
        Echo($"Workspace: {workspace}", true);
    }

    private void BuildWorkspaceMenu()
    {
        var menu = new MenuFlyout();
        foreach (var workspace in Workspaces)
        {
            var item = new RadioMenuFlyoutItem { Text = workspace, GroupName = "workspace", IsChecked = workspace == Workspaces[0] };
            item.Click += (_, _) => ApplyWorkspace(workspace);
            menu.Items.Add(item);
        }

        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem { Text = "Save Current As..." });
        menu.Items.Add(new MenuFlyoutItem { Text = "Workspace Settings..." });
        menu.Items.Add(new MenuFlyoutItem { Text = "Customize..." });
        menu.Opening += (_, _) =>
        {
            foreach (var radio in menu.Items.OfType<RadioMenuFlyoutItem>())
            {
                radio.IsChecked = radio.Text == WorkspaceCombo.Text;
            }
        };
        WorkspaceStatusButton.Flyout = menu;
    }

    private void BuildMinimizeBehaviorMenu()
    {
        var menu = new MenuFlyout();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            foreach (var entry in Ribbon.BuildMinimizeBehaviorMenu().Items.ToList())
            {
                menu.Items.Add(entry);
            }
        };
        MinimizeBehaviorButton.Flyout = menu;
    }

    // ------------------------------------------------------------------ panels

    public void FloatLayersPanel()
    {
        if (LayersGroup.IsFloating)
        {
            return;
        }

        var origin = Canvas.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
        LayersGroup.Float(new Windows.Foundation.Point(origin.X + 120, origin.Y + 70));
    }

    // ------------------------------------------------------------------ theme and chrome

    public void SetLightTheme(bool light)
    {
        RequestedTheme = light ? ElementTheme.Light : ElementTheme.Dark;
        LightThemeToggle.IsChecked = light;
    }

    private void SetCleanScreen(bool clean)
    {
        Ribbon.VisibilityMode = clean ? RibbonVisibilityMode.FullScreen : RibbonVisibilityMode.AlwaysShow;
        TitleBar.Visibility = clean ? Visibility.Collapsed : Visibility.Visible;
        FileTabsRow.Visibility = clean || FileTabsToggle.IsChecked != true ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyChrome()
    {
        var dark = ActualTheme != ElementTheme.Light;

        // Drop-down entries declared in XAML keep their CAD icon in Tag (FollowLastChoice uses it for the primary part);
        // give them a rendered menu icon for the current theme. The ribbon themes the menu surfaces itself.
        _menuIconsDark = dark;
        foreach (var menu in EnumerateMenus())
        {
            CadMenuIcons.Apply(menu.Items, dark);
        }

        Brush B(string key) => RibbonTheme.GetBrush(this, key) ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
        Brush C(byte a, byte r, byte g, byte b) => new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
        ViewportLabel.Foreground = dark ? C(255, 0xC9, 0xD1, 0xDB) : C(255, 0x3F, 0x48, 0x55);
        CubeRing.Stroke = dark ? C(255, 0x5F, 0x6E, 0x86) : C(255, 0xA9, 0xB4, 0xC2);
        CubeFace.Background = dark ? C(255, 0x4A, 0x55, 0x68) : C(255, 0xF7, 0xF8, 0xF9);
        CubeFace.BorderBrush = dark ? C(255, 0x8C, 0x99, 0xAD) : C(255, 0x8C, 0x97, 0xA5);
        foreach (var text in new[] { CubeText, CubeN, CubeS, CubeE, CubeW, WcsText })
        {
            text.Foreground = dark ? C(255, 0xE1, 0xE6, 0xEC) : C(255, 0x2F, 0x37, 0x42);
        }

        WcsChevron.Foreground = CubeText.Foreground;
        WcsBadge.Background = dark ? C(255, 0x3B, 0x44, 0x53) : C(255, 0xF0, 0xF1, 0xF3);
        WcsBadge.BorderBrush = CubeFace.BorderBrush;
        NavBar.Background = B("RibbonCommandBarBackgroundBrush");
        NavBar.BorderBrush = B("RibbonPopupBorderBrush");
        CommandHistoryBox.Background = dark ? C(0xD9, 0x2B, 0x31, 0x3B) : C(0xE6, 0xF0, 0xF1, 0xF3);
        CommandHistory.Foreground = dark ? C(255, 0xAE, 0xB8, 0xC4) : C(255, 0x4E, 0x58, 0x66);
        CommandBar.Background = B("RibbonCommandBarBackgroundBrush");
        CommandBar.BorderBrush = B("RibbonPopupBorderBrush");
        CommandPrompt.Foreground = B("RibbonSecondaryForegroundBrush");
        MTextFrame.Stroke = dark ? C(255, 0xE1, 0xE6, 0xEC) : C(255, 0x2F, 0x37, 0x42);
        MTextText.Foreground = dark ? C(255, 0xF2, 0xF2, 0xF2) : C(255, 0x1F, 0x24, 0x2B);
        MTextCaret.Fill = MTextText.Foreground;
        UpdateTabVisuals();
    }

    private IEnumerable<MenuFlyout> EnumerateMenus()
    {
        IEnumerable<FrameworkElement> Walk(IEnumerable<UIElement> items)
            => items.OfType<FrameworkElement>().SelectMany(i => i is RibbonStackPanel stack ? Walk(stack.Items) : [i]);

        var elements = Ribbon.GetAllItems()
            .Concat(Ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => Walk(g.SlideOutItems)))
            .Concat(NavToolBar.Items.OfType<FrameworkElement>())
            .Concat(StatusBar.EndItems.OfType<FrameworkElement>())
            .Distinct();
        foreach (var element in elements)
        {
            var flyout = element switch
            {
                RibbonSplitButton split => split.Flyout,
                Button button => button.Flyout,
                _ => null,
            };
            if (flyout is MenuFlyout menu)
            {
                yield return menu;
            }
        }
    }

    // ------------------------------------------------------------------ file and layout tabs

    private readonly List<(Border Tab, bool IsFileTab)> _tabs = [];
    private bool _menuIconsDark = true;
    private Popup? _tipPopup;
    private Border? _selectedFileTab;
    private Border? _selectedLayoutTab;

    private void BuildFileTabs()
    {
        foreach (var (name, modified) in new[] { ("Start", false), ("Drawing1", true), ("Floor Plan", false) })
        {
            var tab = CreateTab(name + (modified ? "*" : string.Empty), name != "Start", isFileTab: true);
            FileTabs.Children.Add(tab);
            if (name == "Drawing1")
            {
                _selectedFileTab = tab;
            }
        }

        FileTabs.Children.Add(CreateAddButton());
    }

    private void BuildLayoutTabs()
    {
        foreach (var name in new[] { "Model", "Layout1", "Layout2" })
        {
            var tab = CreateTab(name, false, isFileTab: false);
            LayoutTabs.Children.Add(tab);
            _selectedLayoutTab ??= tab;
        }

        LayoutTabs.Children.Add(CreateAddButton());
    }

    private Border CreateTab(string text, bool closable, bool isFileTab)
    {
        var label = new TextBlock { Text = text, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center, Children = { label } };
        if (closable)
        {
            content.Children.Add(new FontIcon { Glyph = "", FontSize = 8, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 });
        }

        var tab = new Border
        {
            Child = content,
            Padding = new Thickness(12, 0, closable ? 8 : 12, 0),
            MinWidth = isFileTab ? 90 : 0,
            CornerRadius = isFileTab ? new CornerRadius(3, 3, 0, 0) : new CornerRadius(0, 0, 3, 3),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
        };
        if (!isFileTab)
        {
            tab.Height = 26;
        }

        tab.Tapped += (_, _) =>
        {
            if (isFileTab)
            {
                _selectedFileTab = tab;
            }
            else
            {
                _selectedLayoutTab = tab;
                ModelToggle.IsChecked = text == "Model";
                ModelToggle.Label = text == "Model" ? "MODEL" : "PAPER";
            }

            UpdateTabVisuals();
        };
        _tabs.Add((tab, isFileTab));
        return tab;
    }

    private static Button CreateAddButton()
    {
        var button = new Button
        {
            Content = new FontIcon { Glyph = "", FontSize = 10 },
            Width = 26,
            Padding = new Thickness(0),
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        AutomationPropertiesHelper.SetName(button, "New");
        if (Application.Current.Resources.TryGetValue("RibbonChromeButtonStyle", out var style) && style is Style chrome)
        {
            button.Style = chrome;
        }

        return button;
    }

    private void UpdateTabVisuals()
    {
        var canvas = new SolidColorBrush(Canvas.CanvasColor);
        foreach (var (tab, isFileTab) in _tabs)
        {
            var selected = ReferenceEquals(tab, isFileTab ? _selectedFileTab : _selectedLayoutTab);
            tab.Background = selected
                ? (isFileTab ? canvas : RibbonTheme.GetBrush(this, "RibbonCommandBarBackgroundBrush"))
                : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            if (tab.Child is StackPanel { Children: [TextBlock label, ..] })
            {
                label.Foreground = RibbonTheme.GetBrush(this, selected ? "RibbonForegroundBrush" : "RibbonSecondaryForegroundBrush");
                label.FontWeight = selected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
            }
        }

        FileTabsRow.Background = RibbonTheme.GetBrush(this, "RibbonWindowBackgroundBrush");
    }

    /// <summary>
    /// Opens the progressive tooltip of an item (capture automation; tooltips normally open on hover). Set
    /// <see cref="RibbonScreenTipService.ExtendedDelay"/> to zero first to show the extended part at once.
    /// </summary>
    public bool ShowToolTip(string itemId)
    {
        if (Ribbon.FindItem(itemId) is not FrameworkElement element || element is not IRibbonItem item)
        {
            return false;
        }

        // A ToolTip opened from code is placed at the last pointer position on Skia (none during automation), so the
        // capture hosts the same ribbon ToolTip (RibbonToolTipStyle + ScreenTip content) in a popup below the item.
        var content = element switch
        {
            RibbonButton b => RibbonScreenTipService.Create(item.Label, b.ScreenTip, b.Shortcut),
            RibbonControlBase c => RibbonScreenTipService.Create(item.Label, c.ScreenTip, c.Shortcut),
            _ => item.Label,
        };
        if (RibbonScreenTipService.CreateToolTip(content) is not { } tip)
        {
            return false;
        }

        tip.RequestedTheme = ActualTheme;
        var origin = element.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, element.ActualHeight + 4));
        _tipPopup?.IsOpen = false;
        _tipPopup = new Popup { Child = tip, XamlRoot = XamlRoot, HorizontalOffset = origin.X, VerticalOffset = origin.Y, IsLightDismissEnabled = true };
        _tipPopup.IsOpen = true;
        return true;
    }
}

internal static class AutomationPropertiesHelper
{
    public static void SetName(DependencyObject element, string name) => Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(element, name);
}
