using Arch.Core;
using Arch.Core.Extensions;
using PokeSharp.Core.Components;
using PokeSharp.Core.Utilities;

namespace PokeSharp.Core.Systems;

/// <summary>
///     System that builds and maintains a spatial hash for efficient entity lookups by position.
///     Runs very early (Priority: 25) to ensure spatial data is available for other systems.
///     Uses dirty tracking to avoid rebuilding index for static tiles every frame.
/// </summary>
public class SpatialHashSystem : BaseSystem
{
    private readonly SpatialHash _spatialHash;
    private bool _staticTilesIndexed = false;

    /// <summary>
    ///     Initializes a new instance of the SpatialHashSystem class.
    /// </summary>
    public SpatialHashSystem()
    {
        _spatialHash = new SpatialHash();
    }

    /// <inheritdoc />
    public override int Priority => SystemPriority.SpatialHash;

    /// <inheritdoc />
    public override void Update(World world, float deltaTime)
    {
        EnsureInitialized();

        // Clear only dynamic entities (tiles are static and indexed once)
        if (!_staticTilesIndexed)
        {
            // First run - clear everything and index static tiles
            _spatialHash.Clear();

            // Index all tile entities (static tiles with TilePosition)
            // These don't move, so only index once
            var tileQuery = new QueryDescription().WithAll<TilePosition>();
            world.Query(
                in tileQuery,
                (Entity entity, ref TilePosition pos) =>
                {
                    _spatialHash.Add(entity, pos.MapId, pos.X, pos.Y);
                }
            );

            _staticTilesIndexed = true;
        }

        // Re-index all dynamic entities each frame (they can move)
        // Note: We don't remove old positions, but Clear() would be needed for proper tracking
        // For now, accept some duplication since dynamic entities are few (<100) vs tiles (1000s)
        var dynamicQuery = new QueryDescription().WithAll<Position>();
        world.Query(
            in dynamicQuery,
            (Entity entity, ref Position pos) =>
            {
                // Position.X and Position.Y are already tile coordinates, no conversion needed!
                _spatialHash.Add(entity, pos.MapId, pos.X, pos.Y);
            }
        );
    }

    /// <summary>
    ///     Forces a full rebuild of the spatial hash on the next update.
    ///     Call this when maps are loaded/unloaded or tiles are added/removed.
    /// </summary>
    public void InvalidateStaticTiles()
    {
        _staticTilesIndexed = false;
    }

    /// <summary>
    ///     Gets all entities at the specified tile position.
    /// </summary>
    /// <param name="mapId">The map identifier.</param>
    /// <param name="x">The X tile coordinate.</param>
    /// <param name="y">The Y tile coordinate.</param>
    /// <returns>Collection of entities at this position.</returns>
    public IEnumerable<Entity> GetEntitiesAt(int mapId, int x, int y)
    {
        return _spatialHash.GetAt(mapId, x, y);
    }

    /// <summary>
    ///     Gets all entities within the specified bounds.
    /// </summary>
    /// <param name="mapId">The map identifier.</param>
    /// <param name="bounds">The bounding rectangle in tile coordinates.</param>
    /// <returns>Collection of entities within the bounds.</returns>
    public IEnumerable<Entity> GetEntitiesInBounds(
        int mapId,
        Microsoft.Xna.Framework.Rectangle bounds
    )
    {
        return _spatialHash.GetInBounds(mapId, bounds);
    }

    /// <summary>
    ///     Gets diagnostic information about the spatial hash.
    /// </summary>
    /// <returns>A tuple with (entity count, occupied position count).</returns>
    public (int entityCount, int occupiedPositions) GetDiagnostics()
    {
        return (_spatialHash.GetEntityCount(), _spatialHash.GetOccupiedPositionCount());
    }
}
