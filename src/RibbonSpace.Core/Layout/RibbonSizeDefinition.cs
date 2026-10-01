namespace RibbonSpace.Layout;

/// <summary>
/// Maps the adaptive <see cref="RibbonGroupState"/> of a group to the <see cref="RibbonItemSize"/> used by an item.
/// Written in XAML as "Large, Medium, Small" (sizes used in the Large, Medium and Small group states).
/// A single value ("Large") fixes the size.
/// </summary>
public readonly record struct RibbonSizeDefinition(RibbonItemSize Large, RibbonItemSize Medium, RibbonItemSize Small)
{
    /// <summary>Default for large items: Large → Medium → Small.</summary>
    public static RibbonSizeDefinition LargeMediumSmall { get; } = new(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small);

    /// <summary>Always large (e.g. Paste, New Slide).</summary>
    public static RibbonSizeDefinition AlwaysLarge { get; } = new(RibbonItemSize.Large, RibbonItemSize.Large, RibbonItemSize.Large);

    /// <summary>Default for medium items: Medium → Small → Small.</summary>
    public static RibbonSizeDefinition MediumSmall { get; } = new(RibbonItemSize.Medium, RibbonItemSize.Small, RibbonItemSize.Small);

    /// <summary>Always medium.</summary>
    public static RibbonSizeDefinition AlwaysMedium { get; } = new(RibbonItemSize.Medium, RibbonItemSize.Medium, RibbonItemSize.Medium);

    /// <summary>Always small (icon only).</summary>
    public static RibbonSizeDefinition AlwaysSmall { get; } = new(RibbonItemSize.Small, RibbonItemSize.Small, RibbonItemSize.Small);

    /// <summary>Returns the default size definition for a preferred size.</summary>
    public static RibbonSizeDefinition ForPreferredSize(RibbonItemSize size) => size switch
    {
        RibbonItemSize.Large => LargeMediumSmall,
        RibbonItemSize.Medium => MediumSmall,
        _ => AlwaysSmall,
    };

    /// <summary>Returns the item size for a group state (Collapsed returns the Large size used inside the popup).</summary>
    public RibbonItemSize GetSize(RibbonGroupState state) => state switch
    {
        RibbonGroupState.Medium => Medium,
        RibbonGroupState.Small => Small,
        _ => Large,
    };

    /// <summary>Parses "Large", "Large,Medium" or "Large, Medium, Small" (case-insensitive; "Middle" is accepted for Medium).</summary>
    public static RibbonSizeDefinition Parse(string text)
    {
        if (!TryParse(text, out var value))
        {
            throw new FormatException($"'{text}' is not a valid size definition. Use e.g. \"Large, Medium, Small\".");
        }

        return value;
    }

    /// <summary>Tries to parse a size definition.</summary>
    public static bool TryParse(string? text, out RibbonSizeDefinition value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 3)
        {
            return false;
        }

        var sizes = new RibbonItemSize[3];
        for (var i = 0; i < 3; i++)
        {
            var part = parts[Math.Min(i, parts.Length - 1)];
            if (string.Equals(part, "Middle", StringComparison.OrdinalIgnoreCase))
            {
                part = nameof(RibbonItemSize.Medium);
            }

            if (!Enum.TryParse(part, true, out sizes[i]))
            {
                return false;
            }
        }

        value = new RibbonSizeDefinition(sizes[0], sizes[1], sizes[2]);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Large}, {Medium}, {Small}";
}
