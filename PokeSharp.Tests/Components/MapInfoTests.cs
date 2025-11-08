using FluentAssertions;
using PokeSharp.Core.Components.Maps;
using Xunit;

namespace PokeSharp.Tests.Components;

/// <summary>
///     Tests for MapInfo component.
/// </summary>
public class MapInfoTests
{
    [Fact]
    public void Constructor_CalculatesPixelDimensions()
    {
        // Arrange & Act
        var mapInfo = new MapInfo(
            mapId: 1,
            mapName: "TestMap",
            width: 20,
            height: 15,
            tileSize: 16
        );

        // Assert
        mapInfo.MapId.Should().Be(1);
        mapInfo.MapName.Should().Be("TestMap");
        mapInfo.Width.Should().Be(20);
        mapInfo.Height.Should().Be(15);
        mapInfo.TileSize.Should().Be(16);
        mapInfo.PixelWidth.Should().Be(320, "20 tiles * 16 pixels = 320");
        mapInfo.PixelHeight.Should().Be(240, "15 tiles * 16 pixels = 240");
    }

    [Fact]
    public void PixelWidth_MatchesWidthTimesTileSize()
    {
        // Arrange
        var mapInfo = new MapInfo(0, "Map", 30, 25, 32);

        // Act
        var pixelWidth = mapInfo.PixelWidth;

        // Assert
        pixelWidth.Should().Be(960, "30 * 32 = 960");
    }

    [Fact]
    public void PixelHeight_MatchesHeightTimesTileSize()
    {
        // Arrange
        var mapInfo = new MapInfo(0, "Map", 30, 25, 32);

        // Act
        var pixelHeight = mapInfo.PixelHeight;

        // Assert
        pixelHeight.Should().Be(800, "25 * 32 = 800");
    }

    [Fact]
    public void Constructor_WithDefaultTileSize_Uses16()
    {
        // Arrange & Act
        var mapInfo = new MapInfo(2, "DefaultMap", 10, 10);

        // Assert
        mapInfo.TileSize.Should().Be(16, "default tile size should be 16");
        mapInfo.PixelWidth.Should().Be(160, "10 * 16 = 160");
        mapInfo.PixelHeight.Should().Be(160, "10 * 16 = 160");
    }

    [Fact]
    public void Properties_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var mapInfo = new MapInfo(0, "Original", 10, 10, 16)
        {
            MapId = 5,
            MapName = "Modified",
            Width = 20,
            Height = 30,
            TileSize = 32
        };

        // Assert
        mapInfo.MapId.Should().Be(5);
        mapInfo.MapName.Should().Be("Modified");
        mapInfo.Width.Should().Be(20);
        mapInfo.Height.Should().Be(30);
        mapInfo.TileSize.Should().Be(32);
        mapInfo.PixelWidth.Should().Be(640, "20 * 32 = 640");
        mapInfo.PixelHeight.Should().Be(960, "30 * 32 = 960");
    }

    [Fact]
    public void Constructor_WithZeroDimensions_HandlesCorrectly()
    {
        // Arrange & Act
        var mapInfo = new MapInfo(0, "Empty", 0, 0, 16);

        // Assert
        mapInfo.PixelWidth.Should().Be(0);
        mapInfo.PixelHeight.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithLargeDimensions_CalculatesCorrectly()
    {
        // Arrange & Act
        var mapInfo = new MapInfo(0, "Large", 1000, 500, 16);

        // Assert
        mapInfo.PixelWidth.Should().Be(16000, "1000 * 16 = 16000");
        mapInfo.PixelHeight.Should().Be(8000, "500 * 16 = 8000");
    }
}
