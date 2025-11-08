using Microsoft.Extensions.Logging;
using PokeSharp.Rendering.Configuration;
using PokeSharp.Rendering.Loaders;
using PokeSharp.Rendering.Validation;
using Xunit;

namespace PokeSharp.Tests.Validation;

/// <summary>
/// Integration tests for map validation in the loading pipeline
/// </summary>
public class MapValidationIntegrationTests : IDisposable
{
    private readonly ILogger<MapLoader> _logger;
    private readonly string _testMapDirectory;

    public MapValidationIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MapLoader>();

        // Create temp directory for test maps
        _testMapDirectory = Path.Combine(Path.GetTempPath(), "pokesharp_test_maps", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testMapDirectory);
    }

    [Fact]
    public void Load_WithValidationEnabled_ValidMap_Succeeds()
    {
        // Arrange
        var mapPath = CreateTestMap("valid_map.json", CreateValidMapJson());
        var options = new MapLoaderOptions
        {
            ValidateMaps = true,
            ThrowOnValidationError = true,
            ValidateFileReferences = false
        };
        TiledMapLoader.Configure(options, _logger);

        // Act & Assert - Should not throw
        var map = TiledMapLoader.Load(mapPath);
        Assert.NotNull(map);
        Assert.Equal(10, map.Width);
        Assert.Equal(10, map.Height);
    }

    [Fact]
    public void Load_WithValidationEnabled_InvalidMap_ThrowsException()
    {
        // Arrange
        var mapPath = CreateTestMap("invalid_map.json", CreateInvalidMapJson());
        var options = new MapLoaderOptions
        {
            ValidateMaps = true,
            ThrowOnValidationError = true,
            ValidateFileReferences = false
        };
        TiledMapLoader.Configure(options, _logger);

        // Act & Assert
        var exception = Assert.Throws<MapValidationException>(() => TiledMapLoader.Load(mapPath));
        Assert.NotNull(exception.ValidationResult);
        Assert.False(exception.ValidationResult.IsValid);
    }

    [Fact]
    public void Load_WithValidationDisabled_InvalidMap_Succeeds()
    {
        // Arrange
        var mapPath = CreateTestMap("invalid_map.json", CreateInvalidMapJson());
        var options = new MapLoaderOptions
        {
            ValidateMaps = false
        };
        TiledMapLoader.Configure(options, _logger);

        // Act & Assert - Should not throw even with invalid map
        var map = TiledMapLoader.Load(mapPath);
        Assert.NotNull(map);
    }

    [Fact]
    public void Load_WithValidationNonThrowing_InvalidMap_Logs()
    {
        // Arrange
        var mapPath = CreateTestMap("invalid_map.json", CreateInvalidMapJson());
        var options = new MapLoaderOptions
        {
            ValidateMaps = true,
            ThrowOnValidationError = false, // Log errors but don't throw
            ValidateFileReferences = false
        };
        TiledMapLoader.Configure(options, _logger);

        // Act - Should not throw, but should log errors
        var map = TiledMapLoader.Load(mapPath);

        // Assert - Map is loaded despite validation errors
        Assert.NotNull(map);
    }

    #region Helper Methods

    private string CreateTestMap(string filename, string json)
    {
        var path = Path.Combine(_testMapDirectory, filename);
        File.WriteAllText(path, json);
        return path;
    }

    private static string CreateValidMapJson()
    {
        return """
        {
            "version": "1.0",
            "tiledversion": "1.11.2",
            "width": 10,
            "height": 10,
            "tilewidth": 16,
            "tileheight": 16,
            "tilesets": [
                {
                    "firstgid": 1,
                    "name": "TestTileset",
                    "tilewidth": 16,
                    "tileheight": 16,
                    "tilecount": 100,
                    "spacing": 0,
                    "margin": 0,
                    "image": "tileset.png",
                    "imagewidth": 160,
                    "imageheight": 160
                }
            ],
            "layers": [
                {
                    "id": 1,
                    "name": "Ground",
                    "type": "tilelayer",
                    "width": 10,
                    "height": 10,
                    "visible": true,
                    "opacity": 1.0,
                    "data": [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,1]
                }
            ]
        }
        """;
    }

    private static string CreateInvalidMapJson()
    {
        // Map with zero width (invalid)
        return """
        {
            "version": "1.0",
            "tiledversion": "1.11.2",
            "width": 0,
            "height": 10,
            "tilewidth": 16,
            "tileheight": 16,
            "tilesets": [
                {
                    "firstgid": 1,
                    "name": "TestTileset",
                    "tilewidth": 16,
                    "tileheight": 16,
                    "tilecount": 100,
                    "image": "tileset.png",
                    "imagewidth": 160,
                    "imageheight": 160
                }
            ],
            "layers": []
        }
        """;
    }

    public void Dispose()
    {
        // Clean up test directory
        try
        {
            if (Directory.Exists(_testMapDirectory))
            {
                Directory.Delete(_testMapDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}
