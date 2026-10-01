using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RibbonSpace.Localization;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>Content that reports its natural width when measured with a constrained width.</summary>
public interface IRibbonScrollContent
{
    /// <summary>Width required by the content after the last measure.</summary>
    double ExtentWidth { get; }
}

/// <summary>
/// Horizontal clip-and-scroll host with discoverable scroll buttons (tab row, group row). Unlike a ScrollViewer it
/// measures its content with the real available width so adaptive panels can shrink before scrolling kicks in.
/// </summary>
public partial class RibbonScrollPanel : Panel
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(RibbonScrollPanel), new PropertyMetadata(null, (d, e) => ((RibbonScrollPanel)d).OnContentChanged((UIElement?)e.OldValue, (UIElement?)e.NewValue)));

    /// <summary>Identifies <see cref="MeasureInfinite"/>.</summary>
    public static readonly DependencyProperty MeasureInfiniteProperty = DependencyProperty.Register(nameof(MeasureInfinite), typeof(bool), typeof(RibbonScrollPanel), new PropertyMetadata(false));

    private readonly Button _left;
    private readonly Button _right;
    private double _offset;
    private double _extent;

    /// <summary>Creates the panel.</summary>
    public RibbonScrollPanel()
    {
        _left = CreateArrow("", -1);
        _right = CreateArrow("", 1);
        AutomationProperties.SetName(_left, RibbonStrings.Current.ScrollLeft);
        AutomationProperties.SetName(_right, RibbonStrings.Current.ScrollRight);
        Children.Add(_left);
        Children.Add(_right);
        PointerWheelChanged += OnWheel;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    /// <summary>Scrolled content.</summary>
    public UIElement? Content { get => (UIElement?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Measures the content with infinite width (for content that does not adapt, like the tab row).</summary>
    public bool MeasureInfinite { get => (bool)GetValue(MeasureInfiniteProperty); set => SetValue(MeasureInfiniteProperty, value); }

    /// <summary>True when content is wider than the panel.</summary>
    public bool HasOverflow { get; private set; }

    /// <summary>Horizontal offset.</summary>
    public double Offset => _offset;

    private Button CreateArrow(string glyph, int direction)
    {
        var button = new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 10 },
            Width = 22,
            MinWidth = 0,
            MinHeight = 0,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
            IsTabStop = false,
        };
        if (Application.Current?.Resources.TryGetValue("RibbonScrollButtonStyle", out var style) == true && style is Style s)
        {
            button.Style = s;
        }

        button.Click += (_, _) => ScrollBy(direction * Math.Max(120, ActualWidth * 0.6));
        return button;
    }

    private void OnContentChanged(UIElement? oldValue, UIElement? newValue)
    {
        if (oldValue is not null)
        {
            Children.Remove(oldValue);
        }

        if (newValue is not null)
        {
            Children.Insert(0, newValue);
        }

        _offset = 0;
        InvalidateMeasure();
    }

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        if (!HasOverflow)
        {
            return;
        }

        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        ScrollBy(-delta);
        e.Handled = true;
    }

    /// <summary>Scrolls by a delta.</summary>
    public void ScrollBy(double delta)
    {
        _offset = Math.Clamp(_offset + delta, 0, Math.Max(0, _extent - ActualWidth));
        InvalidateArrange();
    }

    /// <summary>Scrolls so an element of the content is visible.</summary>
    public void BringIntoView(FrameworkElement element)
    {
        if (Content is null || !HasOverflow || element.ActualWidth <= 0)
        {
            return;
        }

        try
        {
            var p = element.TransformToVisual(Content).TransformPoint(new Point(0, 0));
            if (p.X < _offset)
            {
                _offset = Math.Max(0, p.X - 24);
            }
            else if (p.X + element.ActualWidth > _offset + ActualWidth)
            {
                _offset = Math.Min(_extent - ActualWidth, p.X + element.ActualWidth - ActualWidth + 24);
            }

            InvalidateArrange();
        }
        catch (ArgumentException)
        {
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var content = Content;
        _left.Measure(availableSize);
        _right.Measure(availableSize);
        if (content is null)
        {
            return new Size(0, 0);
        }

        var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;

        content.Measure(new Size(MeasureInfinite ? double.PositiveInfinity : width, availableSize.Height));
        _extent = content is IRibbonScrollContent sc ? Math.Max(sc.ExtentWidth, content.DesiredSize.Width) : content.DesiredSize.Width;
        var desired = new Size(double.IsInfinity(width) ? _extent : Math.Min(_extent, width), content.DesiredSize.Height);
        return desired;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var content = Content;
        HasOverflow = _extent > finalSize.Width + 0.5;
        _offset = HasOverflow ? Math.Clamp(_offset, 0, _extent - finalSize.Width) : 0;
        content?.Arrange(new Rect(-_offset, 0, Math.Max(_extent, finalSize.Width), finalSize.Height));
        _left.Visibility = HasOverflow && _offset > 0.5 ? Visibility.Visible : Visibility.Collapsed;
        _right.Visibility = HasOverflow && _offset < _extent - finalSize.Width - 0.5 ? Visibility.Visible : Visibility.Collapsed;
        _left.Arrange(new Rect(0, 0, 22, finalSize.Height));
        _right.Arrange(new Rect(finalSize.Width - 22, 0, 22, finalSize.Height));
        Clip = new RectangleGeometry { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };
        return finalSize;
    }
}
