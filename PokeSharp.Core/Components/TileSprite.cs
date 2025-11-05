using Microsoft.Xna.Framework;

namespace PokeSharp.Core.Components;

/// <summary>
///     Rendering data for tile entities.
///     Contains all information needed to render a tile from a tileset.
/// </summary>
public struct TileSprite
{
    /// <summary>
    ///     Gets or sets the tileset identifier (texture asset ID).
    /// </summary>
    public string TilesetId { get; set; }

    /// <summary>
    ///     Gets or sets the global tile ID from Tiled editor.
    /// </summary>
    public int TileGid { get; set; }

    /// <summary>
    ///     Gets or sets the rendering layer for this tile.
    /// </summary>
    public TileLayer Layer { get; set; }

    /// <summary>
    ///     Gets or sets the source rectangle in the tileset texture.
    /// </summary>
    public Rectangle SourceRect { get; set; }

    /// <summary>
    ///     Initializes a new instance of the TileSprite struct.
    /// </summary>
    public TileSprite(string tilesetId, int tileGid, TileLayer layer, Rectangle sourceRect)
    {
        TilesetId = tilesetId;
        TileGid = tileGid;
        Layer = layer;
        SourceRect = sourceRect;
    }
}

/// <summary>
///     Tile rendering layers matching Tiled editor layer structure.
/// </summary>
public enum TileLayer
{
    /// <summary>
    ///     Ground layer - rendered at the back (layerDepth 0.95).
    ///     Used for floor tiles, ground terrain.
    /// </summary>
    Ground = 0,

    /// <summary>
    ///     Object layer - Y-sorted with sprites (layerDepth calculated from Y position).
    ///     Used for trees, rocks, tall grass that sprites can walk behind.
    /// </summary>
    Object = 1,

    /// <summary>
    ///     Overhead layer - rendered on top (layerDepth 0.05).
    ///     Used for roofs, overhangs, bridge tops.
    /// </summary>
    Overhead = 2,
}
