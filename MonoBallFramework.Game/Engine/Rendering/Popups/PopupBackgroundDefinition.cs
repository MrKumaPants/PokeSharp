using System.Text.Json.Serialization;

namespace MonoBallFramework.Game.Engine.Rendering.Popups;

/// <summary>
///     Defines a popup background style for map region/location popups.
///     Based on pokeemerald's region popup system.
///     Backgrounds are bitmap textures that fill the interior of the popup.
/// </summary>
public class PopupBackgroundDefinition
{
    /// <summary>
    ///     Unique identifier for this background style (e.g., "stone", "wood", "brick").
    /// </summary>
    [JsonPropertyName("Id")]
    public required string Id { get; init; }

    /// <summary>
    ///     Display name for this background style.
    /// </summary>
    [JsonPropertyName("DisplayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Type of rendering (always "Bitmap" for backgrounds).
    /// </summary>
    [JsonPropertyName("Type")]
    public string Type { get; init; } = "Bitmap";

    /// <summary>
    ///     Path to the background texture (relative to asset root).
    ///     Example: "Graphics/Maps/Popups/Backgrounds/stone.png"
    /// </summary>
    [JsonPropertyName("TexturePath")]
    public required string TexturePath { get; init; }

    /// <summary>
    ///     Width of the source bitmap in pixels (typically 80 for pokeemerald).
    /// </summary>
    [JsonPropertyName("Width")]
    public int Width { get; init; } = 80;

    /// <summary>
    ///     Height of the source bitmap in pixels (typically 24 for pokeemerald).
    /// </summary>
    [JsonPropertyName("Height")]
    public int Height { get; init; } = 24;

    /// <summary>
    ///     Optional description of this background style.
    /// </summary>
    [JsonPropertyName("Description")]
    public string? Description { get; init; }
}

