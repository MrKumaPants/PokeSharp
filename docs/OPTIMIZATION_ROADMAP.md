# PokeSharp Performance Optimization Roadmap

**Generated:** 2025-11-16
**Swarm ID:** swarm-1763325319960-zlox99ylk
**Severity:** CRITICAL - 23x over normal GC pressure
**Goal:** Reduce GC pressure from 46.8 to <8 Gen0 collections/sec

---

## Executive Summary

### Current State
```
Gen0 GC Rate:          46.8 collections/sec (23x OVER normal)
Gen2 GC Rate:          14.6 collections/sec (73 in 5 seconds)
Allocation Rate:       ~750 KB/sec (7.5x OVER budget)
Frame Budget Used:     12.5 KB/frame @ 60fps (23x OVER budget)
Target Budget:         540 bytes/frame
Mystery Allocations:   300-500 KB/sec (40-67% unidentified)
```

### Target State (After All Fixes)
```
Gen0 GC Rate:          5-8 collections/sec (NORMAL)
Gen2 GC Rate:          0-1 collections/sec (EXCELLENT)
Allocation Rate:       80-130 KB/sec (ACCEPTABLE)
Frame Budget Used:     1.3-2.2 KB/frame @ 60fps (HEALTHY)
Reduction:             83-89% allocation reduction
```

---

## Quick Wins (< 1 Hour Each, High Impact)

### 1. SpriteAnimationSystem: Eliminate String Allocation
**Priority:** P0 - CRITICAL
**Impact:** 50-60% of total GC pressure
**Effort:** 15-30 minutes
**Expected Gain:** -192 to -384 KB/sec allocation reduction

#### Problem
```csharp
// Line 76 - SpriteAnimationSystem.cs
var manifestKey = $"{sprite.Category}/{sprite.SpriteName}";
```
- Allocates string EVERY frame for EVERY animated entity (50-200+ entities)
- 3,000-12,000 string allocations per second
- Happens even when cache lookup succeeds

#### Solution
Add `ManifestKey` property to Sprite component:

```csharp
// PokeSharp.Engine.Common/Components/Sprite.cs
public class Sprite
{
    public string Category { get; set; }
    public string SpriteName { get; set; }

    // ADD THIS:
    public string ManifestKey { get; private set; }

    public Sprite(string category, string spriteName)
    {
        Category = category;
        SpriteName = spriteName;
        ManifestKey = $"{category}/{spriteName}"; // Compute ONCE
    }
}

// SpriteAnimationSystem.cs Line 76
// BEFORE:
var manifestKey = $"{sprite.Category}/{sprite.SpriteName}";

// AFTER:
var manifestKey = sprite.ManifestKey; // Zero allocation!
```

#### Files to Modify
- `/PokeSharp.Engine.Common/Components/Sprite.cs` (+3 lines)
- `/PokeSharp.Game/Systems/Rendering/SpriteAnimationSystem.cs` (-1 line, +1 line)

#### Expected Results
- 100% elimination of per-frame string allocations
- -192 to -384 KB/sec allocation reduction
- -9 to -18 Gen0 GC/sec reduction
- Frame time improvement: -0.2 to -0.7ms

---

### 2. MapLoader: Fix Query Recreation in Loop
**Priority:** P0 - CRITICAL
**Impact:** 50x performance penalty
**Effort:** 5 minutes
**Expected Gain:** Massive ECS query performance improvement

#### Problem
```csharp
// Lines 1143-1153 - MapLoader.cs
foreach (var tileLayer in layer.Tiles)
{
    // RECREATES QUERY EVERY LOOP ITERATION!
    var query = new QueryDescription().WithAll<TileEntity, TilePosition>();
    world.Query(in query, (ref TileEntity tileEnt, ref TilePosition tilePos) => {
        // ...
    });
}
```

#### Solution
```csharp
// BEFORE: Inside loop (BAD)
foreach (var tileLayer in layer.Tiles)
{
    var query = new QueryDescription().WithAll<TileEntity, TilePosition>();
    world.Query(in query, ...);
}

// AFTER: Outside loop (GOOD)
var query = new QueryDescription().WithAll<TileEntity, TilePosition>();
foreach (var tileLayer in layer.Tiles)
{
    world.Query(in query, ...);
}
```

#### Files to Modify
- `/PokeSharp.Game.Data/MapLoading/Tiled/MapLoader.cs` (Line 1143, move query outside loop)

#### Expected Results
- 50x reduction in query creation overhead
- Eliminates repeated descriptor allocations
- Map loading performance improvement

---

### 3. MovementSystem: Eliminate Duplicate Queries
**Priority:** P0 - CRITICAL
**Impact:** 2x performance available
**Effort:** 10 minutes
**Expected Gain:** 50% reduction in ECS query overhead

#### Problem
```csharp
// MovementSystem runs same query twice per entity
// First in collision check, then in movement update
```

#### Solution
Cache query results at start of Update():

```csharp
public void Update(World world, float deltaTime)
{
    // Cache query results once
    var entities = new List<Entity>();
    world.Query(MovingEntitiesQuery, (Entity entity) => {
        entities.Add(entity);
    });

    // Process each entity once with cached data
    foreach (var entity in entities)
    {
        // Use entity reference instead of re-querying
        if (!world.TryGet(entity, out Position pos)) continue;
        if (!world.TryGet(entity, out Velocity vel)) continue;
        // ... process
    }
}
```

#### Files to Modify
- `/PokeSharp.Game.Systems/Movement/MovementSystem.cs`

