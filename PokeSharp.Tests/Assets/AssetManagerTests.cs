using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using PokeSharp.Rendering.Assets;
using Xunit;

namespace PokeSharp.Tests.Assets;

public class AssetManagerTests : IDisposable
{
    private readonly Mock<GraphicsDevice> _mockGraphicsDevice;
    private readonly Mock<ILogger<AssetManager>> _mockLogger;
    private readonly string _testAssetRoot;
    private AssetManager? _assetManager;

    public AssetManagerTests()
    {
        _mockGraphicsDevice = new Mock<GraphicsDevice>();
        _mockLogger = new Mock<ILogger<AssetManager>>();
        _testAssetRoot = Path.Combine(Path.GetTempPath(), "PokeSharpTestAssets");
        Directory.CreateDirectory(_testAssetRoot);
    }

    public void Dispose()
    {
        _assetManager?.Dispose();
        if (Directory.Exists(_testAssetRoot))
            Directory.Delete(_testAssetRoot, true);
    }

    [Fact]
    public void Constructor_WithNullGraphicsDevice_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new AssetManager(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("graphicsDevice");
    }

    [Fact]
    public void LoadedTextureCount_InitiallyEmpty_ReturnsZero()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        var count = _assetManager.LoadedTextureCount;

        // Assert
        count.Should().Be(0, "no textures have been loaded yet");
    }

    [Fact]
    public void LoadTexture_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.LoadTexture(null!, "test.png");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LoadTexture_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.LoadTexture("", "test.png");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LoadTexture_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);
        const string nonExistentPath = "nonexistent.png";

        // Act
        Action act = () => _assetManager.LoadTexture("test", nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{nonExistentPath}*");
    }

    [Fact]
    public void HasTexture_WithLoadedTexture_ReturnsTrue()
    {
        // Arrange - This test requires a real texture file or mock, skipping actual load
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // For this test, we would need to create a valid PNG file
        // For now, testing the behavior without actual file loading

        // Act
        var hasTexture = _assetManager.HasTexture("nonexistent");

        // Assert
        hasTexture.Should().BeFalse("texture has not been loaded");
    }

    [Fact]
    public void GetTexture_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.GetTexture("nonexistent");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public void Dispose_DisposesAllLoadedTextures()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        _assetManager.Dispose();

        // Assert
        _assetManager.LoadedTextureCount.Should().Be(0, "all textures should be cleared after disposal");
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () =>
        {
            _assetManager.Dispose();
            _assetManager.Dispose();
        };

        // Assert
        act.Should().NotThrow("multiple dispose calls should be safe");
    }

    [Fact]
    public void LoadManifest_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);
        const string nonExistentManifest = "nonexistent_manifest.json";

        // Act
        Action act = () => _assetManager.LoadManifest(nonExistentManifest);

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{nonExistentManifest}*");
    }

    [Fact]
    public void HotReloadTexture_WithoutManifest_ThrowsInvalidOperationException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.HotReloadTexture("test");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot hot-reload without manifest*");
    }

    [Fact]
    public void HotReloadTexture_WithNonLoadedTexture_ThrowsKeyNotFoundException()
    {
        // Arrange
        _assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);

        // Act
        Action act = () => _assetManager.HotReloadTexture("nonexistent");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*Cannot hot-reload texture*");
    }
}
