using FluentAssertions;
using PokeSharp.Core.Components.Movement;
using Xunit;

namespace PokeSharp.Tests.Components;

/// <summary>
///     Tests for Velocity component.
/// </summary>
public class VelocityTests
{
    [Fact]
    public void Constructor_SetsVelocityComponents()
    {
        // Arrange & Act
        var velocity = new Velocity(100.5f, 200.75f);

        // Assert
        velocity.VelocityX.Should().Be(100.5f);
        velocity.VelocityY.Should().Be(200.75f);
    }

    [Fact]
    public void Constructor_WithZeroVelocity_CreatesStaticEntity()
    {
        // Arrange & Act
        var velocity = new Velocity(0f, 0f);

        // Assert
        velocity.VelocityX.Should().Be(0f);
        velocity.VelocityY.Should().Be(0f);
    }

    [Fact]
    public void Constructor_WithNegativeVelocity_HandlesCorrectly()
    {
        // Arrange & Act
        var velocity = new Velocity(-50.5f, -100.25f);

        // Assert
        velocity.VelocityX.Should().Be(-50.5f, "negative velocities represent backwards movement");
        velocity.VelocityY.Should().Be(-100.25f);
    }

    [Fact]
    public void Properties_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var velocity = new Velocity(10f, 20f)
        {
            VelocityX = 30f,
            VelocityY = 40f
        };

        // Assert
        velocity.VelocityX.Should().Be(30f);
        velocity.VelocityY.Should().Be(40f);
    }

    [Fact]
    public void DefaultConstructor_InitializesToZero()
    {
        // Arrange & Act
        var velocity = new Velocity();

        // Assert
        velocity.VelocityX.Should().Be(0f, "default struct initialization");
        velocity.VelocityY.Should().Be(0f);
    }

    [Fact]
    public void Velocity_SupportsFloatingPointPrecision()
    {
        // Arrange & Act
        var velocity = new Velocity(0.001f, -0.001f);

        // Assert
        velocity.VelocityX.Should().BeApproximately(0.001f, 0.0001f);
        velocity.VelocityY.Should().BeApproximately(-0.001f, 0.0001f);
    }

    [Fact]
    public void Velocity_SupportsLargeValues()
    {
        // Arrange & Act
        var velocity = new Velocity(10000f, 20000f);

        // Assert
        velocity.VelocityX.Should().Be(10000f);
        velocity.VelocityY.Should().Be(20000f);
    }
}
