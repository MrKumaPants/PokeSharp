using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokeSharp.Core.Components;
using PokeSharp.Core.Systems;
using PokeSharp.Rendering.Assets;
using PokeSharp.Rendering.Components;

namespace PokeSharp.Rendering.Systems;

/// <summary>
///     Unified rendering system that renders tile layers in Tiled's order, with sprites
///     Y-sorted alongside the object layer. This follows standard Tiled conventions:
///     1. Render ground layer
///     2. Y-sort and render object layer tiles + all sprites together
///     3. Render overhead layer (naturally appears on top due to layer order)
/// </summary>
public class ZOrderRenderSystem : BaseSystem
{
    private const int TileSize = 16;
    private const float MaxRenderDistance = 10000f; // Maximum Y coordinate for normalization

    // Layer indices where sprites should be rendered (between object and overhead layers)
    private const int SpriteRenderAfterLayer = 1; // Render sprites after layer index 1 (Objects)
    private readonly AssetManager _assetManager;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ILogger<ZOrderRenderSystem>? _logger;
    private readonly SpriteBatch _spriteBatch;
    private ulong _frameCounter;

    /// <summary>
    ///     Initializes a new instance of the ZOrderRenderSystem class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    /// <param name="assetManager">Asset manager for texture loading.</param>
    /// <param name="logger">Optional logger for debug output.</param>
    public ZOrderRenderSystem(
        GraphicsDevice graphicsDevice,
        AssetManager assetManager,
        ILogger<ZOrderRenderSystem>? logger = null
    )
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _logger = logger;
    }

    /// <inheritdoc />
    public override int Priority => SystemPriority.Render;

    /// <inheritdoc />
    public override void Update(World world, float deltaTime)
    {
        try
        {
            EnsureInitialized();
            _frameCounter++;

            _logger?.LogDebug("═══════════════════════════════════════════════════");
            _logger?.LogDebug(
                "ZOrderRenderSystem - Frame {FrameCounter} - Starting Z-order rendering",
                _frameCounter
            );
            _logger?.LogDebug("═══════════════════════════════════════════════════");

            // Get camera transform matrix (if camera exists)
            var cameraTransform = Matrix.Identity;
            var cameraQuery = new QueryDescription().WithAll<Player, Camera>();
            world.Query(
                in cameraQuery,
                (ref Camera camera) =>
                {
                    cameraTransform = camera.GetTransformMatrix();
                    _logger?.LogDebug(
                        "Camera transform applied: Position=({X:F1}, {Y:F1}), Zoom={Zoom:F2}x",
                        camera.Position.X,
                        camera.Position.Y,
                        camera.Zoom
                    );
                }
            );

            // Begin sprite batch with BackToFront sorting for proper Z-ordering
            _spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                transformMatrix: cameraTransform
            );

            _logger?.LogDebug("SpriteBatch started (BackToFront, AlphaBlend, PointClamp)");

            // Render tile entities by layer
            var totalTilesRendered = 0;

            // Layer 0: Ground layer (flat rendering)
            _logger?.LogDebug("Rendering GROUND layer (flat, at back)...");
            totalTilesRendered += RenderTileLayer(world, TileLayer.Ground);

            // Layer 1: Object layer (Y-sorted with sprites)
            _logger?.LogDebug("Rendering OBJECT layer (Y-sorted with sprites)...");
            totalTilesRendered += RenderTileLayer(world, TileLayer.Object);

            // Render all sprites (player, NPCs, objects)
            var spriteCount = 0;

            // Query for entities WITH GridMovement (moving entities)
            var movingSpriteQuery = new QueryDescription().WithAll<
                Position,
                Sprite,
                GridMovement
            >();
            world.Query(
                in movingSpriteQuery,
                (ref Position position, ref Sprite sprite, ref GridMovement movement) =>
                {
                    spriteCount++;
                    RenderMovingSprite(ref position, ref sprite, ref movement);
                }
            );

            // Query for entities WITHOUT GridMovement (static sprites)
            var staticSpriteQuery = new QueryDescription()
                .WithAll<Position, Sprite>()
                .WithNone<GridMovement>();
            world.Query(
                in staticSpriteQuery,
                (ref Position position, ref Sprite sprite) =>
                {
                    spriteCount++;
                    RenderStaticSprite(ref position, ref sprite);
                }
            );

            _logger?.LogDebug("Rendered {SpriteCount} sprites", spriteCount);

            // Layer 2: Overhead layer (flat rendering on top)
            _logger?.LogDebug("Rendering OVERHEAD layer (flat, on top)...");
            totalTilesRendered += RenderTileLayer(world, TileLayer.Overhead);

            _logger?.LogDebug("Total tiles rendered: {TotalTiles}", totalTilesRendered);

            // End sprite batch
            _spriteBatch.End();

            _logger?.LogDebug("───────────────────────────────────────────────────");
            _logger?.LogDebug(
                "ZOrderRenderSystem - Frame {FrameCounter} - Completed",
                _frameCounter
            );
            _logger?.LogDebug("═══════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "❌ CRITICAL ERROR in ZOrderRenderSystem.Update (Frame {FrameCounter})",
                _frameCounter
            );
            throw;
        }
    }

    private int RenderTileLayer(World world, TileLayer layer)
    {
        var tilesRendered = 0;
        var tilesCulled = 0;

        try
        {
            // Get camera bounds for culling
            var cameraBounds = GetCameraBoundsInTiles(world);

            // Query all tile entities for this layer
            var tileQuery = new QueryDescription().WithAll<TilePosition, TileSprite>();

            world.Query(
                in tileQuery,
                (ref TilePosition pos, ref TileSprite sprite) =>
                {
                    // Filter by layer
                    if (sprite.Layer != layer)
                        return;

                    // Viewport culling: skip tiles outside camera bounds
                    if (cameraBounds.HasValue)
                    {
                        if (
                            pos.X < cameraBounds.Value.Left
                            || pos.X >= cameraBounds.Value.Right
                            || pos.Y < cameraBounds.Value.Top
                            || pos.Y >= cameraBounds.Value.Bottom
                        )
                        {
                            tilesCulled++;
                            return;
                        }
                    }

                    // Get tileset texture
                    if (!_assetManager.HasTexture(sprite.TilesetId))
                    {
                        if (tilesRendered == 0) // Only warn once per layer
                        {
                            _logger?.LogWarning(
                                "  ⚠️  Tileset '{TilesetId}' NOT FOUND - skipping tiles",
                                sprite.TilesetId
                            );
                        }
                        return;
                    }

                    var texture = _assetManager.GetTexture(sprite.TilesetId);
                    var position = new Vector2(pos.X * TileSize, pos.Y * TileSize);

                    // Calculate layer depth based on layer type
                    float layerDepth = layer switch
                    {
                        TileLayer.Ground => 0.95f, // Back
                        TileLayer.Object => CalculateYSortDepth(position.Y + TileSize), // Y-sorted
                        TileLayer.Overhead => 0.05f, // Front
                        _ => 0.5f,
                    };

                    // Render tile
                    _spriteBatch.Draw(
                        texture,
                        position,
                        sprite.SourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        layerDepth
                    );

                    tilesRendered++;
                }
            );

            _logger?.LogDebug(
                "  Rendered {Count} tiles for {Layer} layer (culled {Culled})",
                tilesRendered,
                layer,
                tilesCulled
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "  ❌ ERROR rendering {Layer} layer", layer);
        }

        return tilesRendered;
    }

    /// <summary>
    ///     Gets the camera viewport bounds in tile coordinates for culling.
    ///     Expands bounds slightly to handle edge cases.
    /// </summary>
    private Rectangle? GetCameraBoundsInTiles(World world)
    {
        // Query for camera
        var cameraQuery = new QueryDescription().WithAll<Player, Camera>();
        Rectangle? bounds = null;

        world.Query(
            in cameraQuery,
            (ref Camera camera) =>
            {
                // Convert camera viewport from pixel to tile coordinates
                // Add margin of 2 tiles on each side to handle edge rendering
                const int margin = 2;

                int left =
                    (int)(camera.Position.X / TileSize)
                    - (camera.Viewport.Width / 2 / TileSize) / (int)camera.Zoom
                    - margin;
                int top =
                    (int)(camera.Position.Y / TileSize)
                    - (camera.Viewport.Height / 2 / TileSize) / (int)camera.Zoom
                    - margin;
                int width = (camera.Viewport.Width / TileSize) / (int)camera.Zoom + margin * 2;
                int height = (camera.Viewport.Height / TileSize) / (int)camera.Zoom + margin * 2;

                bounds = new Rectangle(left, top, width, height);
            }
        );

        return bounds;
    }

    private void RenderMovingSprite(
        ref Position position,
        ref Sprite sprite,
        ref GridMovement movement
    )
    {
        try
        {
            // Get texture from AssetManager
            if (!_assetManager.HasTexture(sprite.TextureId))
            {
                _logger?.LogWarning(
                    "    ⚠️  Texture '{TextureId}' NOT FOUND in AssetManager - skipping sprite",
                    sprite.TextureId
                );
                return;
            }

            var texture = _assetManager.GetTexture(sprite.TextureId);

            // Determine source rectangle
            var sourceRect = sprite.SourceRect;
            if (sourceRect.IsEmpty)
                sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);

            // Calculate render position (visual interpolated position)
            var renderPosition = new Vector2(position.PixelX, position.PixelY);

            // BEST PRACTICE FOR MOVING ENTITIES: Use TARGET position for depth sorting
            // When moving/jumping, sort based on where the entity is going, not where they started.
            // This prevents flickering and ensures entities sort correctly during movement.
            // For example, when jumping over a fence, the player should sort as if they're
            // already on the other side of the fence.
            float groundY;
            if (movement.IsMoving)
            {
                // Use target grid position for sorting
                var targetGridY = (int)(movement.TargetPosition.Y / TileSize);
                groundY = (targetGridY + 1) * TileSize; // +1 for bottom of tile
            }
            else
            {
                // Use current grid position
                groundY = (position.Y + 1) * TileSize;
            }

            var layerDepth = CalculateYSortDepth(groundY);

            // Draw sprite
            _spriteBatch.Draw(
                texture,
                renderPosition,
                sourceRect,
                sprite.Tint,
                sprite.Rotation,
                sprite.Origin,
                sprite.Scale,
                SpriteEffects.None,
                layerDepth
            );

            _logger?.LogDebug(
                "    Moving Sprite: TextureId='{TextureId}', RenderPos=({X:F1},{Y:F1}), GroundY={GroundY:F1}, LayerDepth={LayerDepth:F4}",
                sprite.TextureId,
                position.PixelX,
                position.PixelY,
                groundY,
                layerDepth
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "    ❌ ERROR rendering moving sprite with TextureId '{TextureId}' at position ({X}, {Y})",
                sprite.TextureId,
                position.PixelX,
                position.PixelY
            );
        }
    }

    private void RenderStaticSprite(ref Position position, ref Sprite sprite)
    {
        try
        {
            // Get texture from AssetManager
            if (!_assetManager.HasTexture(sprite.TextureId))
            {
                _logger?.LogWarning(
                    "    ⚠️  Texture '{TextureId}' NOT FOUND in AssetManager - skipping sprite",
                    sprite.TextureId
                );
                return;
            }

            var texture = _assetManager.GetTexture(sprite.TextureId);

            // Determine source rectangle
            var sourceRect = sprite.SourceRect;
            if (sourceRect.IsEmpty)
                sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);

            // Calculate render position
            var renderPosition = new Vector2(position.PixelX, position.PixelY);

            // BEST PRACTICE: Calculate layer depth based on entity's GRID position, not visual pixel position.
            // This ensures:
            // 1. Sprites sort correctly even during movement/jumping animations
            // 2. Sort order doesn't change mid-movement (would cause flickering)
            // 3. Entities sort based on which grid tile they occupy, not their interpolated visual position
            //
            // The grid position represents where the entity logically is for gameplay purposes.
            // The pixel position is just the visual interpolation for smooth movement.
            // For a 16x16 tile grid, the entity's ground Y is at the bottom of their grid tile.
            float groundY = (position.Y + 1) * TileSize; // +1 because we want bottom of tile
            var layerDepth = CalculateYSortDepth(groundY);

            // Draw sprite
            _spriteBatch.Draw(
                texture,
                renderPosition,
                sourceRect,
                sprite.Tint,
                sprite.Rotation,
                sprite.Origin,
                sprite.Scale,
                SpriteEffects.None,
                layerDepth
            );

            _logger?.LogDebug(
                "    Sprite: TextureId='{TextureId}', RenderPos=({X:F1},{Y:F1}), GroundY={GroundY:F1}, LayerDepth={LayerDepth:F4}",
                sprite.TextureId,
                position.PixelX,
                position.PixelY,
                groundY,
                layerDepth
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "    ❌ ERROR rendering sprite with TextureId '{TextureId}' at position ({X}, {Y})",
                sprite.TextureId,
                position.PixelX,
                position.PixelY
            );
        }
    }

    /// <summary>
    ///     Calculates layer depth for Y-sorting within the object layer range (0.4-0.6).
    ///     Lower Y positions (top of screen) get higher layer depth (render first/behind).
    ///     Higher Y positions (bottom of screen) get lower layer depth (render last/in front).
    ///     This range allows object tiles and sprites to sort together while staying between
    ///     ground layer (0.95) and overhead layer (0.05).
    /// </summary>
    /// <param name="yPosition">The Y position (typically bottom of sprite/tile).</param>
    /// <returns>Layer depth value between 0.4 (front) and 0.6 (back) for Y-sorting.</returns>
    private static float CalculateYSortDepth(float yPosition)
    {
        // Normalize Y position to 0.0-1.0 range
        var normalized = yPosition / MaxRenderDistance;

        // Map to Y-sort range: 0.6 (back/top) to 0.4 (front/bottom)
        // Lower Y = 0.6, Higher Y = 0.4
        var layerDepth = 0.6f - normalized * 0.2f;

        // Clamp to Y-sort range
        return MathHelper.Clamp(layerDepth, 0.4f, 0.6f);
    }
}
