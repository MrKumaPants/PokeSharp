using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using PokeSharp.Engine.Systems.Factories;
using PokeSharp.Game.Data.PropertyMapping;
using PokeSharp.Engine.Rendering.Assets;
using PokeSharp.Game.Data.MapLoading.Tiled;

namespace PokeSharp.Game.Data.Factories;

/// <summary>
///     Concrete implementation of IGraphicsServiceFactory using Dependency Injection.
///     Resolves loggers and PropertyMapperRegistry from the service provider for proper dependency injection.
/// </summary>
public class GraphicsServiceFactory : IGraphicsServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly PropertyMapperRegistry? _propertyMapperRegistry;

    /// <summary>
    ///     Initializes a new instance of the GraphicsServiceFactory.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating loggers for graphics services.</param>
    /// <param name="propertyMapperRegistry">Optional property mapper registry for map loading.</param>
    public GraphicsServiceFactory(
        ILoggerFactory loggerFactory,
        PropertyMapperRegistry? propertyMapperRegistry = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _propertyMapperRegistry = propertyMapperRegistry;
    }

    /// <inheritdoc />
    public AssetManager CreateAssetManager(
        GraphicsDevice graphicsDevice,
        string assetRoot = "Assets"
    )
    {
        if (graphicsDevice == null)
            throw new ArgumentNullException(nameof(graphicsDevice));

        var logger = _loggerFactory.CreateLogger<AssetManager>();
        return new AssetManager(graphicsDevice, assetRoot, logger);
    }

    /// <inheritdoc />
    public MapLoader CreateMapLoader(
        AssetManager assetManager,
        IEntityFactoryService? entityFactory = null
    )
    {
        if (assetManager == null)
            throw new ArgumentNullException(nameof(assetManager));

        var logger = _loggerFactory.CreateLogger<MapLoader>();
        return new MapLoader(assetManager, _propertyMapperRegistry, entityFactory, logger);
    }
}
