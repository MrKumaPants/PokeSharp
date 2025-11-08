# Dependency Injection Setup for Property Mappers

## Overview

This document describes how to register the Property Mapper system in the Dependency Injection (DI) container.

## Service Registration

### Option 1: Manual Registration (Explicit Control)

```csharp
// In Program.cs or ServiceConfiguration.cs
using PokeSharp.Core.Mapping;
using Microsoft.Extensions.DependencyInjection;

public static class PropertyMapperServiceExtensions
{
    public static IServiceCollection AddPropertyMappers(this IServiceCollection services)
    {
        // Register PropertyMapperRegistry as singleton
        services.AddSingleton(provider =>
        {
            var logger = provider.GetService<ILogger<PropertyMapperRegistry>>();
            var registry = new PropertyMapperRegistry(logger);

            // Register all mappers
            registry.RegisterMapper(new CollisionMapper());
            registry.RegisterMapper(new LedgeMapper());
            registry.RegisterMapper(new EncounterZoneMapper());
            registry.RegisterMapper(new TerrainTypeMapper());
            registry.RegisterMapper(new ScriptMapper());
            registry.RegisterMapper(new InteractionMapper());
            registry.RegisterMapper(new NpcMapper());

            return registry;
        });

        return services;
    }
}

// Usage in Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPropertyMappers();
```

### Option 2: Auto-Discovery (Automatic Registration)

```csharp
public static class PropertyMapperServiceExtensions
{
    public static IServiceCollection AddPropertyMappers(this IServiceCollection services)
    {
        // Register PropertyMapperRegistry
        services.AddSingleton(provider =>
        {
            var logger = provider.GetService<ILogger<PropertyMapperRegistry>>();
            var registry = new PropertyMapperRegistry(logger);

            // Auto-discover all IEntityPropertyMapper implementations
            var mapperTypes = Assembly.GetAssembly(typeof(IPropertyMapper<>))!
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEntityPropertyMapper<>)));

            foreach (var mapperType in mapperTypes)
            {
                var mapper = Activator.CreateInstance(mapperType);
                if (mapper != null)
                {
                    // Use reflection to call RegisterMapper<T>
                    var componentType = mapperType.GetInterfaces()
                        .First(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IEntityPropertyMapper<>))
                        .GetGenericArguments()[0];

                    var registerMethod = typeof(PropertyMapperRegistry)
                        .GetMethod(nameof(PropertyMapperRegistry.RegisterMapper))!
                        .MakeGenericMethod(componentType);

                    registerMethod.Invoke(registry, new[] { mapper });
                }
            }

            return registry;
        });

        return services;
    }
}
```

### Option 3: Hybrid Approach (Recommended)

```csharp
public static class PropertyMapperServiceExtensions
{
    public static IServiceCollection AddPropertyMappers(this IServiceCollection services)
    {
        // Register PropertyMapperRegistry
        services.AddSingleton<PropertyMapperRegistry>(provider =>
        {
            var logger = provider.GetService<ILogger<PropertyMapperRegistry>>();
            return CreatePropertyMapperRegistry(logger);
        });

        return services;
    }

    /// <summary>
    /// Creates and configures a PropertyMapperRegistry with all mappers.
    /// Exposed as public static for testing purposes.
    /// </summary>
    public static PropertyMapperRegistry CreatePropertyMapperRegistry(
        ILogger<PropertyMapperRegistry>? logger = null)
    {
        var registry = new PropertyMapperRegistry(logger);

        // Register tile mappers
        registry.RegisterMapper(new CollisionMapper());
        registry.RegisterMapper(new LedgeMapper());
        registry.RegisterMapper(new EncounterZoneMapper());
        registry.RegisterMapper(new TerrainTypeMapper());
        registry.RegisterMapper(new ScriptMapper());

        // Register entity mappers (for objects)
        registry.RegisterMapper(new InteractionMapper());
        registry.RegisterMapper(new NpcMapper());

        // Add more mappers here as needed
        // registry.RegisterMapper(new WarpPointMapper());
        // registry.RegisterMapper(new ItemMapper());

        return registry;
    }
}
```

## Integration with MapLoader

Update MapLoader to use the injected PropertyMapperRegistry:

