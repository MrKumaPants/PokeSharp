using Arch.Core;
using Arch.Core.Extensions;
using PokeSharp.Core.Components;

namespace PokeSharp.Core.Systems;

/// <summary>
/// System that provides tile-based collision detection for grid movement.
/// Checks TileCollider components to determine if positions are walkable.
/// </summary>
public class CollisionSystem : BaseSystem
{
    /// <inheritdoc/>
    public override int Priority => SystemPriority.Collision;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        // Collision system doesn't require per-frame updates
        // It provides on-demand collision checking via IsPositionWalkable
        EnsureInitialized();
    }

    /// <summary>
    /// Checks if a tile position is walkable (not blocked by collision).
    /// </summary>
    /// <param name="world">The game world to query.</param>
    /// <param name="tileX">The X coordinate in tile space.</param>
    /// <param name="tileY">The Y coordinate in tile space.</param>
    /// <returns>True if the position is walkable, false if blocked or no collision data exists.</returns>
    public static bool IsPositionWalkable(World world, int tileX, int tileY)
    {
        if (world == null)
        {
            return false;
        }

        // Query for entities with both TileMap and TileCollider components
        var query = new QueryDescription().WithAll<TileMap, TileCollider>();

        bool isWalkable = true; // Default to walkable if no collision data found

        world.Query(in query, (Entity entity, ref TileMap tileMap, ref TileCollider collider) =>
        {
            // Check if the position is within map bounds
            if (tileX < 0 || tileY < 0 || tileX >= tileMap.Width || tileY >= tileMap.Height)
            {
                isWalkable = false;
                return;
            }

            // Check collision using TileCollider's IsSolid method
            // IsSolid returns true if tile is solid/impassable
            if (collider.IsSolid(tileX, tileY))
            {
                isWalkable = false;
            }
        });

        return isWalkable;
    }
}
