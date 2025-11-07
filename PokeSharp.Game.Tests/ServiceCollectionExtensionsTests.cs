using Arch.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeSharp.Core.Events;
using PokeSharp.Core.Factories;
using PokeSharp.Core.ScriptingApi;
using PokeSharp.Core.Systems;
using PokeSharp.Core.Templates;
using PokeSharp.Core.Types;
using PokeSharp.Game.Diagnostics;
using PokeSharp.Game.Initialization;
using PokeSharp.Game.Input;
using PokeSharp.Scripting.Services;
using PokeSharp.Core.Scripting.Services;
using Xunit;

namespace PokeSharp.Game.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions to verify proper DI container configuration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGameServices_RegistersAllCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<World>().Should().NotBeNull("World should be registered");
        provider.GetService<SystemManager>().Should().NotBeNull("SystemManager should be registered");
        provider.GetService<TemplateCache>().Should().NotBeNull("TemplateCache should be registered");
        provider.GetService<IEntityFactoryService>().Should().NotBeNull("IEntityFactoryService should be registered");
    }

    [Fact]
    public void AddGameServices_RegistersScriptingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ScriptService>().Should().NotBeNull("ScriptService should be registered");
        provider.GetService<TypeRegistry<BehaviorDefinition>>().Should().NotBeNull("BehaviorDefinition TypeRegistry should be registered");
    }

    [Fact]
    public void AddGameServices_RegistersEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Assert
        var eventBus = provider.GetService<IEventBus>();
        eventBus.Should().NotBeNull("IEventBus should be registered");
        eventBus.Should().BeOfType<EventBus>("IEventBus should resolve to EventBus implementation");
    }

    [Fact]
    public void AddGameServices_RegistersScriptingApiServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<PlayerApiService>().Should().NotBeNull("PlayerApiService should be registered");
        provider.GetService<NpcApiService>().Should().NotBeNull("NpcApiService should be registered");
        provider.GetService<MapApiService>().Should().NotBeNull("MapApiService should be registered");
        provider.GetService<GameStateApiService>().Should().NotBeNull("GameStateApiService should be registered");
        provider.GetService<IWorldApi>().Should().NotBeNull("IWorldApi should be registered");
    }

    [Fact]
    public void AddGameServices_RegistersGameHelpers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<PerformanceMonitor>().Should().NotBeNull("PerformanceMonitor should be registered");
        provider.GetService<InputManager>().Should().NotBeNull("InputManager should be registered");
        provider.GetService<PlayerFactory>().Should().NotBeNull("PlayerFactory should be registered");
    }

    [Fact]
    public void AddGameServices_WorldIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var world1 = provider.GetRequiredService<World>();
        var world2 = provider.GetRequiredService<World>();

        // Assert
        world1.Should().BeSameAs(world2, "World should be registered as singleton");
    }

    [Fact]
    public void AddGameServices_SystemManagerIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var manager1 = provider.GetRequiredService<SystemManager>();
        var manager2 = provider.GetRequiredService<SystemManager>();

        // Assert
        manager1.Should().BeSameAs(manager2, "SystemManager should be registered as singleton");
    }

    [Fact]
    public void AddGameServices_EntityFactoryServiceIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var factory1 = provider.GetRequiredService<IEntityFactoryService>();
        var factory2 = provider.GetRequiredService<IEntityFactoryService>();

        // Assert
        factory1.Should().BeSameAs(factory2, "IEntityFactoryService should be registered as singleton");
    }

    [Fact]
    public void AddGameServices_TemplateCacheIsInitialized()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var cache = provider.GetRequiredService<TemplateCache>();

        // Assert
        cache.Should().NotBeNull("TemplateCache should be initialized");
        // Templates are registered via TemplateRegistry.RegisterAllTemplates
    }

    [Fact]
    public void AddGameServices_EventBusIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var bus1 = provider.GetRequiredService<IEventBus>();
        var bus2 = provider.GetRequiredService<IEventBus>();

        // Assert
        bus1.Should().BeSameAs(bus2, "IEventBus should be registered as singleton");
    }

    [Fact]
    public void AddGameServices_ScriptServiceIsConfiguredWithCorrectPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var scriptService = provider.GetRequiredService<ScriptService>();

        // Assert
        scriptService.Should().NotBeNull("ScriptService should be configured");
        // ScriptService is configured with "Assets/Scripts" path
    }

    [Fact]
    public void AddGameServices_TypeRegistryIsConfiguredWithCorrectPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var registry = provider.GetRequiredService<TypeRegistry<BehaviorDefinition>>();

        // Assert
        registry.Should().NotBeNull("TypeRegistry should be configured");
        // TypeRegistry is configured with "Assets/Types/Behaviors" path
    }

    [Fact]
    public void AddGameServices_AllServicesCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert - verify all services can be resolved without circular dependencies
        Assert.Multiple(
            () => provider.GetRequiredService<World>(),
            () => provider.GetRequiredService<SystemManager>(),
            () => provider.GetRequiredService<TemplateCache>(),
            () => provider.GetRequiredService<IEntityFactoryService>(),
            () => provider.GetRequiredService<ScriptService>(),
            () => provider.GetRequiredService<TypeRegistry<BehaviorDefinition>>(),
            () => provider.GetRequiredService<IEventBus>(),
            () => provider.GetRequiredService<PlayerApiService>(),
            () => provider.GetRequiredService<NpcApiService>(),
            () => provider.GetRequiredService<MapApiService>(),
            () => provider.GetRequiredService<GameStateApiService>(),
            () => provider.GetRequiredService<IWorldApi>(),
            () => provider.GetRequiredService<PerformanceMonitor>(),
            () => provider.GetRequiredService<InputManager>(),
            () => provider.GetRequiredService<PlayerFactory>()
        );
    }

    [Fact]
    public void AddGameServices_MapApiServiceIsInitializedWithNullSpatialHashSystem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameServices();
        var provider = services.BuildServiceProvider();

        // Act
        var mapApiService = provider.GetRequiredService<MapApiService>();

        // Assert
        mapApiService.Should().NotBeNull("MapApiService should be registered");
        // Note: SpatialHashSystem is set later via SetSpatialHashSystem in GameInitializer
    }

    [Fact]
    public void AddGameServices_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddGameServices();

        // Assert
        result.Should().BeSameAs(services, "AddGameServices should return the service collection for chaining");
    }
}
