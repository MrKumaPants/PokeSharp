using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PokeSharp.Core.Events;
using PokeSharp.Core.Factories;
using PokeSharp.Core.Systems;
using PokeSharp.Core.Types;
using PokeSharp.Core.Templates;
using PokeSharp.Game.Diagnostics;
using PokeSharp.Game.Input;
using PokeSharp.Game.Initialization;
using PokeSharp.Scripting.Services;

namespace PokeSharp.Game.Tests.TestFixtures;

/// <summary>
/// Base fixture for game tests providing common setup and teardown functionality.
/// </summary>
public class GameTestFixture : IDisposable
{
    private bool _disposed;

    public World World { get; private set; }
    public IServiceProvider ServiceProvider { get; private set; }
    public SystemManager SystemManager { get; private set; }
    public Mock<ILogger<PokeSharpGame>> MockLogger { get; private set; }
    public Mock<ILoggerFactory> MockLoggerFactory { get; private set; }

    public GameTestFixture()
    {
        // Create test world
        World = World.Create();

        // Setup mocks
        MockLogger = new Mock<ILogger<PokeSharpGame>>();
        MockLoggerFactory = new Mock<ILoggerFactory>();

        // Configure logger factory to return mocks
        MockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        // Build service collection
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(World);
        services.AddSingleton<SystemManager>();
        services.AddSingleton(sp =>
        {
            var cache = new TemplateCache();
            return cache;
        });
        services.AddSingleton<IEntityFactoryService, EntityFactoryService>();
        services.AddSingleton<IEventBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EventBus>>();
            return new EventBus(logger);
        });

        ServiceProvider = services.BuildServiceProvider();
        SystemManager = ServiceProvider.GetRequiredService<SystemManager>();
    }

    /// <summary>
    /// Creates a clean world for isolated tests.
    /// </summary>
    public World CreateCleanWorld()
    {
        return World.Create();
    }

    /// <summary>
    /// Creates a configured SystemManager for tests.
    /// </summary>
    public SystemManager CreateSystemManager(ILogger<SystemManager>? logger = null)
    {
        return new SystemManager(logger);
    }

    /// <summary>
    /// Resets the test world to a clean state.
    /// </summary>
    public void ResetWorld()
    {
        World?.Dispose();
        World = World.Create();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            World?.Dispose();
            (ServiceProvider as IDisposable)?.Dispose();
        }

        _disposed = true;
    }
}
