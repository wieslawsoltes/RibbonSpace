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

    /// <summary>Identifies <see cref="ExtendedDescription"/>.</summary>
    public static readonly DependencyProperty ExtendedDescriptionProperty = DependencyProperty.Register(nameof(ExtendedDescription), typeof(string), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="ExtendedImage"/>.</summary>
    public static readonly DependencyProperty ExtendedImageProperty = DependencyProperty.Register(nameof(ExtendedImage), typeof(object), typeof(RibbonScreenTip), new PropertyMetadata(null, OnChanged));

    /// <summary>Identifies <see cref="IsExtended"/>.</summary>
    public static readonly DependencyProperty IsExtendedProperty = DependencyProperty.Register(nameof(IsExtended), typeof(bool), typeof(RibbonScreenTip), new PropertyMetadata(false, OnChanged));

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _extendTimer;

    /// <summary>Creates a ScreenTip.</summary>
    public RibbonScreenTip()
    {
        DefaultStyleKey = typeof(RibbonScreenTip);
        RibbonTheme.EnsureResources();
        Loaded += (_, _) => StartProgressiveTimer();
        Unloaded += (_, _) => StopProgressiveTimer();
    }

    /// <summary>
    /// Extended help shown after <see cref="RibbonScreenTipService.ExtendedDelay"/> while the tooltip stays open
    /// (AutoCAD-style progressive tooltip).
    /// </summary>
    public string? ExtendedDescription { get => (string?)GetValue(ExtendedDescriptionProperty); set => SetValue(ExtendedDescriptionProperty, value); }

    /// <summary>Illustration shown with the extended help (any icon description, image or element).</summary>
    public object? ExtendedImage { get => GetValue(ExtendedImageProperty); set => SetValue(ExtendedImageProperty, value); }

    /// <summary>True while the extended part is shown.</summary>
    public bool IsExtended { get => (bool)GetValue(IsExtendedProperty); set => SetValue(IsExtendedProperty, value); }

    /// <summary>True when there is extended content.</summary>
    public bool HasExtendedContent => !string.IsNullOrWhiteSpace(ExtendedDescription) || ExtendedImage is not null;

    private void StartProgressiveTimer()
    {
        StopProgressiveTimer();
        IsExtended = false;
        if (!HasExtendedContent || !RibbonScreenTipService.IsExtendedEnabled)
        {
            return;
        }

        var delay = RibbonScreenTipService.ExtendedDelay;
        if (delay <= TimeSpan.Zero || DispatcherQueue is null)
        {
            IsExtended = true;
            return;
        }

        _extendTimer = DispatcherQueue.CreateTimer();
        _extendTimer.Interval = delay;
        _extendTimer.IsRepeating = false;
        _extendTimer.Tick += (_, _) =>
        {
            StopProgressiveTimer();
            IsExtended = true;
        };
        _extendTimer.Start();
    }

    private void StopProgressiveTimer()
    {
        _extendTimer?.Stop();
        _extendTimer = null;
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

        var extended = IsExtended && HasExtendedContent;
        if (GetTemplateChild("PART_Extended") is FrameworkElement part)
        {
            part.Visibility = extended ? Visibility.Visible : Visibility.Collapsed;
        }

        SetVisible("PART_ExtendedDescription", extended ? ExtendedDescription : null);
        if (GetTemplateChild("PART_ExtendedImage") is FrameworkElement extendedImage)
        {
            extendedImage.Visibility = extended && ExtendedImage is not null ? Visibility.Visible : Visibility.Collapsed;
        }

        // Extended tooltips are wider, like AutoCAD's.
        MaxWidth = extended ? 440 : 320;
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
    /// <summary>
    /// Delay before a progressive tooltip shows its extended part (<c>ExtendedDescription</c> / <c>ExtendedImage</c>).
    /// Zero shows it immediately.
    /// </summary>
    public static TimeSpan ExtendedDelay { get; set; } = TimeSpan.FromSeconds(1.5);

    /// <summary>Enables the extended part of progressive tooltips (AutoCAD "Show extended tooltips").</summary>
    public static bool IsExtendedEnabled { get; set; } = true;

    /// <summary>
    /// Wraps tooltip content in a <see cref="ToolTip"/> with the ribbon ScreenTip look (<c>RibbonToolTipStyle</c>: theme
    /// colours and popup corners). Returns <c>null</c> for no content.
    /// </summary>
    public static ToolTip? CreateToolTip(object? content)
    {
        if (content is null || (content is string text && string.IsNullOrWhiteSpace(text)))
        {
            return null;
        }

        var tip = new ToolTip { Content = content };
        if (Application.Current?.Resources.TryGetValue("RibbonToolTipStyle", out var style) == true && style is Style tipStyle)
        {
            tip.Style = tipStyle;
        }

        return tip;
    }

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
                    ExtendedDescription = control.ExtendedDescription,
                    ExtendedImage = control.ExtendedImage,
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
                    ExtendedDescription = model.ExtendedDescription,
                    ExtendedImage = model.ExtendedImage,
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
