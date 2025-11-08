using FluentAssertions;
using PokeSharp.Core.Components.Movement;
using Xunit;

namespace PokeSharp.Tests.Components;

public class PositionTests
{
    [Fact]
    public void Constructor_WithDefaultTileSize_CalculatesPixelCoordinates()
    {
        // Arrange & Act
        var position = new Position(5, 10);

        // Assert
        position.X.Should().Be(5);
        position.Y.Should().Be(10);
        position.PixelX.Should().Be(80, "5 * 16 (default tile size) = 80");
        position.PixelY.Should().Be(160, "10 * 16 (default tile size) = 160");
        position.MapId.Should().Be(0, "default map ID is 0");
    }

    [Fact]
    public void Constructor_WithCustomTileSize_CalculatesPixelCoordinates()
    {
        // Arrange & Act
        var position = new Position(3, 4, tileSize: 32);

        // Assert
        position.X.Should().Be(3);
        position.Y.Should().Be(4);
        position.PixelX.Should().Be(96, "3 * 32 (custom tile size) = 96");
        position.PixelY.Should().Be(128, "4 * 32 (custom tile size) = 128");
    }

    [Fact]
    public void Constructor_WithCustomMapId_SetsMapIdCorrectly()
    {
        // Arrange & Act
        var position = new Position(2, 3, mapId: 5);

        // Assert
        position.MapId.Should().Be(5, "map ID should be set to the specified value");
    }

    [Fact]
    public void SyncPixelsToGrid_WithDefaultTileSize_UpdatesPixelCoordinates()
    {
        // Arrange
        var position = new Position(0, 0)
        {
            X = 7,
            Y = 9,
            PixelX = 0, // Out of sync
            PixelY = 0  // Out of sync
        };

        // Act
        position.SyncPixelsToGrid();

        // Assert
        position.PixelX.Should().Be(112, "7 * 16 = 112");
        position.PixelY.Should().Be(144, "9 * 16 = 144");
    }

    [Fact]
    public void SyncPixelsToGrid_WithCustomTileSize_UpdatesPixelCoordinates()
    {
        // Arrange
        var position = new Position(0, 0)
        {
            X = 4,
            Y = 6,
            PixelX = 0, // Out of sync
            PixelY = 0  // Out of sync
        };

        // Act
        position.SyncPixelsToGrid(tileSize: 24);

        // Assert
        position.PixelX.Should().Be(96, "4 * 24 = 96");
        position.PixelY.Should().Be(144, "6 * 24 = 144");
    }

    [Fact]
    public void GridCoordinates_CanBeModifiedDirectly()
    {
        // Arrange
        var position = new Position(1, 2);

        // Act
        position.X = 10;
        position.Y = 20;

        // Assert
        position.X.Should().Be(10);
        position.Y.Should().Be(20);
    }

    [Fact]
    public void PixelCoordinates_CanBeModifiedDirectly()
    {
        // Arrange
        var position = new Position(1, 2);

        // Act
        position.PixelX = 123.5f;
        position.PixelY = 456.7f;

        // Assert
        position.PixelX.Should().Be(123.5f);
        position.PixelY.Should().Be(456.7f);
    }

    [Fact]
    public void MapId_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var position = new Position(0, 0, mapId: 1);

        // Act
        position.MapId = 99;

        // Assert
        position.MapId.Should().Be(99, "map ID should be updatable");
    }

    [Fact]
    public void Constructor_WithNegativeCoordinates_WorksCorrectly()
    {
        // Arrange & Act
        var position = new Position(-5, -10, tileSize: 16);

        // Assert
        position.X.Should().Be(-5);
        position.Y.Should().Be(-10);
        position.PixelX.Should().Be(-80, "-5 * 16 = -80");
        position.PixelY.Should().Be(-160, "-10 * 16 = -160");
    }

    [Fact]
    public void SyncPixelsToGrid_WithZeroCoordinates_SetsPixelsToZero()
    {
        // Arrange
        var position = new Position(0, 0)
        {
            PixelX = 999,
            PixelY = 999
        };

        // Act
        position.SyncPixelsToGrid();

        // Assert
        position.PixelX.Should().Be(0);
        position.PixelY.Should().Be(0);
    }
}
