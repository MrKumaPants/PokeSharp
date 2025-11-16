# Arch ECS Best Practices Research Report

## Executive Summary

This document compiles best practices for Arch ECS (Entity Component System) and Entity Framework usage, focusing on performance-critical patterns for game development in C#.

**Research Date:** 2025-11-16
**Target Framework:** Arch ECS for C# (.NET Standard 2.1, .NET Core 6/8)
**Project Context:** PokeSharp - Pokemon-style game engine

---

## 1. Arch ECS Overview

### What is Arch ECS?

Arch is a high-performance C# based **Archetype & Chunks** Entity Component System (ECS) with optional multithreading. It's designed for game development and data-oriented programming.

### Key Performance Characteristics

- **Average Performance:** Arch is on average quite faster than other C# ECS implementations
- **Memory Layout:** Uses archetype-based storage for optimal cache locality
- **Compatibility:** Supports .NET Standard 2.1, .NET Core 6 and 8
- **Platform Support:** Works with Unity, Godot, and MonoGame

---

## 2. Core Performance Principles

### 2.1 Query Optimization

**Rule:** The less you query, the faster it is. This applies to:
- **Number of components queried** (fewer is better)
- **Size of components** (smaller is faster)

**Archetype Advantage:**
```
Archetype-based ECS projects are in lead when querying multiple components,
as returned components are sequentially stored in memory providing high cache hit rate.
```

### 2.2 Memory Locality

**Best Practice:** Data for all instances of a component are contiguously stored in physical memory, enabling efficient access when systems operate over many entities.

**Implementation in PokeSharp:**
```csharp
// ✅ GOOD: Query cached descriptions (zero allocations)
world.Query(in EcsQueries.MovementWithAnimation,
    (Entity entity, ref Position position, ref GridMovement movement, ref Animation animation) =>
    {
        // All Position components are contiguous in memory
        // All GridMovement components are contiguous
        // High cache hit rate!
    });
```

### 2.3 Bulk Entity Creation

**Rule:** If bulk creation is available, use it. Bulk creation is faster than creating entities one by one.

**Anti-Pattern to Avoid:**
```csharp
// ❌ BAD: Creating entities one by one in a loop
for (int i = 0; i < 1000; i++)
{
    var entity = world.Create<Position, Velocity>();
}

// ✅ GOOD: Use bulk operations when available
var entities = world.Reserve<Position, Velocity>(1000);
```

---

## 3. Query Caching and Reuse

### 3.1 Centralized Query Cache

**Best Practice:** Cache QueryDescription instances to eliminate per-frame allocations.

**PokeSharp Implementation:**
```csharp
// ✅ EXCELLENT: Centralized query cache (PokeSharp.Engine.Systems.Queries.Queries)
public static class Queries
{
    public static readonly QueryDescription Movement = QueryCache.Get<Position, GridMovement>();
    public static readonly QueryDescription MovementWithAnimation = QueryCache.Get<Position, GridMovement, Animation>();
    public static readonly QueryDescription MovementWithoutAnimation = QueryCache.GetWithNone<Position, GridMovement, Animation>();
}

// Usage: Zero allocations per frame
world.Query(in Queries.Movement, (ref Position pos, ref GridMovement mov) => { });
```

### 3.2 Query Matching Strategy

**Performance Note:** Query caching is needed because potentially expensive matching is done only during initialization. This allows iteration within a frame without paying for matching logic per use.

---

## 4. System Iteration Patterns

### 4.1 Component Access Optimization

**Critical Optimization: Single Query vs Multiple Queries**

From ElevationRenderSystem analysis:
```csharp
// ❌ OLD APPROACH: Two separate queries + expensive Has() checks
world.Query(tilesWithOffset, ...);
world.Query(tilesWithoutOffset, ...);
if (world.Has<LayerOffset>(entity)) { ... } // 200+ expensive checks!

// ✅ NEW APPROACH: Single query with TryGet for optional components
world.Query(in _tileQuery, (Entity entity, ref TilePosition pos, ref TileSprite sprite) =>
{
    // TryGet is faster than Has() + Get() (single lookup)
    if (world.TryGet(entity, out LayerOffset offset))
    {
        // Use offset
    }
    else
    {
        // Standard position
    }
});

// Performance: Eliminated 11ms spikes from Has() checks
```

