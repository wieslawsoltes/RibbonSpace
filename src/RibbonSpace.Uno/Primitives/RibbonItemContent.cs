using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace RibbonSpace.Controls.Primitives;

/// <summary>
/// Lays out the icon, label (optimally split on two lines for large items, like Office) and drop-down chevron of
/// a ribbon command for every <see cref="RibbonItemSize"/> and the simplified presentation.
/// </summary>
public partial class RibbonItemContent : Panel
{
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly DependencyProperty IconProperty = Register(nameof(Icon), typeof(object), null);

    /// <summary>Identifies <see cref="LargeIcon"/>.</summary>
    public static readonly DependencyProperty LargeIconProperty = Register(nameof(LargeIcon), typeof(object), null);

    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly DependencyProperty LabelProperty = Register(nameof(Label), typeof(string), null);

    /// <summary>Identifies <see cref="ItemSize"/>.</summary>
    public static readonly DependencyProperty ItemSizeProperty = Register(nameof(ItemSize), typeof(RibbonItemSize), RibbonItemSize.Medium);

    /// <summary>Identifies <see cref="ShowLabel"/>.</summary>
    public static readonly DependencyProperty ShowLabelProperty = Register(nameof(ShowLabel), typeof(bool), true);

    /// <summary>Identifies <see cref="ShowChevron"/>.</summary>
    public static readonly DependencyProperty ShowChevronProperty = Register(nameof(ShowChevron), typeof(bool), false);

    /// <summary>Identifies <see cref="IsSimplified"/>.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty = Register(nameof(IsSimplified), typeof(bool), false);

    /// <summary>Identifies <see cref="Metrics"/>.</summary>
    public static readonly DependencyProperty MetricsProperty = Register(nameof(Metrics), typeof(RibbonMetrics), RibbonMetrics.Comfortable);

    /// <summary>Identifies <see cref="Foreground"/>.</summary>
    public static readonly DependencyProperty ForegroundProperty = Register(nameof(Foreground), typeof(Brush), null);

    /// <summary>Identifies <see cref="IconForeground"/>.</summary>
    public static readonly DependencyProperty IconForegroundProperty = Register(nameof(IconForeground), typeof(Brush), null);

    /// <summary>Identifies <see cref="Part"/>.</summary>
    public static readonly DependencyProperty PartProperty = Register(nameof(Part), typeof(RibbonItemContentPart), RibbonItemContentPart.All);

    /// <summary>Identifies <see cref="ColorBar"/>.</summary>
    public static readonly DependencyProperty ColorBarProperty = Register(nameof(ColorBar), typeof(Brush), null);

    private readonly RibbonIconPresenter _icon = new();
    private readonly TextBlock _line1 = CreateText();
    private readonly TextBlock _line2 = CreateText();
    private readonly FontIcon _chevron = new() { Glyph = "", FontSize = 8, IsHitTestVisible = false };
    private readonly Microsoft.UI.Xaml.Shapes.Rectangle _colorBar = new() { Height = 4, IsHitTestVisible = false };
    private string? _splitFor;
    private double _splitFontSize;

    /// <summary>Creates the content panel.</summary>
    public RibbonItemContent()
    {
        Children.Add(_icon);
        Children.Add(_line1);
        Children.Add(_line2);
        Children.Add(_chevron);
        Children.Add(_colorBar);
        _chevron.FontFamily = Application.Current?.Resources.TryGetValue("SymbolThemeFontFamily", out var f) == true && f is FontFamily family ? family : new FontFamily("Segoe Fluent Icons");
    }

    /// <summary>Icon for small / medium sizes.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Icon for the large size (falls back to <see cref="Icon"/>).</summary>
    public object? LargeIcon { get => GetValue(LargeIconProperty); set => SetValue(LargeIconProperty, value); }

    /// <summary>Label.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Presentation size.</summary>
    public RibbonItemSize ItemSize { get => (RibbonItemSize)GetValue(ItemSizeProperty); set => SetValue(ItemSizeProperty, value); }

