# Null-Handling Standards for PokeSharp

## Overview

This document establishes consistent null-handling patterns across the PokeSharp codebase to improve code clarity, safety, and maintainability.

## Core Principles

1. **Required dependencies MUST NOT be null** - Use validation to enforce this
2. **Optional dependencies MAY be null** - Use nullable reference types to document this
3. **Initialization state should be explicit** - Don't rely on null to track initialization

---

## Standard Patterns

### Pattern 1: Required Constructor Dependencies

✅ **Correct:**
```csharp
public class MovementSystem : SystemBase
{
    private readonly SpatialHashSystem _spatialHash;

    public MovementSystem(SpatialHashSystem spatialHash)
    {
        _spatialHash = spatialHash ?? throw new ArgumentNullException(nameof(spatialHash));
        // OR (C# 11+):
        ArgumentNullException.ThrowIfNull(spatialHash);
        _spatialHash = spatialHash;
    }
}
```

❌ **Avoid:**
```csharp
private readonly SpatialHashSystem? _spatialHash;  // DON'T make required deps nullable

public MovementSystem(SpatialHashSystem? spatialHash)  // DON'T allow null
{
    _spatialHash = spatialHash;  // DON'T skip validation
}
```

**Rationale:** Required dependencies should fail fast at construction time, not later during usage.

---

### Pattern 2: Optional Dependencies (Loggers, etc.)

✅ **Correct:**
```csharp
public class MovementSystem : SystemBase
{
    private readonly ILogger<MovementSystem>? _logger;

    public MovementSystem(SpatialHashSystem spatialHash, ILogger<MovementSystem>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(spatialHash);
        _spatialHash = spatialHash;
        _logger = logger;  // Nullable - no validation needed
    }

    public void SomeMethod()
    {
        _logger?.LogDebug("Using null-conditional operator");
    }
}
```

❌ **Avoid:**
```csharp
private readonly ILogger<MovementSystem> _logger;  // DON'T omit ? for optional deps

public MovementSystem(ILogger<MovementSystem> logger)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));  // DON'T enforce non-null
}
```

**Rationale:** Loggers and similar optional services should be explicitly nullable. Use null-conditional operators (`?.`) to safely access them.

---

### Pattern 3: Initialization State Tracking

✅ **Correct (Option A - Explicit Flag):**
```csharp
public abstract class SystemBase : ISystem
{
    private bool _initialized;
    protected World World { get; private set; } = null!;  // null-forgiving for delayed init

    public virtual void Initialize(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        World = world;
        _initialized = true;
    }

    protected void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException($"{GetType().Name} not initialized");
    }
}
```

✅ **Correct (Option B - Nullable Check):**
```csharp
public abstract class BaseSystem : ISystem
{
    protected World? World { get; private set; }

    public virtual void Initialize(World world)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
    }

    protected void EnsureInitialized()
    {
        if (World == null)
            throw new InvalidOperationException($"{GetType().Name} not initialized");
    }
}
```

❌ **Avoid:**
```csharp
protected World World { get; private set; }  // DON'T use non-nullable without initialization

protected void EnsureInitialized()
{
    // Implicit null check relies on NullReferenceException - BAD!
    _ = World.IsAlive(Entity.Null);
}
```

**Rationale:** Initialization state should be explicit. Choose either Option A (preferred for better nullability annotations) or Option B (simpler but less type-safe).

---

### Pattern 4: Property Access Guards

✅ **Correct:**
```csharp
public class ParallelSystemBase
{
    private ParallelQueryExecutor? _parallelExecutor;

    protected ParallelQueryExecutor ParallelExecutor
    {
        get
        {
            if (_parallelExecutor == null)
                throw new InvalidOperationException("ParallelExecutor not initialized");
            return _parallelExecutor;
        }
    }

    // OR (C# 9+ with init):
    protected ParallelQueryExecutor ParallelExecutor
    {
        get => _parallelExecutor
            ?? throw new InvalidOperationException("ParallelExecutor not initialized");
    }
}
```