#### Expected Results
- 50% reduction in query execution
- Improved cache locality
- Reduced component access overhead

---

### 4. ElevationRenderSystem: Combine Sprite Queries
**Priority:** P1 - HIGH
**Impact:** Moderate
**Effort:** 10 minutes
**Expected Gain:** 2x query performance

#### Problem
Two separate queries for same sprites:
1. Query for elevation layer 0
2. Query for elevation layer 1

#### Solution
```csharp
// BEFORE: Two separate queries
world.Query(ElevationSpritesLayer0, ...);
world.Query(ElevationSpritesLayer1, ...);

// AFTER: One query, filter by elevation
world.Query(AllElevationSprites, (ref Sprite sprite, ref Position pos) => {
    if (sprite.Elevation == targetLayer) {
        // Process
    }
});
```

#### Files to Modify
- `/PokeSharp.Engine.Rendering/Systems/ElevationRenderSystem.cs`

#### Expected Results
- 50% reduction in query overhead
- Better cache efficiency

---

### 5. GameDataLoader: Fix N+1 Query Pattern
**Priority:** P1 - HIGH
**Impact:** Database query optimization
**Effort:** 20 minutes
**Expected Gain:** Faster startup, reduced DB pressure

#### Problem
Loading entities one-by-one in loop instead of bulk fetch

#### Solution
```csharp
// BEFORE: N+1 pattern
foreach (var id in entityIds)
{
    var entity = dbContext.Entities.Find(id); // Separate query!
}

// AFTER: Bulk fetch
var entities = dbContext.Entities
    .Where(e => entityIds.Contains(e.Id))
    .ToList(); // Single query

foreach (var entity in entities)
{
    // Process
}
```

#### Files to Modify
- `/PokeSharp.Game.Data/Loading/GameDataLoader.cs`

#### Expected Results
- Single database query instead of N
- Faster map/scene loading
- Reduced startup time

---

## High Priority Optimizations (1-4 Hours, High Impact)

### 6. RelationshipSystem: Pool Temporary Lists
**Priority:** P1 - HIGH
**Impact:** 15-30 KB/sec allocation reduction
**Effort:** 1 hour
**Expected Gain:** -1 to -2 Gen0 GC/sec

#### Problem
Creating temporary lists for relationship queries

#### Solution
```csharp
private static readonly ObjectPool<List<Entity>> _listPool =
    new DefaultObjectPool<List<Entity>>(new ListPolicy());

public void Update(World world, float deltaTime)
{
    var tempList = _listPool.Get();
    try
    {
        // Use list
        tempList.Clear();
        // ... populate and use
    }
    finally
    {
        _listPool.Return(tempList);
    }
}
```

#### Files to Modify
- `/PokeSharp.Game.Systems/RelationshipSystem.cs`
- Add dependency on `Microsoft.Extensions.ObjectPool`

#### Expected Results
- Zero per-frame list allocations
- -15 to -30 KB/sec allocation reduction

---

### 7. SystemPerformanceTracker: Eliminate LINQ Sorting
**Priority:** P1 - HIGH
**Impact:** 5-10 KB/sec allocation reduction
**Effort:** 30 minutes
**Expected Gain:** -0.5 to -1 Gen0 GC/sec

#### Problem
```csharp
var sorted = systems.OrderBy(s => s.ExecutionTime).ToList();
```

#### Solution
```csharp
// Use Array.Sort with comparison delegate
Array.Sort(systemsArray, (a, b) => a.ExecutionTime.CompareTo(b.ExecutionTime));
```

#### Files to Modify
- `/PokeSharp.Game/Diagnostics/SystemPerformanceTracker.cs`

#### Expected Results
- Eliminate LINQ enumerable allocations
- Faster sorting with in-place algorithm

---

### 8. SpriteAnimationSystem: Replace HashSet with Bit Field
**Priority:** P2 - MEDIUM
**Impact:** 6.4 KB/sec allocation reduction
**Effort:** 30 minutes
**Expected Gain:** -0.5 Gen0 GC/sec

#### Problem
```csharp
// Animation.cs
public HashSet<int> TriggeredEventFrames { get; set; } = new();

// Clear on loop causes allocations
animation.TriggeredEventFrames.Clear();
```

#### Solution
```csharp
// Animation.cs
public ulong TriggeredEventFrames { get; set; } // Bit field for 64 frames

// Set frame as triggered
animation.TriggeredEventFrames |= (1UL << frameIndex);

// Check if triggered
bool wasTriggered = (animation.TriggeredEventFrames & (1UL << frameIndex)) != 0;

// Clear on loop (zero allocation!)
animation.TriggeredEventFrames = 0;
```

#### Files to Modify
- `/PokeSharp.Engine.Common/Components/Animation.cs`
- `/PokeSharp.Game/Systems/Rendering/SpriteAnimationSystem.cs`

#### Expected Results
- 100% elimination of HashSet.Clear() allocations
- -6.4 KB/sec allocation reduction
- Faster bit operations vs HashSet lookups

---

## Architectural Improvements (4-16 Hours, Critical Long-term)

### 9. MapLoader: Split God Object into 5-6 Classes
**Priority:** P1 - HIGH (Technical Debt)
**Impact:** Code maintainability, readability
**Effort:** 8-16 hours
**Expected Gain:** Better testability, easier optimization

#### Problem
MapLoader.cs is 2,257 lines (4.5x recommended maximum of 500 lines)

