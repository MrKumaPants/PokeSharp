# PokeSharp Logging Standards and Conventions

## Overview

This document defines comprehensive logging standards for the PokeSharp game engine, ensuring consistent, performant, and actionable logging across all subsystems.

## Table of Contents

1. [Log Level Guidelines](#log-level-guidelines)
2. [Structured Logging Patterns](#structured-logging-patterns)
3. [Message Templates](#message-templates)
4. [Exception Logging Standards](#exception-logging-standards)
5. [Performance Considerations](#performance-considerations)
6. [Event ID Registry](#event-id-registry)
7. [Best Practices](#best-practices)
8. [Anti-Patterns](#anti-patterns)

---

## Log Level Guidelines

### Trace (Disabled in Production)

**Purpose**: Method entry/exit, per-frame details, micro-optimizations

**When to use**:
- Method entry/exit points in hot paths
- Per-frame operations (input polling, collision checks)
- Detailed algorithm step tracking
- Loop iteration details

**Examples**:
```csharp
_logger?.LogTrace("Movement blocked by collision at ({X}, {Y}) from direction {Direction}", x, y, direction);
_logger?.LogTrace("Collected sprite ID for lazy loading: {SpriteId}", spriteId);
```

**Performance Impact**: HIGH - Should be completely disabled in production builds

---

### Debug (Disabled in Production)

**Purpose**: Detailed diagnostic information for development and troubleshooting

**When to use**:
- System initialization details
- Resource loading/unloading confirmation
- Entity creation/destruction events
- Cache invalidation events
- Spatial hash rebuilds
- Internal state transitions

**Examples**:
```csharp
_logger?.LogDebug("Spatial hash invalidated for map '{MapName}'", mapName);
_logger?.LogDebug("Destroyed {Count} entities for map {MapId} (including image layers)", count, mapId);
_logger?.LogDebug("Spawned '{ObjectName}' ({TemplateId}) at ({X}, {Y})", name, templateId, x, y);
```

**Performance Impact**: MEDIUM - Acceptable in development, disabled in production

---

### Information (Enabled in Production)

**Purpose**: Important business events and state changes that indicate normal operation

**When to use**:
- Map loaded/unloaded
- Player created
- Game state changes
- System startup/shutdown
- Performance statistics (periodic)
- Asset loading completion
- Major workflow milestones

**Examples**:
```csharp
_logger?.LogInformation("Registered map: {MapName} (ID: {MapId}) with {TilesetCount} tilesets", mapName, mapId, count);
_logger?.LogInformation("All systems initialized successfully");
_logger?.LogInformation("Performance: Avg frame time: {AvgMs:F2}ms ({Fps:F1} FPS)", avgMs, fps);
```

**Performance Impact**: LOW - Should be used sparingly for important events only

---

### Warning (Enabled in Production)

**Purpose**: Recoverable issues that don't prevent operation but indicate potential problems

**When to use**:
- Missing optional data (using defaults)
- Fallback strategies activated
- Deprecated API usage
- Resource not found (with fallback)
- Performance degradation detected
- Configuration issues (using defaults)
- Memory threshold warnings

**Examples**:
```csharp
_logger?.LogWarning("NPC definition not found: '{NpcId}' (falling back to map properties)", npcId);
_logger?.LogWarning("High memory usage: {MemoryMb:F2}MB (threshold: {ThresholdMb:F2}MB)", memoryMb, thresholdMb);
_logger?.LogSlowFrame(frameTimeMs, targetMs); // EventId: 3000
```

**Performance Impact**: LOW - Minimal overhead, safe for production

---

### Error (Enabled in Production)

**Purpose**: Failures that prevent specific operations but don't crash the application

**When to use**:
- File not found (no fallback)
- Parse errors
- Invalid configuration
- Network failures
- Resource loading failures
- System initialization failures (for specific system)
- Component operation failures

**Examples**:
```csharp
_logger?.LogError(ex, "Update system {SystemName} failed during execution", systemName);
_logger?.LogError(ex, "Failed to load external tileset from {Path}", tilesetPath);
_logger?.LogError("Invalid tile dimensions: {Width}x{Height}", tileWidth, tileHeight);
```

**Performance Impact**: LOW - Exception context capture has minimal overhead

---

### Critical (Enabled in Production)

**Purpose**: Application-level failures that require immediate attention

**When to use**:
- Initialization failed (application cannot start)
- Unrecoverable state corruption
- Critical resource exhaustion
- Security violations
- Data corruption detected
- System-wide failures

**Examples**:
```csharp
_logger?.LogCritical("Game initialization failed: {Reason}", reason);
_logger?.LogCritical("World corruption detected: {Details}", details);
_logger?.LogCritical("Out of memory: {MemoryMb}MB allocated", memoryMb);
```

**Performance Impact**: NEGLIGIBLE - Should be extremely rare

---

## Structured Logging Patterns

### Event IDs for Categorization

Event IDs are organized by subsystem range:

| Range      | Subsystem                  |
|------------|----------------------------|
| 1000-1999  | Movement & Collision       |
| 2000-2999  | ECS & Entity Processing    |
| 3000-3999  | Performance & Metrics      |
| 4000-4999  | Asset Loading              |
| 5000-5999  | System Initialization      |
| 6000-6999  | Memory Management          |
| 7000-7999  | Input Processing           |
| 8000-8999  | Rendering                  |
| 9000-9999  | Scripting & Hot Reload     |
| 10000-10999| Networking (reserved)      |

### Parameter Naming Conventions

Use consistent parameter names across all log messages:

**Entity & Component IDs**:
- `{EntityId}` - Arch entity ID
- `{ComponentType}` - Component type name
- `{SystemName}` - System class name

**Map & Position**:
- `{MapId}` - Numeric map identifier
- `{MapName}` - Human-readable map name
- `{X}`, `{Y}` - Tile coordinates
- `{Direction}` - Cardinal direction (North/South/East/West)

**Performance**:
- `{ElapsedMs}` - Elapsed milliseconds
- `{AvgMs}` - Average milliseconds
- `{PeakMs}` - Peak milliseconds
- `{CallCount}` - Number of calls
- `{Fps}` - Frames per second

**Resources**:
- `{TextureId}` - Texture identifier
- `{SpriteId}` - Sprite identifier (category/name)
- `{TilesetId}` - Tileset identifier

**Counts**:
- `{Count}` - Generic count
- `{EntityCount}` - Entity count
- `{TileCount}` - Tile count

### Correlation IDs for Multi-System Operations

Use logging scopes to correlate related operations:

```csharp
using (_logger?.BeginScope($"Loading:{mapName}"))
{
    // All logs within this scope will be grouped
    LoadTilesets();
    ProcessLayers();
    SpawnEntities();
}
```

### Structured Properties

Use structured properties instead of string interpolation:

```csharp
// ✅ GOOD - Structured logging
_logger?.LogInformation("Map loaded: {MapName} ({Width}x{Height})", mapName, width, height);

// ❌ BAD - String interpolation
_logger?.LogInformation($"Map loaded: {mapName} ({width}x{height})");
```

---

## Message Templates

### Startup Messages

```csharp
[LoggerMessage(EventId = 5000, Level = LogLevel.Information,
    Message = "Initializing {Count} systems")]
public static partial void LogSystemsInitializing(this ILogger logger, int count);

[LoggerMessage(EventId = 5001, Level = LogLevel.Debug,
    Message = "Initializing system: {SystemName}")]
public static partial void LogSystemInitializing(this ILogger logger, string systemName);

[LoggerMessage(EventId = 5002, Level = LogLevel.Information,
    Message = "All systems initialized successfully")]
public static partial void LogSystemsInitialized(this ILogger logger);
```

### Performance Messages

```csharp
[LoggerMessage(EventId = 3000, Level = LogLevel.Warning,
    Message = "Slow frame: {FrameTimeMs:F2}ms (target: {TargetMs:F2}ms)")]
public static partial void LogSlowFrame(this ILogger logger, float frameTimeMs, float targetMs);

[LoggerMessage(EventId = 3002, Level = LogLevel.Information,
    Message = "Performance: Avg frame time: {AvgMs:F2}ms ({Fps:F1} FPS) | Min: {MinMs:F2}ms | Max: {MaxMs:F2}ms")]
public static partial void LogFrameTimeStats(this ILogger logger, float avgMs, float fps, float minMs, float maxMs);

[LoggerMessage(EventId = 3003, Level = LogLevel.Information,
    Message = "System {SystemName} - Avg: {AvgMs:F2}ms | Max: {MaxMs:F2}ms | Calls: {UpdateCount}")]
public static partial void LogSystemStats(this ILogger logger, string systemName, double avgMs, double maxMs, long updateCount);
```

### Error Messages

```csharp
// Standard error format: "Failed to {Operation}: {ErrorMessage} | {ContextKey}: {ContextValue}"
_logger?.LogError(ex, "Failed to initialize system: {SystemName} | Priority: {Priority}", systemName, priority);

_logger?.LogError(ex, "Failed to load texture: {TextureId} | Path: {Path}", textureId, path);

_logger?.LogError(ex, "Failed to spawn entity: {TemplateId} | Position: ({X}, {Y})", templateId, x, y);
```

### State Transition Messages

```csharp
// Format: "State transition: {FromState} → {ToState} | Reason: {Reason}"
_logger?.LogInformation("Transitioning from map {OldMapId} to {NewMapId}", oldMapId, newMapId);

_logger?.LogInformation("Game state changed: {OldState} → {NewState} | Trigger: {Trigger}", oldState, newState, trigger);
```

### Resource Lifecycle Messages

```csharp
// Load: "Loaded {ResourceType} '{ResourceId}' in {ElapsedMs:F2}ms ({Details})"
_logger?.LogDebug("Loaded texture '{TextureId}' in {TimeMs:F2}ms ({Width}x{Height}px)", textureId, timeMs, width, height);

// Unload: "Unloaded {ResourceType} '{ResourceId}' | {Context}"
_logger?.LogDebug("Unloaded {Count} sprite textures for map {MapId}", count, mapId);
```

---

## Exception Logging Standards

### When to Log Exceptions

1. **At Catch Site** - Log when you have contextual information:
```csharp
try
{
    system.Initialize(world);
}
catch (Exception ex)
{
    _logger?.LogError(ex, "Failed to initialize system: {SystemName}", systemName);
    throw; // Re-throw if unrecoverable
}
```

2. **At Propagation Boundary** - Log when exception crosses subsystem boundaries:
```csharp
public void LoadMap(string mapPath)
{
    try
    {
        // Map loading logic
    }
    catch (Exception ex)
    {
        _logger?.LogExceptionWithContext(ex, "Map loading failed: {MapPath}", mapPath);
        throw new MapLoadException($"Failed to load map: {mapPath}", ex);
    }
}
```

3. **Never Log Same Exception Twice** - Choose either catch site OR boundary, not both

### Exception Context Information

Always include:
- **Operation being performed**: "initializing system", "loading texture", etc.
- **Entity/Resource identifiers**: EntityId, MapId, TextureId, etc.
- **Relevant state**: game state, map state, system state
- **User-facing impact**: what the user will experience

```csharp
public static void LogExceptionWithContext(
    this ILogger logger,
    Exception ex,
    string message,
    params object?[] args)
{
    var contextData = new Dictionary<string, object>
    {
        ["ThreadId"] = Environment.CurrentManagedThreadId,
        ["Timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        ["MachineName"] = Environment.MachineName,
        ["ExceptionType"] = ex.GetType().Name,
        ["ExceptionSource"] = ex.Source ?? "Unknown"
    };

    var contextString = string.Join(", ", contextData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    var fullMessage = $"{message} | Context: {contextString}";

    logger.LogError(ex, fullMessage, args);
}
```

### Stack Trace Inclusion Rules

- **Always include** for Error and Critical levels
- **Include** for Warning if `LogLevel.Debug` is enabled
- **Never include** for Information, Debug, or Trace (stack traces in exceptions only)

---

## Performance Considerations

### 1. Source Generator Messages (Zero Allocation)

Use `LoggerMessage` attribute for high-frequency logs:

```csharp
[LoggerMessage(EventId = 2000, Level = LogLevel.Debug,
    Message = "Processing {EntityCount} entities in {SystemName}")]
public static partial void LogEntityProcessing(this ILogger logger, int entityCount, string systemName);
```

**Benefits**:
- Zero allocations at runtime
- Compile-time code generation
- Type-safe parameters
- 30-50% faster than manual logging

### 2. Conditional Logging

Always check `IsEnabled` before expensive operations:

```csharp
if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
{
    var expensiveDetails = ComputeExpensiveDebugInfo();
    _logger.LogDebug("System state: {Details}", expensiveDetails);
}
```

### 3. Lazy Evaluation

Use lazy evaluation for expensive message construction:

```csharp
// ✅ GOOD - String built only if logging enabled
_logger?.LogDebug("Entities: {Entities}", () => string.Join(", ", entityIds));

// ❌ BAD - String built even if logging disabled
_logger?.LogDebug($"Entities: {string.Join(", ", entityIds)}");
```

### 4. Avoid Allocations in Hot Paths

**Hot paths** (called 60-120 times per second):
- Input processing
- Entity queries
- Collision detection
- Rendering

**Rules for hot paths**:
- Use Trace level only (disabled in production)
- Never create strings or collections
- Never call ToString() on complex objects
- Use source generators exclusively

```csharp
// ✅ GOOD - Source generator, zero allocations
_logger?.LogCollisionBlocked(x, y, direction.ToString());

// ❌ BAD - Allocates string even if disabled
_logger?.LogDebug($"Collision at ({x}, {y}) from {direction}");
```

### 5. Buffering and Batching Strategies

For high-volume debug logging (development only):

```csharp
// Periodic performance stats (every 5 seconds at 60fps)
if (_performanceTracker.FrameCount % 300 == 0)
{
    _performanceTracker.LogPerformanceStats();
}
```

### 6. Scoped Logging for Context

Use scopes for grouping related operations (minimal overhead):

```csharp
using (_logger?.BeginScope($"Loading:{mapName}"))
{
    // All subsequent logs inherit this scope
    LoadTilesets();    // [Loading:town] Loaded tileset: primary
    ProcessLayers();   // [Loading:town] Processing 3 layers
}
```

---

## Event ID Registry

Complete event ID assignments for all subsystems:

### Movement & Collision (1000-1999)

| Event ID | Level   | Message |
|----------|---------|---------|
| 1000     | Debug   | Movement blocked: out of bounds ({X}, {Y}) for map {MapId} |
| 1001     | Debug   | Ledge jump blocked: landing out of bounds ({X}, {Y}) |
| 1002     | Debug   | Ledge jump blocked: landing position blocked ({X}, {Y}) |
| 1003     | Debug   | Ledge jump: ({StartX}, {StartY}) -> ({EndX}, {EndY}) direction: {Direction} |
| 1004     | Trace   | Movement blocked by collision at ({X}, {Y}) from direction {Direction} |

### ECS & Entity Processing (2000-2999)

| Event ID | Level        | Message |
|----------|--------------|---------|
| 2000     | Debug        | Processing {EntityCount} entities in {SystemName} |
| 2001     | Information  | Indexed {Count} static tiles into spatial hash |
| 2002     | Information  | Processing {Count} animated tiles |
| 2003     | Debug        | Created {Count} entities from template {TemplateId} |
| 2004     | Information  | Pooled entity returned to pool {PoolType} |

### Performance & Metrics (3000-3999)

| Event ID | Level        | Message |
|----------|--------------|---------|
| 3000     | Warning      | Slow frame: {FrameTimeMs:F2}ms (target: {TargetMs:F2}ms) |
| 3001     | Warning      | Slow system: {SystemName} took {ElapsedMs:F2}ms (threshold: {ThresholdMs:F2}ms) |
| 3002     | Information  | Performance: Avg frame time: {AvgMs:F2}ms ({Fps:F1} FPS) |
| 3003     | Information  | System {SystemName} - Avg: {AvgMs:F2}ms \| Max: {MaxMs:F2}ms \| Calls: {UpdateCount} |

### Asset Loading (4000-4999)

| Event ID | Level        | Message |
|----------|--------------|---------|
| 4000     | Debug        | Loaded texture '{TextureId}' in {TimeMs:F2}ms ({Width}x{Height}px) |
| 4001     | Warning      | Slow texture load: '{TextureId}' took {TimeMs:F2}ms |
| 4002     | Information  | Loaded map: {MapName} ({Width}x{Height}) with {TileCount} tiles |
| 4003     | Debug        | Unloaded texture: {TextureId} |

### System Initialization (5000-5999)

| Event ID | Level        | Message |
|----------|--------------|---------|
| 5000     | Information  | Initializing {Count} systems |
| 5001     | Debug        | Initializing system: {SystemName} |
| 5002     | Information  | All systems initialized successfully |
| 5003     | Debug        | Registered system: {SystemName} (Priority: {Priority}) |

### Memory Management (6000-6999)

| Event ID | Level        | Message |
|----------|--------------|---------|
| 6000     | Information  | Memory: {MemoryMb:F2}MB \| GC Collections - Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2} |
| 6001     | Warning      | High memory usage: {MemoryMb:F2}MB (threshold: {ThresholdMb:F2}MB) |
| 6002     | Debug        | Memory after GC: {AfterMb:F2}MB (freed {FreedMb:F2}MB) |

---

## Best Practices

### 1. Use Structured Logging

```csharp
// ✅ GOOD - Structured properties
_logger?.LogInformation("Map loaded: {MapName} with {TileCount} tiles", mapName, tileCount);

// ❌ BAD - String interpolation
_logger?.LogInformation($"Map loaded: {mapName} with {tileCount} tiles");
```

### 2. Log at Appropriate Levels

```csharp
// ✅ GOOD - Information for important events
_logger?.LogInformation("Player entered map: {MapName}", mapName);

// ❌ BAD - Debug would be too verbose
_logger?.LogDebug("Player entered map: {MapName}", mapName); // Wrong level
```

### 3. Include Contextual Information

```csharp
// ✅ GOOD - Rich context
_logger?.LogError(ex, "Failed to spawn entity: {TemplateId} at ({X}, {Y}) on map {MapId}",
    templateId, x, y, mapId);

// ❌ BAD - Insufficient context
_logger?.LogError(ex, "Failed to spawn entity");
```

### 4. Use Scopes for Related Operations

```csharp
// ✅ GOOD - Scoped logging
using (_logger?.BeginScope($"Map:{mapName}"))
{
    LoadTilesets();
    CreateEntities();
}

// ❌ BAD - Repeating context
_logger?.LogDebug("[Map:{MapName}] Loading tilesets", mapName);
_logger?.LogDebug("[Map:{MapName}] Creating entities", mapName);
```

### 5. Consistent Parameter Names

```csharp
// ✅ GOOD - Consistent naming
_logger?.LogInformation("Entity {EntityId} created on map {MapId}", entityId, mapId);
_logger?.LogDebug("Entity {EntityId} destroyed", entityId);

// ❌ BAD - Inconsistent naming
_logger?.LogInformation("Entity {Id} created on map {Map}", entityId, mapId);
_logger?.LogDebug("Entity {Entity} destroyed", entityId);
```

### 6. Format Numbers Consistently

```csharp
// ✅ GOOD - Consistent formatting
_logger?.LogInformation("Frame time: {FrameTimeMs:F2}ms", frameTimeMs); // 16.67ms
_logger?.LogInformation("FPS: {Fps:F1}", fps); // 60.0

// ❌ BAD - Inconsistent formatting
_logger?.LogInformation("Frame time: {FrameTimeMs}ms", frameTimeMs); // 16.666666ms
```

### 7. Null-Conditional Logging

Always use null-conditional operator for optional logger:

```csharp
// ✅ GOOD - Safe null handling
_logger?.LogInformation("Map loaded: {MapName}", mapName);

// ❌ BAD - Null reference risk
if (_logger != null)
    _logger.LogInformation("Map loaded: {MapName}", mapName);
```

---

## Anti-Patterns

### 1. Logging in Hot Paths (Production)

```csharp
// ❌ BAD - Logging every frame (60-120 FPS)
public void Update(float deltaTime)
{
    _logger?.LogDebug("Update called with deltaTime: {DeltaTime}", deltaTime);
    // ... update logic
}

// ✅ GOOD - Periodic logging only
if (_frameCount % 300 == 0) // Every 5 seconds at 60 FPS
{
    _logger?.LogDebug("Average delta time: {AvgDeltaTime:F3}s", avgDeltaTime);
}
```

### 2. String Interpolation in Log Messages

```csharp
// ❌ BAD - String interpolation (allocates regardless of log level)
_logger?.LogDebug($"Entity {entityId} at position ({x}, {y})");

// ✅ GOOD - Structured logging (no allocation if disabled)
_logger?.LogDebug("Entity {EntityId} at position ({X}, {Y})", entityId, x, y);
```

### 3. Excessive Exception Logging

```csharp
// ❌ BAD - Logging same exception multiple times
public void LoadMap(string path)
{
    try
    {
        LoadMapInternal(path);
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Load failed"); // Logged here
        throw; // And will be logged again by caller
    }
}

// ✅ GOOD - Log once at appropriate boundary
public void LoadMap(string path)
{
    LoadMapInternal(path); // Let exceptions propagate
}
```

### 4. Logging Personal or Sensitive Data

```csharp
// ❌ BAD - Logging sensitive information
_logger?.LogInformation("User logged in: {Username} with password {Password}", username, password);

// ✅ GOOD - Log minimal necessary information
_logger?.LogInformation("User logged in: {UserId}", userId);
```

### 5. Ignoring Log Level Checks for Expensive Operations

```csharp
// ❌ BAD - Expensive computation always runs
_logger?.LogDebug("System state: {State}", ComputeExpensiveState());

// ✅ GOOD - Conditional computation
if (_logger?.IsEnabled(LogLevel.Debug) == true)
{
    var state = ComputeExpensiveState();
    _logger.LogDebug("System state: {State}", state);
}
```

### 6. Vague or Generic Messages

```csharp
// ❌ BAD - Vague message
_logger?.LogError(ex, "Error occurred");

// ✅ GOOD - Specific, actionable message
_logger?.LogError(ex, "Failed to load tileset '{TilesetId}' for map {MapId}: texture file not found",
    tilesetId, mapId);
```

### 7. Logging Without Context

```csharp
// ❌ BAD - No context
_logger?.LogWarning("Missing sprite");

// ✅ GOOD - Rich context
_logger?.LogWarning("Missing sprite '{SpriteId}' for NPC '{NpcId}' on map {MapId} - using placeholder",
    spriteId, npcId, mapId);
```

---

## Code Examples

### Example 1: System Initialization

```csharp
public class SystemManager
{
    private readonly ILogger<SystemManager>? _logger;

    public void Initialize(World world)
    {
        if (_initialized)
            throw new InvalidOperationException("SystemManager has already been initialized.");

        var totalSystems = _updateSystems.Count + _renderSystems.Count;
        _logger?.LogSystemsInitializing(totalSystems); // EventId: 5000

        foreach (var system in _updateSystems)
        {
            try
            {
                _logger?.LogSystemInitializing(system.GetType().Name); // EventId: 5001
                system.Initialize(world);
            }
            catch (Exception ex)
            {
                _logger?.LogExceptionWithContext(ex,
                    "Failed to initialize update system: {SystemName}",
                    system.GetType().Name);
                throw;
            }
        }

        _initialized = true;
        _logger?.LogSystemsInitialized(); // EventId: 5002
    }
}
```

### Example 2: Map Loading

```csharp
public class MapLoader
{
    private readonly ILogger<MapLoader>? _logger;

    public Entity LoadMap(World world, string mapId)
    {
        using (_logger?.BeginScope($"Loading:{mapId}"))
        {
            _logger?.LogInformation("Loading map from definition: {MapId}", mapId);

            var mapDef = _mapDefinitionService.GetMap(mapId);
            if (mapDef == null)
            {
                throw new FileNotFoundException($"Map definition not found: {mapId}");
            }

            var tilesCreated = ProcessLayers(world, tmxDoc, mapId, loadedTilesets);
            var objectsCreated = SpawnMapObjects(world, tmxDoc, mapId);

            _logger?.LogInformation(
                "Map loaded: {MapName} ({Width}x{Height}) | Tiles: {TileCount} | Objects: {ObjectCount}",
                mapDef.DisplayName, tmxDoc.Width, tmxDoc.Height, tilesCreated, objectsCreated);

            return mapInfoEntity;
        }
    }
}
```

### Example 3: Performance Monitoring

```csharp
public class SystemManager
{
    private readonly ILogger<SystemManager>? _logger;
    private readonly SystemPerformanceTracker _performanceTracker;

    public void Update(World world, float deltaTime)
    {
        _performanceTracker.IncrementFrame();

        foreach (var system in _cachedEnabledUpdateSystems)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                system.Update(world, deltaTime);
                sw.Stop();

                var elapsedMs = sw.Elapsed.TotalMilliseconds;
                TrackSystemPerformance(system.GetType().Name, elapsedMs);

                // Warn on slow systems (threshold-based)
                if (elapsedMs > 16.67) // ~1 frame at 60 FPS
                {
                    _logger?.LogWarning(
                        "Slow system: {SystemName} took {ElapsedMs:F2}ms (threshold: 16.67ms)",
                        system.GetType().Name, elapsedMs);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Update system {SystemName} failed during execution",
                    system.GetType().Name);
            }
        }

        // Periodic performance stats (every 5 seconds at 60fps)
        if (_performanceTracker.FrameCount % 300 == 0)
        {
            _performanceTracker.LogPerformanceStats();
        }
    }
}
```

### Example 4: Resource Management

```csharp
public class MapLifecycleManager
{
    private readonly ILogger<MapLifecycleManager>? _logger;

    public void UnloadMap(int mapId)
    {
        if (!_loadedMaps.TryGetValue(mapId, out var metadata))
        {
            _logger?.LogWarning("Attempted to unload unknown map: {MapId}", mapId);
            return;
        }

        using (_logger?.BeginScope($"Unloading:{metadata.Name}"))
        {
            _logger?.LogInformation("Unloading map: {MapName} (ID: {MapId})",
                metadata.Name, mapId);

            var tilesDestroyed = DestroyMapEntities(mapId);
            var tilesetsUnloaded = UnloadMapTextures(metadata.TilesetTextureIds);
            var spritesUnloaded = UnloadSpriteTextures(mapId, metadata.SpriteTextureIds);

            _loadedMaps.Remove(mapId);

            _logger?.LogInformation(
                "Map unloaded: {Entities} entities, {Tilesets} tilesets, {Sprites} sprites freed",
                tilesDestroyed, tilesetsUnloaded, spritesUnloaded);
        }
    }
}
```

---

## Logging Decision Tree

```
Is this a per-frame operation? (60-120 calls/sec)
├─ YES → Use Trace level only (disabled in production)
│         ├─ Use source generator for zero allocation
│         └─ Never create strings or collections
└─ NO  → Continue...

Is this an error or exception?
├─ YES → Does it prevent the operation?
│         ├─ YES → Use Error level
│         │         └─ Include: operation, entity IDs, state, user impact
│         └─ NO  → Use Warning level
│                   └─ Explain fallback/recovery
└─ NO  → Continue...

Is this a state change or important event?
├─ YES → Does it affect user experience?
│         ├─ YES → Use Information level
│         │         └─ Example: Map loaded, game started
│         └─ NO  → Use Debug level
│                   └─ Example: Cache invalidated, system enabled
└─ NO  → Use Debug or Trace level
          └─ Trace for micro-details, Debug for diagnostics
```

---

## Summary

**Golden Rules**:
1. Use structured logging with `LoggerMessage` source generators
2. Always check `IsEnabled` before expensive operations
3. Never log in hot paths (60+ FPS) in production
4. Include rich contextual information (IDs, state, impact)
5. Use consistent parameter naming across all subsystems
6. Log exceptions once at the appropriate boundary
7. Use scopes for grouping related operations
8. Follow the event ID registry for categorization

**Performance Targets**:
- Trace/Debug: Disabled in production (zero overhead)
- Information: < 0.1ms per call
- Warning/Error: < 0.5ms per call
- Source generators: Zero allocations

**Compliance**:
- All new code must use source generators for log messages
- All hot path logging must be Trace level only
- All errors must include operation context
- All performance logs must include metrics (ms, count, etc.)
