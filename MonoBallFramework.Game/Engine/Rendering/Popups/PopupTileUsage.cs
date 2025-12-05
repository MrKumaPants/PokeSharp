using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonoBallFramework.Game.Engine.Rendering.Popups;

/// <summary>
///     Defines how tiles are used in the popup frame.
///     Based on pokeemerald's tile-based frame rendering.
/// </summary>
public class PopupTileUsage
{
    /// <summary>
    ///     Tile indices for the top edge (includes corners at start/end).
    ///     In pokeemerald: 12 tiles total.
    /// </summary>
    [JsonPropertyName("TopEdge")]
    public List<int> TopEdge { get; init; } = new();

    /// <summary>
    ///     Tile indices for the left edge (3 tiles: top, middle, bottom).
    /// </summary>
    [JsonPropertyName("LeftEdge")]
    public List<int> LeftEdge { get; init; } = new();

    /// <summary>
    ///     Tile indices for the right edge (3 tiles: top, middle, bottom).
    /// </summary>
    [JsonPropertyName("RightEdge")]
    public List<int> RightEdge { get; init; } = new();

    /// <summary>
    ///     Tile indices for the bottom edge (includes corners at start/end).
    ///     In pokeemerald: 12 tiles total.
    /// </summary>
    [JsonPropertyName("BottomEdge")]
    public List<int> BottomEdge { get; init; } = new();
}

