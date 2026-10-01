using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RibbonSpace.Controls;

/// <summary>Rich tooltip (Office "ScreenTip"): bold title with shortcut, description, image and help footer.</summary>
public partial class RibbonScreenTip : Control
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Shortcut"/>.</summary>
    public static readonly DependencyProperty ShortcutProperty = DependencyProperty.Register(nameof(Shortcut), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="Image"/>.</summary>
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(object), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="HelpText"/>.</summary>
    public static readonly DependencyProperty HelpTextProperty = DependencyProperty.Register(nameof(HelpText), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="DisabledReason"/>.</summary>
    public static readonly DependencyProperty DisabledReasonProperty = DependencyProperty.Register(nameof(DisabledReason), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="TitleText"/>.</summary>
    public static readonly DependencyProperty TitleTextProperty = DependencyProperty.Register(nameof(TitleText), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(string.Empty));

    /// <summary>Creates a ScreenTip.</summary>
    public RibbonScreenTip()
    {
        DefaultStyleKey = typeof(RibbonScreenTip);
        RibbonTheme.EnsureResources();
    }

    /// <summary>Title (defaults to the item label).</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Shortcut.</summary>
    public string? Shortcut { get => (string?)GetValue(ShortcutProperty); set => SetValue(ShortcutProperty, value); }

    /// <summary>Description.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Illustration (any icon description).</summary>
    public object? Image { get => GetValue(ImageProperty); set => SetValue(ImageProperty, value); }

    /// <summary>Footer ("Tell me more").</summary>
    public string? HelpText { get => (string?)GetValue(HelpTextProperty); set => SetValue(HelpTextProperty, value); }

    /// <summary>Why the command is disabled.</summary>
    public string? DisabledReason { get => (string?)GetValue(DisabledReasonProperty); set => SetValue(DisabledReasonProperty, value); }

    /// <summary>Formatted title including the shortcut (template use).</summary>
    public string TitleText { get => (string)GetValue(TitleTextProperty); private set => SetValue(TitleTextProperty, value); }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RibbonScreenTip)d).Update();

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Update();
    }

    private void Update()
    {
        TitleText = Model.RibbonScreenTip.FormatTitle(Title, Shortcut);
        SetVisible("PART_Description", Description);
        SetVisible("PART_HelpText", HelpText);
        SetVisible("PART_DisabledReason", DisabledReason);
        if (GetTemplateChild("PART_Image") is FrameworkElement image)
        {
            image.Visibility = Image is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void SetVisible(string part, string? value)
    {
        if (GetTemplateChild(part) is FrameworkElement element)
        {
            element.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}

/// <summary>Builds tooltip content for ribbon items.</summary>
public static class RibbonScreenTipService
{
    /// <summary>Creates tooltip content from a label, a ScreenTip value (string / RibbonScreenTip / model) and a shortcut.</summary>
    public static object? Create(string? label, object? screenTip, string? shortcut) => Create(label, screenTip, shortcut, isEnabled: true);

    /// <summary>
    /// Creates tooltip content from a label, a ScreenTip value (string / RibbonScreenTip / model) and a shortcut. A new
    /// element is returned on every call (the declared <see cref="RibbonScreenTip"/> is used as a template and never
    /// modified), so the same ScreenTip can serve an item and its linked copies. The disabled reason is only shown when
    /// <paramref name="isEnabled"/> is false.
    /// </summary>
    public static object? Create(string? label, object? screenTip, string? shortcut, bool isEnabled)
    {
        var title = label?.Replace('\n', ' ');
        switch (screenTip)
        {
            case RibbonScreenTip control:
                return new RibbonScreenTip
                {
                    Title = string.IsNullOrEmpty(control.Title) ? title : control.Title,
                    Shortcut = string.IsNullOrEmpty(control.Shortcut) ? shortcut : control.Shortcut,
                    Description = control.Description,
                    HelpText = control.HelpText,
                    DisabledReason = isEnabled ? null : control.DisabledReason,
                    Image = control.Image,
                };
            case Model.RibbonScreenTip model:
                return new RibbonScreenTip
                {
                    Title = model.Title ?? title,
                    Shortcut = model.Shortcut ?? shortcut,
                    Description = model.Description,
                    HelpText = model.HelpText,
                    DisabledReason = isEnabled ? null : model.DisabledReason,
                    Image = model.Image,
                };
            case string description when !string.IsNullOrWhiteSpace(description):
                return new RibbonScreenTip { Title = title, Shortcut = shortcut, Description = description };
            case null when !string.IsNullOrWhiteSpace(title):
                return Model.RibbonScreenTip.FormatTitle(title, shortcut);
            default:
                return screenTip;
        }
    }
}