```csharp
// In Program.cs or wherever services are configured
builder.Services.AddSingleton<MapLoader>(provider =>
{
    var assetManager = provider.GetRequiredService<IAssetProvider>();
    var propertyMapperRegistry = provider.GetRequiredService<PropertyMapperRegistry>();
    var entityFactory = provider.GetService<IEntityFactoryService>();
    var logger = provider.GetService<ILogger<MapLoader>>();

    return new MapLoader(assetManager, propertyMapperRegistry, entityFactory, logger);
});
```

## Testing Setup

For unit tests, create a registry without DI:

```csharp
[SetUp]
public void Setup()
{
    // Create registry manually for testing
    _registry = PropertyMapperServiceExtensions.CreatePropertyMapperRegistry();

    // Or create minimal registry with only needed mappers
    _registry = new PropertyMapperRegistry();
    _registry.RegisterMapper(new CollisionMapper());
    _registry.RegisterMapper(new LedgeMapper());
}
```

## Migration Path

### Phase 1: Backward Compatible (Current)
- MapLoader constructor accepts `PropertyMapperRegistry?` (nullable)
- Falls back to legacy hardcoded mapping if null
- No breaking changes

```csharp
public MapLoader(
    IAssetProvider assetManager,
    PropertyMapperRegistry? propertyMapperRegistry = null,  // ← Nullable for backward compatibility
    IEntityFactoryService? entityFactory = null,
    ILogger<MapLoader>? logger = null)
```

### Phase 2: Gradual Adoption
- Existing code continues to work
- New code uses DI to inject registry
- Tests can use either approach

### Phase 3: Full Migration
- Make `PropertyMapperRegistry` required (non-nullable)
- Remove legacy fallback code
- Update all instantiation sites

## Example: Complete Setup

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PokeSharp.Core.Mapping;
using PokeSharp.Rendering.Loaders;

var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.SetMinimumLevel(LogLevel.Debug);
});

// Add property mappers
builder.Services.AddPropertyMappers();

// Add asset management
builder.Services.AddSingleton<IAssetProvider, AssetManager>();

// Add entity factory
builder.Services.AddSingleton<IEntityFactoryService, EntityFactoryService>();

// Add map loader with injected dependencies
builder.Services.AddSingleton<MapLoader>();

var host = builder.Build();

// Use MapLoader
var mapLoader = host.Services.GetRequiredService<MapLoader>();
var world = World.Create();
var mapEntity = mapLoader.LoadMapEntities(world, "maps/pallet_town.json");
```

## Best Practices

1. **Singleton Registration**: PropertyMapperRegistry should be singleton
   - Mappers are stateless
   - Registry is thread-safe
   - No need for multiple instances

2. **Explicit Registration**: Prefer explicit mapper registration over auto-discovery
   - Clear visibility of registered mappers
   - Compile-time safety
   - Easier to debug

3. **Testability**: Expose factory method for testing
   - Tests can create registry without DI
   - Easier to mock/stub mappers
   - Faster test setup

4. **Logging**: Inject logger for debugging
   - Track which mappers are registered
   - Monitor mapping failures
   - Useful for troubleshooting

## Performance Considerations

- Registry creation is one-time cost at startup
- Mapper instances are created once and reused
- No per-tile allocation overhead
- Reflection only used during registration, not during mapping

## Adding New Mappers

To add a new mapper:

1. Create mapper class implementing `IEntityPropertyMapper<T>`
2. Add registration in `CreatePropertyMapperRegistry` method
3. That's it! No other changes needed.

```csharp
// 1. Create mapper
public class WarpPointMapper : IEntityPropertyMapper<WarpPoint>
{
    // Implementation...
}

// 2. Register in CreatePropertyMapperRegistry
public static PropertyMapperRegistry CreatePropertyMapperRegistry(...)
{
    var registry = new PropertyMapperRegistry(logger);

    // Existing mappers...
    registry.RegisterMapper(new CollisionMapper());
    registry.RegisterMapper(new LedgeMapper());

    // New mapper
    registry.RegisterMapper(new WarpPointMapper());  // ← Add this line

    return registry;
}
```

## Conclusion

The hybrid approach provides the best balance of:
- ✅ Explicit control over registered mappers
- ✅ Easy testing without DI
- ✅ Clear, maintainable code
- ✅ Backward compatibility
- ✅ Extensibility

Choose the approach that best fits your project's needs and team preferences.