❌ **Avoid:**
```csharp
protected ParallelQueryExecutor ParallelExecutor { get; private set; } = null!;

// Relying on null-forgiving without guard - can throw NullReferenceException
```

**Rationale:** If a property can be uninitialized, provide a clear error message instead of allowing `NullReferenceException`.

---

### Pattern 5: Method Parameter Validation

✅ **Correct:**
```csharp
public void ProcessEntity(World world, Entity entity, string name)
{
    ArgumentNullException.ThrowIfNull(world);
    // Entity is struct - no null check needed
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    // Method logic...
}
```

❌ **Avoid:**
```csharp
public void ProcessEntity(World world, Entity entity, string name)
{
    // DON'T skip validation
    world.Add(entity, new Name(name));  // May throw NullReferenceException
}
```

**Rationale:** Public methods should validate parameters at the entry point.

---

## Decision Matrix

| Scenario | Pattern | Example |
|----------|---------|---------|
| Required dependency | Non-nullable + validation | `ArgumentNullException.ThrowIfNull(service)` |
| Optional dependency | Nullable + null-conditional | `ILogger<T>? logger = null` + `logger?.Log()` |
| Delayed initialization (known safe) | Non-nullable + `null!` + flag | `World { get; set; } = null!;` + `_initialized` |
| Delayed initialization (safer) | Nullable + check | `World? { get; set; }` + `if (World == null)` |
| Uninitialized property access | Guarded property | `get => _field ?? throw` |
| Public method parameters | Explicit validation | `ArgumentNullException.ThrowIfNull()` |

---

## Migration Strategy

1. **Phase 1** (Completed): Document standards
2. **Phase 2** (Optional): Update high-traffic code paths (systems, managers)
3. **Phase 3** (Optional): Update remaining code during refactoring

**Note:** Don't refactor purely for consistency unless touching the code for other reasons. The mixed patterns are not bugs, just style inconsistencies.

---

## Examples from Codebase

### ✅ Good Example: MovementSystem

```csharp
public class MovementSystem : ParallelSystemBase
{
    private readonly ILogger<MovementSystem>? _logger;  // Optional
    private readonly SpatialHashSystem _spatialHashSystem;  // Required

    public MovementSystem(
        SpatialHashSystem spatialHashSystem,
        ILogger<MovementSystem>? logger = null)
    {
        _spatialHashSystem = spatialHashSystem
            ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        _logger = logger;
    }

    public override void Update(World world, float deltaTime)
    {
        EnsureInitialized();  // Checks if World is set
        _logger?.LogDebug("Processing movement");
    }
}
```

### ✅ Good Example: SystemBase

```csharp
public abstract class SystemBase : ISystem
{
    private bool _initialized;
    protected World World { get; private set; } = null!;

    public virtual void Initialize(World world)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        _initialized = true;
    }

    protected void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException($"{GetType().Name} not initialized");
    }
}
```

---

## Tools & Enforcement

- **C# Nullable Reference Types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Compiler Warnings**: CS8600-CS8629 (nullable warnings) should not be suppressed
- **Code Review**: Check null-handling patterns during PR reviews
- **Analyzer Rules**: Consider adding custom analyzers for project-specific patterns

---

## Summary

| ✅ DO | ❌ DON'T |
|-------|----------|
| Use `ArgumentNullException.ThrowIfNull()` for required params | Skip null validation on required dependencies |
| Use nullable types (`T?`) for optional dependencies | Mark required dependencies as nullable |
| Use null-conditional operator (`?.`) for optional access | Rely on `NullReferenceException` for validation |
| Be explicit about initialization state | Use null to track initialization without documentation |
| Provide clear error messages | Allow generic `NullReferenceException` |

---

**Last Updated:** 2025-01-10
**Status:** Active Standard

