using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RibbonSpace.Controls;

/// <summary>KeyTip badge shown in KeyTip mode.</summary>
public partial class RibbonKeyTip : Control
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(RibbonKeyTip), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the KeyTip attached property (KeyTips for arbitrary elements).</summary>
    public static readonly DependencyProperty KeyTipProperty = DependencyProperty.RegisterAttached("KeyTip", typeof(string), typeof(RibbonKeyTip), new PropertyMetadata(null));

    /// <summary>Creates a badge.</summary>
    public RibbonKeyTip()
    {
        DefaultStyleKey = typeof(RibbonKeyTip);
        IsHitTestVisible = false;
        IsTabStop = false;
    }

    /// <summary>KeyTip text.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Target element.</summary>
    public FrameworkElement? Target { get; internal set; }

    /// <summary>Gets the attached KeyTip.</summary>
    public static string? GetKeyTip(DependencyObject element) => (string?)element.GetValue(KeyTipProperty);

    /// <summary>Sets the attached KeyTip.</summary>
    public static void SetKeyTip(DependencyObject element, string? value) => element.SetValue(KeyTipProperty, value);
}
