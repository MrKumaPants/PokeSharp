namespace PokeSharp.Rendering;

/// <summary>
///     Centralized constants for rendering configuration.
///     Contains all hardcoded values used throughout the rendering pipeline.
/// </summary>
public static class RenderingConstants
{
    /// <summary>
    ///     Default image dimensions used as fallback when actual dimensions cannot be determined.
    ///     This is used when tileset images don't have width/height metadata available.
    /// </summary>
    /// <remarks>
    ///     256x256 is chosen as a common tileset size for 16x16 tiles (16 tiles per row/column).
    ///     If your tilesets use different dimensions, they should be specified in the TMX file.
    /// </remarks>
    public const int DefaultImageWidth = 256;

    /// <summary>
    ///     Default image height used as fallback when actual dimensions cannot be determined.
    /// </summary>
    /// <seealso cref="DefaultImageWidth" />
    public const int DefaultImageHeight = 256;

    /// <summary>
    ///     Maximum Y coordinate for render distance normalization in z-ordering.
    ///     Used to calculate depth values for proper sprite layering.
    /// </summary>
    /// <remarks>
    ///     This value should be larger than any expected Y coordinate in your game world.
    ///     Sprites with Y coordinates beyond this will be clamped for rendering purposes.
    /// </remarks>
    public const float MaxRenderDistance = 10000f;

    /// <summary>
    ///     Layer index after which sprites should be rendered.
    ///     Sprites are rendered between object layers and overhead layers.
    /// </summary>
    /// <remarks>
    ///     Layer ordering:
    ///     - Layer 0: Ground
    ///     - Layer 1: Objects
    ///     - [Sprites rendered here]
    ///     - Layer 2+: Overhead (trees, roofs, etc.)
    /// </remarks>
    public const int SpriteRenderAfterLayer = 1;

    /// <summary>
    ///     Standard tile size in pixels (width and height).
    /// </summary>
    /// <remarks>
    ///     Most Pokemon-style games use 16x16 pixel tiles.
    ///     If your game uses different tile sizes, they should be specified in the TMX file.
    /// </remarks>
    public const int TileSize = 16;

    /// <summary>
    ///     Frame interval for performance logging (in frames).
    ///     Performance statistics are logged every N frames to avoid log spam.
    /// </summary>
    /// <remarks>
    ///     300 frames at 60fps = 5 seconds between log entries.
    ///     Adjust this value to log more or less frequently.
    /// </remarks>
    public const int PerformanceLogInterval = 300;

    /// <summary>
    ///     Default assets root directory name.
    ///     This is the base directory where game assets are stored.
    /// </summary>
    /// <remarks>
    ///     This default can be overridden in AssetManager constructor.
    ///     All asset paths are resolved relative to this root directory.
    /// </remarks>
    public const string DefaultAssetRoot = "Assets";
}