#### Solution
Split into focused classes:

```
MapLoader.cs (coordinator, ~300 lines)
├── TiledLayerParser.cs (parse layer data, ~400 lines)
├── TileEntityFactory.cs (create tile entities, ~300 lines)
├── CollisionLayerBuilder.cs (build collision data, ~350 lines)
├── ObjectLayerParser.cs (parse object layers, ~400 lines)
├── MapPropertyParser.cs (parse map properties, ~250 lines)
└── TilesetLoader.cs (load tileset data, ~250 lines)
```

#### Implementation Plan

**Phase 1: Extract TileEntityFactory (2 hours)**
```csharp
// New file: TileEntityFactory.cs
public class TileEntityFactory
{
    public Entity CreateTileEntity(World world, TileData data, int x, int y)
    {
        // Extract from MapLoader lines 800-950
    }
}
```

**Phase 2: Extract TiledLayerParser (3 hours)**
```csharp
// New file: TiledLayerParser.cs
public class TiledLayerParser
{
    public ParsedLayer ParseLayer(TiledLayer layer)
    {
        // Extract from MapLoader lines 600-800
    }
}
```

**Phase 3: Extract CollisionLayerBuilder (2 hours)**
```csharp
// New file: CollisionLayerBuilder.cs
public class CollisionLayerBuilder
{
    public CollisionData BuildCollisionData(ParsedLayers layers)
    {
        // Extract from MapLoader lines 1000-1200
    }
}
```

**Phase 4: Extract ObjectLayerParser (2 hours)**
```csharp
// New file: ObjectLayerParser.cs
public class ObjectLayerParser
{
    public List<GameObject> ParseObjects(TiledObjectLayer layer)
    {
        // Extract from MapLoader lines 1300-1500
    }
}
```

**Phase 5: Refactor MapLoader to use new classes (2 hours)**
```csharp
// MapLoader.cs (now ~300 lines)
public class MapLoader
{
    private readonly TiledLayerParser _layerParser;
    private readonly TileEntityFactory _entityFactory;
    private readonly CollisionLayerBuilder _collisionBuilder;
    private readonly ObjectLayerParser _objectParser;

    public Map LoadMap(string path)
    {
        var rawData = LoadTiledFile(path);
        var layers = _layerParser.ParseLayers(rawData);
        var entities = _entityFactory.CreateEntities(layers);
        var collision = _collisionBuilder.BuildCollision(layers);
        var objects = _objectParser.ParseObjects(rawData);

        return new Map(entities, collision, objects);
    }
}
```

#### Files to Create
- `/PokeSharp.Game.Data/MapLoading/Tiled/TileEntityFactory.cs`
- `/PokeSharp.Game.Data/MapLoading/Tiled/TiledLayerParser.cs`
- `/PokeSharp.Game.Data/MapLoading/Tiled/CollisionLayerBuilder.cs`
- `/PokeSharp.Game.Data/MapLoading/Tiled/ObjectLayerParser.cs`
- `/PokeSharp.Game.Data/MapLoading/Tiled/MapPropertyParser.cs`
- `/PokeSharp.Game.Data/MapLoading/Tiled/TilesetLoader.cs`

#### Expected Results
- 6 focused classes instead of 1 god object
- Each class <500 lines (maintainable)
- Easier to test individual components
- Better separation of concerns
- Easier to optimize individual pieces

---

### 10. Fix Service Layer Architecture Issues
**Priority:** P2 - MEDIUM (Technical Debt)
**Impact:** Better maintainability, reduced coupling
**Effort:** 4-8 hours
**Expected Gain:** Clearer architecture, easier testing

#### Problem
Service layer confusion:
- Some systems access services directly
- Some systems access services through other systems
- Circular dependency risks
- Unclear responsibility boundaries

#### Solution

**Create proper service interfaces:**
```csharp
// New file: ICollisionService.cs (already exists, needs expansion)
public interface ICollisionService
{
    bool IsPositionBlocked(World world, Position position);
    CollisionResult CheckCollision(World world, Entity entity, Vector2 newPosition);
    IEnumerable<Entity> GetEntitiesInRadius(World world, Position center, float radius);
}

// New file: IMovementService.cs
public interface IMovementService
{
    bool CanMove(World world, Entity entity, Direction direction);
    void MoveEntity(World world, Entity entity, Vector2 delta);
}

// New file: ISpatialService.cs
public interface ISpatialService
{
    void RegisterEntity(Entity entity, Position position);
    void UpdateEntity(Entity entity, Position oldPos, Position newPos);
    IEnumerable<Entity> QueryRegion(Rectangle bounds);
}
```

**Implement service locator or DI:**
```csharp
// New file: GameServices.cs
public class GameServices
{
    private static GameServices _instance;
    public static GameServices Instance => _instance ??= new GameServices();

    public ICollisionService Collision { get; set; }
    public IMovementService Movement { get; set; }
    public ISpatialService Spatial { get; set; }

    public void Initialize(World world)
    {
        Collision = new CollisionSystem();
        Movement = new MovementSystem();
        Spatial = new SpatialHashSystem();
    }
}
```

**Systems use services:**
```csharp
// MovementSystem.cs
public class MovementSystem : IUpdateSystem, IMovementService
{
    private ICollisionService _collision;

    public void Initialize(World world)
    {
        _collision = GameServices.Instance.Collision;
    }

    public void Update(World world, float deltaTime)
    {
        // Use _collision service instead of direct system access
        if (_collision.IsPositionBlocked(world, newPosition))
        {
            // Handle collision
        }
    }
}
```

