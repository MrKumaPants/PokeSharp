# LogTemplates System - Complete Guide

## Overview

The PokeSharp logging system uses **Serilog 4.2.0** with **Spectre.Console markup** for rich, colored console output. All logging is standardized through **LogTemplates** - zero-allocation extension methods powered by `[LoggerMessage]` source generators.

**Key Features:**
- Zero allocation logging via source generators
- Colored console output with Spectre.Console markup
- Consistent categorization with visual prefixes
- Async file logging with rotation
- Comprehensive performance monitoring
- Cross-platform Windows/Linux support

## Architecture

### Components

1. **Serilog.Sinks.Spectre** - Interprets Spectre.Console markup in log messages
2. **LogTemplates.cs** - 56 hand-crafted extension methods with `WithAccent()` helper
3. **LogTemplates.Generated.cs** - 41 source-generated templates
4. **LogAccent Enum** - 12 category types with color/prefix mappings
5. **PerformanceMonitor** - Runtime FPS, memory, GC tracking

### Logging Flow

```
Logger.LogXXX()
  → LogTemplates extension method
  → Spectre markup string
  → Serilog.Sinks.Spectre
  → Colored console output
```

## Log Categories & Prefixes

Each log category has a unique **colored prefix** and **semantic meaning**:

| Category | Prefix | Color | Usage | Example |
|----------|--------|-------|-------|---------|
| **Initialization** | `▶` | skyblue1 | Startup, bootstrapping | `▶ Template cache ready` |
| **Asset** | `A` | aqua | Asset loading (textures, sprites, data) | `A Loading game data from Assets/Data` |
| **Map** | `M` | springgreen1 | Map loading, transitions, lifecycle | `M ✓ Map loaded: town_01` |
| **Performance** | `P` | plum1 | FPS, frame times, benchmarks | `P Frame: 16.7ms @ 60fps` |
| **Memory** | `MEM` | lightsteelblue1 | Memory usage, GC stats | `MEM 128.5MB (Gen0:10, Gen1:2)` |
| **Render** | `R` | mediumorchid1 | Rendering, sprites, textures | `R ✓ Sprite loaded: player_sprite` |
| **Entity** | `E` | gold1 | Entity lifecycle (spawn, destroy, pool) | `E ✓ Entity spawned: #12345` |
| **Input** | `I` | deepskyblue3 | Input events, key presses | `I Key pressed: Space` |
| **Workflow** | `WF` | steelblue1 | Multi-step operations, workflows | `WF Template JSON loaded` |
| **System** | `SYS` | orange3 | ECS system execution | `SYS MovementSystem: 2.5ms` |
| **Script** | `S` | deepskyblue1 | Scripting engine, Lua execution | `S Script compiled: player_interact.lua` |

## Event ID Registry

### Hand-Crafted Templates (LogTemplates.cs)

| Event ID | Level | Template | Category | Usage |
|----------|-------|----------|----------|-------|
| 100 | Info | `LogTextureLoaded` | Asset | Texture file loaded |
| 101 | Info | `LogTextureCreated` | Render | Texture created in VRAM |
| 200 | Info | `LogMapLoaded` | Map | Map successfully loaded |
| 201 | Info | `LogMapTransition` | Map | Map transition started |
| 202 | Debug | `LogLayerProcessed` | Map | Tiled layer processed |
| 300 | Info | `LogEntitySpawned` | Entity | Entity created in world |
| 301 | Info | `LogEntityDestroyed` | Entity | Entity removed from world |
| 302 | Debug | `LogEntityPoolCreated` | Entity | Entity pool initialized |
| 303 | Debug | `LogEntityPoolHit` | Entity | Entity recycled from pool |
| 304 | Debug | `LogEntityPoolMiss` | Entity | New entity created (pool miss) |
| 400 | Info | `LogSystemRegistered` | System | ECS system registered |
| 401 | Debug | `LogSystemUpdateStarted` | System | System update began |
| 402 | Debug | `LogSystemUpdateCompleted` | System | System update finished |
| 403 | Info | `LogSystemPerformance` | Performance | System performance stats |
| 500 | Info | `LogFramePerformance` | Performance | Frame timing statistics |
| 501 | Warn | `LogSlowFrame` | Performance | Frame exceeded budget |
| 502 | Info | `LogMemoryStatistics` | Memory | Memory & GC stats |
| 503 | Warn | `LogHighMemoryUsage` | Memory | Memory threshold exceeded |
| 600 | Info | `LogScriptLoaded` | Script | Lua script loaded |
| 601 | Info | `LogScriptExecuted` | Script | Script execution completed |
| 602 | Error | `LogScriptError` | Script | Script runtime error |

