# ADR: System Interface Evolution - Clean Architecture

**Status**: Implemented
**Date**: 2025-11-10
**Updated**: 2025-11-10
**Context**: SystemManager Architecture

## Context

The PokeSharp ECS architecture migrated from a single `ISystem` interface to separate `IUpdateSystem` and `IRenderSystem` interfaces to better separate concerns and enable optimizations like parallel execution.

## Current State (Post-Simplification)

`SystemManager` now maintains **two clean lists**:

1. **`_updateSystems`** (List<IUpdateSystem>) - All update systems
   - Uses `UpdatePriority` property for ordering
   - Called via `Update(World, float deltaTime)`
   - Executes during the game's Update phase

2. **`_renderSystems`** (List<IRenderSystem>) - All render systems
   - Uses `RenderOrder` property for ordering
   - Called via `Render(World)`
   - Executes during the game's Draw phase

### Clean Separation

Systems are registered once in their appropriate list with no duplication:

```csharp
public virtual void RegisterUpdateSystem(IUpdateSystem system)
{
    _updateSystems.Add(system);
    _updateSystems.Sort((a, b) => a.UpdatePriority.CompareTo(b.UpdatePriority));
}

public virtual void RegisterRenderSystem(IRenderSystem system)
{
    _renderSystems.Add(system);
    _renderSystems.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
}
```

## Decision

**We have simplified to a clean, two-list architecture** with no backward compatibility burden:

### Rationale

1. **Clean Architecture**: No duplication, each system is registered once
2. **Separation of Concerns**: Update and render logic are completely separated
3. **Performance**: Optimized execution paths enable parallel processing
4. **Simplicity**: Reduced cognitive load, easier to understand and maintain

### Usage Patterns

```csharp
// Game loop (MonoGame-style)
protected override void Update(GameTime gameTime)
{
    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _systemManager.Update(_world, deltaTime);  // Execute all update systems
}

protected override void Draw(GameTime gameTime)
{
    _systemManager.Render(_world);  // Execute all render systems
}
```

### System Hierarchy

```
IUpdateSystem (for game logic)
├─ UpdatePriority: int
├─ Enabled: bool
└─ Update(World, float)

IRenderSystem (for rendering)
├─ RenderOrder: int
├─ Enabled: bool
└─ Render(World)

SystemBase (abstract base class, implements ISystem for Initialize())
├─ Priority: int (legacy, can be ignored)
├─ Initialize(World)
└─ Used by most systems for common functionality
```

**Note**: `ISystem` still exists for the `Initialize(World)` method, but systems are registered via their specific interfaces (`IUpdateSystem` or `IRenderSystem`).

## Consequences

### Positive

✅ **Clean Architecture**: Single responsibility per system, no list duplication
✅ **Clear Separation**: Update and render concerns are completely separate
✅ **Simplified Code**: Fewer lists, less complexity, easier maintenance
✅ **Performance**: Optimized execution paths (e.g., parallel update systems)
✅ **Better Testing**: Clear boundaries make unit testing easier
✅ **Predictable Behavior**: No hidden dual-registration logic

### Negative

None - this is a pure simplification with no downsides.

### Changes Made

1. **Removed**: Legacy `_systems` list and `RegisterSystem(ISystem)` method
2. **Simplified**: `Update()` and `Render()` methods now work directly with specific lists
3. **Updated**: Game loop calls simplified to `Update()` and `Render()`
4. **Cleaned**: Performance tracking now looks up systems from appropriate list

## Related Systems

- **`SystemBase`**: Abstract base class implementing `ISystem` for common functionality
- **`ParallelSystemBase`**: Extends `SystemBase` with parallel execution support
- **`ParallelSystemManager`**: Extends `SystemManager` with dependency-aware parallel execution
- **`SystemPerformanceTracker`**: Dedicated class for tracking system performance metrics
- **`PerformanceConfiguration`**: Configurable performance thresholds and limits

## Implementation Details

### System Registration

```csharp
// Register update systems (game logic)
systemManager.RegisterUpdateSystem(new MovementSystem(spatialQuery, logger));
systemManager.RegisterUpdateSystem(new CollisionSystem(spatialQuery, logger));
systemManager.RegisterUpdateSystem(new NPCBehaviorSystem(logger, loggerFactory, apis));

// Register render systems (drawing)
systemManager.RegisterRenderSystem(new RenderSystem(spriteBatch, assetManager));
```

### System Count

The `SystemCount` property returns the combined total of update and render systems:

```csharp
public int SystemCount => _updateSystems.Count + _renderSystems.Count;
```

### Initialization

Both update and render systems are initialized in a single pass:

```csharp
public void Initialize(World world)
{
    // Initialize all update systems
    foreach (var system in _updateSystems)
        if (system is ISystem legacySystem)
            legacySystem.Initialize(world);

    // Initialize all render systems
    foreach (var system in _renderSystems)
        if (system is ISystem legacySystem)
            legacySystem.Initialize(world);
}
```

## References

- **Implementation**: `PokeSharp.Core/Systems/SystemManager.cs`
- **Interfaces**: `PokeSharp.Core/Systems/IUpdateSystem.cs`, `IRenderSystem.cs`, `ISystem.cs`
- **Related ADR**: ADR_PARALLEL_EXECUTION_SYSTEM.md for parallel execution architecture
- **Performance**: `PokeSharp.Core/Systems/SystemPerformanceTracker.cs`
- **Configuration**: `PokeSharp.Core/Configuration/PerformanceConfiguration.cs`