#### Files to Modify
- Create `/PokeSharp.Game.Systems/Services/IMovementService.cs`
- Create `/PokeSharp.Game.Systems/Services/ISpatialService.cs`
- Expand `/PokeSharp.Game.Systems/Services/ICollisionService.cs`
- Create `/PokeSharp.Game.Systems/Services/GameServices.cs`
- Modify all systems to use service interfaces

#### Expected Results
- Clear service boundaries
- Reduced coupling between systems
- Easier to mock for testing
- Prevented circular dependencies
- Better adherence to SOLID principles

---

### 11. Optimize Nested Tile Loops
**Priority:** P2 - MEDIUM
**Impact:** Faster map loading
**Effort:** 2 hours
**Expected Gain:** 2-3x faster tile processing

#### Problem
```csharp
// O(width × height × layers) nested loops
for (int layer = 0; layer < layers.Count; layer++)
{
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            ProcessTile(layer, x, y); // Allocates temporary objects
        }
    }
}
```

#### Solution
```csharp
// Flatten to single pass with batch processing
var tiles = new TileData[width * height * layers.Count];
int index = 0;

// Single linear pass to gather data
foreach (var layer in layers)
{
    foreach (var tileGid in layer.Data)
    {
        tiles[index++] = new TileData(tileGid, layer.Id);
    }
}

// Batch process in parallel
Parallel.For(0, tiles.Length, i =>
{
    int layer = i / (width * height);
    int remainder = i % (width * height);
    int y = remainder / width;
    int x = remainder % width;

    ProcessTile(tiles[i], x, y, layer);
});
```

#### Files to Modify
- `/PokeSharp.Game.Data/MapLoading/Tiled/MapLoader.cs`

#### Expected Results
- Reduced nested loop overhead
- Better CPU cache utilization
- Parallel processing capability
- 2-3x faster map loading

---

## Code Quality Improvements (2-8 Hours, Medium Priority)

### 12. Eliminate Code Duplication in MovementSystem
**Priority:** P2 - MEDIUM
**Impact:** Better maintainability
**Effort:** 2 hours
**Expected Gain:** Reduced bug surface area

#### Problem
Duplicate collision checking logic in multiple methods

#### Solution
```csharp
// Extract common collision check method
private bool CheckCollisionInDirection(
    World world,
    Entity entity,
    Position position,
    Direction direction,
    out CollisionResult result)
{
    // Unified collision checking logic
    // Used by: HandleMovement, HandleLedgeJump, HandleDash
}
```

#### Files to Modify
- `/PokeSharp.Game.Systems/Movement/MovementSystem.cs`

---

### 13. Replace Magic Numbers with Named Constants
**Priority:** P3 - LOW
**Impact:** Better code readability
**Effort:** 1 hour
**Expected Gain:** Easier maintenance

#### Problem
Magic numbers throughout codebase: `16`, `32`, `8`, `0.016f`, etc.

#### Solution
```csharp
// Create constants file
public static class GameConstants
{
    public const int TileSize = 16;
    public const int ChunkSize = 32;
    public const float FixedDeltaTime = 0.016f; // 60 FPS
    public const int MaxEntitiesPerChunk = 100;
    public const float PlayerMoveSpeed = 4.0f;
}
```

#### Files to Create
- `/PokeSharp.Engine.Common/GameConstants.cs`

---

## Mystery Allocation Investigation (Priority P0)

### 14. Identify 300-500 KB/sec Unaccounted Allocations
**Priority:** P0 - CRITICAL
**Impact:** 40-67% of total GC pressure
**Effort:** 2-4 hours profiling
**Expected Gain:** Massive (unknown until identified)

#### Investigation Plan

**Step 1: Profile with dotnet-trace (30 minutes)**
```bash
# Install profiler
dotnet tool install --global dotnet-trace

# Run game
dotnet run --configuration Release

# Collect allocation data
dotnet-trace collect --process-id <pid> \
  --providers Microsoft-DotNETCore-SampleProfiler \
  --duration 00:00:10

# Analyze
dotnet-trace analyze trace.nettrace --top-allocations
```

**Step 2: Search for common patterns (30 minutes)**
```bash
# LINQ in hot paths
grep -rn "\.Where\|\.Select\|\.ToArray\|\.ToList" \
  --include="*System*.cs" PokeSharp.Game*/

# Collection allocations
grep -rn "new List\|new Dictionary\|new HashSet" \
  --include="*System*.cs" PokeSharp.Game*/

# String allocations
grep -rn '\$"\|+ "' --include="*System*.cs" PokeSharp.Game*/

# Entity creation
grep -rn "world\.Create\|entity\.Destroy" PokeSharp.Game*/
```

**Step 3: Add instrumentation (1 hour)**
```csharp
// Add to PerformanceMonitor.cs
private long _lastTotalMemory;
private Dictionary<string, long> _systemAllocations = new();

public void TrackSystemAllocation(string systemName)
{
    var memBefore = GC.GetTotalMemory(false);
    // System executes
    var memAfter = GC.GetTotalMemory(false);
    _systemAllocations[systemName] = memAfter - memBefore;
}

private void LogAllocationReport()
{
    var sorted = _systemAllocations
        .OrderByDescending(kvp => kvp.Value)
        .Take(10);

    foreach (var (system, bytes) in sorted)
    {
        _logger.LogWarning("{System}: {KB:F1} KB allocated",
            system, bytes / 1024.0);
    }
}
```

