using FluentAssertions;
using PokeSharp.Core.Components.Movement;
using Xunit;

namespace PokeSharp.Tests.Components;

/// <summary>
///     Tests for Direction enum and extension methods.
/// </summary>
public class DirectionTests
{
    [Theory]
    [InlineData(Direction.Down, 0, 1)]
    [InlineData(Direction.Left, -1, 0)]
    [InlineData(Direction.Right, 1, 0)]
    [InlineData(Direction.Up, 0, -1)]
    [InlineData(Direction.None, 0, 0)]
    public void ToTileDelta_ReturnsCorrectMovementVector(Direction direction, int expectedX, int expectedY)
    {
        // Act
        var (deltaX, deltaY) = direction.ToTileDelta();

        // Assert
        deltaX.Should().Be(expectedX);
        deltaY.Should().Be(expectedY);
    }

    [Theory]
    [InlineData(Direction.Down, "walk_down")]
    [InlineData(Direction.Left, "walk_left")]
    [InlineData(Direction.Right, "walk_right")]
    [InlineData(Direction.Up, "walk_up")]
    [InlineData(Direction.None, "walk_down")]
    public void ToWalkAnimation_ReturnsCorrectAnimationName(Direction direction, string expected)
    {
        // Act
        var animationName = direction.ToWalkAnimation();

        // Assert
        animationName.Should().Be(expected);
    }

    [Theory]
    [InlineData(Direction.Down, "idle_down")]
    [InlineData(Direction.Left, "idle_left")]
    [InlineData(Direction.Right, "idle_right")]
    [InlineData(Direction.Up, "idle_up")]
    [InlineData(Direction.None, "idle_down")]
    public void ToIdleAnimation_ReturnsCorrectAnimationName(Direction direction, string expected)
    {
        // Act
        var animationName = direction.ToIdleAnimation();

        // Assert
        animationName.Should().Be(expected);
    }

    [Theory]
    [InlineData(Direction.Down, Direction.Up)]
    [InlineData(Direction.Left, Direction.Right)]
    [InlineData(Direction.Right, Direction.Left)]
    [InlineData(Direction.Up, Direction.Down)]
    [InlineData(Direction.None, Direction.None)]
    public void Opposite_ReturnsOppositeDirection(Direction direction, Direction expected)
    {
        // Act
        var opposite = direction.Opposite();

        // Assert
        opposite.Should().Be(expected);
    }

    [Fact]
    public void DirectionEnum_HasCorrectValues()
    {
        // Assert
        ((int)Direction.None).Should().Be(-1);
        ((int)Direction.Down).Should().Be(0);
        ((int)Direction.Left).Should().Be(1);
        ((int)Direction.Right).Should().Be(2);
        ((int)Direction.Up).Should().Be(3);
    }

    [Fact]
    public void Opposite_CalledTwice_ReturnsOriginalDirection()
    {
        // Arrange
        var original = Direction.Down;

        // Act
        var oppositeOnce = original.Opposite();
        var oppositeTwice = oppositeOnce.Opposite();

        // Assert
        oppositeTwice.Should().Be(original, "opposite of opposite should be the original");
    }
}