### 4.2 Reuse Static Instances

**Memory Allocation Optimization:**

```csharp
// ❌ BAD: Creating new instances every frame (400-600 allocations/frame!)
world.Query(in query, (ref Position pos) =>
{
    var renderPos = new Vector2(pos.X, pos.Y); // Allocation!
    var sourceRect = new Rectangle(0, 0, 16, 16); // Allocation!
});

// ✅ GOOD: Reuse static instances (PokeSharp pattern)
private static Vector2 _reusablePosition = Vector2.Zero;
private static Rectangle _reusableSourceRect = Rectangle.Empty;

world.Query(in query, (ref Position pos) =>
{
    _reusablePosition.X = pos.X;
    _reusablePosition.Y = pos.Y;
    // Zero allocations!
});
```

**Result:** Eliminated 400-600 allocations per frame in ElevationRenderSystem.

---

## 5. Common Anti-Patterns to Avoid

### 5.1 Archetype Transition Thrashing

**Problem:** Adding/removing components causes expensive archetype transitions.

**PokeSharp Solution: Component Pooling**
```csharp
// ❌ BAD: Remove component after use (causes archetype transition - 186ms spikes!)
world.Remove<MovementRequest>(entity);

// ✅ GOOD: Mark component inactive instead (component pooling)
request.Active = false; // No archetype change!
```

**Performance Impact:** Eliminated 186ms spikes in MovementSystem.

### 5.2 Redundant Spatial Hash Queries

**Problem:** Multiple collision checks cause redundant queries.

**Optimization:**
```csharp
// ❌ BAD: 3 separate queries (6.25ms)
var isLedge = collisionService.IsLedge(x, y);
var jumpDir = collisionService.GetLedgeJumpDirection(x, y);
var walkable = collisionService.IsPositionWalkable(x, y);

// ✅ GOOD: Single combined query (1.5ms - 75% reduction!)
var (isLedge, jumpDir, walkable) = collisionService.GetTileCollisionInfo(x, y, elevation, direction);
```

### 5.3 Unnecessary Query Component Size

**Rule:** Only query the components you need. Smaller component sets = faster iteration.

```csharp
// ❌ BAD: Querying extra components
world.Query(QueryCache.Get<Position, Velocity, Health, Armor>(), ...);

// ✅ GOOD: Only query what you need
world.Query(QueryCache.Get<Position, Velocity>(), ...);
```

### 5.4 God Objects and Singleton Abuse

**ECS Principle:** Favor dependency injection over global state. Avoid anti-patterns like God Objects.

**PokeSharp Pattern:**
```csharp
// ✅ GOOD: Dependency injection in systems
public MovementSystem(ICollisionService collisionService, ILogger<MovementSystem>? logger = null)
{
    _collisionService = collisionService ?? throw new ArgumentNullException(nameof(collisionService));
    _logger = logger;
}
```

---

## 6. Advanced Patterns

### 6.1 Change Version Tracking

**Optimization:** Skip chunks where components haven't changed since last system execution.

**Unity ECS Approach:** If components in a chunk have a ChangeVersion lower than LastSystemVersion (not accessed with write permission since last execution), skip the chunk.

**PokeSharp Adaptation:** Camera dirty flag pattern
```csharp
// Only recalculate if camera changed (dirty flag optimization)
if (!camera.IsDirty && _cachedCameraTransform != Matrix.Identity)
    return;

camera.IsDirty = false; // Reset after recalculation
```

### 6.2 Component Value Filtering

**Pattern:** Group entities by specific component values, not just by archetype.

**Use Case:** Cull entities outside camera bounds
```csharp
if (cameraBounds.HasValue)
    if (pos.X < cameraBounds.Value.Left || pos.X >= cameraBounds.Value.Right)
    {
        tilesCulled++;
        return; // Skip this entity
    }
```

### 6.3 Sparse Set for Fast Lookups

