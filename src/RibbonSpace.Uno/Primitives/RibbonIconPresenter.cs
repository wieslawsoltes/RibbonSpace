using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using RibbonSpace.Model;
using RibbonSpace.Theming;

namespace RibbonSpace.Controls.Primitives;

/// <summary>
/// Renders any icon description at a fixed size: glyph strings, SVG/XAML path strings ("M..."), image URIs
/// ("ms-appx:///..."), <see cref="RibbonIcon"/>, <see cref="IconSource"/>, <see cref="IconElement"/> or
/// <see cref="ImageSource"/>.
/// </summary>
public partial class RibbonIconPresenter : Grid
{
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RibbonIconPresenter), new PropertyMetadata(null, (d, _) => ((RibbonIconPresenter)d).Rebuild()));

    /// <summary>Identifies <see cref="IconSize"/>.</summary>
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(RibbonIconPresenter), new PropertyMetadata(16d, (d, _) => ((RibbonIconPresenter)d).Rebuild()));

    /// <summary>Identifies <see cref="Foreground"/>.</summary>
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(RibbonIconPresenter), new PropertyMetadata(null, (d, _) => ((RibbonIconPresenter)d).ApplyForeground()));

    private FrameworkElement? _element;
    private bool _fixedForeground;

    /// <summary>Creates a presenter.</summary>
    public RibbonIconPresenter()
    {
        IsHitTestVisible = false;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
    }

    /// <summary>Icon description.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Rendered size.</summary>
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }

    /// <summary>Icon brush.</summary>
    public Brush? Foreground { get => (Brush?)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>True when an icon is shown.</summary>
    public bool HasIcon => _element is not null;

    private void Rebuild()
    {
        // A UIElement icon hosted directly must leave the old (discarded) Viewbox first, or it could not be parented again.
        if (_element is Viewbox oldBox)
        {
            oldBox.Child = null;
        }

        Children.Clear();
        _element = null;
        _fixedForeground = false;
        var size = IconSize;
        var icon = Icon;
        if (icon is null)
        {
            Width = Height = 0;
            return;
        }

        Width = Height = size;
        _element = CreateElement(icon, size, out _fixedForeground);
        if (_element is not null)
        {
            Children.Add(_element);
        }

        ApplyForeground();
    }

    private void ApplyForeground()
    {
        if (_fixedForeground || _element is null || Foreground is null)
        {
            return;
        }

        switch (_element)
        {
            case IconElement iconElement:
                iconElement.Foreground = Foreground;
                break;
            case TextBlock text:
                text.Foreground = Foreground;
                break;
            case Viewbox { Child: TextBlock boxedText }:
                boxedText.Foreground = Foreground;
                break;
            case Viewbox { Child: IconElement boxedIcon }:
                boxedIcon.Foreground = Foreground;
                break;
            case Viewbox { Child: Canvas canvas }:
                foreach (var path in canvas.Children.OfType<Path>())
                {
                    // Layers with their own color keep it; themed layers follow the foreground.
                    if (ReferenceEquals(path.Tag, ThemedStroke))
                    {
                        path.Stroke = Foreground;
                    }
                    else if (ReferenceEquals(path.Tag, ThemedFill))
                    {
                        path.Fill = Foreground;
                    }
                }

                break;
        }
    }

    /// <summary>Classifies a string icon.</summary>
    public static RibbonIconKind Classify(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("ms-appx:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("ms-appdata:", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return RibbonIconKind.Image;
        }

        if (trimmed.Length > 6 && (trimmed[0] is 'M' or 'm' or 'F' || (trimmed[0] == '{' && trimmed.Contains('}', StringComparison.Ordinal)) || (trimmed[0] == '[' && trimmed.Contains(']', StringComparison.Ordinal))) && trimmed.Any(char.IsDigit))
        {
            return RibbonIconKind.Path;
        }

        return trimmed.Length <= 2 && trimmed.All(c => c >= '' && c <= '' || char.IsSurrogate(c)) ? RibbonIconKind.Glyph : RibbonIconKind.Text;
    }

    private static FrameworkElement? CreateElement(object icon, double size, out bool fixedForeground)
    {
        fixedForeground = false;
        switch (icon)
        {
            case string s when s.Length > 0:
                var kind = Classify(s);
                return CreateElement(kind == RibbonIconKind.Path ? new RibbonIcon(kind, s, ViewBoxSize: RibbonIconLayer.ParseViewBox(s) ?? 24) : new RibbonIcon(kind, s), size, out fixedForeground);
            case RibbonIcon ri:
                Brush? fixedBrush = null;
                if (RibbonColor.TryParse(ri.Foreground, out var color))
                {
                    fixedBrush = new SolidColorBrush(color.ToColor());
                    fixedForeground = true;
                }

                return ri.Kind switch
                {
                    RibbonIconKind.Glyph => WithBrush(new FontIcon { Glyph = ri.Value, FontSize = size, FontFamily = ri.FontFamily is null ? SymbolFont() : new FontFamily(ri.FontFamily) }, fixedBrush),
                    RibbonIconKind.Path => CreatePath(ri.Value, ri.ViewBoxSize, size, fixedBrush),
                    RibbonIconKind.Image => new Image { Source = CreateImageSource(ri.Value), Width = size, Height = size, Stretch = Stretch.Uniform },
                    _ => CreateText(ri.Value, size, fixedBrush),
                };
            case IconSource source:
                return CreateFromIconSource(source, size, out fixedForeground);
            case IconElement element:
                return CloneIconElement(element, size, out fixedForeground);
            case ImageSource imageSource:
                return new Image { Source = imageSource, Width = size, Height = size, Stretch = Stretch.Uniform };
            case FrameworkElement fe when IsFree(fe):
                fixedForeground = true;
                return new Viewbox { Width = size, Height = size, Child = fe };
            default:
                return null;
        }
    }

    private static FrameworkElement CreateText(string text, double size, Brush? brush)
    {
        var block = WithBrush(new TextBlock { Text = text, FontSize = size * 0.8, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, IsTextScaleFactorEnabled = false }, brush);
        return text.Length <= 2 ? block : new Viewbox { Width = size, Height = size, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Child = block };
    }

    private static FontFamily SymbolFont()
        => Application.Current?.Resources.TryGetValue("SymbolThemeFontFamily", out var font) == true && font is FontFamily family
            ? family
            : new FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets");

    private static T WithBrush<T>(T element, Brush? brush)
        where T : FrameworkElement
    {
        if (brush is not null)
        {
            switch (element)
            {
                case IconElement icon:
                    icon.Foreground = brush;
                    break;
                case TextBlock text:
                    text.Foreground = brush;
                    break;
            }
        }

        return element;
    }

    private static ImageSource? CreateImageSource(string uri)
    {
        // Relative paths ("Assets/cut.png") are application resources.
        var value = uri.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var absolute) && !Uri.TryCreate("ms-appx:///" + value.TrimStart('/', '\\').Replace('\\', '/'), UriKind.Absolute, out absolute))
        {
            return null;
        }

        try
        {
            return absolute.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                ? new SvgImageSource(absolute)
                : new BitmapImage(absolute);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a vector path element. <paramref name="data"/> is path mini-language data, or several layers separated by
    /// <c>|</c>, each optionally starting with a <c>{stroke=1.5;color=#E8C66E}</c> header (see
    /// <see cref="RibbonIcon.Layers"/>). Layers without a color follow the icon foreground (theme, disabled state).
    /// </summary>
    public static FrameworkElement CreatePath(string data, double viewBoxSize, double size, Brush? fill)
    {
        var canvas = new Canvas { Width = viewBoxSize, Height = viewBoxSize };
        foreach (var layer in RibbonIconLayer.Parse(data))
        {
            var path = ParsePath(layer.Data);
            if (path is null)
            {
                continue;
            }

            var layerBrush = RibbonColor.TryParse(layer.Color, out var color) ? new SolidColorBrush(color.ToColor()) : null;
            var brush = layerBrush ?? fill;
            if (layer.StrokeThickness > 0)
            {
                path.StrokeThickness = layer.StrokeThickness;
                path.StrokeStartLineCap = path.StrokeEndLineCap = PenLineCap.Round;
                path.StrokeLineJoin = PenLineJoin.Round;
                path.Fill = null;
                path.Stroke = brush;
                path.Tag = layerBrush is null ? ThemedStroke : null;
            }
            else
            {
                path.Fill = brush;
                path.Tag = layerBrush is null ? ThemedFill : null;
            }

            if (layer.Opacity < 1)
            {
                path.Opacity = layer.Opacity;
            }

            canvas.Children.Add(path);
        }

        return new Viewbox { Width = size, Height = size, Stretch = Stretch.Uniform, Child = canvas };
    }

    private static readonly object ThemedFill = new();
    private static readonly object ThemedStroke = new();

    // A UIElement can only have one parent: element icons already shown elsewhere (e.g. by the source item of a linked
    // copy) are not stolen.
    private static bool IsFree(FrameworkElement element) => VisualTreeHelper.GetParent(element) is null && element.Parent is null;


    private static Path? ParsePath(string data)
    {
        try
        {
            var escaped = System.Security.SecurityElement.Escape(data);
            return (Path)XamlReader.Load($"<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Data=\"{escaped}\" />");
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static FrameworkElement? CreateFromIconSource(IconSource source, double size, out bool fixedForeground)
    {
        fixedForeground = source.Foreground is not null;
        FrameworkElement? element = source switch
        {
            FontIconSource f => new FontIcon { Glyph = f.Glyph, FontSize = size, FontFamily = f.FontFamily ?? SymbolFont(), FontWeight = f.FontWeight },
            SymbolIconSource s => new SymbolIcon(s.Symbol),
            PathIconSource p => new PathIcon { Data = p.Data },
            BitmapIconSource b => new BitmapIcon { UriSource = b.UriSource, ShowAsMonochrome = b.ShowAsMonochrome },
            ImageIconSource i => new Image { Source = i.ImageSource, Stretch = Stretch.Uniform },
            _ => null,
        };
        if (element is null)
        {
            return null;
        }

        element.Width = element.Height = size;
        if (element is IconElement ie && source.Foreground is not null)
        {
            ie.Foreground = source.Foreground;
        }

        return element is SymbolIcon or PathIcon ? new Viewbox { Width = size, Height = size, Child = element } : element;
    }

    private static FrameworkElement? CloneIconElement(IconElement element, double size, out bool fixedForeground)
    {
        // Known icon elements are cloned (the same icon may be shown by an item and its linked copies); other icon
        // elements are hosted directly when free.
        fixedForeground = element.ReadLocalValue(IconElement.ForegroundProperty) != DependencyProperty.UnsetValue;
        IconElement? clone = element switch
        {
            FontIcon f => new FontIcon { Glyph = f.Glyph, FontSize = size, FontFamily = f.FontFamily, FontWeight = f.FontWeight, FontStyle = f.FontStyle },
            SymbolIcon s => new SymbolIcon(s.Symbol),
            PathIcon p => new PathIcon { Data = p.Data },
            BitmapIcon b => new BitmapIcon { UriSource = b.UriSource, ShowAsMonochrome = b.ShowAsMonochrome },
            ImageIcon i => new ImageIcon { Source = i.Source },
            _ => IsFree(element) ? element : null,
        };
        if (clone is null)
        {
            return null;
        }

        if (fixedForeground)
        {
            clone.Foreground = element.Foreground;
        }

        if (clone is FontIcon)
        {
            return clone;
        }

        clone.Width = clone.Height = size;
        return new Viewbox { Width = size, Height = size, Child = clone };
    }
}
