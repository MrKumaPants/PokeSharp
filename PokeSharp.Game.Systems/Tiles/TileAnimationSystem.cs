using System;
using System.Collections.Concurrent;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeSharp.Game.Components.Tiles;
using PokeSharp.Engine.Common.Logging;
using PokeSharp.Engine.Systems.Parallel;
using EcsQueries = PokeSharp.Engine.Systems.Queries.Queries;
using PokeSharp.Engine.Systems.Management;
using PokeSharp.Engine.Core.Systems;
using static PokeSharp.Engine.Systems.Parallel.ParallelQueryExecutor;

namespace PokeSharp.Game.Systems;

/// <summary>
///     System that updates animated tile frames based on time.
///     Handles Pokemon-style tile animations (water ripples, grass swaying, flowers).
///     Priority: 850 (after Animation:800, before Render:1000).
///     Uses parallel execution for optimal performance with many animated tiles.
///     Optimized with source rectangle caching to eliminate expensive calculations.
/// </summary>
public class TileAnimationSystem(ILogger<TileAnimationSystem>? logger = null) : ParallelSystemBase, IUpdateSystem
{
    private readonly ILogger<TileAnimationSystem>? _logger = logger;
    private int _animatedTileCount = -1; // Track for logging on first update

    // Cache for source rectangles (thread-safe for parallel queries)
    // Key: (tilesetFirstGid, tileGid, tileWidth, tileHeight, tilesPerRow, spacing, margin)
    // Value: Pre-calculated Rectangle
    // Eliminates expensive division/modulo per frame change
    private readonly ConcurrentDictionary<TileRectKey, Rectangle> _sourceRectCache = new();

    /// <summary>
    /// Gets the priority for execution order. Lower values execute first.
    /// Tile animation executes at priority 850, after animation (800) and camera follow (825).
    /// </summary>
    public override int Priority => SystemPriority.TileAnimation;

    /// <inheritdoc />
    public override void Update(World world, float deltaTime)
    {
        EnsureInitialized();

        if (!Enabled)
            return;

        // Execute tile animation updates in parallel
        // Each tile is independent, making this ideal for parallel processing
        ParallelQuery<AnimatedTile, TileSprite>(
            in EcsQueries.AnimatedTiles,
            (Entity entity, ref AnimatedTile animTile, ref TileSprite sprite) =>
            {
                UpdateTileAnimation(ref animTile, ref sprite, deltaTime);
            }
        );

        // Log animated tile count on first update (sequential count for logging)
        if (_animatedTileCount < 0)
        {
            var tileCount = 0;
            world.Query(in EcsQueries.AnimatedTiles, (Entity entity) => tileCount++);

            if (tileCount > 0)
            {
                _animatedTileCount = tileCount;
                _logger?.LogAnimatedTilesProcessed(_animatedTileCount);
            }
        }
    }

    /// <summary>
    ///     Updates a single animated tile's frame timer and advances frames when needed.
    ///     Updates the TileSprite component's SourceRect to display the new frame.
    /// </summary>
    /// <param name="animTile">The animated tile data.</param>
    /// <param name="sprite">The tile sprite to update.</param>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    private void UpdateTileAnimation(
        ref AnimatedTile animTile,
        ref TileSprite sprite,
        float deltaTime
    )
    {
        // Validate animation data
        if (
            animTile.FrameTileIds == null
            || animTile.FrameTileIds.Length == 0
            || animTile.FrameDurations == null
            || animTile.FrameDurations.Length == 0
        )
            return;

        // Update frame timer
        animTile.FrameTimer += deltaTime;

        // Get current frame duration
        var currentIndex = animTile.CurrentFrameIndex;
        if (currentIndex < 0 || currentIndex >= animTile.FrameDurations.Length)
        {
            currentIndex = 0;
            animTile.CurrentFrameIndex = 0;
        }

        var currentDuration = animTile.FrameDurations[currentIndex];

        // Check if we need to advance to next frame
        if (animTile.FrameTimer >= currentDuration)
        {
            // Advance to next frame
            animTile.CurrentFrameIndex = (currentIndex + 1) % animTile.FrameTileIds.Length;
            animTile.FrameTimer = 0f;

            // Update sprite's source rectangle for the new frame (using cache)
            var newFrameTileId = animTile.FrameTileIds[animTile.CurrentFrameIndex];
            sprite.SourceRect = GetOrCalculateTileSourceRect(newFrameTileId, ref animTile);
        }
    }

    /// <summary>
    ///     Gets a cached source rectangle or calculates and caches it if not present.
    ///     Thread-safe for parallel execution.
    /// </summary>
    private Rectangle GetOrCalculateTileSourceRect(int tileGid, ref AnimatedTile animTile)
    {
        // Create cache key from all relevant tile properties
        var key = new TileRectKey(
            animTile.TilesetFirstGid,
            tileGid,
            animTile.TileWidth,
            animTile.TileHeight,
            animTile.TilesPerRow,
            animTile.TileSpacing,
            animTile.TileMargin
        );

        // Thread-safe cache lookup with lazy calculation
        return _sourceRectCache.GetOrAdd(key, static k =>
        {
            return CalculateTileSourceRect(k.TileGid, k.FirstGid, k.TileWidth, k.TileHeight,
                k.TilesPerRow, k.Spacing, k.Margin);
        });
    }

    /// <summary>
    ///     Calculates the source rectangle for a tile ID using tileset info.
    ///     This is only called once per unique tile configuration, then cached.
    /// </summary>
    private static Rectangle CalculateTileSourceRect(
        int tileGid,
        int firstGid,
        int tileWidth,
        int tileHeight,
        int tilesPerRow,
        int spacing,
        int margin
    )
    {
        if (firstGid <= 0)
            throw new InvalidOperationException("AnimatedTile missing tileset first GID.");

        var localId = tileGid - firstGid;
        if (localId < 0)
            throw new InvalidOperationException(
                $"Tile GID {tileGid} is not part of tileset starting at {firstGid}."
            );

        if (tileWidth <= 0 || tileHeight <= 0)
            throw new InvalidOperationException(
                $"AnimatedTile missing tile dimensions for TilesetFirstGid={firstGid}"
            );

        if (tilesPerRow <= 0)
            throw new InvalidOperationException(
                $"AnimatedTile missing tiles-per-row for TilesetFirstGid={firstGid}"
            );

        spacing = Math.Max(0, spacing);
        margin = Math.Max(0, margin);

        var tileX = localId % tilesPerRow;
        var tileY = localId / tilesPerRow;

        var sourceX = margin + tileX * (tileWidth + spacing);
        var sourceY = margin + tileY * (tileHeight + spacing);

        return new Rectangle(sourceX, sourceY, tileWidth, tileHeight);
    }

    /// <summary>
    ///     Declares components this system reads for parallel execution analysis.
    /// </summary>
    public override List<Type> GetReadComponents() => new()
    {
        typeof(AnimatedTile)
    };

    /// <summary>
    ///     Declares components this system writes for parallel execution analysis.
    /// </summary>
    public override List<Type> GetWriteComponents() => new()
    {
        typeof(AnimatedTile),
        typeof(TileSprite)
    };
}

/// <summary>
/// Cache key for tile source rectangles.
/// Immutable record for thread-safe dictionary key.
/// </summary>
internal readonly record struct TileRectKey(
    int FirstGid,
    int TileGid,
    int TileWidth,
    int TileHeight,
    int TilesPerRow,
    int Spacing,
    int Margin
);
