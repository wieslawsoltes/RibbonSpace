namespace RibbonSpace.Model;

/// <summary>
/// Rich tooltip ("ScreenTip" / "Super tooltip") with title, shortcut, description, image and help footer.
/// </summary>
public sealed class RibbonScreenTip : ObservableObject
{
    private string? _title;
    private string? _description;
    private string? _shortcut;
    private string? _helpText;
    private string? _disabledReason;
    private RibbonIcon? _image;

    /// <summary>Creates an empty ScreenTip.</summary>
    public RibbonScreenTip()
    {
    }

    /// <summary>Creates a ScreenTip.</summary>
    public RibbonScreenTip(string? title, string? description = null, string? shortcut = null)
    {
        _title = title;
        _description = description;
        _shortcut = shortcut;
    }

    /// <summary>Bold title; defaults to the item label.</summary>
    public string? Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>Explanatory body text.</summary>
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    /// <summary>Keyboard shortcut shown next to the title, e.g. "Ctrl+B".</summary>
    public string? Shortcut { get => _shortcut; set => SetProperty(ref _shortcut, value); }

    /// <summary>Footer text such as "Tell me more" or "Press F1 for help".</summary>
    public string? HelpText { get => _helpText; set => SetProperty(ref _helpText, value); }

    /// <summary>Text explaining why the command is disabled.</summary>
    public string? DisabledReason { get => _disabledReason; set => SetProperty(ref _disabledReason, value); }

    /// <summary>Optional illustration.</summary>
    public RibbonIcon? Image { get => _image; set => SetProperty(ref _image, value); }

    /// <summary>Formats the title with the shortcut, e.g. "Bold (Ctrl+B)".</summary>
    public static string FormatTitle(string? title, string? shortcut)
        => string.IsNullOrWhiteSpace(shortcut) ? title ?? string.Empty : $"{title} ({shortcut})";
}