**Data Structure:** Sparse integer set allows:
- **O(N)** contiguous iteration
- **O(1)** insertion/removal
- **O(1)** querying for specific integers

**Use Case:** Spatial hash grids for collision detection.

---

## 7. Bulk Operations

### 7.1 Batch Processing

**Best Practice:** Use bulk query operations for batch entity processing.

**PokeSharp Implementation:**
```csharp
// Collect entities with component data efficiently
var healthData = bulkQuery.CollectWithComponent<Health>(query);

// Apply action to all matching entities
bulkQuery.ForEach<Health>(query, (Entity entity, ref Health health) =>
{
    health.CurrentHP = Math.Min(health.CurrentHP + 10, health.MaxHP);
});

// Batch destroy
int destroyed = bulkQuery.DestroyMatching(query);
```

### 7.2 Network Roundtrip Minimization

**Pattern:** Batch operations to reduce roundtrips (applies to network and database operations).

---

## 8. Profiling and Monitoring

### 8.1 Performance Tracking

**PokeSharp Pattern:**
```csharp
// Conditional detailed profiling (only when enabled)
if (_enableDetailedProfiling)
{
    var sw = Stopwatch.StartNew();
    RenderTiles(world);
    sw.Stop();
    _logger?.LogDebug("Tile render: {TimeMs}ms", sw.Elapsed.TotalMilliseconds);
}
```

### 8.2 Performance Logging Intervals

**Best Practice:** Log every N frames to avoid log spam.

```csharp
if (_frameCounter % RenderingConstants.PerformanceLogInterval == 0)
{
    _logger?.LogRenderStats(totalEntities, tilesRendered, spriteCount, _frameCounter);
}
```

---

## 9. Entity Framework Performance (For Data Layer)

While Arch ECS handles game entities, Entity Framework might be used for save data, configuration, etc.

### 9.1 Read-Only Queries

**Critical:** Use `AsNoTracking()` for read-only operations to avoid change tracking overhead.

```csharp
// ✅ GOOD: Read-only query
var items = dbContext.Items.AsNoTracking().Where(x => x.Category == "Potion").ToList();
```

### 9.2 Projection over Full Entities

**Rule:** Use `.Select()` to retrieve only needed columns.

```csharp
// ❌ BAD: Loading entire entity
var items = dbContext.Items.ToList();

// ✅ GOOD: Project only needed fields
var itemNames = dbContext.Items.Select(x => new { x.Id, x.Name }).ToList();
```

### 9.3 Avoid N+1 Queries

**Solution:** Use eager loading with `.Include()`.

```csharp
// ❌ BAD: N+1 problem
var trainers = dbContext.Trainers.ToList();
foreach (var trainer in trainers)
{
    var pokemon = dbContext.Pokemon.Where(p => p.TrainerId == trainer.Id).ToList(); // N queries!
}

// ✅ GOOD: Eager loading
var trainers = dbContext.Trainers.Include(t => t.Pokemon).ToList(); // 1 query
```

### 9.4 Pagination for Large Sets

**Always paginate:**
```csharp
var page = dbContext.Items
    .Skip(pageNumber * pageSize)
    .Take(pageSize)
    .ToList();
```

### 9.5 Database Indexes

**Critical:** The main factor in query speed is whether indexes are properly utilized. Create indexes on frequently queried columns.

### 9.6 Raw SQL When Needed

**Use Case:** When EF generates suboptimal SQL, write raw queries.

```csharp
var results = dbContext.Items.FromSqlRaw("SELECT * FROM Items WHERE Category = {0}", category).ToList();
```

---

## 10. PokeSharp-Specific Findings

### 10.1 Observed Patterns (From Codebase Analysis)

**Strengths:**
1. ✅ Centralized query cache (`Queries.cs`) - zero allocations
2. ✅ Component pooling for `MovementRequest` - avoids archetype transitions
3. ✅ Static reusable instances (Vector2, Rectangle) - eliminates 400-600 allocations/frame
4. ✅ Single TryGet pattern for optional components - eliminates 200+ Has() calls
5. ✅ Dirty flag optimization for camera - avoids redundant calculations
6. ✅ Cached collision queries - 75% reduction (6.25ms → 1.5ms)
7. ✅ Pre-calculated direction names array - avoids ToString() allocations

