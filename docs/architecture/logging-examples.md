# Logging Code Examples

Comprehensive code examples demonstrating correct logging patterns in PokeSharp.

## Table of Contents

1. [Source Generator Messages](#source-generator-messages)
2. [System Initialization](#system-initialization)
3. [Map Loading & Unloading](#map-loading--unloading)
4. [Performance Monitoring](#performance-monitoring)
5. [Exception Handling](#exception-handling)
6. [Resource Management](#resource-management)
7. [Scoped Logging](#scoped-logging)
8. [Conditional Logging](#conditional-logging)

---

## Source Generator Messages

### Defining Source Generator Log Messages

```csharp
// PokeSharp.Engine.Common/Logging/LogMessages.cs

using Microsoft.Extensions.Logging;

namespace PokeSharp.Engine.Common.Logging;

/// <summary>
/// High-performance logging messages using source generators.
/// These methods generate zero-allocation logging code at compile time.
/// </summary>
public static partial class LogMessages
{
    // Movement System Messages (1000-1999)
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Movement blocked: out of bounds ({X}, {Y}) for map {MapId}"
    )]
    public static partial void LogMovementBlocked(this ILogger logger, int x, int y, int mapId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Debug,
        Message = "Ledge jump: ({StartX}, {StartY}) -> ({EndX}, {EndY}) direction: {Direction}"
    )]
    public static partial void LogLedgeJump(
        this ILogger logger,
        int startX,
        int startY,
        int endX,
        int endY,
        string direction
    );

    // Performance Messages (3000-3999)
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Warning,
        Message = "Slow frame: {FrameTimeMs:F2}ms (target: {TargetMs:F2}ms)"
    )]
    public static partial void LogSlowFrame(this ILogger logger, float frameTimeMs, float targetMs);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Performance: Avg frame time: {AvgMs:F2}ms ({Fps:F1} FPS) | Min: {MinMs:F2}ms | Max: {MaxMs:F2}ms"
    )]
    public static partial void LogFrameTimeStats(
        this ILogger logger,
        float avgMs,
        float fps,
        float minMs,
        float maxMs
    );

    // Asset Loading Messages (4000-4999)
    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Debug,
        Message = "Loaded texture '{TextureId}' in {TimeMs:F2}ms ({Width}x{Height}px)"
    )]
    public static partial void LogTextureLoaded(
        this ILogger logger,
        string textureId,
        double timeMs,
        int width,
        int height
    );

    // System Initialization Messages (5000-5999)
    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message = "Initializing {Count} systems"
    )]
    public static partial void LogSystemsInitializing(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "All systems initialized successfully"
    )]
    public static partial void LogSystemsInitialized(this ILogger logger);

    // Memory Messages (6000-6999)
    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Warning,
        Message = "High memory usage: {MemoryMb:F2}MB (threshold: {ThresholdMb:F2}MB)"
    )]
    public static partial void LogHighMemoryUsage(
        this ILogger logger,
        double memoryMb,
        double thresholdMb
    );
}
```

### Using Source Generator Messages

```csharp
using PokeSharp.Engine.Common.Logging;

public class MovementSystem
{
    private readonly ILogger<MovementSystem>? _logger;

    public void ProcessMovement(int x, int y, int mapId)
    {
        if (IsOutOfBounds(x, y, mapId))
        {
            // Zero allocation - source generator creates optimal code
            _logger?.LogMovementBlocked(x, y, mapId);
            return;
        }
    }
}
```

---

## System Initialization

### System Manager Initialization

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Engine.Systems.Management;

public class SystemManager
{
    private readonly ILogger<SystemManager>? _logger;
    private readonly List<IUpdateSystem> _updateSystems = new();
    private readonly List<IRenderSystem> _renderSystems = new();
    private bool _initialized;

    public SystemManager(ILogger<SystemManager>? logger = null)
    {
        _logger = logger;
    }

    public void Initialize(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (_initialized)
        {
            throw new InvalidOperationException("SystemManager has already been initialized.");
        }

        // Log initialization start with total count
        var totalSystems = _updateSystems.Count + _renderSystems.Count;
        _logger?.LogSystemsInitializing(totalSystems); // EventId: 5000

        lock (_lock)
        {
            // Initialize update systems
            foreach (var system in _updateSystems)
            {
                if (system is ISystem legacySystem)
                {
                    try
                    {
                        var systemName = system.GetType().Name;
                        _logger?.LogSystemInitializing(systemName); // EventId: 5001

                        legacySystem.Initialize(world);

                        _logger?.LogDebug("System initialized successfully: {SystemName}", systemName);
                    }
                    catch (Exception ex)
                    {
                        // Log with rich context at the catch site
                        _logger?.LogExceptionWithContext(
                            ex,
                            "Failed to initialize update system: {SystemName}",
                            system.GetType().Name
                        );
                        throw; // Re-throw - this is unrecoverable
                    }
                }
            }

            // Initialize render systems
            foreach (var system in _renderSystems)
            {
                if (system is ISystem legacySystem)
                {
                    try
                    {
                        var systemName = system.GetType().Name;
                        _logger?.LogSystemInitializing(systemName); // EventId: 5001

                        legacySystem.Initialize(world);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogExceptionWithContext(
                            ex,
                            "Failed to initialize render system: {SystemName}",
                            system.GetType().Name
                        );
                        throw;
                    }
                }
            }
        }

        _initialized = true;
        _logger?.LogSystemsInitialized(); // EventId: 5002
    }

    public void RegisterUpdateSystem(IUpdateSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        lock (_lock)
        {
            _updateSystems.Add(system);
            _updateSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // Use structured logging with consistent parameter names
            _logger?.LogDebug(
                "Registered update system: {SystemName} (Priority: {Priority})",
                system.GetType().Name,
                system.Priority
            );
        }
    }
}
```

---

## Map Loading & Unloading

### Map Loader with Scoped Logging

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Game.Data.MapLoading.Tiled;

public class MapLoader
{
    private readonly ILogger<MapLoader>? _logger;
    private readonly IAssetProvider _assetManager;

    public Entity LoadMap(World world, string mapId)
    {
        // Use scoped logging to group all related operations
        using (_logger?.BeginScope($"Loading:{mapId}"))
        {
            _logger?.LogInformation(
                "Loading map from definition: {MapId}",
                mapId
            );

            // Get map definition
            var mapDef = _mapDefinitionService.GetMap(mapId);
            if (mapDef == null)
            {
                throw new FileNotFoundException($"Map definition not found: {mapId}");
            }

            _logger?.LogDebug(
                "Map definition loaded: {DisplayName} | Tiled path: {TiledPath}",
                mapDef.DisplayName,
                mapDef.TiledDataPath
            );

            // Load Tiled data
            var tmxDoc = LoadTiledDocument(mapDef.TiledDataPath);

            // Process layers
            var tilesCreated = ProcessLayers(world, tmxDoc, mapId, loadedTilesets);
            _logger?.LogDebug("Created {TileCount} tile entities", tilesCreated);

            // Spawn objects
            var objectsCreated = SpawnMapObjects(world, tmxDoc, mapId);
            _logger?.LogDebug("Spawned {ObjectCount} map objects", objectsCreated);

            // Create metadata
            var mapInfoEntity = CreateMapMetadata(world, tmxDoc, mapDef);

            // Log final summary with all metrics
            _logger?.LogInformation(
                "Map loaded: {MapName} ({Width}x{Height}) | Tiles: {TileCount} | Objects: {ObjectCount} | Tilesets: {TilesetCount}",
                mapDef.DisplayName,
                tmxDoc.Width,
                tmxDoc.Height,
                tilesCreated,
                objectsCreated,
                loadedTilesets.Count
            );

            return mapInfoEntity;
        }
    }

    private int ProcessLayers(World world, TmxDocument tmxDoc, int mapId, List<LoadedTileset> tilesets)
    {
        var tilesCreated = 0;

        for (var layerIndex = 0; layerIndex < tmxDoc.Layers.Count; layerIndex++)
        {
            var layer = tmxDoc.Layers[layerIndex];

            // Use structured logging for layer processing
            _logger?.LogDebug(
                "Processing layer: {LayerName} (Index: {LayerIndex}, Elevation: {Elevation})",
                layer.Name,
                layerIndex,
                DetermineElevation(layer, layerIndex)
            );

            var layerTileCount = CreateTileEntities(world, tmxDoc, mapId, tilesets, layer);
            tilesCreated += layerTileCount;
        }

        return tilesCreated;
    }
}
```

### Map Lifecycle Manager

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Game.Systems;

public class MapLifecycleManager
{
    private readonly ILogger<MapLifecycleManager>? _logger;
    private readonly Dictionary<int, MapMetadata> _loadedMaps = new();

    public void TransitionToMap(int newMapId)
    {
        if (newMapId == _currentMapId)
        {
            _logger?.LogDebug("Already on map {MapId}, skipping transition", newMapId);
            return;
        }

        var oldMapId = _currentMapId;
        _currentMapId = newMapId;

        // Log state transition with clear before/after
        _logger?.LogInformation(
            "Map transition: {OldMapId} → {NewMapId}",
            oldMapId,
            newMapId
        );

        // Clean up old maps
        var mapsToUnload = _loadedMaps.Keys
            .Where(id => id != _currentMapId && id != _previousMapId)
            .ToList();

        foreach (var mapId in mapsToUnload)
        {
            UnloadMap(mapId);
        }
    }

    public void UnloadMap(int mapId)
    {
        if (!_loadedMaps.TryGetValue(mapId, out var metadata))
        {
            _logger?.LogWarning("Attempted to unload unknown map: {MapId}", mapId);
            return;
        }

        // Use scope for all unload operations
        using (_logger?.BeginScope($"Unloading:{metadata.Name}"))
        {
            _logger?.LogInformation("Unloading map: {MapName} (ID: {MapId})", metadata.Name, mapId);

            // Track metrics for summary
            var tilesDestroyed = DestroyMapEntities(mapId);
            var tilesetsUnloaded = UnloadMapTextures(metadata.TilesetTextureIds);
            var spritesUnloaded = UnloadSpriteTextures(mapId, metadata.SpriteTextureIds);

            _loadedMaps.Remove(mapId);

            // Single summary log with all metrics
            _logger?.LogInformation(
                "Map unloaded: {Entities} entities, {Tilesets} tilesets, {Sprites} sprites freed",
                tilesDestroyed,
                tilesetsUnloaded,
                spritesUnloaded
            );
        }
    }

    public void ForceCleanup()
    {
        _logger?.LogWarning("Force cleanup triggered - unloading all inactive maps");

        var mapsToUnload = _loadedMaps.Keys.Where(id => id != _currentMapId).ToList();

        foreach (var mapId in mapsToUnload)
        {
            UnloadMap(mapId);
        }

        // Log memory stats before and after GC
        var beforeMemory = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var afterMemory = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        var freedMemory = beforeMemory - afterMemory;

        _logger?.LogInformation(
            "Force cleanup completed: {FreedMb:F2}MB freed ({BeforeMb:F2}MB → {AfterMb:F2}MB)",
            freedMemory,
            beforeMemory,
            afterMemory
        );
    }
}
```

---

## Performance Monitoring

### System Performance Tracking

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Engine.Systems.Management;

public class SystemManager
{
    private readonly ILogger<SystemManager>? _logger;
    private readonly SystemPerformanceTracker _performanceTracker;

    public void Update(World world, float deltaTime)
    {
        ArgumentNullException.ThrowIfNull(world);

        _performanceTracker.IncrementFrame();

        foreach (var system in _cachedEnabledUpdateSystems)
        {
            try
            {
                // Measure system execution time
                var sw = Stopwatch.StartNew();
                system.Update(world, deltaTime);
                sw.Stop();

                var elapsedMs = sw.Elapsed.TotalMilliseconds;

                // Track performance for periodic logging
                TrackSystemPerformance(system.GetType().Name, elapsedMs);

                // Warn on slow systems (threshold-based)
                if (elapsedMs > 16.67) // ~1 frame at 60 FPS
                {
                    _logger?.LogWarning(
                        "Slow system: {SystemName} took {ElapsedMs:F2}ms (threshold: 16.67ms)",
                        system.GetType().Name,
                        elapsedMs
                    );
                }
            }
            catch (Exception ex)
            {
                // Log system failure with context
                _logger?.LogError(
                    ex,
                    "Update system {SystemName} failed during execution",
                    system.GetType().Name
                );
                // Don't re-throw - log and continue with other systems
            }
        }

        // Periodic performance stats (every 5 seconds at 60fps)
        if (_performanceTracker.FrameCount % 300 == 0)
        {
            _performanceTracker.LogPerformanceStats();
        }
    }
}

public class SystemPerformanceTracker
{
    private readonly ILogger? _logger;
    private readonly Dictionary<string, SystemMetrics> _systemMetrics = new();
    private long _frameCount;

    public void LogPerformanceStats()
    {
        if (_logger == null || !_logger.IsEnabled(LogLevel.Information))
            return;

        var avgFrameTime = CalculateAverageFrameTime();
        var fps = 1000.0f / avgFrameTime;

        // Log overall frame stats
        _logger.LogFrameTimeStats(avgFrameTime, fps, _minFrameTime, _maxFrameTime);

        // Log individual system stats (only if Debug enabled)
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var kvp in _systemMetrics.OrderByDescending(x => x.Value.AverageUpdateMs))
            {
                _logger.LogDebug(
                    "System {SystemName} - Avg: {AvgMs:F2}ms | Max: {MaxMs:F2}ms | Calls: {UpdateCount}",
                    kvp.Key,
                    kvp.Value.AverageUpdateMs,
                    kvp.Value.MaxUpdateMs,
                    kvp.Value.UpdateCount
                );
            }
        }
    }
}
```

---

## Exception Handling

### Exception Logging with Context

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Engine.Rendering.Assets;

public class AssetManager
{
    private readonly ILogger<AssetManager>? _logger;

    public void LoadTexture(string textureId, string path)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            var texture = LoadTextureFromFile(path);

            sw.Stop();

            // Success - log with metrics
            _logger?.LogTextureLoaded(
                textureId,
                sw.Elapsed.TotalMilliseconds,
                texture.Width,
                texture.Height
            );

            // Warn if slow (threshold-based)
            if (sw.Elapsed.TotalMilliseconds > 100)
            {
                _logger?.LogWarning(
                    "Slow texture load: '{TextureId}' took {TimeMs:F2}ms",
                    textureId,
                    sw.Elapsed.TotalMilliseconds
                );
            }
        }
        catch (FileNotFoundException ex)
        {
            // Specific exception - include path context
            _logger?.LogError(
                ex,
                "Failed to load texture '{TextureId}': file not found at '{Path}'",
                textureId,
                path
            );
            throw; // Re-throw - caller needs to handle
        }
        catch (Exception ex)
        {
            // Generic exception - use ExceptionWithContext for rich info
            _logger?.LogExceptionWithContext(
                ex,
                "Failed to load texture '{TextureId}' from '{Path}'",
                textureId,
                path
            );
            throw;
        }
    }
}
```

### Custom Exception Context Extension

```csharp
using Microsoft.Extensions.Logging;

namespace PokeSharp.Engine.Common.Logging;

public static class LoggerExtensions
{
    /// <summary>
    /// Logs an exception with additional context information.
    /// Includes thread ID, timestamp, machine name, and exception type.
    /// </summary>
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
}
```

---

## Resource Management

### Timed Operations

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Game.Data.Loading;

public class GameDataLoader
{
    private readonly ILogger<GameDataLoader>? _logger;

    public void LoadAllData()
    {
        // Use LogTimed extension for automatic timing
        _logger?.LogTimed(
            "Load all game data",
            () =>
            {
                LoadPokemonData();
                LoadMoveData();
                LoadAbilityData();
                LoadItemData();
            },
            warnThresholdMs: 1000.0 // Warn if > 1 second
        );
    }

    public PokemonData LoadPokemonData()
    {
        // LogTimed with return value
        return _logger?.LogTimed(
            "Load Pokemon data",
            () =>
            {
                var data = ReadFromDatabase();
                ValidateData(data);
                return data;
            },
            warnThresholdMs: 500.0
        ) ?? new PokemonData();
    }
}
```

### Memory Tracking

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Engine.Systems.Pooling;

public class EntityPoolManager
{
    private readonly ILogger<EntityPoolManager>? _logger;

    public void MonitorMemoryUsage()
    {
        // Periodic memory logging (every 10 seconds)
        if (_frameCount % 600 == 0)
        {
            _logger?.LogMemoryStats(includeGcStats: true);

            // Check threshold
            var memoryMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            if (memoryMb > _memoryThresholdMb)
            {
                _logger?.LogHighMemoryUsage(memoryMb, _memoryThresholdMb);
            }
        }
    }

    public void PerformCleanup()
    {
        _logger?.LogInformation("Performing memory cleanup");

        // Log memory before and after
        _logger?.LogMemoryStatsWithCollection();

        // Cleanup pools
        var freedEntities = CleanupEntityPools();
        var freedComponents = CleanupComponentPools();

        _logger?.LogInformation(
            "Cleanup completed: {Entities} entities, {Components} components freed",
            freedEntities,
            freedComponents
        );
    }
}
```

---

## Scoped Logging

### Hierarchical Scopes

```csharp
using Microsoft.Extensions.Logging;

namespace PokeSharp.Game.Initialization;

public class GameInitializer
{
    private readonly ILogger<GameInitializer>? _logger;

    public void Initialize()
    {
        using (_logger?.BeginScope("Initialization"))
        {
            _logger?.LogInformation("Starting game initialization");

            InitializeSystems();
            LoadGameData();
            CreatePlayer();

            _logger?.LogInformation("Game initialization complete");
        }
    }

    private void InitializeSystems()
    {
        using (_logger?.BeginScope("Systems"))
        {
            _logger?.LogInformation("Initializing core systems");

            InitializeEcs();
            InitializeRendering();
            InitializeInput();
        }
    }

    private void LoadGameData()
    {
        using (_logger?.BeginScope("GameData"))
        {
            _logger?.LogInformation("Loading game data");

            LoadPokemon();
            LoadMoves();
            LoadItems();
        }
    }
}

// Output:
// [Initialization] Starting game initialization
// [Initialization > Systems] Initializing core systems
// [Initialization > Systems] ECS initialized
// [Initialization > Systems] Rendering initialized
// [Initialization > Systems] Input initialized
// [Initialization > GameData] Loading game data
// [Initialization > GameData] Loaded 151 Pokemon
// [Initialization > GameData] Loaded 165 moves
// [Initialization > GameData] Loaded 376 items
// [Initialization] Game initialization complete
```

---

## Conditional Logging

### Debug-Only Expensive Logging

```csharp
using Microsoft.Extensions.Logging;

namespace PokeSharp.Game.Data.MapLoading.Tiled;

public class MapLoader
{
    private readonly ILogger<MapLoader>? _logger;

    public void LoadMap(string mapId)
    {
        _logger?.LogInformation("Loading map: {MapId}", mapId);

        // Collect sprite IDs
        var spriteIds = CollectSpriteIds();

        // Detailed debug logging (only if Debug enabled)
        if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Collected {Count} sprite IDs for map {MapId}",
                spriteIds.Count,
                mapId
            );

            // Expensive operation - only runs if Debug enabled
            foreach (var spriteId in spriteIds.OrderBy(x => x))
            {
                _logger.LogDebug("  - Required sprite: {SpriteId}", spriteId);
            }
        }
    }
}
```

### Trace-Level Hot Path Logging

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Game.Systems.Movement;

public class CollisionSystem
{
    private readonly ILogger<CollisionSystem>? _logger;

    public void Update(World world, float deltaTime)
    {
        // Hot path - Trace level only (disabled in production)
        world.Query(in _movementQuery, (Entity entity, ref Position pos, ref MovementRequest request) =>
        {
            var targetX = pos.X + request.DeltaX;
            var targetY = pos.Y + request.DeltaY;

            if (IsBlocked(targetX, targetY, pos.MapId))
            {
                // TRACE LEVEL - zero overhead in production
                _logger?.LogCollisionBlocked(targetX, targetY, request.Direction.ToString());

                request.Blocked = true;
                return;
            }

            // Process movement...
        });
    }
}
```

---

## Summary

**Key Takeaways**:

1. **Always use source generators** for zero-allocation logging
2. **Check IsEnabled** before expensive operations
3. **Use scopes** for grouping related operations
4. **Include rich context** in all log messages
5. **Follow event ID conventions** from the registry
6. **Never log in hot paths** except at Trace level
7. **Log exceptions once** at the appropriate boundary
8. **Use consistent parameter names** across all subsystems
