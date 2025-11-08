using FluentAssertions;
using PokeSharp.Core.Components.Movement;
using Xunit;

namespace PokeSharp.Tests.Components;

/// <summary>
///     Tests for Collision component.
/// </summary>
public class CollisionTests
{
    [Fact]
    public void Constructor_WithTrue_CreatesSolidCollision()
    {
        // Arrange & Act
        var collision = new Collision(true);

        // Assert
        collision.IsSolid.Should().BeTrue("entity should block movement");
    }

    [Fact]
    public void Constructor_WithFalse_CreatesPassableCollision()
    {
        // Arrange & Act
        var collision = new Collision(false);

        // Assert
        collision.IsSolid.Should().BeFalse("entity should be passable");
    }

    [Fact]
    public void IsSolid_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var collision = new Collision(true)
        {
            IsSolid = false
        };

        // Assert
        collision.IsSolid.Should().BeFalse("IsSolid should be modifiable");
    }

    [Fact]
    public void DefaultConstructor_InitializesToFalse()
    {
        // Arrange & Act
        var collision = new Collision();

        // Assert
        collision.IsSolid.Should().BeFalse("default struct initialization should be false");
    }

    [Fact]
    public void Collision_CanToggleBetweenStates()
    {
        // Arrange
        var collision = new Collision(false);

        // Act & Assert
        collision.IsSolid = true;
        collision.IsSolid.Should().BeTrue();

        collision.IsSolid = false;
        collision.IsSolid.Should().BeFalse();
    }
}