### Generated Templates (LogTemplates.Generated.cs)

| Event ID | Level | Template | Category | Usage |
|----------|-------|----------|----------|-------|
| 1001 | Info | `LogSpriteTextureLoaded` | Render | Sprite texture loaded |
| 1002 | Info | `LogSpriteSheetParsed` | Render | Sprite sheet parsed |
| 1003 | Info | `LogAnimationCreated` | Render | Animation created |
| 1004 | Debug | `LogRenderBatchCreated` | Render | Render batch initialized |
| 1005 | Debug | `LogDrawCallExecuted` | Render | Draw call executed |
| 1006 | Debug | `LogShaderCompiled` | Render | Shader compiled |
| 2001 | Info | `LogSystemInitialized` | System | ECS system initialized |
| 2002 | Debug | `LogSystemUpdateCompleted` | System | System update completed |
| 2003 | Debug | `LogComponentAdded` | System | Component added to entity |
| 2004 | Debug | `LogComponentRemoved` | System | Component removed |
| 2005 | Debug | `LogQueryCreated` | System | ECS query created |
| 2006 | Debug | `LogQueryExecuted` | System | ECS query executed |
| 2007 | Info | `LogSystemPerformanceReport` | System | Performance metrics |
| 2008 | Warn | `LogSystemSlowUpdate` | System | System update slow |
| 3001 | Info | `LogMapDataLoaded` | Map | Map data loaded |
| 3002 | Info | `LogTilesetLoaded` | Map | Tileset loaded |
| 3003 | Info | `LogLayerRendered` | Map | Map layer rendered |
| 3004 | Debug | `LogTileDrawn` | Map | Individual tile drawn |
| 3005 | Info | `LogMapObjectSpawned` | Map | Map object spawned |
| 3006 | Debug | `LogCollisionLayerProcessed` | Map | Collision layer processed |
| 3007 | Info | `LogMapTransitionStarted` | Map | Map transition started |
| 3008 | Info | `LogMapTransitionCompleted` | Map | Map transition finished |
| 3009 | Debug | `LogMapUnloaded` | Map | Map unloaded from memory |
| 3010 | Info | `LogMapCached` | Map | Map cached for reuse |
| 3011 | Debug | `LogMapCacheHit` | Map | Map loaded from cache |
| 4001 | Info | `LogAssetLoaded` | Asset | Generic asset loaded |
| 4002 | Info | `LogAssetCached` | Asset | Asset cached |
| 4003 | Debug | `LogAssetUnloaded` | Asset | Asset unloaded |
| 4004 | Info | `LogGameDataLoaded` | Asset | Game data loaded |
| 4005 | Info | `LogNpcDefinitionLoaded` | Asset | NPC definition loaded |
| 4006 | Info | `LogItemDataLoaded` | Asset | Item data loaded |
| 4007 | Debug | `LogAssetManagerInitialized` | Asset | Asset manager initialized |
| 4008 | Info | `LogContentLoaded` | Asset | MonoGame content loaded |
| 4009 | Warn | `LogAssetNotFound` | Asset | Asset missing |
| 5001 | Info | `LogScriptCompiled` | Script | Script compiled successfully |
| 5002 | Info | `LogScriptFunctionCalled` | Script | Script function invoked |
| 5003 | Debug | `LogScriptVariableSet` | Script | Script variable assigned |
| 5004 | Error | `LogScriptCompilationFailed` | Script | Script compilation error |
| 5005 | Error | `LogScriptRuntimeError` | Script | Script runtime error |
| 5006 | Info | `LogScriptApiRegistered` | Script | Script API registered |
| 5007 | Debug | `LogScriptContextCreated` | Script | Script context created |
| 5008 | Debug | `LogScriptCacheHit` | Script | Script loaded from cache |
| 5009 | Info | `LogScriptReloaded` | Script | Script hot-reloaded |
| 5010 | Warn | `LogScriptTimeout` | Script | Script execution timeout |
| 5011 | Debug | `LogScriptGarbageCollected` | Script | Lua GC executed |

**Total: 97 LogTemplates** (56 hand-crafted + 41 generated)

## Usage Guide

### Basic Usage

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

public class MapLoader
{
    private readonly ILogger<MapLoader> _logger;

    public MapLoader(ILogger<MapLoader> logger)
    {
        _logger = logger;
    }

