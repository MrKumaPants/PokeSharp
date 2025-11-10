# PokeSharp Dependency Injection

## ⚠️ Deprecated - Custom DI Removed

**As of 2025-01-10**: The custom `ServiceContainer` and `SystemFactory` have been removed in favor of using **Microsoft.Extensions.DependencyInjection** throughout the codebase.

### Why the Change?

The custom DI infrastructure was:
1. **Never actually used** in the codebase - all systems were created with explicit constructor injection
2. **A Service Locator anti-pattern** - Made dependencies implicit instead of explicit
3. **Redundant** - Microsoft's DI container was already being used in `Program.cs`
4. **Violating Single Responsibility Principle** - `SystemManager` was managing both system lifecycle AND dependency injection

### Current Approach ✅

PokeSharp now uses a **single DI container** (Microsoft.Extensions.DependencyInjection) configured in `Program.cs`:

```csharp
// In Program.cs
var services = new ServiceCollection();
services.AddSingleton<ILoggerFactory>(loggerFactory);
services.AddGameServices();  // Extension method
var serviceProvider = services.BuildServiceProvider();
```

### How Systems Are Created

Systems are created with **explicit constructor injection**:

```csharp
// In GameInitializer.cs
var spatialHashLogger = _loggerFactory.CreateLogger<SpatialHashSystem>();
var spatialHashSystem = new SpatialHashSystem(spatialHashLogger);
_systemManager.RegisterUpdateSystem(spatialHashSystem);

var movementLogger = _loggerFactory.CreateLogger<MovementSystem>();
var movementSystem = new MovementSystem(spatialHashSystem, movementLogger);
_systemManager.RegisterUpdateSystem(movementSystem);
```

### Benefits of This Approach

✅ **Explicit Dependencies** - All dependencies visible in constructors
✅ **Compile-Time Safety** - Missing dependencies cause build errors, not runtime failures
✅ **Easy Testing** - Dependencies can be mocked/stubbed directly
✅ **Single DI Container** - No confusion about which container manages what
✅ **SOLID Compliance** - Follows Dependency Inversion Principle properly
✅ **Standard .NET Pattern** - Uses familiar Microsoft.Extensions.DependencyInjection

### Creating New Systems

Systems should declare all dependencies in their constructor:

```csharp
public class MySystem : SystemBase, IUpdateSystem
{
    private readonly SpatialHashSystem _spatialHash;
    private readonly ILogger<MySystem>? _logger;

    public MySystem(
        SpatialHashSystem spatialHash,
        ILogger<MySystem>? logger = null)
    {
        _spatialHash = spatialHash ?? throw new ArgumentNullException(nameof(spatialHash));
        _logger = logger;
    }

    public int UpdatePriority => SystemPriority.Movement;
    public override int Priority => SystemPriority.Movement;

    public override void Update(World world, float deltaTime)
    {
        EnsureInitialized();
        // Use _spatialHash and _logger...
    }
}
```

Then instantiate in an initializer with dependencies:

```csharp
// In GameInitializer or similar
var mySystem = new MySystem(spatialHashSystem, logger);
systemManager.RegisterUpdateSystem(mySystem);
```

## Documentation References

For historical context, see:
- [Migration Guide](/docs/DI_MIGRATION_GUIDE.md) - Original migration plan (now obsolete)
- [Architecture Decision Record](/docs/ARCHITECTURE_DECISION_DI.md) - Original design rationale

## Current Best Practices

1. ✅ Use `Microsoft.Extensions.DependencyInjection` for all service registration
2. ✅ Create systems with explicit constructor injection
3. ✅ Use `ILogger<T>` for logging (from Microsoft.Extensions.Logging)
4. ✅ Validate required dependencies with `ArgumentNullException.ThrowIfNull()`
5. ✅ Use nullable types (`T?`) for optional dependencies
6. ✅ Register systems by passing instances: `RegisterUpdateSystem(system)`
7. ✅ Inherit from `SystemBase` or `ParallelSystemBase` for new systems

## Migration Notes

If you have old documentation or examples referencing:
- `ServiceContainer` → Use `IServiceCollection` / `IServiceProvider`
- `SystemFactory` → Create systems manually with `new`
- `systemManager.RegisterSystem<T>()` → Use `systemManager.RegisterUpdateSystem(new T(...))`
- `systemManager.RegisterService()` → Use `services.AddSingleton()` in `Program.cs`

These patterns are no longer supported.