**Areas for Consideration:**
1. Consider bulk entity creation for map initialization
2. Review if change version tracking could optimize more systems
3. Monitor for any remaining archetype transitions during gameplay

### 10.2 Tile Size Caching Pattern

```csharp
// Dictionary cache for map-specific tile sizes
private readonly Dictionary<int, int> _tileSizeCache = new();

private int GetTileSize(World world, int mapId)
{
    if (_tileSizeCache.TryGetValue(mapId, out var cachedSize))
        return cachedSize;

    // Query and cache
    _tileSizeCache[mapId] = tileSize;
    return tileSize;
}
```

### 10.3 Lazy Texture Loading with Delegate Caching

**Optimization:** Create delegate once to eliminate reflection overhead.

```csharp
// ❌ OLD: Reflection on every texture miss (~2.0ms)
var method = loader.GetType().GetMethod("LoadSpriteTexture");
method.Invoke(loader, new object[] { category, spriteName });

// ✅ NEW: Cached delegate (~0.5-1.0ms, 60% improvement)
private Action<string, string>? _spriteLoadDelegate;

// Created once during initialization
_spriteLoadDelegate = (Action<string, string>)
    Delegate.CreateDelegate(typeof(Action<string, string>), loader, method);

// Direct invocation - zero reflection!
_spriteLoadDelegate(category, spriteName);
```

---

## 11. Performance Benchmarks

### 11.1 Measured Improvements in PokeSharp

| Optimization | Before | After | Improvement |
|-------------|--------|-------|-------------|
| Component Pooling (MovementRequest) | 186ms spikes | <1ms | 99.5% |
| Single TryGet vs Has() checks | 11ms | <1ms | ~90% |
| Combined Collision Query | 6.25ms | 1.5ms | 75% |
| Static Instance Reuse | 400-600 allocs/frame | 0 allocs | 100% |
| Lazy Load Delegate | ~2.0ms | 0.5-1.0ms | 60% |

### 11.2 Entity Scale Recommendations

**PokeSharp Context:** "Optimized for Pokemon-style games with <50 moving entities"

**Guidance:**
- **<50 entities:** Sequential queries are optimal (current approach)
- **50-500 entities:** Consider parallel queries for performance gains
- **>500 entities:** Mandatory parallelization with Arch's multithreading support

---

## 12. Summary: Top 10 Best Practices

1. **Cache Query Descriptions** - Use centralized static query cache
2. **Minimize Components Queried** - Only query what you need
3. **Use TryGet for Optional Components** - Faster than Has() + Get()
4. **Avoid Archetype Transitions** - Use component pooling patterns
5. **Reuse Static Instances** - Eliminate allocations (Vector2, Rectangle, etc.)
6. **Batch Collision Queries** - Combine related queries into single calls
7. **Implement Dirty Flags** - Skip unchanged data with change tracking
8. **Use Viewport Culling** - Don't process off-screen entities
9. **Enable Conditional Profiling** - Monitor without adding overhead
10. **Leverage Data Locality** - Archetype storage = cache-friendly iteration

---

## 13. References

### Primary Sources
- Arch ECS GitHub: https://github.com/genaray/Arch
- Arch ECS Documentation: https://arch-ecs.gitbook.io/arch
- ECS FAQ: https://github.com/SanderMertens/ecs-faq
- Game Programming Patterns: https://gameprogrammingpatterns.com/component.html

### Benchmarks
- C# ECS Benchmarks: https://github.com/friflo/ECS.CSharp.Benchmark-common-use-cases
- Arch Performance Comparisons: https://github.com/Doraku/Ecs.CSharp.Benchmark

### Entity Framework
- Microsoft EF Core Performance: https://learn.microsoft.com/en-us/ef/core/performance/
- EF Core Best Practices: https://code-maze.com/entity-framework-core-best-practices/

---

**Report Compiled By:** Research Agent (Hive Mind Swarm)
**Status:** Complete
**Next Steps:** Share findings with Coder and Optimizer agents for implementation
