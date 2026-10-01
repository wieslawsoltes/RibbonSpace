using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace RibbonSpace.Demo.Cad;

/// <summary>
/// Model space of the CAD showcase: a dark (or light) drawing area with grid, a sample floor plan drawn from simple
/// model-space entities, a selected object with grips, a crosshair cursor, wheel zoom and middle-button / pan-mode
/// panning. It is a showcase, not a CAD engine.
/// </summary>
public sealed partial class CadDrawingCanvas : Grid
{
    private readonly Canvas _gridLayer = new() { IsHitTestVisible = false };
    private readonly Canvas _drawing = new() { IsHitTestVisible = false };
    private readonly Canvas _overlay = new() { IsHitTestVisible = false };
    private readonly Line _crossH = new() { StrokeThickness = 1 };
    private readonly Line _crossV = new() { StrokeThickness = 1 };
    private readonly Rectangle _pickBox = new() { Width = 9, Height = 9, StrokeThickness = 1 };
    private readonly List<Entity> _entities = [];
    private readonly List<(Point P, string Text, double Height, double Angle, string Layer)> _texts = [];
    private Point _center = new(6000, 3600);
    private double _scale = 0.06;
    private bool _fitted;
    private Point? _panStart;
    private Point _cursor = new(double.NaN, double.NaN);
    private bool _dark = true;

