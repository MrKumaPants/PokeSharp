using Arch.Core;
using PokeSharp.Core.Components;
using PokeSharp.Core.Utilities;
using Xunit;

namespace PokeSharp.Core.Tests.Utilities;

/// <summary>
///     Unit tests for the SpatialHash utility class.
/// </summary>
public class SpatialHashTests
{
    [Fact]
    public void Add_SingleEntity_ShouldBeRetrievable()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity = world.Create(new TilePosition(5, 10, 0));

        // Act
        hash.Add(entity, 0, 5, 10);

        // Assert
        var results = hash.GetAt(0, 5, 10).ToList();
        Assert.Single(results);
        Assert.Equal(entity, results[0]);
    }

    [Fact]
    public void Add_MultipleEntitiesAtSamePosition_ShouldReturnAll()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity1 = world.Create(new TilePosition(5, 10, 0));
        var entity2 = world.Create(new TilePosition(5, 10, 0));
        var entity3 = world.Create(new TilePosition(5, 10, 0));

        // Act
        hash.Add(entity1, 0, 5, 10);
        hash.Add(entity2, 0, 5, 10);
        hash.Add(entity3, 0, 5, 10);

        // Assert
        var results = hash.GetAt(0, 5, 10).ToList();
        Assert.Equal(3, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void GetAt_EmptyPosition_ShouldReturnEmpty()
    {
        // Arrange
        var hash = new SpatialHash();

        // Act
        var results = hash.GetAt(0, 5, 10).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void GetAt_DifferentMap_ShouldReturnEmpty()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity = world.Create(new TilePosition(5, 10, 0));
        hash.Add(entity, 0, 5, 10);

        // Act - query different map
        var results = hash.GetAt(1, 5, 10).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Remove_ExistingEntity_ShouldReturnTrue()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity = world.Create(new TilePosition(5, 10, 0));
        hash.Add(entity, 0, 5, 10);

        // Act
        var removed = hash.Remove(entity, 0, 5, 10);

        // Assert
        Assert.True(removed);
        Assert.Empty(hash.GetAt(0, 5, 10));
    }

    [Fact]
    public void Remove_NonExistentEntity_ShouldReturnFalse()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity = world.Create(new TilePosition(5, 10, 0));

        // Act
        var removed = hash.Remove(entity, 0, 5, 10);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Clear_WithEntities_ShouldRemoveAll()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity1 = world.Create(new TilePosition(5, 10, 0));
        var entity2 = world.Create(new TilePosition(8, 12, 0));
        hash.Add(entity1, 0, 5, 10);
        hash.Add(entity2, 0, 8, 12);

        // Act
        hash.Clear();

        // Assert
        Assert.Empty(hash.GetAt(0, 5, 10));
        Assert.Empty(hash.GetAt(0, 8, 12));
        Assert.Equal(0, hash.GetEntityCount());
    }

    [Fact]
    public void GetInBounds_ShouldReturnEntitiesInRectangle()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();

        // Create 3x3 grid of entities
        var entities = new List<Entity>();
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                var entity = world.Create(new TilePosition(x, y, 0));
                hash.Add(entity, 0, x, y);
                entities.Add(entity);
            }
        }

        // Act - get 2x2 subset
        var bounds = new Microsoft.Xna.Framework.Rectangle(0, 0, 2, 2);
        var results = hash.GetInBounds(0, bounds).ToList();

        // Assert - should get 4 entities at (0,0), (1,0), (0,1), (1,1)
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public void GetInBounds_EmptyRegion_ShouldReturnEmpty()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity = world.Create(new TilePosition(10, 10, 0));
        hash.Add(entity, 0, 10, 10);

        // Act - query different region
        var bounds = new Microsoft.Xna.Framework.Rectangle(0, 0, 5, 5);
        var results = hash.GetInBounds(0, bounds).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void GetEntityCount_ShouldReturnTotalEntities()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity1 = world.Create(new TilePosition(5, 10, 0));
        var entity2 = world.Create(new TilePosition(8, 12, 0));
        var entity3 = world.Create(new TilePosition(5, 10, 0)); // Same position as entity1

        // Act
        hash.Add(entity1, 0, 5, 10);
        hash.Add(entity2, 0, 8, 12);
        hash.Add(entity3, 0, 5, 10);

        // Assert
        Assert.Equal(3, hash.GetEntityCount());
    }

    [Fact]
    public void GetOccupiedPositionCount_ShouldReturnUniquePositions()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entity1 = world.Create(new TilePosition(5, 10, 0));
        var entity2 = world.Create(new TilePosition(8, 12, 0));
        var entity3 = world.Create(new TilePosition(5, 10, 0)); // Same position as entity1

        // Act
        hash.Add(entity1, 0, 5, 10);
        hash.Add(entity2, 0, 8, 12);
        hash.Add(entity3, 0, 5, 10);

        // Assert
        Assert.Equal(2, hash.GetOccupiedPositionCount()); // Only 2 unique positions
    }

    [Fact]
    public void MultipleMapSupport_ShouldIsolateEntities()
    {
        // Arrange
        var hash = new SpatialHash();
        var world = World.Create();
        var entityMap0 = world.Create(new TilePosition(5, 10, 0));
        var entityMap1 = world.Create(new TilePosition(5, 10, 1));

        // Act
        hash.Add(entityMap0, 0, 5, 10);
        hash.Add(entityMap1, 1, 5, 10);

        // Assert
        var resultsMap0 = hash.GetAt(0, 5, 10).ToList();
        var resultsMap1 = hash.GetAt(1, 5, 10).ToList();

        Assert.Single(resultsMap0);
        Assert.Single(resultsMap1);
        Assert.Equal(entityMap0, resultsMap0[0]);
        Assert.Equal(entityMap1, resultsMap1[0]);
    }
}