    public void LoadMap(string mapId)
    {
        _logger.LogMapLoaded(mapId, 256, 192); // Event ID 200
    }
}
```

### Advanced: Creating New Templates

**Hand-Crafted (LogTemplates.cs):**
```csharp
[LoggerMessage(EventId = 700, Level = LogLevel.Information,
    Message = "{AccentPrefix} Quest started | [cyan]{QuestId}[/] | [yellow]{QuestName}[/]")]
private static partial void LogQuestStartedCore(
    this ILogger logger,
    string accentPrefix,
    string questId,
    string questName);

public static void LogQuestStarted(this ILogger logger, string questId, string questName)
{
    logger.LogQuestStartedCore(WithAccent(LogAccent.Workflow), questId, questName);
}
```

**Source-Generated (LogTemplates.Generated.cs):**
```csharp
[LoggerMessage(EventId = 6001, Level = LogLevel.Information,
    Message = "[steelblue1]WF[/] [green]✓[/] Quest started | [cyan]{QuestId}[/] | [yellow]{QuestName}[/]")]
public static partial void LogQuestStarted(this ILogger logger, string questId, string questName);
```

### Spectre Markup Reference

**Colors:** `[red]`, `[green]`, `[cyan]`, `[yellow]`, `[aqua]`, `[orange3]`, `[springgreen1]`, `[mediumorchid1]`, `[deepskyblue1]`

**Formatting:** `[bold]`, `[dim]`, `[italic]`, `[underline]`

**Glyphs:** `✓` (success), `✗` (error), `▶` (start), `◆` (bullet)

## Performance Characteristics

### Zero Allocation Logging

LogTemplates use `[LoggerMessage]` source generators which achieve:
- **Zero allocations** for filtered logs (when log level disabled)
- **Minimal allocations** for active logs (pre-allocated format strings)
- **10-100x faster** than string interpolation or `string.Format()`

### Benchmarks

From `tests/LoggingTests/PerformanceTests.cs`:

| Benchmark | Target | Result |
|-----------|--------|--------|
| Memory increase (1000 logs) | < 5MB | ✓ Pass |
| GC Gen0 (10k logs) | < 50 collections | ✓ Pass |
| GC Gen1 (10k logs) | < 10 collections | ✓ Pass |
| GC Gen2 (10k logs) | < 3 collections | ✓ Pass |
| Filtered log overhead (100k) | < 50ms | ✓ Pass |
| Template logging (3000 calls) | < 3 seconds | ✓ Pass |
| Async file logging (1000 logs) | < 500ms | ✓ Pass |
| Parallel throughput | > 1000 msg/sec | ✓ Pass |

### Runtime Monitoring

`PerformanceMonitor` tracks:
- **Frame times** (16.67ms target @ 60 FPS)
- **FPS** (rolling 60-frame average)
- **Memory usage** (warns above 500MB)
- **GC collections** (Gen0, Gen1, Gen2)
- **Slow frames** (> 50% over budget)

Logs automatically every 5 seconds (300 frames @ 60fps).

## Configuration

### appsettings.json (Production)

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Spectre", "Serilog.Sinks.File", "Serilog.Sinks.Async" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "Spectre",
              "Args": {
                "outputTemplate": "[{Timestamp:HH:mm:ss.fff}] [{Level:u5}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
              }
            }
          ]
        }
      },
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "File",
              "Args": {
                "path": "logs/pokesharp-.log",
                "rollingInterval": "Day",
                "retainedFileCountLimit": 7,
                "fileSizeLimitBytes": 10485760
              }
            }
          ]
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithThreadId", "WithMachineName" ],
    "Properties": {
      "Application": "PokeSharp"
    }
  }
}
```

### SerilogConfiguration.cs (Development Fallback)

```csharp
loggerConfig
    .MinimumLevel.Is(LogEventLevel.Debug)
    .WriteTo.Spectre(
        outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u5}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.Async(a => a.File(
        path: "logs/pokesharp-development-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 3,
        fileSizeLimitBytes: 50 * 1024 * 1024
    ));
```

### Windows ANSI Color Support

Program.cs enables Virtual Terminal Processing for colored output:

```csharp
if (OperatingSystem.IsWindows())
{
    const int STD_OUTPUT_HANDLE = -11;
    const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    var handle = GetStdHandle(STD_OUTPUT_HANDLE);
    if (handle != IntPtr.Zero)
    {
        GetConsoleMode(handle, out uint mode);
        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        SetConsoleMode(handle, mode);
    }
}
```

**Note:** Use **Windows Terminal** or **PowerShell 7+** for best color support. Legacy `cmd.exe` may have limited ANSI support.