    /// <summary>Shows the label for medium / simplified presentations (large always shows it).</summary>
    public bool ShowLabel { get => (bool)GetValue(ShowLabelProperty); set => SetValue(ShowLabelProperty, value); }

    /// <summary>Shows a drop-down chevron.</summary>
    public bool ShowChevron { get => (bool)GetValue(ShowChevronProperty); set => SetValue(ShowChevronProperty, value); }

    /// <summary>Simplified (single line) presentation.</summary>
    public bool IsSimplified { get => (bool)GetValue(IsSimplifiedProperty); set => SetValue(IsSimplifiedProperty, value); }

    /// <summary>Metrics.</summary>
    public RibbonMetrics Metrics { get => (RibbonMetrics)GetValue(MetricsProperty); set => SetValue(MetricsProperty, value); }

    /// <summary>Text / chevron brush.</summary>
    public Brush? Foreground { get => (Brush?)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>Icon brush (falls back to <see cref="Foreground"/>).</summary>
    public Brush? IconForeground { get => (Brush?)GetValue(IconForegroundProperty); set => SetValue(IconForegroundProperty, value); }

    /// <summary>Which part of a split button this content renders.</summary>
    public RibbonItemContentPart Part { get => (RibbonItemContentPart)GetValue(PartProperty); set => SetValue(PartProperty, value); }

    /// <summary>Optional color bar below the icon (font color, highlight, fill pickers).</summary>
    public Brush? ColorBar { get => (Brush?)GetValue(ColorBarProperty); set => SetValue(ColorBarProperty, value); }

    private bool IsLarge => ItemSize == RibbonItemSize.Large && !IsSimplified;

    private static DependencyProperty Register(string name, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type, object? defaultValue)
        => DependencyProperty.Register(name, type, typeof(RibbonItemContent), new PropertyMetadata(defaultValue, OnChanged));

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var content = (RibbonItemContent)d;
        content.Sync();
        content.InvalidateMeasure();
    }

    private static TextBlock CreateText() => new()
    {
        TextWrapping = TextWrapping.NoWrap,
        TextTrimming = TextTrimming.None,
        IsHitTestVisible = false,
        IsTextScaleFactorEnabled = false,
        TextLineBounds = TextLineBounds.Full,
    };

    private void Sync()
    {
        var metrics = Metrics ?? RibbonMetrics.Comfortable;
        var large = IsLarge;
        var part = Part;
        _icon.Icon = part == RibbonItemContentPart.LabelAndChevron ? null : (large ? LargeIcon ?? Icon : Icon);
        _icon.IconSize = large ? metrics.LargeIconSize : metrics.SmallIconSize;
        _icon.Foreground = IconForeground ?? Foreground;
        _colorBar.Fill = ColorBar;
        _colorBar.Visibility = ColorBar is null || part == RibbonItemContentPart.LabelAndChevron ? Visibility.Collapsed : Visibility.Visible;
        _colorBar.Width = _icon.IconSize;
        _colorBar.Height = large ? 5 : 3;
        foreach (var text in new[] { _line1, _line2 })
        {
            text.FontSize = metrics.FontSize;
            text.Foreground = Foreground;
            text.LineHeight = metrics.FontSize + 2;
            text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        }

        if (Foreground is not null)
        {
            _chevron.Foreground = Foreground;
        }
        _chevron.FontSize = large ? 8 : 7;
        _chevron.Visibility = ShowChevron && part != RibbonItemContentPart.IconOnly ? Visibility.Visible : Visibility.Collapsed;

        bool showText;
        if (part == RibbonItemContentPart.IconOnly || string.IsNullOrEmpty(Label))
        {
            showText = false;
        }
        else if (large || !_icon.HasIcon)
        {
            showText = true;
        }
        else if (ItemSize == RibbonItemSize.Small && !IsSimplified)
        {
            showText = false;
        }
        else
        {
            showText = ShowLabel;
        }

        if (!showText)
        {
            _line1.Text = _line2.Text = string.Empty;
            _line1.Visibility = _line2.Visibility = Visibility.Collapsed;
            return;
        }

        _line1.Visibility = Visibility.Visible;
        if (!large)
        {
            _line1.Text = Label ?? string.Empty;
            _line2.Text = string.Empty;
            _line2.Visibility = Visibility.Collapsed;
            return;
        }

        _line2.Visibility = Visibility.Visible;
        if (_splitFor != Label || Math.Abs(_splitFontSize - metrics.FontSize) > 0.01 || _line1.Text.Length + _line2.Text.Length == 0)
        {
            SplitLabel(Label!);
            _splitFor = Label;
            _splitFontSize = metrics.FontSize;
        }
    }

