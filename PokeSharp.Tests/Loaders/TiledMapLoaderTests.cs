using PokeSharp.Rendering.Loaders;
using Xunit;

namespace PokeSharp.Tests.Loaders;

/// <summary>
///     Unit tests for TiledMapLoader compression support.
/// </summary>
public class TiledMapLoaderTests
{
    [Fact]
    public void Load_UncompressedMap_LoadsSuccessfully()
    {
        // Arrange
        var mapPath = "TestData/test-map.json";

        // Act
        var tmxDoc = TiledMapLoader.Load(mapPath);

        // Assert
        Assert.NotNull(tmxDoc);
        Assert.Equal(3, tmxDoc.Width);
        Assert.Equal(3, tmxDoc.Height);
        Assert.Single(tmxDoc.Layers);

        var layer = tmxDoc.Layers[0];
        Assert.Equal("Ground", layer.Name);
        Assert.NotNull(layer.Data);
        Assert.Equal(3, layer.Data!.GetLength(0)); // Height
        Assert.Equal(3, layer.Data!.GetLength(1)); // Width

        // Verify tile data
        Assert.Equal(1, layer.Data[0, 0]);
        Assert.Equal(2, layer.Data[0, 1]);
        Assert.Equal(3, layer.Data[0, 2]);
        Assert.Equal(4, layer.Data[1, 0]);
        Assert.Equal(5, layer.Data[1, 1]);
        Assert.Equal(6, layer.Data[1, 2]);
        Assert.Equal(7, layer.Data[2, 0]);
        Assert.Equal(8, layer.Data[2, 1]);
        Assert.Equal(9, layer.Data[2, 2]);
    }

    [Fact]
    public void Load_ZstdCompressedMap_LoadsSuccessfully()
    {
        // Arrange
        var mapPath = "TestData/test-map-zstd-3x3.json";

        // Act
        var tmxDoc = TiledMapLoader.Load(mapPath);

        // Assert
        Assert.NotNull(tmxDoc);
        Assert.Equal(3, tmxDoc.Width);
        Assert.Equal(3, tmxDoc.Height);
        Assert.Single(tmxDoc.Layers);

        var layer = tmxDoc.Layers[0];
        Assert.Equal("Ground", layer.Name);
        Assert.NotNull(layer.Data);
        Assert.Equal(3, layer.Data!.GetLength(0)); // Height
        Assert.Equal(3, layer.Data!.GetLength(1)); // Width

        // Verify decompressed tile data matches expected values
        Assert.Equal(1, layer.Data[0, 0]);
        Assert.Equal(2, layer.Data[0, 1]);
        Assert.Equal(3, layer.Data[0, 2]);
        Assert.Equal(4, layer.Data[1, 0]);
        Assert.Equal(5, layer.Data[1, 1]);
        Assert.Equal(6, layer.Data[1, 2]);
        Assert.Equal(7, layer.Data[2, 0]);
        Assert.Equal(8, layer.Data[2, 1]);
        Assert.Equal(9, layer.Data[2, 2]);
    }

    [Fact]
    public void Load_ZstdCompressedMap_ProducesSameResultAsUncompressed()
    {
        // Arrange
        var uncompressedPath = "TestData/test-map.json";
        var compressedPath = "TestData/test-map-zstd-3x3.json";

        // Act
        var uncompressedDoc = TiledMapLoader.Load(uncompressedPath);
        var compressedDoc = TiledMapLoader.Load(compressedPath);

        // Assert - Both should produce identical tile data
        Assert.Equal(uncompressedDoc.Width, compressedDoc.Width);
        Assert.Equal(uncompressedDoc.Height, compressedDoc.Height);
        Assert.Equal(uncompressedDoc.Layers.Count, compressedDoc.Layers.Count);

        for (var layerIdx = 0; layerIdx < uncompressedDoc.Layers.Count; layerIdx++)
        {
            var uncompressedLayer = uncompressedDoc.Layers[layerIdx];
            var compressedLayer = compressedDoc.Layers[layerIdx];

            Assert.Equal(uncompressedLayer.Name, compressedLayer.Name);
            Assert.NotNull(uncompressedLayer.Data);
            Assert.NotNull(compressedLayer.Data);

            var height = uncompressedLayer.Data!.GetLength(0);
            var width = uncompressedLayer.Data!.GetLength(1);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    Assert.Equal(
                        uncompressedLayer.Data[y, x],
                        compressedLayer.Data![y, x]
                    );
                }
            }
        }
    }

    [Fact]
    public void Load_InvalidPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var invalidPath = "nonexistent/path/to/map.json";

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => TiledMapLoader.Load(invalidPath));
        Assert.Contains(invalidPath, exception.Message);
    }

    [Fact]
    public void Load_MalformedJson_ThrowsJsonException()
    {
        // Arrange - Create temporary malformed JSON file
        var tempPath = Path.Combine(Path.GetTempPath(), "malformed_map.json");
        File.WriteAllText(tempPath, "{ invalid json syntax ]");

        try
        {
            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() => TiledMapLoader.Load(tempPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void Load_MapWithTilesets_ParsesTilesetData()
    {
        // Arrange
        var mapPath = "TestData/test-map.json";

        // Act
        var tmxDoc = TiledMapLoader.Load(mapPath);

        // Assert
        Assert.NotNull(tmxDoc.Tilesets);
        Assert.NotEmpty(tmxDoc.Tilesets);

        var tileset = tmxDoc.Tilesets[0];
        Assert.NotNull(tileset);
        Assert.True(tileset.FirstGid > 0, "FirstGid should be greater than 0");
    }

    [Fact]
    public void Load_MapWithLayers_ParsesLayerMetadata()
    {
        // Arrange
        var mapPath = "TestData/test-map.json";

        // Act
        var tmxDoc = TiledMapLoader.Load(mapPath);

        // Assert
        Assert.Single(tmxDoc.Layers);
        var layer = tmxDoc.Layers[0];

        Assert.Equal("Ground", layer.Name);
        Assert.True(layer.Visible, "Layer should be visible by default");
        Assert.InRange(layer.Opacity, 0.0f, 1.0f);
    }

    [Fact]
    public void Load_MapDimensions_MatchJsonData()
    {
        // Arrange
        var mapPath = "TestData/test-map.json";

        // Act
        var tmxDoc = TiledMapLoader.Load(mapPath);

        // Assert
        Assert.Equal(3, tmxDoc.Width);
        Assert.Equal(3, tmxDoc.Height);
        Assert.Equal(16, tmxDoc.TileWidth);
        Assert.Equal(16, tmxDoc.TileHeight);
    }
}
