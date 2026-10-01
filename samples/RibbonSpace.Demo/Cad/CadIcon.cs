using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.UI.Xaml.Markup;
using RibbonSpace.Model;

namespace RibbonSpace.Demo.Cad;

/// <summary>
/// XAML markup extension returning a CAD icon as a <see cref="RibbonIcon"/> with the right design grid:
/// <c>Icon="{cad:CadIcon Name=Line}"</c> (32-unit command icons from <see cref="CadIcons"/>) or
/// <c>ExtendedImage="{cad:CadIcon Name=LineHelp}"</c> (160-unit illustrations from <see cref="CadArt"/>).
/// </summary>
/// <remarks>
/// Layered path strings start with <c>{</c>, so they cannot be written as plain XAML attribute values, and a plain
/// string icon is drawn on the default 24-unit grid; this extension wraps the value with its view box size.
/// </remarks>
[MarkupExtensionReturnType(ReturnType = typeof(RibbonIcon))]
public sealed partial class CadIcon : MarkupExtension
{
    private static readonly Dictionary<string, RibbonIcon?> Cache = new(StringComparer.Ordinal);

    /// <summary>Property name in <see cref="CadIcons"/> or <see cref="CadArt"/>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Resolves an icon by name (null when unknown).</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CadIcons))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CadArt))]
    public static RibbonIcon? Get(string name)
    {
        if (Cache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        RibbonIcon? icon = null;
        if (typeof(CadIcons).GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is string value)
        {
            icon = CadIcons.Icon(value);
        }
        else if (typeof(CadArt).GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is string art)
        {
            icon = new RibbonIcon(RibbonIconKind.Path, art, ViewBoxSize: CadArt.SizeOf(name));
        }

        Cache[name] = icon;
        return icon;
    }

    /// <inheritdoc />
    protected override object ProvideValue() => Get(Name) ?? (object)"?";
}
