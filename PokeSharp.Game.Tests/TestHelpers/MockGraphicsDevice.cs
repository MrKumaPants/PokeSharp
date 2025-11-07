using Microsoft.Xna.Framework.Graphics;
using Moq;

namespace PokeSharp.Game.Tests.TestHelpers;

/// <summary>
/// Helper class for creating mock GraphicsDevice instances for testing.
/// </summary>
public static class MockGraphicsDevice
{
    /// <summary>
    /// Creates a mock GraphicsDevice for testing purposes.
    /// Note: Full GraphicsDevice mocking is complex, so this provides basic setup.
    /// For tests requiring actual rendering, consider integration tests.
    /// </summary>
    public static Mock<GraphicsDevice> Create()
    {
        var mockDevice = new Mock<GraphicsDevice>();

        // Setup basic properties that tests might need
        mockDevice.Setup(x => x.Viewport)
            .Returns(new Viewport(0, 0, 800, 600));

        return mockDevice;
    }

    /// <summary>
    /// Creates a mock GraphicsDevice with custom viewport dimensions.
    /// </summary>
    public static Mock<GraphicsDevice> CreateWithViewport(int width, int height)
    {
        var mockDevice = new Mock<GraphicsDevice>();

        mockDevice.Setup(x => x.Viewport)
            .Returns(new Viewport(0, 0, width, height));

        return mockDevice;
    }
}
