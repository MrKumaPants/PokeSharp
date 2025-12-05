using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;

namespace MonoBallFramework.Game.Engine.Rendering.Services;

/// <summary>
/// Default implementation of IRenderingService.
/// Manages shared rendering resources with proper lifecycle.
/// </summary>
public class RenderingService : IRenderingService
{
    private readonly ILogger<RenderingService> _logger;
    private bool _disposed;

    public RenderingService(
        GraphicsDevice graphicsDevice,
        ILogger<RenderingService> logger)
    {
        GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SpriteBatch = new SpriteBatch(graphicsDevice);

        _logger.LogInformation("RenderingService initialized with shared SpriteBatch");
    }

    public SpriteBatch SpriteBatch { get; }
    public GraphicsDevice GraphicsDevice { get; }

    public void Dispose()
    {
        if (_disposed) return;

        SpriteBatch?.Dispose();
        _disposed = true;
        _logger.LogDebug("RenderingService disposed");

        GC.SuppressFinalize(this);
    }
}
