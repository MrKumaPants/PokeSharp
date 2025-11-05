using Arch.Core;
using PokeSharp.Core.Components;
using PokeSharp.Core.Systems;
using Xunit;

namespace PokeSharp.Core.Tests.Systems;

/// <summary>
///     Integration tests for the CollisionSystem with SpatialHash.
/// </summary>
public class CollisionSystemTests
{
    [Fact]
    public void IsPositionWalkable_WithSolidTile_ShouldReturnFalse()
    {
        // Arrange
        var world = World.Create();
        var spatialHashSystem = new SpatialHashSystem();
        spatialHashSystem.Initialize(world);

        // Create a solid wall tile at (5, 10)
        var wallEntity = world.Create(
            new TilePosition(5, 10, 0),
            new TileSprite("tileset", 1, TileLayer.Ground, default),
            new Collision(true) // Solid
        );

        // Build spatial hash
        spatialHashSystem.Update(world, 0.016f);

        // Act
        var isWalkable = CollisionSystem.IsPositionWalkable(spatialHashSystem, 0, 5, 10);

        // Assert
        Assert.False(isWalkable, "Position with solid tile should not be walkable");
    }

    [Fact]
    public void IsPositionWalkable_WithNonSolidTile_ShouldReturnTrue()
    {
        // Arrange
        var world = World.Create();
        var spatialHashSystem = new SpatialHashSystem();
        spatialHashSystem.Initialize(world);

        // Create a non-solid grass tile at (5, 10)
        var grassEntity = world.Create(
            new TilePosition(5, 10, 0),
            new TileSprite("tileset", 23, TileLayer.Ground, default),
            new Collision(false) // Non-solid
        );

        // Build spatial hash
        spatialHashSystem.Update(world, 0.016f);

        // Act
        var isWalkable = CollisionSystem.IsPositionWalkable(spatialHashSystem, 0, 5, 10);

        // Assert
        Assert.True(isWalkable, "Position with non-solid tile should be walkable");
    }

    [Fact]
    public void IsPositionWalkable_WithNoTile_ShouldReturnTrue()
    {
        // Arrange
        var world = World.Create();
        var spatialHashSystem = new SpatialHashSystem();
        spatialHashSystem.Initialize(world);

        // Build spatial hash (empty)
        spatialHashSystem.Update(world, 0.016f);

        // Act
        var isWalkable = CollisionSystem.IsPositionWalkable(spatialHashSystem, 0, 5, 10);

        // Assert
        Assert.True(isWalkable, "Empty position should be walkable");
    }

    [Fact]
    public void IsPositionWalkable_WithLedge_ShouldRespectDirection()
    {
        // Arrange
        var world = World.Create();
        var spatialHashSystem = new SpatialHashSystem();
        spatialHashSystem.Initialize(world);

        // Create a ledge tile that can only be jumped down
        var ledgeEntity = world.Create(
            new TilePosition(5, 10, 0),
            new TileSprite("tileset", 15, TileLayer.Object, default),
            new Collision(true),
            new TileLedge(Direction.Down) // Can jump down, can't go up
        );

        // Build spatial hash
        spatialHashSystem.Update(world, 0.016f);

        // Act & Assert
        var canJumpDown = CollisionSystem.IsPositionWalkable(
            spatialHashSystem,
            0,
            5,
            10,
            Direction.Down
        );
        var canClimbUp = CollisionSystem.IsPositionWalkable(
            spatialHashSystem,
            0,
            5,
            10,
            Direction.Up
        );

        Assert.True(canJumpDown, "Should be able to jump down ledge");
        Assert.False(canClimbUp, "Should NOT be able to climb up ledge");
    }

    [Fact]
    public void IsPositionWalkable_WithMultipleEntities_OneSolid_ShouldReturnFalse()
    {
        // Arrange
        var world = World.Create();
        var spatialHashSystem = new SpatialHashSystem();
        spatialHashSystem.Initialize(world);

        // Create multiple entities at same position
        var grassEntity = world.Create(
            new TilePosition(5, 10, 0),
            new TileSprite("tileset", 23, TileLayer.Ground, default)
        // No collision = walkable
        );

        var npcEntity = world.Create(
            new TilePosition(5, 10, 0), // Same position
            new Collision(true) // Solid NPC blocks
        );

        // Build spatial hash
        spatialHashSystem.Update(world, 0.016f);

        // Act
        var isWalkable = CollisionSystem.IsPositionWalkable(spatialHashSystem, 0, 5, 10);

        // Assert
        Assert.False(isWalkable, "Position should be blocked if ANY entity is solid");
    }
}