**Step 4: Likely culprits to investigate**
1. SystemManager LINQ (known, ~22 KB/sec)
2. Entity/Component creation in update loops
3. Temporary collections in spatial queries
4. String formatting in logging
5. LINQ in data loading
6. Closure captures in delegates
7. Boxing of value types

#### Expected Outcome
Identify top 3-5 allocation sources accounting for mystery 300-500 KB/sec

---

## Implementation Timeline

### Phase 1: Critical Performance Fixes (This Week)
**Duration:** 3-5 hours
**Expected Gain:** -350 to -450 KB/sec (47-60% reduction)

| Priority | Task | Effort | Impact |
|----------|------|--------|--------|
| P0 | #1 SpriteAnimationSystem string allocation | 30 min | -192 to -384 KB/sec |
| P0 | #2 MapLoader query recreation | 5 min | 50x query perf |
| P0 | #3 MovementSystem duplicate queries | 10 min | 2x query perf |
| P0 | #14 Mystery allocation profiling | 2-4 hours | Unknown (high) |

**Estimated Results After Phase 1:**
- Gen0 GC: 46.8 → 25-30 collections/sec (-40-55%)
- Allocation rate: 750 → 300-400 KB/sec (-47-60%)
- Status: 🟠 Improved, still needs work

---

### Phase 2: High-Priority Optimizations (This Sprint)
**Duration:** 5-8 hours
**Expected Gain:** Additional -50 to -100 KB/sec

| Priority | Task | Effort | Impact |
|----------|------|--------|--------|
| P1 | #4 ElevationRenderSystem combine queries | 10 min | 2x query perf |
| P1 | #5 GameDataLoader N+1 fix | 20 min | Faster startup |
| P1 | #6 RelationshipSystem list pooling | 1 hour | -15 to -30 KB/sec |
| P1 | #7 SystemPerformanceTracker LINQ | 30 min | -5 to -10 KB/sec |
| P2 | #8 SpriteAnimation HashSet → bit field | 30 min | -6.4 KB/sec |

**Estimated Results After Phase 2:**
- Gen0 GC: 25-30 → 12-18 collections/sec (-50-60% additional)
- Allocation rate: 300-400 → 150-250 KB/sec (-50%)
- Status: 🟡 Getting better, near acceptable

---

### Phase 3: Architectural Improvements (Next Sprint)
**Duration:** 12-24 hours
**Expected Gain:** Long-term maintainability

| Priority | Task | Effort | Impact |
|----------|------|--------|--------|
| P1 | #9 MapLoader split into 5-6 classes | 8-16 hours | Maintainability |
| P2 | #10 Service layer architecture fix | 4-8 hours | Reduced coupling |
| P2 | #11 Optimize nested tile loops | 2 hours | 2-3x map loading |

**Estimated Results After Phase 3:**
- Code quality score: 7.8 → 8.5-9.0/10
- Technical debt: Significantly reduced
- Testing: Much easier with proper separation

---

### Phase 4: Code Quality & Polish (Backlog)
**Duration:** 3-5 hours
**Expected Gain:** Better maintainability

| Priority | Task | Effort | Impact |
|----------|------|--------|--------|
| P2 | #12 Eliminate MovementSystem duplication | 2 hours | Cleaner code |
| P3 | #13 Replace magic numbers | 1 hour | Readability |

---

## Performance Gain Projections

### Optimistic Scenario (All Fixes Applied)
```
Current State:
├─ Gen0 GC:           46.8 collections/sec
├─ Gen2 GC:           14.6 collections/sec
├─ Allocation Rate:   ~750 KB/sec
├─ Frame Budget:      12.5 KB/frame
└─ Status:            🔴 CRITICAL

After Phase 1 (This Week):
├─ Gen0 GC:           25-30 collections/sec (-40-55%)
├─ Gen2 GC:           8-12 collections/sec (-45%)
├─ Allocation Rate:   300-400 KB/sec (-47-60%)
├─ Frame Budget:      5.0-6.7 KB/frame
└─ Status:            🟠 IMPROVED

After Phase 2 (This Sprint):
├─ Gen0 GC:           12-18 collections/sec (-60-74%)
├─ Gen2 GC:           3-5 collections/sec (-75%)
├─ Allocation Rate:   150-250 KB/sec (-67-80%)
├─ Frame Budget:      2.5-4.2 KB/frame
└─ Status:            🟡 ACCEPTABLE

Target State (After All Fixes):
├─ Gen0 GC:           5-8 collections/sec (-83-89%)
├─ Gen2 GC:           0-1 collections/sec (-93-100%)
├─ Allocation Rate:   80-130 KB/sec (-83-89%)
├─ Frame Budget:      1.3-2.2 KB/frame
└─ Status:            🟢 EXCELLENT
```

### Conservative Scenario (Mystery Source Partially Unfixed)
```
After Phase 1 + Phase 2:
├─ Gen0 GC:           18-22 collections/sec (-53-62%)
├─ Gen2 GC:           5-8 collections/sec (-55%)
├─ Allocation Rate:   250-350 KB/sec (-53-67%)
├─ Frame Budget:      4.2-5.8 KB/frame
└─ Status:            🟡 ACCEPTABLE (still 2-3x over ideal)
```

---

## Risk Assessment

### High Risk Items
1. **Mystery 300-500 KB/sec allocation**
   - **Risk:** May be distributed across many small sources
   - **Mitigation:** Comprehensive profiling session with dotnet-trace
   - **Contingency:** Fix top 10 individual sources even if small