    private void SplitLabel(string label)
    {
        var explicitBreak = label.IndexOf('\n', StringComparison.Ordinal);
        if (explicitBreak >= 0)
        {
            _line1.Text = label[..explicitBreak].Trim();
            _line2.Text = label[(explicitBreak + 1)..].Trim();
            return;
        }

        var words = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
        {
            _line1.Text = label;
            _line2.Text = string.Empty;
            return;
        }

        // Choose the split minimizing the widest line (the chevron counts on the second line).
        var best = 1;
        var bestWidth = double.MaxValue;
        var probe = CreateText();
        probe.FontSize = _line1.FontSize;
        double Width(string s)
        {
            probe.Text = s;
            probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return probe.DesiredSize.Width;
        }

        var chevron = ShowChevron ? 12 : 0;
        for (var i = 1; i < words.Length; i++)
        {
            var w = Math.Max(Width(string.Join(' ', words[..i])), Width(string.Join(' ', words[i..])) + chevron);
            if (w < bestWidth - 0.5)
            {
                bestWidth = w;
                best = i;
            }
        }

        _line1.Text = string.Join(' ', words[..best]);
        _line2.Text = string.Join(' ', words[best..]);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        Sync();
        var metrics = Metrics ?? RibbonMetrics.Comfortable;
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
        foreach (var child in Children)
        {
            child.Measure(infinite);
        }

        var iconW = _icon.HasIcon ? _icon.IconSize : 0;
        var chevronW = _chevron.Visibility == Visibility.Visible ? 10 : 0;
        var l1 = _line1.Visibility == Visibility.Visible ? _line1.DesiredSize : default;
        var l2 = _line2.Visibility == Visibility.Visible ? _line2.DesiredSize : default;
        var pad = metrics.ItemPadding;

        if (IsLarge)
        {
            var secondLine = string.IsNullOrEmpty(_line2.Text) ? chevronW : l2.Width + (chevronW > 0 ? chevronW + 2 : 0);
            var width = Math.Max(Math.Max(iconW, l1.Width), secondLine) + (pad * 2) - 2;
            if (Part == RibbonItemContentPart.IconOnly)
            {
                return new Size(Math.Max(width, metrics.LargeItemMinWidth), metrics.LargeIconSize + 6);
            }

            if (Part == RibbonItemContentPart.LabelAndChevron)
            {
                return new Size(Math.Max(width, metrics.LargeItemMinWidth), metrics.LargeItemHeight - metrics.LargeIconSize - 6);
            }

            return new Size(Math.Max(width, metrics.LargeItemMinWidth), metrics.LargeItemHeight);
        }

        var height = IsSimplified ? metrics.SimplifiedItemHeight : metrics.RowHeight;
        var textW = l1.Width > 0 ? l1.Width + (iconW > 0 ? 5 : 0) : 0;
        var w = iconW + textW + (chevronW > 0 ? chevronW + 3 : 0) + (pad * 2) - (Part == RibbonItemContentPart.IconOnly ? 1 : 0);
        return new Size(Math.Max(w, height), height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var metrics = Metrics ?? RibbonMetrics.Comfortable;
        var pad = metrics.ItemPadding;
        var iconW = _icon.HasIcon ? _icon.IconSize : 0;
        var hidden = new Rect(0, 0, 0, 0);
        if (IsLarge)
        {
            var hasBar = _colorBar.Visibility == Visibility.Visible;
            if (Part != RibbonItemContentPart.LabelAndChevron)
            {
                var iconTop = Part == RibbonItemContentPart.IconOnly ? (finalSize.Height - iconW) / 2 + 1 : 3;
                _icon.Arrange(new Rect((finalSize.Width - iconW) / 2, iconTop - (hasBar ? 2 : 0), iconW, iconW));
                _colorBar.Arrange(hasBar ? new Rect((finalSize.Width - iconW) / 2, iconTop + iconW - 3, iconW, 5) : hidden);
            }
            else
            {
                _icon.Arrange(hidden);
                _colorBar.Arrange(hidden);
            }

            var textTop = Part == RibbonItemContentPart.LabelAndChevron ? 0 : metrics.LargeIconSize + 5;
            if (Part == RibbonItemContentPart.IconOnly)
            {
                _line1.Arrange(hidden);
                _line2.Arrange(hidden);
                _chevron.Arrange(hidden);
                return finalSize;
            }

            var lineH = metrics.FontSize + 2;
            _line1.Arrange(new Rect((finalSize.Width - _line1.DesiredSize.Width) / 2, textTop, _line1.DesiredSize.Width, lineH));
            var chevronW = _chevron.Visibility == Visibility.Visible ? 10 : 0;
            if (string.IsNullOrEmpty(_line2.Text))
            {
                _line2.Arrange(hidden);
                _chevron.Arrange(chevronW > 0 ? new Rect((finalSize.Width - chevronW) / 2, textTop + lineH, chevronW, lineH) : hidden);
            }
            else
            {
                var total = _line2.DesiredSize.Width + (chevronW > 0 ? chevronW + 2 : 0);
                var x = (finalSize.Width - total) / 2;
                _line2.Arrange(new Rect(x, textTop + lineH, _line2.DesiredSize.Width, lineH));
                _chevron.Arrange(chevronW > 0 ? new Rect(x + _line2.DesiredSize.Width + 2, textTop + lineH, chevronW, lineH) : hidden);
            }

            return finalSize;
        }

        var cy = finalSize.Height / 2;
        var xPos = pad - (Part == RibbonItemContentPart.IconOnly ? 1 : 0);
        var bar = _colorBar.Visibility == Visibility.Visible;
        _icon.Arrange(iconW > 0 ? new Rect(xPos, cy - (iconW / 2) - (bar ? 1.5 : 0), iconW, iconW) : hidden);
        _colorBar.Arrange(bar ? new Rect(xPos, cy + (iconW / 2) - 2, iconW, 3) : hidden);
        xPos += iconW;
        if (_line1.Visibility == Visibility.Visible && _line1.Text.Length > 0)
        {
            xPos += iconW > 0 ? 5 : 0;
            _line1.Arrange(new Rect(xPos, cy - (_line1.DesiredSize.Height / 2), _line1.DesiredSize.Width, _line1.DesiredSize.Height));
            xPos += _line1.DesiredSize.Width;
        }
        else
        {
            _line1.Arrange(hidden);
        }

        _line2.Arrange(hidden);
        if (_chevron.Visibility == Visibility.Visible)
        {
            _chevron.Arrange(new Rect(finalSize.Width - pad - 10, cy - 5, 10, 10));
        }
        else
        {
            _chevron.Arrange(hidden);
        }

        return finalSize;
    }
}

/// <summary>Part rendered by a <see cref="RibbonItemContent"/>.</summary>
public enum RibbonItemContentPart
{
    /// <summary>Icon, label and chevron.</summary>
    All,
    /// <summary>Only the icon (upper half of a large split button, or the primary part of a small split button).</summary>
    IconOnly,
    /// <summary>Label and chevron (lower half of a large split button).</summary>
    LabelAndChevron,
}