## Best Practices

### ✅ DO

1. **Use LogTemplates for all logging** - Zero allocation, consistent formatting
2. **Choose correct log level:**
   - `Debug` - Detailed diagnostics (disabled in Production)
   - `Information` - Significant events (map loaded, entity spawned)
   - `Warning` - Recoverable issues (slow frames, high memory)
   - `Error` - Failures requiring attention
3. **Use semantic categories** - Choose appropriate LogAccent prefix
4. **Include relevant data** - Entity IDs, timings, counts
5. **Log important lifecycle events** - Initialization, transitions, cleanup

### ❌ DON'T

1. **Don't use string interpolation** - Use LogTemplates parameters instead
   ```csharp
   // ❌ BAD
   _logger.LogInformation($"Map {mapId} loaded");

   // ✅ GOOD
   _logger.LogMapLoaded(mapId, width, height);
   ```

2. **Don't log in hot paths** - Cache results or use Debug level
   ```csharp
   // ❌ BAD (logs every frame)
   void Update(float dt) {
       _logger.LogInformation("Update {DeltaTime}", dt);
   }

   // ✅ GOOD (logs every 5 seconds)
   if (_frameCounter % 300 == 0) {
       _logger.LogFramePerformance(avgMs, fps, minMs, maxMs);
   }
   ```

3. **Don't log sensitive data** - Never log passwords, tokens, user data
4. **Don't create templates without Event IDs** - Use next available ID in range
5. **Don't mix manual logging with templates** - Standardize on templates

## Troubleshooting

### Colors not showing in console

**Solution:** Enable Windows ANSI support (already in Program.cs) or use Windows Terminal.

**Verify:**
```bash
# Run game in Windows Terminal (not cmd.exe)
cd PokeSharp.Game/bin/Debug/net9.0
./PokeSharp.Game.exe
```

### Logs not appearing

**Check log level:** Ensure `appsettings.json` allows the level (Debug < Information < Warning < Error)

**Check namespace override:** `Override` section may filter specific namespaces

### High memory usage

**Check async sink:** File sink should use `Async` wrapper to prevent blocking

**Check retention:** `retainedFileCountLimit` should be 3-7 days max

### Performance degradation

**Profile with PerformanceMonitor:** Automatically logs every 5 seconds

**Run performance tests:**
```bash
cd tests/LoggingTests
dotnet test --filter "FullyQualifiedName~PerformanceTests"
```

## Migration Guide

### Converting from direct ILogger calls:

**Before:**
```csharp
_logger.LogInformation("Map {MapId} loaded with {TileCount} tiles", mapId, tileCount);
```

**After:**
```csharp
_logger.LogMapLoaded(mapId, width, height); // Uses Event ID 200
```

### Adding new LogTemplate:

1. Choose Event ID range:
   - 100-199: Assets & Rendering
   - 200-299: Maps
   - 300-399: Entities
   - 400-499: Systems
   - 500-599: Performance & Memory
   - 600-699: Scripting
   - 700+: Available

2. Add to `LogTemplates.Generated.cs`:
```csharp
[LoggerMessage(EventId = 701, Level = LogLevel.Information,
    Message = "[steelblue1]WF[/] [green]✓[/] Your message | [cyan]{Param1}[/]")]
public static partial void LogYourEvent(this ILogger logger, string param1);
```

3. Rebuild project - source generator creates implementation

## Related Files

- `/PokeSharp.Engine.Common/Logging/LogTemplates.cs` - Hand-crafted templates
- `/PokeSharp.Engine.Common/Logging/LogTemplates.Generated.cs` - Source-generated templates
- `/PokeSharp.Engine.Common/Logging/SerilogConfiguration.cs` - Serilog setup
- `/PokeSharp.Game/Config/appsettings.json` - Production config
- `/PokeSharp.Game/Diagnostics/PerformanceMonitor.cs` - Runtime monitoring
- `/tests/LoggingTests/PerformanceTests.cs` - Performance benchmarks
- `/tests/PokeSharp.Engine.Systems.Tests/Management/SystemPerformanceTrackerTests.cs` - System tests

## Summary

**97 total LogTemplates** provide zero-allocation, colored, categorized logging with:
- Consistent visual prefixes (12 categories)
- Comprehensive event coverage (assets, maps, entities, systems, scripts)
- Production-ready performance (13 performance tests passing)
- Cross-platform Windows/Linux support
- Rich console output with Spectre.Console markup

**Use LogTemplates for all logging to ensure consistency, performance, and maintainability.**