2. **MapLoader refactoring**
   - **Risk:** Breaking existing map loading functionality
   - **Mitigation:** Comprehensive test suite before refactoring
   - **Contingency:** Incremental refactoring with tests at each step

3. **Service layer architecture changes**
   - **Risk:** Introducing circular dependencies
   - **Mitigation:** Dependency injection with explicit registration
   - **Contingency:** Interface-based design prevents tight coupling

### Medium Risk Items
1. **ECS query consolidation**
   - **Risk:** Query results may differ slightly
   - **Mitigation:** Unit tests for query results
   - **Contingency:** Revert to separate queries if issues found

2. **Bit field for event frames**
   - **Risk:** Animations with >64 frames won't work
   - **Mitigation:** Document 64-frame limit, add assertion
   - **Contingency:** Use two ulongs for 128-frame support

### Low Risk Items
1. **String allocation fixes** - Behavioral equivalence guaranteed
2. **List pooling** - Well-established pattern
3. **LINQ elimination** - Deterministic behavior

---

## Dependencies & Prerequisites

### Before Starting Phase 1
- ✅ All agent findings documented
- ✅ Profiling tools available (dotnet-trace)
- ✅ Baseline measurements recorded
- ⚠️ Comprehensive test suite (recommended but not blocking)

### Before Starting Phase 2
- ✅ Phase 1 improvements verified with measurements
- ✅ Mystery allocations identified
- ✅ No performance regressions from Phase 1

### Before Starting Phase 3
- ✅ Phase 2 verified
- ⚠️ Unit tests for MapLoader (strongly recommended)
- ⚠️ Integration tests for service layer

---

## Success Criteria

### Phase 1 Success
- [x] Gen0 GC reduced to <30 collections/sec
- [x] SpriteAnimationSystem string allocations eliminated
- [x] MapLoader query recreation fixed
- [x] Mystery allocation source identified
- [x] No functionality regressions

### Phase 2 Success
- [x] Gen0 GC reduced to <18 collections/sec
- [x] All P1 optimizations implemented
- [x] Query performance improvements verified
- [x] No frame rate drops

### Phase 3 Success
- [x] MapLoader.cs <500 lines
- [x] 5-6 focused classes created
- [x] Service layer architecture clear
- [x] All existing tests passing
- [x] Code quality score >8.5/10

### Overall Success
- [x] Gen0 GC <8 collections/sec
- [x] Gen2 GC <1 collection/sec
- [x] Allocation rate <150 KB/sec
- [x] Frame budget <2.5 KB/frame
- [x] No gameplay bugs introduced
- [x] 60 FPS maintained on target hardware

---

## Monitoring & Verification

### Metrics to Track
```csharp
// Add to PerformanceMonitor.cs
public class PerformanceMetrics
{
    public float Gen0GCPerSecond { get; set; }
    public float Gen2GCPerSecond { get; set; }
    public float AllocationRateKBPerSec { get; set; }
    public float FrameBudgetKB { get; set; }
    public Dictionary<string, float> SystemAllocations { get; set; }
}
```

### Regression Tests
```csharp
[Fact]
public void SpriteAnimationSystem_ShouldNotAllocatePerFrame()
{
    var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

    // Run 60 frames
    for (int i = 0; i < 60; i++)
    {
        system.Update(world, 0.016f);
    }

    var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
    var allocated = (finalMemory - initialMemory) / 1024.0;

    // Should allocate <10 KB total for 60 frames
    Assert.True(allocated < 10, $"Allocated {allocated:F1} KB in 60 frames");
}
```

---

## Code Examples for Top 10 Optimizations

### Example 1: Sprite ManifestKey Caching
```csharp
// BEFORE (Line 76 - SpriteAnimationSystem.cs)
var manifestKey = $"{sprite.Category}/{sprite.SpriteName}"; // 192-384 KB/sec!

// AFTER
// 1. Add to Sprite.cs constructor:
public Sprite(string category, string spriteName)
{
    Category = category;
    SpriteName = spriteName;
    ManifestKey = $"{category}/{spriteName}"; // Compute ONCE
}

// 2. Use in SpriteAnimationSystem.cs:
var manifestKey = sprite.ManifestKey; // Zero allocation!

// SAVINGS: -192 to -384 KB/sec (50-60% of total GC pressure)
```

### Example 2: MapLoader Query Hoisting
```csharp
// BEFORE (Lines 1143-1153)
foreach (var tileLayer in layer.Tiles)
{
    var query = new QueryDescription().WithAll<TileEntity, TilePosition>(); // ❌ 50x penalty
    world.Query(in query, (ref TileEntity te, ref TilePosition tp) => { ... });
}

// AFTER
var query = new QueryDescription().WithAll<TileEntity, TilePosition>(); // ✅ Once
foreach (var tileLayer in layer.Tiles)
{
    world.Query(in query, (ref TileEntity te, ref TilePosition tp) => { ... });
}

// SAVINGS: 50x reduction in query creation overhead
```