    public CadDrawingCanvas()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        Children.Add(_gridLayer);
        Children.Add(_drawing);
        Children.Add(_overlay);
        _overlay.Children.Add(_crossH);
        _overlay.Children.Add(_crossV);
        _overlay.Children.Add(_pickBox);
        BuildPlan();
        SizeChanged += (_, _) =>
        {
            if (!_fitted && ActualWidth > 0)
            {
                ZoomExtents();
                _fitted = true;
            }
            else
            {
                Render();
            }
        };
        PointerMoved += OnPointerMoved;
        PointerExited += (_, _) => SetCursor(new Point(double.NaN, double.NaN));
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        ActualThemeChanged += (_, _) => ApplyTheme();
        Loaded += (_, _) => ApplyTheme();
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Cross);
    }

    /// <summary>Raised with model coordinates while the crosshair moves.</summary>
    public event EventHandler<Point>? CursorMoved;

    /// <summary>Screen area covered by overlays, kept free by <see cref="ZoomExtents"/>.</summary>
    public Thickness ViewInsets { get; set; } = new(40, 30, 150, 130);

    /// <summary>Pan mode (navigation bar): left drag pans.</summary>
    public bool IsPanMode { get; set; }

    /// <summary>Shows the drawing grid (status bar GRID toggle).</summary>
    public bool IsGridVisible
    {
        get => _gridLayer.Visibility == Visibility.Visible;
        set => _gridLayer.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Draws lineweights (status bar LWT toggle).</summary>
    public bool ShowLineweights { get; set; }

    /// <summary>Hidden layers.</summary>
    public HashSet<string> HiddenLayers { get; } = [];

    /// <summary>Background of the model space for the current theme.</summary>
    public Color CanvasColor => _dark ? Color.FromArgb(255, 0x21, 0x28, 0x30) : Color.FromArgb(255, 0xFA, 0xFB, 0xFC);

    /// <summary>Places the crosshair (screen coordinates); NaN hides it.</summary>
    public void SetCursor(Point position)
    {
        _cursor = position;
        UpdateCrosshair();
    }

    public void ZoomExtents()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        // Plan with dimensions and title: -1700..12400 × -2300..8600, fitted into the area left free by the
        // overlays (command line, navigation bar, view cube).
        var insets = ViewInsets;
        var width = Math.Max(100, ActualWidth - insets.Left - insets.Right);
        var height = Math.Max(100, ActualHeight - insets.Top - insets.Bottom);
        _scale = Math.Min(width / 15000, height / 11400);
        var screenCenter = new Point(insets.Left + (width / 2), insets.Top + (height / 2));
        _center = new Point(5350 - ((screenCenter.X - (ActualWidth / 2)) / _scale), 3150 + ((screenCenter.Y - (ActualHeight / 2)) / _scale));
        Render();
    }

    public void Zoom(double factor, Point? at = null)
    {
        var anchor = at ?? new Point(ActualWidth / 2, ActualHeight / 2);
        var model = ToModel(anchor);
        _scale = Math.Clamp(_scale * factor, 0.005, 2);
        var after = ToModel(anchor);
        _center = new Point(_center.X + (model.X - after.X), _center.Y + (model.Y - after.Y));
        Render();
    }

    public void Redraw() => Render();

    private void ApplyTheme()
    {
        _dark = ActualTheme != ElementTheme.Light;
        Background = new SolidColorBrush(CanvasColor);
        var cross = _dark ? Color.FromArgb(255, 0xE6, 0xEA, 0xEF) : Color.FromArgb(255, 0x30, 0x36, 0x3E);
        _crossH.Stroke = _crossV.Stroke = _pickBox.Stroke = new SolidColorBrush(cross);
        Render();
    }

    /// <summary>Converts model coordinates to canvas coordinates.</summary>
    public Point ModelToScreen(double x, double y) => ToScreen(x, y);

    /// <summary>Current zoom (pixels per drawing unit).</summary>
    public double ViewScale => _scale;

    private Point ToScreen(double x, double y) => new(((x - _center.X) * _scale) + (ActualWidth / 2), (ActualHeight / 2) - ((y - _center.Y) * _scale));

    private Point ToModel(Point p) => new(((p.X - (ActualWidth / 2)) / _scale) + _center.X, _center.Y - ((p.Y - (ActualHeight / 2)) / _scale));

    // ------------------------------------------------------------------ input

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(this).Position;
        if (_panStart is { } start)
        {
            _center = new Point(_center.X - ((p.X - start.X) / _scale), _center.Y + ((p.Y - start.Y) / _scale));
            _panStart = p;
            Render();
        }

        SetCursor(p);
        CursorMoved?.Invoke(this, ToModel(p));
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsMiddleButtonPressed || (IsPanMode && point.Properties.IsLeftButtonPressed))
        {
            _panStart = point.Position;
            CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_panStart is not null)
        {
            _panStart = null;
            ReleasePointerCaptures();
        }
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        Zoom(point.Properties.MouseWheelDelta > 0 ? 1.15 : 1 / 1.15, point.Position);
        e.Handled = true;
    }

    private void UpdateCrosshair()
    {
        var visible = !double.IsNaN(_cursor.X) && ActualWidth > 0;
        _crossH.Visibility = _crossV.Visibility = _pickBox.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
        {
            return;
        }

        // AutoCAD-style crosshair: full-size lines with a gap around the pick box.
        _crossH.X1 = 0;
        _crossH.X2 = ActualWidth;
        _crossH.Y1 = _crossH.Y2 = Math.Round(_cursor.Y) + 0.5;
        _crossV.Y1 = 0;
        _crossV.Y2 = ActualHeight;
        _crossV.X1 = _crossV.X2 = Math.Round(_cursor.X) + 0.5;
        Canvas.SetLeft(_pickBox, Math.Round(_cursor.X) - 4);
        Canvas.SetTop(_pickBox, Math.Round(_cursor.Y) - 4);
    }

    // ------------------------------------------------------------------ rendering

    private Color LayerColor(string layer) => (layer, _dark) switch
    {
        ("A-WALL", true) => Color.FromArgb(255, 0xC9, 0xCF, 0xD6),
        ("A-WALL", false) => Color.FromArgb(255, 0x3A, 0x41, 0x4A),
        ("A-DOOR", true) => Color.FromArgb(255, 0xFF, 0xD5, 0x4F),
        ("A-DOOR", false) => Color.FromArgb(255, 0xB0, 0x7A, 0x00),
        ("A-GLAZ", true) => Color.FromArgb(255, 0x4D, 0xD0, 0xE1),
        ("A-GLAZ", false) => Color.FromArgb(255, 0x00, 0x83, 0x8F),
        ("A-FURN", true) => Color.FromArgb(255, 0x9C, 0xCC, 0x65),
        ("A-FURN", false) => Color.FromArgb(255, 0x4E, 0x7D, 0x2A),
        ("A-ANNO-DIMS", true) => Color.FromArgb(255, 0xEF, 0x6B, 0x68),
        ("A-ANNO-DIMS", false) => Color.FromArgb(255, 0xC6, 0x28, 0x28),
        ("A-ANNO-TEXT", true) => Color.FromArgb(255, 0xE8, 0xEC, 0xF0),
        ("A-ANNO-TEXT", false) => Color.FromArgb(255, 0x1F, 0x24, 0x2B),
        ("SELECT", _) => Color.FromArgb(255, 0x3D, 0xA9, 0xF5),
        ("CENTER", true) => Color.FromArgb(255, 0xB3, 0x88, 0xFF),
        ("CENTER", false) => Color.FromArgb(255, 0x6A, 0x3D, 0xB8),
        _ => _dark ? Color.FromArgb(255, 0xE8, 0xEC, 0xF0) : Color.FromArgb(255, 0x1F, 0x24, 0x2B),
    };

    private void Render()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        RenderGrid();
        _drawing.Children.Clear();
        foreach (var group in _entities.GroupBy(e => (e.Layer, e.Fill, e.Dashed)))
        {
            if (HiddenLayers.Contains(group.Key.Layer))
            {
                continue;
            }

            var geometry = new PathGeometry();
            foreach (var entity in group)
            {
                entity.AddTo(geometry, this);
            }

            var color = LayerColor(group.Key.Layer);
            var path = new Path { Data = geometry };
            if (group.Key.Fill)
            {
                path.Fill = new SolidColorBrush(color);
            }
            else
            {
                path.Stroke = new SolidColorBrush(color);
                path.StrokeThickness = ShowLineweights && group.Key.Layer == "A-WALL" ? 2 : 1;
                if (group.Key.Dashed)
                {
                    path.StrokeDashArray = [6, 4];
                }
            }

            _drawing.Children.Add(path);
        }

        foreach (var (p, text, height, angle, layer) in _texts)
        {
            if (HiddenLayers.Contains(layer))
            {
                continue;
            }

            var size = height * _scale;
            if (size < 4)
            {
                continue;
            }

            var block = new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = new SolidColorBrush(LayerColor(layer)),
                FontFamily = new FontFamily("Arial"),
            };
            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var s = ToScreen(p.X, p.Y);
            var w = block.DesiredSize.Width;
            var h = block.DesiredSize.Height;
            Canvas.SetLeft(block, s.X - (w / 2));
            Canvas.SetTop(block, s.Y - (h / 2));
            if (angle != 0)
            {
                block.RenderTransform = new RotateTransform { Angle = -angle, CenterX = w / 2, CenterY = h / 2 };
            }

            _drawing.Children.Add(block);
        }

        RenderSelection();
        UpdateCrosshair();
    }

    private void RenderGrid()
    {
        _gridLayer.Children.Clear();
        var minor = 500.0;
        while (minor * _scale < 8)
        {
            minor *= 5;
        }

        var major = minor * 5;
        var topLeft = ToModel(new Point(0, 0));
        var bottomRight = ToModel(new Point(ActualWidth, ActualHeight));
        var minorGeometry = new PathGeometry();
        var majorGeometry = new PathGeometry();
        for (var x = Math.Floor(topLeft.X / minor) * minor; x <= bottomRight.X; x += minor)
        {
            var sx = Math.Round(ToScreen(x, 0).X) + 0.5;
            AddLine(Math.Abs(x % major) < 0.5 ? majorGeometry : minorGeometry, new Point(sx, 0), new Point(sx, ActualHeight));
        }

        for (var y = Math.Floor(bottomRight.Y / minor) * minor; y <= topLeft.Y; y += minor)
        {
            var sy = Math.Round(ToScreen(0, y).Y) + 0.5;
            AddLine(Math.Abs(y % major) < 0.5 ? majorGeometry : minorGeometry, new Point(0, sy), new Point(ActualWidth, sy));
        }

        var minorColor = _dark ? Color.FromArgb(255, 0x29, 0x31, 0x3B) : Color.FromArgb(255, 0xEC, 0xEE, 0xF1);
        var majorColor = _dark ? Color.FromArgb(255, 0x33, 0x3D, 0x49) : Color.FromArgb(255, 0xDC, 0xE0, 0xE5);
        _gridLayer.Children.Add(new Path { Data = minorGeometry, Stroke = new SolidColorBrush(minorColor), StrokeThickness = 1 });
        _gridLayer.Children.Add(new Path { Data = majorGeometry, Stroke = new SolidColorBrush(majorColor), StrokeThickness = 1 });

        // World axes through the origin (red X, green Y), like a 2D grid with axes shown.
        var origin = ToScreen(0, 0);
        _gridLayer.Children.Add(new Line { X1 = Math.Round(origin.X) + 0.5, X2 = Math.Round(origin.X) + 0.5, Y1 = 0, Y2 = ActualHeight, Stroke = new SolidColorBrush(Color.FromArgb(0x70, 0x5C, 0xB8, 0x5C)), StrokeThickness = 1 });
        _gridLayer.Children.Add(new Line { Y1 = Math.Round(origin.Y) + 0.5, Y2 = Math.Round(origin.Y) + 0.5, X1 = 0, X2 = ActualWidth, Stroke = new SolidColorBrush(Color.FromArgb(0x70, 0xE0, 0x5A, 0x5A)), StrokeThickness = 1 });
    }

    // The dining table is shown selected: dashed highlight and blue grips, as after a pick in AutoCAD.
    private void RenderSelection()
    {
        if (HiddenLayers.Contains("A-FURN"))
        {
            return;
        }

        var center = ToScreen(8600, 2500);
        var r = 600 * _scale;
        var highlight = new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Stroke = new SolidColorBrush(LayerColor("SELECT")),
            StrokeThickness = 2,
            StrokeDashArray = [3, 2],
        };
        Canvas.SetLeft(highlight, center.X - r);
        Canvas.SetTop(highlight, center.Y - r);
        _drawing.Children.Add(highlight);
        foreach (var (dx, dy) in new[] { (0.0, 0.0), (r, 0.0), (-r, 0.0), (0.0, r), (0.0, -r) })
        {
            var grip = new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 0x2E, 0x7F, 0xE0)), Stroke = new SolidColorBrush(Color.FromArgb(255, 0xBF, 0xDF, 0xFF)), StrokeThickness = 1 };
            Canvas.SetLeft(grip, center.X + dx - 4);
            Canvas.SetTop(grip, center.Y + dy - 4);
            _drawing.Children.Add(grip);
        }
    }

    private static void AddLine(PathGeometry geometry, Point a, Point b)
    {
        var figure = new PathFigure { StartPoint = a, IsClosed = false, IsFilled = false };
        figure.Segments.Add(new LineSegment { Point = b });
        geometry.Figures.Add(figure);
    }

    // ------------------------------------------------------------------ model

    private abstract record Entity(string Layer, bool Fill = false, bool Dashed = false)
    {
        public abstract void AddTo(PathGeometry geometry, CadDrawingCanvas view);
    }

    private sealed record PolyEntity(string Layer, Point[] Points, bool Closed, bool Fill = false, bool Dashed = false) : Entity(Layer, Fill, Dashed)
    {
        public override void AddTo(PathGeometry geometry, CadDrawingCanvas view)
        {
            var figure = new PathFigure { StartPoint = view.ToScreen(Points[0].X, Points[0].Y), IsClosed = Closed, IsFilled = Fill };
            foreach (var p in Points.Skip(1))
            {
                figure.Segments.Add(new LineSegment { Point = view.ToScreen(p.X, p.Y) });
            }

            geometry.Figures.Add(figure);
        }
    }

    private sealed record ArcEntity(string Layer, Point Center, double Radius, double Start, double Sweep, bool Dashed = false) : Entity(Layer, false, Dashed)
    {
        public override void AddTo(PathGeometry geometry, CadDrawingCanvas view)
        {
            Point At(double deg) => view.ToScreen(Center.X + (Radius * Math.Cos(deg * Math.PI / 180)), Center.Y + (Radius * Math.Sin(deg * Math.PI / 180)));
            var r = Radius * view._scale;
            if (Math.Abs(Sweep) >= 360)
            {
                var figure = new PathFigure { StartPoint = At(0), IsClosed = true, IsFilled = false };
                figure.Segments.Add(new ArcSegment { Point = At(180), Size = new Size(r, r), SweepDirection = SweepDirection.Counterclockwise });
                figure.Segments.Add(new ArcSegment { Point = At(360), Size = new Size(r, r), SweepDirection = SweepDirection.Counterclockwise });
                geometry.Figures.Add(figure);
                return;
            }

            var arc = new PathFigure { StartPoint = At(Start), IsClosed = false, IsFilled = false };
            arc.Segments.Add(new ArcSegment
            {
                Point = At(Start + Sweep),
                Size = new Size(r, r),
                IsLargeArc = Math.Abs(Sweep) > 180,
                // Model space is y-up, the screen y-down: a counter-clockwise model arc is clockwise on screen.
                SweepDirection = Sweep > 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
            });
            geometry.Figures.Add(arc);
        }
    }

    private void Rect(string layer, double x1, double y1, double x2, double y2, bool fill = false)
        => _entities.Add(new PolyEntity(layer, [new(x1, y1), new(x2, y1), new(x2, y2), new(x1, y2)], true, fill));

    private void Line(string layer, double x1, double y1, double x2, double y2, bool dashed = false)
        => _entities.Add(new PolyEntity(layer, [new(x1, y1), new(x2, y2)], false, false, dashed));

    private void Poly(string layer, bool closed, params double[] xy)
    {
        var points = new Point[xy.Length / 2];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new Point(xy[i * 2], xy[(i * 2) + 1]);
        }

        _entities.Add(new PolyEntity(layer, points, closed));
    }

    private void Circle(string layer, double x, double y, double r) => _entities.Add(new ArcEntity(layer, new Point(x, y), r, 0, 360));

    private void Arc(string layer, double x, double y, double r, double start, double sweep) => _entities.Add(new ArcEntity(layer, new Point(x, y), r, start, sweep));

    private void Text(string text, double x, double y, double height = 250, double angle = 0, string layer = "A-ANNO-TEXT")
        => _texts.Add((new Point(x, y), text, height, angle, layer));

    // A wall run along X or Y split by openings (start, end along the run).
    private void WallX(double y1, double y2, double x1, double x2, params (double From, double To)[] openings)
    {
        var x = x1;
        foreach (var (from, to) in openings.OrderBy(o => o.From))
        {
            Rect("A-WALL", x, y1, from, y2, fill: true);
            x = to;
        }

        Rect("A-WALL", x, y1, x2, y2, fill: true);
    }

    private void WallY(double x1, double x2, double y1, double y2, params (double From, double To)[] openings)
    {
        var y = y1;
        foreach (var (from, to) in openings.OrderBy(o => o.From))
        {
            Rect("A-WALL", x1, y, x2, from, fill: true);
            y = to;
        }

        Rect("A-WALL", x1, y, x2, y2, fill: true);
    }

    private void WindowX(double y1, double y2, double x1, double x2)
    {
        var t = (y2 - y1) / 3;
        Rect("A-GLAZ", x1, y1, x2, y2);
        Line("A-GLAZ", x1, y1 + t, x2, y1 + t);
        Line("A-GLAZ", x1, y1 + (2 * t), x2, y1 + (2 * t));
    }

    private void WindowY(double x1, double x2, double y1, double y2)
    {
        var t = (x2 - x1) / 3;
        Rect("A-GLAZ", x1, y1, x2, y2);
        Line("A-GLAZ", x1 + t, y1, x1 + t, y2);
        Line("A-GLAZ", x1 + (2 * t), y1, x1 + (2 * t), y2);
    }

    // Door hinged at (hx, hy), leaf of width w opening to angle (degrees) from the closed direction.
    private void Door(double hx, double hy, double w, double closedAngle, double sweep)
    {
        var open = (closedAngle + sweep) * Math.PI / 180;
        Line("A-DOOR", hx, hy, hx + (w * Math.Cos(open)), hy + (w * Math.Sin(open)));
        Arc("A-DOOR", hx, hy, w, closedAngle, sweep);
    }

    private void DimX(double x1, double x2, double y, double from, string? text = null)
    {
        Line("A-ANNO-DIMS", x1, from, x1, y - 150);
        Line("A-ANNO-DIMS", x2, from, x2, y - 150);
        Line("A-ANNO-DIMS", x1 - 150, y, x2 + 150, y);
        Line("A-ANNO-DIMS", x1 - 90, y - 90, x1 + 90, y + 90);
        Line("A-ANNO-DIMS", x2 - 90, y - 90, x2 + 90, y + 90);
        Text(text ?? ((int)(x2 - x1)).ToString(System.Globalization.CultureInfo.InvariantCulture), (x1 + x2) / 2, y + 190, 200, 0, "A-ANNO-DIMS");
    }

    private void DimY(double y1, double y2, double x, double from)
    {
        Line("A-ANNO-DIMS", from, y1, x - 150, y1);
        Line("A-ANNO-DIMS", from, y2, x - 150, y2);
        Line("A-ANNO-DIMS", x, y1 - 150, x, y2 + 150);
        Line("A-ANNO-DIMS", x - 90, y1 - 90, x + 90, y1 + 90);
        Line("A-ANNO-DIMS", x - 90, y2 - 90, x + 90, y2 + 90);
        Text(((int)(y2 - y1)).ToString(System.Globalization.CultureInfo.InvariantCulture), x - 190, (y1 + y2) / 2, 200, 90, "A-ANNO-DIMS");
    }

    private void BuildPlan()
    {
        const double T = 250;   // exterior wall
        const double t = 120;   // interior wall

        // Exterior walls with window and door openings.
        WallX(0, T, 0, 12000, (1200, 3200), (4000, 5500), (9000, 10000));
        WallX(8000 - T, 8000, 0, 12000, (1300, 3300), (5700, 6800), (9000, 10800));
        WallY(0, T, T, 8000 - T, (1500, 3400));
        WallY(12000 - T, 12000, T, 8000 - T, (1500, 3000), (5800, 7200));
        WindowX(0, T, 1200, 3200);
        WindowX(0, T, 4000, 5500);
        WindowX(8000 - T, 8000, 1300, 3300);
        WindowX(8000 - T, 8000, 5700, 6800);
        WindowX(8000 - T, 8000, 9000, 10800);
        WindowY(0, T, 1500, 3400);
        WindowY(12000 - T, 12000, 1500, 3000);
        WindowY(12000 - T, 12000, 5800, 7200);

        // Interior walls.
        WallX(4800 - (t / 2), 4800 + (t / 2), T, 12000 - T, (3800, 4700), (5560, 6360), (7800, 8700));
        WallY(6500 - (t / 2), 6500 + (t / 2), T, 4800 - (t / 2), (1300, 3600));
        WallY(5000 - (t / 2), 5000 + (t / 2), 4800 + (t / 2), 8000 - T);
        WallY(7500 - (t / 2), 7500 + (t / 2), 4800 + (t / 2), 8000 - T);

        // Doors.
        Door(4700, 4860, 900, 180, -90);
        Door(6360, 4860, 800, 180, -90);
        Door(7800, 4860, 900, 0, 90);
        Door(10000, T, 1000, 180, -90);

        // Living room: sofa, coffee table, armchair, TV unit, rug outline.
        Rect("A-FURN", 700, 700, 3300, 1600);
        Rect("A-FURN", 700, 700, 3300, 950);
        Line("A-FURN", 1567, 950, 1567, 1600);
        Line("A-FURN", 2433, 950, 2433, 1600);
        Rect("A-FURN", 1400, 2100, 2600, 2800);
        Rect("A-FURN", 3900, 1500, 4800, 2400);
        Rect("A-FURN", 3900, 1500, 4800, 1700);
        Rect("A-FURN", 900, 4300, 3100, 4680);
        _entities.Add(new PolyEntity("A-FURN", [new(900, 1850), new(3700, 1850), new(3700, 3500), new(900, 3500)], true, false, true));

        // Kitchen: L-shaped counter, sink, hob, dining table with chairs.
        Poly("A-FURN", true, 11150, 600, 11750, 600, 11750, 4740, 8800, 4740, 8800, 4140, 11150, 4140);
        Rect("A-FURN", 11250, 2000, 11650, 2900);
        Circle("A-FURN", 11450, 2450, 120);
        foreach (var (x, y) in new[] { (9500.0, 4300.0), (9900.0, 4300.0), (9500.0, 4580.0), (9900.0, 4580.0) })
        {
            Circle("A-FURN", x, y, 110);
        }

        Circle("A-FURN", 8600, 2500, 600);
        Rect("A-FURN", 8375, 3150, 8825, 3550);
        Rect("A-FURN", 8375, 1450, 8825, 1850);
        Rect("A-FURN", 7550, 2275, 7950, 2725);
        Rect("A-FURN", 9250, 2275, 9650, 2725);

        // Bedroom: bed with pillows, night stands, wardrobe.
        Rect("A-FURN", 900, 5500, 2600, 7600);
        Rect("A-FURN", 1050, 7050, 1650, 7450);
        Rect("A-FURN", 1850, 7050, 2450, 7450);
        Line("A-FURN", 900, 6800, 2600, 6800);
        Line("A-FURN", 900, 6800, 2600, 6400);
        Rect("A-FURN", 350, 7150, 800, 7600);
        Rect("A-FURN", 2700, 7150, 3150, 7600);
        Rect("A-FURN", 3700, 7100, 4850, 7700);
        Line("A-FURN", 3700, 7100, 4850, 7700);

        // Bathroom: tub, toilet, basin.
        Rect("A-FURN", 5150, 6950, 7300, 7700);
        Rect("A-FURN", 5300, 7050, 7150, 7600);
        Circle("A-FURN", 6900, 7325, 60);
        Rect("A-FURN", 5150, 5600, 5550, 6100);
        Arc("A-FURN", 5850, 5850, 300, 90, 180);
        Line("A-FURN", 5850, 5550, 5850, 6150);
        Rect("A-FURN", 6800, 5050, 7350, 5550);
        Circle("A-FURN", 7075, 5300, 170);

        // Second bedroom: bed, desk, chair.
        Rect("A-FURN", 9600, 5300, 11400, 7300);
        Rect("A-FURN", 9800, 6850, 10400, 7200);
        Rect("A-FURN", 10600, 6850, 11200, 7200);
        Line("A-FURN", 9600, 6600, 11400, 6600);
        Rect("A-FURN", 7700, 7100, 9000, 7700);
        Circle("A-FURN", 8350, 6750, 230);

        // Centre lines of the structural grid.
        Line("CENTER", 6500, -400, 6500, 8400, dashed: true);
        Line("CENTER", -400, 4800, 12400, 4800, dashed: true);

        // Dimensions.
        DimX(0, 12000, -1300, -300);
        DimX(0, 6500, -700, -300);
        DimX(6500, 12000, -700, -300);
        DimY(0, 8000, -1300, -300);
        DimY(0, 4800, -700, -300);
        DimY(4800, 8000, -700, -300);

        // Room names and areas.
        Text("LIVING ROOM", 3250, 3150, 230);
        Text("30.1 m²", 3250, 2830, 170);
        Text("KITCHEN / DINING", 9300, 3700, 230);
        Text("24.3 m²", 9300, 3380, 170);
        Text("BEDROOM", 2500, 6200, 230);
        Text("14.9 m²", 2500, 5880, 170);
        Text("BATH", 6250, 6550, 200);
        Text("BEDROOM 2", 9800, 5950, 200);
        Text("FLOOR PLAN  1:100", 6000, -2000, 280);
    }
}
