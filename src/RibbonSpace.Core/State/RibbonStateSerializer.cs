using System.Text.Json;
using System.Text.Json.Serialization;

namespace RibbonSpace.State;

/// <summary>JSON (de)serialization of <see cref="RibbonState"/> using source generation (AOT / trimming safe).</summary>
public static class RibbonStateSerializer
{
    /// <summary>Serializes a state to JSON.</summary>
    public static string Serialize(RibbonState state, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, indented ? RibbonStateJsonContext.Indented.RibbonState : RibbonStateJsonContext.Default.RibbonState);
    }

    /// <summary>Deserializes a state. Invalid JSON or newer schemas return <c>null</c>.</summary>
    public static RibbonState? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var state = JsonSerializer.Deserialize(json, RibbonStateJsonContext.Default.RibbonState);
            if (state is null || state.SchemaVersion > RibbonState.CurrentSchemaVersion)
            {
                return null;
            }

            state.Normalize();
            state.SchemaVersion = RibbonState.CurrentSchemaVersion;
            return state;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Saves a state to a file (creating the directory). The file is replaced atomically.</summary>
    public static async Task SaveAsync(RibbonState state, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, Serialize(state), cancellationToken).ConfigureAwait(false);
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Loads a state from a file; returns <c>null</c> when missing, unreadable or invalid.</summary>
    public static async Task<RibbonState?> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return File.Exists(path) ? Deserialize(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>Source-generated JSON metadata for <see cref="RibbonState"/>.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RibbonState))]
internal sealed partial class RibbonStateJsonContext : JsonSerializerContext
{
    private static RibbonStateJsonContext? _indented;

    /// <summary>Indented context.</summary>
    public static RibbonStateJsonContext Indented => _indented ??= new RibbonStateJsonContext(new JsonSerializerOptions(Default.Options) { WriteIndented = true });
}
