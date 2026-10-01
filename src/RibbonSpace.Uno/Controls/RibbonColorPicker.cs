using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;
using RibbonSpace.Theming;
using Windows.UI;

namespace RibbonSpace.Controls;

/// <summary>
/// Office color palette: Automatic, theme colors with generated shades, standard colors, recent colors,
/// No Color and More Colors. Usable standalone (toolbars, dialogs) or inside <see cref="RibbonColorPicker"/>.
/// </summary>
public partial class RibbonColorPalette : Control
{
    /// <summary>Identifies <see cref="SelectedColor"/>.</summary>
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(RibbonColorPalette), new PropertyMetadata(null, (d, _) => ((RibbonColorPalette)d).UpdateSelectionVisuals()));

    /// <summary>Identifies <see cref="ShowAutomatic"/>.</summary>
    public static readonly DependencyProperty ShowAutomaticProperty = DependencyProperty.Register(nameof(ShowAutomatic), typeof(bool), typeof(RibbonColorPalette), new PropertyMetadata(true, (d, _) => ((RibbonColorPalette)d).Build()));

    /// <summary>Identifies <see cref="AutomaticColor"/>.</summary>
    public static readonly DependencyProperty AutomaticColorProperty = DependencyProperty.Register(nameof(AutomaticColor), typeof(Color), typeof(RibbonColorPalette), new PropertyMetadata(Microsoft.UI.Colors.Black, (d, _) => ((RibbonColorPalette)d).Build()));

    /// <summary>Identifies <see cref="ShowNoColor"/>.</summary>
    public static readonly DependencyProperty ShowNoColorProperty = DependencyProperty.Register(nameof(ShowNoColor), typeof(bool), typeof(RibbonColorPalette), new PropertyMetadata(false, (d, _) => ((RibbonColorPalette)d).Build()));

    /// <summary>Identifies <see cref="ShowMoreColors"/>.</summary>
    public static readonly DependencyProperty ShowMoreColorsProperty = DependencyProperty.Register(nameof(ShowMoreColors), typeof(bool), typeof(RibbonColorPalette), new PropertyMetadata(true, (d, _) => ((RibbonColorPalette)d).Build()));

    /// <summary>Identifies <see cref="ShowThemeColors"/>.</summary>
    public static readonly DependencyProperty ShowThemeColorsProperty = DependencyProperty.Register(nameof(ShowThemeColors), typeof(bool), typeof(RibbonColorPalette), new PropertyMetadata(true, (d, _) => ((RibbonColorPalette)d).Build()));

    // Arrow keys move between swatches (XY focus); Tab enters / leaves the palette once.
    private readonly StackPanel _root = new() { Spacing = 4, Padding = new Thickness(4), XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled, TabFocusNavigation = KeyboardNavigationMode.Once };
    private readonly List<Button> _swatches = [];
    private bool _previewing;

    /// <summary>Creates a palette.</summary>
    public RibbonColorPalette()
    {
        DefaultStyleKey = typeof(RibbonColorPalette);
        RibbonTheme.EnsureResources();
        ThemeColors.CollectionChanged += (_, _) => Build();
        StandardColors.CollectionChanged += (_, _) => Build();
        RecentColors.CollectionChanged += (_, _) => Build();
        Unloaded += (_, _) => EndPreview();
        Build();
    }

    /// <summary>Raised when a color is picked (null = No Color).</summary>
    public event EventHandler<Color?>? ColorSelected;

    /// <summary>Raised while hovering swatches (live preview, null when leaving).</summary>
    public event EventHandler<Color?>? ColorPreview;

    /// <summary>Raised when "More Colors..." is clicked.</summary>
    public event EventHandler? MoreColorsRequested;

    /// <summary>Selected color.</summary>
    public Color? SelectedColor { get => (Color?)GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }

    /// <summary>Shows "Automatic".</summary>
    public bool ShowAutomatic { get => (bool)GetValue(ShowAutomaticProperty); set => SetValue(ShowAutomaticProperty, value); }

    /// <summary>Automatic color.</summary>
    public Color AutomaticColor { get => (Color)GetValue(AutomaticColorProperty); set => SetValue(AutomaticColorProperty, value); }

    /// <summary>Shows "No Color".</summary>
    public bool ShowNoColor { get => (bool)GetValue(ShowNoColorProperty); set => SetValue(ShowNoColorProperty, value); }

    /// <summary>Shows "More Colors...".</summary>
    public bool ShowMoreColors { get => (bool)GetValue(ShowMoreColorsProperty); set => SetValue(ShowMoreColorsProperty, value); }

    /// <summary>Shows the theme grid.</summary>
    public bool ShowThemeColors { get => (bool)GetValue(ShowThemeColorsProperty); set => SetValue(ShowThemeColorsProperty, value); }

    /// <summary>Theme base colors (empty = Office theme).</summary>
    public ObservableCollection<Color> ThemeColors { get; } = [];

    /// <summary>Standard colors (empty = Office standard colors).</summary>
    public ObservableCollection<Color> StandardColors { get; } = [];

    private readonly Dictionary<Color, string> _colorNames = [];

    /// <summary>
    /// Names shown in swatch tooltips and announced by screen readers (e.g. "Blue, Accent 1"). Colours without a
    /// name use their hex value.
    /// </summary>
    public IReadOnlyDictionary<Color, string> ColorNames => _colorNames;

    /// <summary>Sets (or clears with <c>null</c>) the display name of a colour.</summary>
    public void SetColorName(Color color, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            if (!_colorNames.Remove(color))
            {
                return;
            }
        }
        else if (_colorNames.TryGetValue(color, out var existing) && existing == name)
        {
            return;
        }
        else
        {
            _colorNames[color] = name;
        }

        Build();
    }

    private RibbonColorSwatch Swatch(Color color)
        => new(color.ToRibbonColor(), _colorNames.TryGetValue(color, out var name) ? name : color.ToRibbonColor().ToHex());

    /// <summary>Recent colors (maintained automatically, max 10).</summary>
    public ObservableCollection<Color> RecentColors { get; } = [];

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Host") is Border host)
        {
            if (VisualTreeHelper.GetParent(_root) is Border old)
            {
                old.Child = null;
            }

            host.Child = _root;
        }
    }

    private void Build()
    {
        var strings = RibbonStrings.Current;
        _root.Children.Clear();
        _swatches.Clear();
        if (ShowAutomatic)
        {
            _root.Children.Add(CommandRow(strings.Automatic, AutomaticColor, () => Select(AutomaticColor)));
        }

        if (ShowThemeColors)
        {
            _root.Children.Add(Header(strings.ThemeColors));
            var baseColors = ThemeColors.Count > 0
                ? ThemeColors.Select(Swatch).ToArray()
                : Palettes.OfficeThemeColors;
            var grid = Theming.RibbonColorPalette.BuildThemeGrid(baseColors);
            _root.Children.Add(SwatchRow(grid[0]));
            var shades = new StackPanel { Spacing = 0 };
            for (var r = 1; r < grid.Count; r++)
            {
                shades.Children.Add(SwatchRow(grid[r], spacing: 0));
            }

            _root.Children.Add(shades);
        }

        _root.Children.Add(Header(strings.StandardColors));
        _root.Children.Add(SwatchRow(StandardColors.Count > 0 ? StandardColors.Select(Swatch).ToArray() : Palettes.StandardColorsSwatches));
        if (RecentColors.Count > 0)
        {
            _root.Children.Add(Header(strings.RecentColors));
            _root.Children.Add(SwatchRow(RecentColors.Take(10).Select(Swatch).ToArray()));
        }

        if (ShowNoColor)
        {
            _root.Children.Add(CommandRow(strings.NoColor, null, () => Select(null), ""));
        }

        if (ShowMoreColors)
        {
            _root.Children.Add(CommandRow(strings.MoreColors, null, () => MoreColorsRequested?.Invoke(this, EventArgs.Empty), ""));
        }
    }

    private static class Palettes
    {
        public static IReadOnlyList<RibbonColorSwatch> OfficeThemeColors => Theming.RibbonColorPalette.OfficeThemeColors;

        public static IReadOnlyList<RibbonColorSwatch> StandardColorsSwatches => Theming.RibbonColorPalette.StandardColors;
    }

    /// <summary>Ends a running live preview (raises <see cref="ColorPreview"/> with null).</summary>
    public void EndPreview()
    {
        if (_previewing)
        {
            _previewing = false;
            ColorPreview?.Invoke(this, null);
        }
    }

    private void Preview(Color? color)
    {
        if (color is null && !_previewing)
        {
            return;
        }

        _previewing = color is not null;
        ColorPreview?.Invoke(this, color);
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var swatch in _swatches)
        {
            UpdateSelectionVisual(swatch);
        }
    }

    private void UpdateSelectionVisual(Button swatch)
    {
        var selected = swatch.Tag is Color color && SelectedColor == color;
        VisualStateManager.GoToState(swatch, selected ? "Selected" : "Unselected", false);
    }

    /// <summary>Focuses the selected swatch (or the first one) for keyboard use.</summary>
    public bool FocusSelected()
    {
        var target = _swatches.FirstOrDefault(b => b.Tag is Color c && SelectedColor == c) ?? _swatches.FirstOrDefault();
        return target?.Focus(FocusState.Programmatic) == true;
    }

    /// <summary>Selects a color (adds it to recent colors) and raises <see cref="ColorSelected"/>.</summary>
    public void Select(Color? color)
    {
        EndPreview();
        SelectedColor = color;
        if (color is { } c && !RecentColors.Contains(c) && !IsPaletteColor(c))
        {
            RecentColors.Insert(0, c);
            while (RecentColors.Count > 10)
            {
                RecentColors.RemoveAt(RecentColors.Count - 1);
            }
        }

        ColorSelected?.Invoke(this, color);
    }

    private bool IsPaletteColor(Color color)
    {
        var rc = color.ToRibbonColor();
        return Theming.RibbonColorPalette.StandardColors.Any(s => s.Color == rc) || Theming.RibbonColorPalette.BuildThemeGrid().SelectMany(r => r).Any(s => s.Color == rc);
    }

    private static TextBlock Header(string text) => new() { Text = text, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(2, 4, 2, 0) };

    private StackPanel SwatchRow(IReadOnlyList<RibbonColorSwatch> swatches, double spacing = 3)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing == 0 ? 3 : spacing, Margin = new Thickness(2, 0, 2, 0) };
        foreach (var swatch in swatches)
        {
            var color = swatch.Color.ToColor();
            var button = new Button
            {
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                MinWidth = 0,
                MinHeight = 0,
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Tag = color,
            };
            if (Application.Current.Resources.TryGetValue("RibbonSwatchButtonStyle", out var style))
            {
                button.Style = (Style)style;
                button.Background = new SolidColorBrush(color);
            }

            ToolTipService.SetToolTip(button, swatch.Name);
            AutomationProperties.SetName(button, swatch.Name);
            button.Click += (_, _) => Select(color);
            button.PointerEntered += (_, _) => Preview(color);
            button.PointerExited += (_, _) => Preview(null);
            button.Loaded += (s, _) => UpdateSelectionVisual((Button)s);
            _swatches.Add(button);
            row.Children.Add(button);
        }

        return row;
    }

    private static Button CommandRow(string text, Color? swatch, Action action, string? glyph = null)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if (swatch is { } c)
        {
            var box = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(c), BorderThickness = new Thickness(1) };
            RibbonTheme.SetThemeBrush(box, Border.BorderBrushProperty, "RibbonSwatchBorderBrush");
            content.Children.Add(box);
        }
        else if (glyph is not null)
        {
            content.Children.Add(new FontIcon { Glyph = glyph, FontSize = 14 });
        }

        content.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        var button = new Button { Content = content, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
        if (Application.Current.Resources.TryGetValue("RibbonMenuItemButtonStyle", out var style))
        {
            button.Style = (Style)style;
        }

        button.Click += (_, _) => action();
        return button;
    }
}