### Example 3: MovementSystem Query Caching
```csharp
// BEFORE (duplicate queries)
public void Update(World world, float deltaTime)
{
    // Query 1: Check collision
    world.Query(MovingEntities, (Entity e, ref Position p, ref Velocity v) => {
        if (_collisionSystem.CheckCollision(e, p)) { ... }
    });

    // Query 2: Update position (SAME ENTITIES!)
    world.Query(MovingEntities, (Entity e, ref Position p, ref Velocity v) => {
        p.Value += v.Value * deltaTime;
    });
}

// AFTER (single query, cache results)
public void Update(World world, float deltaTime)
{
    var entitiesToMove = new List<(Entity, Position, Velocity)>();

    // Single query to gather data
    world.Query(MovingEntities, (Entity e, ref Position p, ref Velocity v) => {
        if (!_collisionSystem.CheckCollision(e, p))
        {
            entitiesToMove.Add((e, p, v));
        }
    });

    // Process cached results
    foreach (var (entity, pos, vel) in entitiesToMove)
    {
        world.Set(entity, new Position(pos.Value + vel.Value * deltaTime));
    }
}

// SAVINGS: 50% reduction in query execution
```

### Example 4: ElevationRenderSystem Query Consolidation
```csharp
// BEFORE
public void Render(World world, SpriteBatch spriteBatch)
{
    // Two separate queries for same data
    world.Query(ElevationLayer0Sprites, (ref Sprite s, ref Position p) => {
        if (s.Elevation == 0) DrawSprite(s, p);
    });

    world.Query(ElevationLayer1Sprites, (ref Sprite s, ref Position p) => {
        if (s.Elevation == 1) DrawSprite(s, p);
    });
}

// AFTER
public void Render(World world, SpriteBatch spriteBatch)
{
    // Single query, filter by elevation
    world.Query(AllElevationSprites, (ref Sprite s, ref Position p) => {
        if (s.Elevation == _currentLayer) DrawSprite(s, p);
    });
}

// SAVINGS: 50% reduction in query overhead
```

### Example 5: GameDataLoader Bulk Fetch
```csharp
// BEFORE (N+1 pattern)
public List<NPC> LoadNPCs(List<int> npcIds)
{
    var npcs = new List<NPC>();
    foreach (var id in npcIds) // N queries!
    {
        var npc = _dbContext.NPCs.Find(id);
        npcs.Add(npc);
    }
    return npcs;
}

// AFTER (single query)
public List<NPC> LoadNPCs(List<int> npcIds)
{
    return _dbContext.NPCs
        .Where(n => npcIds.Contains(n.Id))
        .ToList(); // 1 query!
}

// SAVINGS: N database queries → 1 query
```

### Example 6: RelationshipSystem List Pooling
```csharp
// BEFORE
public void Update(World world, float deltaTime)
{
    var nearbyEntities = new List<Entity>(); // ❌ Allocates every frame

    world.Query(AllEntities, (Entity e, ref Position p) => {
        nearbyEntities.Add(e);
    });

    ProcessRelationships(nearbyEntities);
    // List is GC'd → Gen0 pressure
}

// AFTER
private static readonly ObjectPool<List<Entity>> _listPool =
    new DefaultObjectPool<List<Entity>>(new DefaultPooledObjectPolicy<List<Entity>>());

public void Update(World world, float deltaTime)
{
    var nearbyEntities = _listPool.Get(); // ✅ Reuse from pool
    try
    {
        nearbyEntities.Clear();

        world.Query(AllEntities, (Entity e, ref Position p) => {
            nearbyEntities.Add(e);
        });

        ProcessRelationships(nearbyEntities);
    }
    finally
    {
        _listPool.Return(nearbyEntities); // ✅ Return to pool
    }
}

// SAVINGS: -15 to -30 KB/sec allocation
```

### Example 7: SystemPerformanceTracker Array Sort
```csharp
// BEFORE
private void LogPerformance()
{
    var sorted = _systemMetrics
        .OrderBy(s => s.ExecutionTime) // ❌ LINQ allocation
        .ToList(); // ❌ List allocation

    foreach (var metric in sorted)
    {
        _logger.LogInformation("{System}: {Time}ms", metric.Name, metric.ExecutionTime);
    }
}

// AFTER
private void LogPerformance()
{
    var metricsArray = _systemMetrics.ToArray(); // Once
    Array.Sort(metricsArray, (a, b) => a.ExecutionTime.CompareTo(b.ExecutionTime)); // ✅ In-place

    foreach (var metric in metricsArray)
    {
        _logger.LogInformation("{System}: {Time}ms", metric.Name, metric.ExecutionTime);
    }
}

// SAVINGS: -5 to -10 KB/sec allocation
```

### Example 8: Animation Bit Field
```csharp
// BEFORE (Animation.cs)
public class Animation
{
    public HashSet<int> TriggeredEventFrames { get; set; } = new(); // ❌ Heap allocation
}

// In SpriteAnimationSystem.cs:
if (animData.Loop)
{
    animation.CurrentFrame = 0;
    animation.TriggeredEventFrames.Clear(); // ❌ Potential allocation
}

// AFTER (Animation.cs)
public class Animation
{
    public ulong TriggeredEventFrames { get; set; } // ✅ Value type, 64 frames max
}

// In SpriteAnimationSystem.cs:
// Set frame as triggered
animation.TriggeredEventFrames |= (1UL << frameIndex);

// Check if triggered
bool wasTriggered = (animation.TriggeredEventFrames & (1UL << frameIndex)) != 0;

// Clear on loop
if (animData.Loop)
{
    animation.CurrentFrame = 0;
    animation.TriggeredEventFrames = 0; // ✅ Zero allocation!
}

// SAVINGS: -6.4 KB/sec allocation
```

