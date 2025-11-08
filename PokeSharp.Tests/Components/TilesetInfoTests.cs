using FluentAssertions;
using Microsoft.Xna.Framework;
using PokeSharp.Core.Components.Tiles;
using Xunit;

namespace PokeSharp.Tests.Components;

/// <summary>
///     Tests for TilesetInfo component.
/// </summary>
public class TilesetInfoTests
{
    [Fact]
    public void Constructor_AllPropertiesSet()
    {
        // Arrange & Act
        var tilesetInfo = new TilesetInfo(
            tilesetId: "tileset1",
            firstGid: 1,
            tileWidth: 16,
            tileHeight: 16,
            imageWidth: 256,
            imageHeight: 128
        );

        // Assert
        tilesetInfo.TilesetId.Should().Be("tileset1");
        tilesetInfo.FirstGid.Should().Be(1);
        tilesetInfo.TileWidth.Should().Be(16);
        tilesetInfo.TileHeight.Should().Be(16);
        tilesetInfo.ImageWidth.Should().Be(256);
        tilesetInfo.ImageHeight.Should().Be(128);
    }

    [Fact]
    public void TilesPerRow_CalculatesCorrectly()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 1, 16, 16, 256, 128);

        // Act
        var tilesPerRow = tilesetInfo.TilesPerRow;

        // Assert
        tilesPerRow.Should().Be(16, "256 / 16 = 16 tiles per row");
    }

    [Fact]
    public void TilesPerColumn_CalculatesCorrectly()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 1, 16, 16, 256, 128);

        // Act
        var tilesPerColumn = tilesetInfo.TilesPerColumn;

        // Assert
        tilesPerColumn.Should().Be(8, "128 / 16 = 8 tiles per column");
    }

    [Fact]
    public void CalculateSourceRect_FirstTile_ReturnsTopLeft()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 1, 16, 16, 256, 128);

        // Act
        var rect = tilesetInfo.CalculateSourceRect(1);

        // Assert
        rect.X.Should().Be(0, "first tile is at X=0");
        rect.Y.Should().Be(0, "first tile is at Y=0");
        rect.Width.Should().Be(16);
        rect.Height.Should().Be(16);
    }

    [Fact]
    public void CalculateSourceRect_MiddleTile_CalculatesCorrectPosition()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 1, 16, 16, 256, 128);

        // Act - Tile GID 18 = local ID 17 = row 1, col 1
        var rect = tilesetInfo.CalculateSourceRect(18);

        // Assert
        rect.X.Should().Be(16, "second column starts at X=16");
        rect.Y.Should().Be(16, "second row starts at Y=16");
        rect.Width.Should().Be(16);
        rect.Height.Should().Be(16);
    }

    [Fact]
    public void CalculateSourceRect_LastTileInFirstRow_ReturnsCorrectPosition()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 1, 16, 16, 256, 128);

        // Act - Last tile in first row (GID 16 = local ID 15)
        var rect = tilesetInfo.CalculateSourceRect(16);

        // Assert
        rect.X.Should().Be(240, "15th column starts at 15 * 16 = 240");
        rect.Y.Should().Be(0, "still in first row");
        rect.Width.Should().Be(16);
        rect.Height.Should().Be(16);
    }

    [Fact]
    public void CalculateSourceRect_WithNegativeGid_ReturnsEmpty()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 10, 16, 16, 256, 128);

        // Act - GID less than FirstGid
        var rect = tilesetInfo.CalculateSourceRect(5);

        // Assert
        rect.Should().Be(Rectangle.Empty, "GID before FirstGid should return empty rectangle");
    }

    [Fact]
    public void CalculateSourceRect_WithCustomTileSize_WorksCorrectly()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("ts", 1, 32, 32, 320, 160);

        // Act
        var rect = tilesetInfo.CalculateSourceRect(1);

        // Assert
        rect.Width.Should().Be(32, "custom tile width");
        rect.Height.Should().Be(32, "custom tile height");
        tilesetInfo.TilesPerRow.Should().Be(10, "320 / 32 = 10");
        tilesetInfo.TilesPerColumn.Should().Be(5, "160 / 32 = 5");
    }

    [Fact]
    public void CalculateSourceRect_SecondRow_FirstTile_CorrectPosition()
    {
        // Arrange - 16 tiles per row
        var tilesetInfo = new TilesetInfo("ts", 1, 16, 16, 256, 128);

        // Act - GID 17 = local ID 16 = row 1, col 0
        var rect = tilesetInfo.CalculateSourceRect(17);

        // Assert
        rect.X.Should().Be(0, "first column");
        rect.Y.Should().Be(16, "second row");
    }

    [Fact]
    public void Properties_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var tilesetInfo = new TilesetInfo("original", 1, 16, 16, 256, 128)
        {
            TilesetId = "modified",
            FirstGid = 100,
            TileWidth = 32,
            TileHeight = 32,
            ImageWidth = 512,
            ImageHeight = 256
        };

        // Assert
        tilesetInfo.TilesetId.Should().Be("modified");
        tilesetInfo.FirstGid.Should().Be(100);
        tilesetInfo.TilesPerRow.Should().Be(16, "512 / 32 = 16");
        tilesetInfo.TilesPerColumn.Should().Be(8, "256 / 32 = 8");
    }
}