/// <summary>Office color picker split button (Font Color, Highlight, Shape Fill) with a color bar under the icon.</summary>
public partial class RibbonColorPicker : RibbonSplitButton
{
    /// <summary>Identifies <see cref="SelectedColor"/>.</summary>
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(RibbonColorPicker), new PropertyMetadata((Color?)Microsoft.UI.Colors.Red, (d, _) => ((RibbonColorPicker)d).UpdateColorBar()));

    /// <summary>Identifies <see cref="IsSplit"/>.</summary>
    public static readonly DependencyProperty IsSplitProperty = DependencyProperty.Register(nameof(IsSplit), typeof(bool), typeof(RibbonColorPicker), new PropertyMetadata(true));

    private readonly RibbonColorPalette _palette = new();
    private readonly Flyout _flyout;

    /// <summary>Creates a color picker.</summary>
    public RibbonColorPicker()
    {
        Size = RibbonItemSize.Small;
        Icon ??= "";
        _flyout = new Flyout { Content = _palette, Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
        if (Application.Current?.Resources.TryGetValue("RibbonFlyoutPresenterStyle", out var style) == true)
        {
            _flyout.FlyoutPresenterStyle = (Style)style;
        }

        Flyout = _flyout;
        _flyout.Opening += (_, _) => _palette.RequestedTheme = ActualTheme;
        _flyout.Opened += (_, _) => _palette.FocusSelected();
        _flyout.Closed += (_, _) => _palette.EndPreview();
        _palette.ColorSelected += (_, color) =>
        {
            _flyout.Hide();
            SelectedColor = color;
            ColorSelected?.Invoke(this, color);
            ExecuteCommand(color is null ? NoColorParameter : color);
        };
        _palette.ColorPreview += (_, color) => ColorPreview?.Invoke(this, color);
        _palette.MoreColorsRequested += (_, _) => ShowMoreColorsDialog();
        UpdateColorBar();
    }

    private async void ShowMoreColorsDialog()
    {
        _flyout.Hide();
        try
        {
            var color = await RibbonColorDialog.ShowAsync(XamlRoot, SelectedColor ?? Microsoft.UI.Colors.Black, ActualTheme);
            if (color is not null)
            {
                _palette.Select(color);
            }
        }
        catch (Exception ex)
        {
            // Only one ContentDialog can be open per window (and the window may be closing): never crash the app.
            System.Diagnostics.Debug.WriteLine($"RibbonColorPicker: More Colors dialog failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    protected override void InvokePrimary()
    {
        // Non-split pickers open the palette instead of applying the current color.
        if (!IsSplit)
        {
            OpenDropDown();
            return;
        }

        base.InvokePrimary();
    }

    /// <summary>Parameter passed to the command for "No Color".</summary>
    public static object NoColorParameter { get; } = new();

    /// <summary>Raised when a color is picked.</summary>
    public event EventHandler<Color?>? ColorSelected;

    /// <summary>Raised while hovering swatches (live preview).</summary>
    public event EventHandler<Color?>? ColorPreview;

    /// <summary>Selected color (null = No Color).</summary>
    public Color? SelectedColor { get => (Color?)GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }

    /// <summary>Click applies the current color (true) or opens the palette (false).</summary>
    public bool IsSplit { get => (bool)GetValue(IsSplitProperty); set => SetValue(IsSplitProperty, value); }

    /// <summary>The palette.</summary>
    public RibbonColorPalette Palette => _palette;

    /// <summary>Shows "Automatic".</summary>
    public bool ShowAutomatic { get => _palette.ShowAutomatic; set => _palette.ShowAutomatic = value; }

    /// <summary>Shows "No Color".</summary>
    public bool ShowNoColor { get => _palette.ShowNoColor; set => _palette.ShowNoColor = value; }

    /// <summary>Shows "More Colors...".</summary>
    public bool ShowMoreColors { get => _palette.ShowMoreColors; set => _palette.ShowMoreColors = value; }

    /// <summary>Automatic color.</summary>
    public Color AutomaticColor { get => _palette.AutomaticColor; set => _palette.AutomaticColor = value; }

    private void UpdateColorBar()
    {
        _palette.SelectedColor = SelectedColor;
        ColorBar = SelectedColor is { } c ? new SolidColorBrush(c) : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        CommandParameter = SelectedColor;
    }

    /// <inheritdoc />
    protected override FrameworkElement? CreateLinkedCopyCore()
    {
        var copy = new RibbonColorPicker();
        RibbonItemHelper.LinkCommon(this, copy);
        RibbonItemHelper.Link(this, copy, SelectedColorProperty, twoWay: true);
        RibbonItemHelper.Link(this, copy, IsSplitProperty);
        copy.ShowAutomatic = ShowAutomatic;
        copy.ShowNoColor = ShowNoColor;
        copy.ShowMoreColors = ShowMoreColors;
        copy.ColorSelected += (_, color) =>
        {
            ColorSelected?.Invoke(this, color);
            ExecuteCommand(color is null ? NoColorParameter : color);
        };
        copy.ColorPreview += (_, color) => ColorPreview?.Invoke(this, color);
        copy.Click += (_, _) =>
        {
            if (copy.IsSplit)
            {
                ExecuteCommand(SelectedColor);
            }
        };
        return copy;
    }
}

/// <summary>"More Colors" dialog (hue / saturation / lightness sliders, RGB hex input, preview).</summary>
public static class RibbonColorDialog
{
    /// <summary>Shows the dialog; returns the chosen color or null when cancelled.</summary>
    public static async Task<Color?> ShowAsync(XamlRoot? xamlRoot, Color initial, ElementTheme theme = ElementTheme.Default)
    {
        if (xamlRoot is null)
        {
            return null;
        }

        var strings = RibbonStrings.Current;
        var (h, s, l) = initial.ToRibbonColor().ToHsl();
        var preview = new Border { Width = 96, Height = 64, CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        RibbonTheme.SetThemeBrush(preview, Border.BorderBrushProperty, "RibbonSwatchBorderBrush");
        var original = new Border { Width = 96, Height = 64, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(initial) };
        var hue = new Slider { Header = "Hue", Minimum = 0, Maximum = 360, Value = h, Width = 260 };
        var sat = new Slider { Header = "Saturation", Minimum = 0, Maximum = 100, Value = s * 100, Width = 260 };
        var light = new Slider { Header = "Lightness", Minimum = 0, Maximum = 100, Value = l * 100, Width = 260 };
        var hex = new TextBox { Header = "Hex", Text = initial.ToRibbonColor().ToHex(), Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        var current = initial;
        var updating = false;

        void FromSliders()
        {
            if (updating) return;
            updating = true;
            current = RibbonColor.FromHsl(hue.Value, sat.Value / 100, light.Value / 100).ToColor();
            preview.Background = new SolidColorBrush(current);
            hex.Text = current.ToRibbonColor().ToHex();
            updating = false;
        }

        hue.ValueChanged += (_, _) => FromSliders();
        sat.ValueChanged += (_, _) => FromSliders();
        light.ValueChanged += (_, _) => FromSliders();
        hex.TextChanged += (_, _) =>
        {
            if (updating || !RibbonColor.TryParse(hex.Text, out var parsed)) return;
            updating = true;
            current = parsed.ToColor();
            preview.Background = new SolidColorBrush(current);
            var (ph, ps, pl) = parsed.ToHsl();
            hue.Value = ph;
            sat.Value = ps * 100;
            light.Value = pl * 100;
            updating = false;
        };
        FromSliders();

        var dialog = new ContentDialog
        {
            Title = strings.MoreColors.TrimEnd('.'),
            PrimaryButtonText = strings.Ok,
            CloseButtonText = strings.Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    hue, sat, light, hex,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children = { new StackPanel { Children = { new TextBlock { Text = "New" }, preview } }, new StackPanel { Children = { new TextBlock { Text = "Current" }, original } } } },
                },
            },
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary ? current : (Color?)null;
    }
}