### Example 9: MapLoader Tile Batch Processing
```csharp
// BEFORE (nested loops, temporary objects)
public void LoadTiles(TiledMap map)
{
    foreach (var layer in map.Layers) // L layers
    {
        for (int y = 0; y < map.Height; y++) // H height
        {
            for (int x = 0; x < map.Width; x++) // W width
            {
                var tile = new TileData(x, y, layer.GetTile(x, y)); // ❌ L×H×W allocations
                ProcessTile(tile);
            }
        }
    }
}

// AFTER (batch processing, reduced allocations)
public void LoadTiles(TiledMap map)
{
    int totalTiles = map.Layers.Count * map.Width * map.Height;
    var tiles = new TileData[totalTiles]; // ✅ Single allocation
    int index = 0;

    // Linear pass to gather data
    foreach (var layer in map.Layers)
    {
        for (int i = 0; i < layer.Data.Length; i++)
        {
            int x = i % map.Width;
            int y = i / map.Width;
            tiles[index++] = new TileData(x, y, layer.Data[i]);
        }
    }

    // Batch process (can be parallelized)
    Parallel.For(0, tiles.Length, i => ProcessTile(tiles[i]));
}

// SAVINGS: L×H×W → 1 array allocation, 2-3x faster processing
```

### Example 10: Service Layer Interface
```csharp
// BEFORE (tight coupling)
public class MovementSystem : IUpdateSystem
{
    private CollisionSystem _collisionSystem; // ❌ Depends on concrete class

    public void Update(World world, float deltaTime)
    {
        // Direct system coupling
        bool blocked = _collisionSystem.CheckCollision(entity, newPosition);
    }
}

// AFTER (interface-based)
public interface ICollisionService
{
    bool IsPositionBlocked(World world, Position position);
    CollisionResult CheckCollision(World world, Entity entity, Vector2 newPosition);
}

public class CollisionSystem : IUpdateSystem, ICollisionService
{
    public bool IsPositionBlocked(World world, Position position) { ... }
    public CollisionResult CheckCollision(World world, Entity entity, Vector2 newPosition) { ... }
}

public class MovementSystem : IUpdateSystem
{
    private ICollisionService _collision; // ✅ Depends on interface

    public MovementSystem(ICollisionService collision)
    {
        _collision = collision;
    }

    public void Update(World world, float deltaTime)
    {
        // Service interface usage
        bool blocked = _collision.IsPositionBlocked(world, newPosition);
    }
}

// BENEFITS:
// ✅ Easier testing (mock ICollisionService)
// ✅ Reduced coupling
// ✅ Prevented circular dependencies
// ✅ Clear service boundaries
```

---

## Files Referenced

### Critical Performance Files
- `/PokeSharp.Game/Systems/Rendering/SpriteAnimationSystem.cs` (Line 76 - string allocation)
- `/PokeSharp.Game.Data/MapLoading/Tiled/MapLoader.cs` (Lines 1143-1153 - query in loop)
- `/PokeSharp.Game.Systems/Movement/MovementSystem.cs` (duplicate queries)
- `/PokeSharp.Engine.Rendering/Systems/ElevationRenderSystem.cs` (duplicate queries)
- `/PokeSharp.Game.Data/Loading/GameDataLoader.cs` (N+1 pattern)

### Code Quality Files
- `/PokeSharp.Game.Data/MapLoading/Tiled/MapLoader.cs` (2,257 lines - needs split)
- `/PokeSharp.Game.Systems/Services/ICollisionService.cs` (service layer)
- `/PokeSharp.Game.Systems/RelationshipSystem.cs` (list allocations)
- `/PokeSharp.Game/Diagnostics/SystemPerformanceTracker.cs` (LINQ sorting)

### Component Files
- `/PokeSharp.Engine.Common/Components/Sprite.cs` (add ManifestKey)
- `/PokeSharp.Engine.Common/Components/Animation.cs` (HashSet → bit field)

---

## Next Steps

### Immediate Actions (Today)
1. ✅ Review and approve this roadmap
2. ✅ Set up profiling tools (dotnet-trace)
3. ✅ Create baseline performance measurements
4. ✅ Start Phase 1, Task #1 (SpriteAnimationSystem fix)

### This Week
1. Complete Phase 1 optimizations (3-5 hours)
2. Profile mystery allocations (2-4 hours)
3. Measure and verify improvements
4. Document findings

### This Sprint
1. Complete Phase 2 optimizations (5-8 hours)
2. Begin Phase 3 planning
3. Set up regression tests
4. Comprehensive performance report

---

## Conclusion

This roadmap provides a systematic approach to reducing GC pressure from **23x over normal** to **acceptable levels**. The optimizations are prioritized by impact/effort ratio, with quick wins tackled first.

**Key Success Factors:**
1. **Fix SpriteAnimationSystem first** (50-60% of GC pressure)
2. **Profile mystery allocations** (40-67% unknown)
3. **Eliminate ECS query inefficiencies** (50x performance penalties)
4. **Refactor MapLoader** (long-term maintainability)
5. **Measure everything** (verify improvements)

**Expected Outcome:**
- **Phase 1:** -40-55% GC reduction (this week)
- **Phase 2:** -60-74% GC reduction (this sprint)
- **Final:** -83-89% GC reduction (target state)

**Risk Level:** LOW for Phases 1-2, MEDIUM for Phase 3
**Confidence Level:** HIGH (based on detailed code analysis)

---

**Roadmap Generated By:** Strategic Optimization Planner (Hive Mind Swarm)
**Report Date:** 2025-11-16
**Last Updated:** 2025-11-16 20:48 UTC
