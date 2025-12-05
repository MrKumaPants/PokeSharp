using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonoBallFramework.Game.Engine.Rendering.Popups;

/// <summary>
///     Defines a popup outline/border style for map region/location popups.
///     Based on pokeemerald's region popup system.
///     Supports both tile sheet rendering (GBA-accurate) and legacy 9-slice rendering.
/// </summary>
public class PopupOutlineDefinition
{
    /// <summary>
    ///     Unique identifier for this outline style (e.g., "stone_outline", "wood_outline").
    /// </summary>
    [JsonPropertyName("Id")]
    public required string Id { get; init; }

    /// <summary>
    ///     Display name for this outline style.
    /// </summary>
    [JsonPropertyName("DisplayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Type of rendering: "TileSheet" for GBA-accurate tile-based rendering, "9Slice" for legacy 9-slice.
    /// </summary>
    [JsonPropertyName("Type")]
    public string Type { get; init; } = "TileSheet";

    /// <summary>
    ///     Path to the outline texture (relative to asset root).
    ///     Example: "Graphics/Maps/Popups/Outlines/stone_outline.png"
    /// </summary>
    [JsonPropertyName("TexturePath")]
    public required string TexturePath { get; init; }

    // === Tile Sheet Properties (GBA-accurate) ===

    /// <summary>
    ///     Width of each tile in pixels (typically 8 for GBA).
    /// </summary>
    [JsonPropertyName("TileWidth")]
    public int TileWidth { get; init; } = 8;

    /// <summary>
    ///     Height of each tile in pixels (typically 8 for GBA).
    /// </summary>
    [JsonPropertyName("TileHeight")]
    public int TileHeight { get; init; } = 8;

    /// <summary>
    ///     Total number of tiles in the tile sheet.
    /// </summary>
    [JsonPropertyName("TileCount")]
    public int TileCount { get; init; }

    /// <summary>
    ///     Array of tile definitions.
    /// </summary>
    [JsonPropertyName("Tiles")]
    public List<PopupTileDefinition>? Tiles { get; init; }

    /// <summary>
    ///     Mapping of tile indices to their usage in the frame.
    /// </summary>
    [JsonPropertyName("TileUsage")]
    public PopupTileUsage? TileUsage { get; init; }

    // === Legacy 9-Slice Properties (backwards compatibility) ===

    /// <summary>
    ///     Width of the corner slices in pixels (for legacy 9-slice rendering).
    /// </summary>
    [JsonPropertyName("cornerWidth")]
    public int CornerWidth { get; init; } = 8;

    /// <summary>
    ///     Height of the corner slices in pixels (for legacy 9-slice rendering).
    /// </summary>
    [JsonPropertyName("cornerHeight")]
    public int CornerHeight { get; init; } = 8;

    /// <summary>
    ///     Width of the border frame in pixels (for legacy 9-slice rendering).
    /// </summary>
    [JsonPropertyName("borderWidth")]
    public int BorderWidth { get; init; } = 8;

    /// <summary>
    ///     Height of the border frame in pixels (for legacy 9-slice rendering).
    /// </summary>
    [JsonPropertyName("borderHeight")]
    public int BorderHeight { get; init; } = 8;

    /// <summary>
    ///     Gets whether this outline uses tile sheet rendering.
    /// </summary>
    [JsonIgnore]
    public bool IsTileSheet => Type == "TileSheet" && Tiles != null && TileUsage != null;
}
