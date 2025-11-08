using PokeSharp.Rendering.Loaders.Tmx;

namespace PokeSharp.Rendering.Validation;

/// <summary>
///     Result of map validation containing errors and warnings.
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);

    public override string ToString()
    {
        var result = $"Valid: {IsValid}\n";
        if (Errors.Count > 0)
            result += $"Errors ({Errors.Count}):\n  - {string.Join("\n  - ", Errors)}\n";
        if (Warnings.Count > 0)
            result += $"Warnings ({Warnings.Count}):\n  - {string.Join("\n  - ", Warnings)}\n";
        return result;
    }
}

/// <summary>
///     Interface for validating Tiled map documents.
///     Implements Chain of Responsibility Pattern for composable validation.
/// </summary>
public interface IMapValidator
{
    /// <summary>
    ///     Validates a Tiled map document.
    /// </summary>
    /// <param name="map">The map to validate.</param>
    /// <param name="mapPath">Path to the map file (for relative path validation).</param>
    /// <returns>Validation result containing errors and warnings.</returns>
    ValidationResult Validate(TmxDocument map, string mapPath);
}
