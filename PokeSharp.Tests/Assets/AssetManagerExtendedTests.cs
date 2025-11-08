using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using PokeSharp.Rendering.Assets;
using System.Text.Json;
using Xunit;

namespace PokeSharp.Tests.Assets;

/// <summary>
///     Extended tests for AssetManager to improve code coverage.
/// </summary>
public class AssetManagerExtendedTests : IDisposable
{
    private readonly Mock<GraphicsDevice> _mockGraphicsDevice;
    private readonly Mock<ILogger<AssetManager>> _mockLogger;
    private readonly string _testAssetRoot;
    private AssetManager? _assetManager;

    public AssetManagerExtendedTests()
    {
        _mockGraphicsDevice = new Mock<GraphicsDevice>();
        _mockLogger = new Mock<ILogger<AssetManager>>();
        _testAssetRoot = Path.Combine(Path.GetTempPath(), "PokeSharpExtendedTestAssets");
        Directory.CreateDirectory(_testAssetRoot);
    }

    public void Dispose()
    {
        _assetManager?.Dispose();
        if (Directory.Exists(_testAssetRoot))
            Directory.Delete(_testAssetRoot, true);
    }

    [Fact]
    public void AssetRoot_ReturnsCorrectPath()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        var assetRoot = _assetManager.AssetRoot;

        // Assert
        assetRoot.Should().Be(_testAssetRoot, "AssetRoot should return the path provided in constructor");
    }

    [Fact]
    public void LoadManifest_WithMalformedJson_ThrowsJsonException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);
        var malformedManifestPath = Path.Combine(_testAssetRoot, "malformed_manifest.json");
        File.WriteAllText(malformedManifestPath, "{ invalid json ]");

        // Act
        Action act = () => _assetManager.LoadManifest(malformedManifestPath);

        // Assert
        act.Should().Throw<JsonException>("malformed JSON should throw JsonException");
    }

    [Fact]
    public void LoadManifest_WithEmptyTilesets_DoesNotThrow()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);
        var manifestPath = Path.Combine(_testAssetRoot, "empty_tilesets.json");
        var manifestContent = @"{
            ""tilesets"": [],
            ""sprites"": [],
            ""maps"": []
        }";
        File.WriteAllText(manifestPath, manifestContent);

        // Act
        Action act = () => _assetManager.LoadManifest(manifestPath);

        // Assert
        act.Should().NotThrow("empty tilesets array should be handled gracefully");
    }

    [Fact]
    public void LoadTexture_WithNullRelativePath_ThrowsArgumentException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.LoadTexture("test", null!);

        // Assert
        act.Should().Throw<ArgumentException>("null relativePath should throw");
    }

    [Fact]
    public void LoadTexture_WithEmptyRelativePath_ThrowsArgumentException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.LoadTexture("test", "");

        // Assert
        act.Should().Throw<ArgumentException>("empty relativePath should throw");
    }

    [Fact]
    public void Constructor_WithCustomAssetRoot_SetsAssetRoot()
    {
        // Arrange
        var customRoot = "/custom/path";

        // Act
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, customRoot);

        // Assert
        _assetManager.AssetRoot.Should().Be(customRoot, "custom asset root should be set");
    }

    [Fact]
    public void Constructor_WithDefaultAssetRoot_UsesDefault()
    {
        // Act
        _assetManager = new AssetManager(_mockGraphicsDevice.Object);

        // Assert
        _assetManager.AssetRoot.Should().NotBeNullOrEmpty("default asset root should be set");
    }

    [Fact]
    public void HasTexture_AfterDispose_ReturnsFalse()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);
        _assetManager.Dispose();

        // Act
        var hasTexture = _assetManager.HasTexture("any");

        // Assert
        hasTexture.Should().BeFalse("no textures should exist after disposal");
    }
}
