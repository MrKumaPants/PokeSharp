# LogTemplates Usage Guide

**Version:** 1.0.0
**Last Updated:** 2025-11-16

## Overview

The `LogTemplates` class provides high-performance, zero-allocation logging using C# Source Generators (`LoggerMessage` attribute). All templates include rich Spectre.Console formatting for enhanced readability.

## Quick Start

### Basic Usage

```csharp
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;

public class SpriteLoader
{
    private readonly ILogger<SpriteLoader> _logger;

    public SpriteLoader(ILogger<SpriteLoader> logger)
    {
        _logger = logger;
    }

    public void LoadSprite(string spriteId, string textureId, int width, int height)
    {
        // Use source-generated log template
        _logger.LogSpriteTextureLoaded(spriteId, textureId, width, height);
    }
}
```

### Output Example

```
[11:23:45] [INF] [green]✓[/] Sprite loaded | [cyan]player/may[/] | [yellow]sprites/may_sheet[/] | 128x256px
```

## Template Categories

### 1. Rendering Templates (1000-1999)

**Sprite Loading:**
```csharp
_logger.LogSpriteTextureLoaded("player/may", "sprites/may_sheet", 128, 256);
_logger.LogSpriteManifestLoaded("may_walking", 8, 32);
_logger.LogSpriteNotFound("invalid_sprite", "players");
```

**Animation:**
```csharp
_logger.LogSpriteAnimationUpdated("Player", entityId, "walking_down", 2, 4);
```

**Render Statistics:**
```csharp
_logger.LogRenderPassCompleted(tileCount: 1200, spriteCount: 45, drawCalls: 1245);
_logger.LogElevationLayerRendered(elevation: 3, entityCount: 150, timeMs: 0.25);
```

### 2. Systems Templates (2000-2999)

**System Lifecycle:**
```csharp
_logger.LogSystemRegistered("SpriteAnimationSystem", priority: 875);
_logger.LogSystemLifecycle("RenderSystem", "enabled");
_logger.LogSystemUpdateCompleted("MovementSystem", timeMs: 0.12);
```

**Spatial Hashing:**
```csharp
_logger.LogSpatialHashRebuilt(entityCount: 2500, cellCount: 100, timeMs: 1.25);
```

**Component Pooling:**
```csharp
_logger.LogComponentPoolCreated("Position", capacity: 1000);
_logger.LogComponentPoolStats("Sprite", active: 250, available: 750, hitRate: 95.3);
```

**Pathfinding:**
```csharp
_logger.LogPathfindingCompleted(
    startX: 10, startY: 5,
    endX: 25, endY: 18,
    nodesExplored: 143,
    timeMs: 2.8
);
```

### 3. Map Loading Templates (3000-3999)

**Map Lifecycle:**
```csharp
_logger.LogMapLoadingStarted("littleroot_town", "Littleroot Town");
_logger.LogMapDefinitionLoaded("littleroot_town", "Data/Maps/littleroot.json");
```

**Tileset Loading:**
```csharp
_logger.LogTilesetLoaded("tileset_general", tileWidth: 16, tileHeight: 16, tileCount: 256);
_logger.LogExternalTilesetLoaded("tileset_anim", animationCount: 12, source: "../tilesets/anim.json");
```

**Layer Processing:**
```csharp
_logger.LogTileLayerParsed("ground", width: 50, height: 40, elevation: 0);
_logger.LogAnimatedTilesCreated(count: 24, tilesetName: "water_tiles");
_logger.LogImageLayerCreated("background", textureId: "bg_clouds", depth: 0.95f);
```

**Object Spawning:**
```csharp
_logger.LogMapObjectSpawned("Professor Oak", "npc/professor", x: 15, y: 20);
_logger.LogNpcDefinitionApplied("oak_001", "Professor Oak", "scripts/oak_intro.cs");
```

**Sprite Collection:**
```csharp
_logger.LogSpriteCollectionCompleted(count: 12, mapId: "route_101");
_logger.LogMapTexturesTracked(mapId: 1, count: 3);
```

### 4. Data Loading Templates (4000-4999)

**Initialization:**
```csharp
_logger.LogGameDataLoaderInitialized("Data/pokesharp.db");
_logger.LogDatabaseMigrated("v1.2.0", timeMs: 125.5);
```

**Templates & Assets:**
```csharp
_logger.LogTemplateLoaded("npc/generic", componentCount: 8);
_logger.LogDeserializerRegistered("Position");
_logger.LogAssetLoadedWithType("player_sprite", "Texture2D", timeMs: 12.3);
```

**Caching:**
```csharp
_logger.LogAssetCacheHit("tileset_general", "Texture2D");
_logger.LogAssetCacheMiss("new_texture");
_logger.LogAssetEvicted("old_texture", "LRU eviction");
```

**Type Registration:**
```csharp
_logger.LogTypeRegistered("NPCBehavior", "PokeSharp.Game.Scripting");
```

### 5. Scripting Templates (5000-5999)

**Compilation:**
```csharp
_logger.LogScriptCompilationStarted("scripts/oak_intro.cs");
_logger.LogScriptCompilationSucceeded("oak_intro", version: 3, timeMs: 450.0);
_logger.LogScriptCompilationFailed("invalid_script", errorCount: 2, timeMs: 125.0);
_logger.LogScriptDiagnosticError(line: 42, column: 15, message: "Missing semicolon", code: "CS1002");
```

**Hot-Reload:**
```csharp
_logger.LogHotReloadStarted("FileSystemWatcher", debounceMs: 300);
_logger.LogScriptChangeDebounced("oak_intro.cs", totalDebounced: 5);
_logger.LogScriptBackupCreated("oak_intro", version: 2);
_logger.LogScriptRollback("oak_intro", version: 2, method: "cache");
```

**Runtime:**
```csharp
_logger.LogNpcBehaviorAttached(entityId: 1234, scriptTypeId: "oak_intro");
_logger.LogScriptExecutionError("oak_intro", "OnInteract", "NullReferenceException");
_logger.LogScriptWatcherError(isCritical: true, message: "File system access denied");
```

## Performance Considerations

### Source Generator Benefits

All LogTemplates use `[LoggerMessage]` attribute for:
- ✅ **Zero allocations** in hot paths
- ✅ **Compile-time code generation** (no reflection)
- ✅ **10-100x faster** than string interpolation
- ✅ **Structured logging** support

### Log Level Guidelines

| Level | When to Use | Performance Impact |
|-------|-------------|--------------------|
| **Debug** | Per-frame stats, cache operations | High frequency - keep minimal |
| **Information** | Lifecycle events, successful operations | Medium frequency |
| **Warning** | Recoverable errors, missing optional data | Low frequency |
| **Error** | Failures, exceptions, critical issues | Very low frequency |

### Example: Per-Frame Logging

```csharp
// ❌ BAD: This logs every frame (60+ times per second)
_logger.LogInformation("Frame {FrameNumber}", frameNumber);

// ✅ GOOD: Use Debug level for high-frequency logs
_logger.LogSystemUpdateCompleted("RenderSystem", timeMs);
```

## Color Markup Reference

### Common Patterns

```csharp
// Success indicator
[green]✓[/] Operation succeeded

// Warning indicator
[orange1]⚠[/] Warning message

// Error indicator
[red]✗[/] Error occurred

// Rollback/Undo indicator
[orange1]↶[/] Rolled back

// Names and identifiers
[cyan]EntityName[/]

// Numeric values and counts
[yellow]123[/]

// Secondary/metadata
[grey]metadata[/] or [dim]secondary info[/]
```

### Full Color List

- `[green]` - Success, OK status
- `[cyan]` - Primary identifiers (names, IDs)
- `[yellow]` - Values, counts, metrics
- `[magenta]` - Coordinates, secondary values
- `[orange1]` - Warnings, rollbacks
- `[red]` - Errors, failures
- `[grey]` - Debug metadata
- `[dim]` - Low-priority secondary info
- `[aqua]` - Timing, performance metrics

## Migrating Existing Logs

### Before (Manual String Formatting)

```csharp
_logger.LogInformation(
    "Sprite loaded: {SpriteId} ({TextureId}) - {Width}x{Height}px",
    spriteId, textureId, width, height
);
```

### After (LogTemplate)

```csharp
_logger.LogSpriteTextureLoaded(spriteId, textureId, width, height);
```

### Benefits of Migration

1. **Type Safety**: Compile-time parameter validation
2. **Consistency**: Uniform formatting across codebase
3. **Performance**: Zero allocations via source generator
4. **Maintainability**: Single source of truth for log formats
5. **Event IDs**: Structured logging with unique identifiers

## Testing

### Unit Test Example

```csharp
[Fact]
public void LogSpriteTextureLoaded_FormatsCorrectly()
{
    // Arrange
    var logger = new TestLogger<SpriteLoader>();

    // Act
    logger.LogSpriteTextureLoaded("player/may", "sprites/may_sheet", 128, 256);

    // Assert
    var logEntry = logger.GetLastEntry();
    Assert.Equal(LogLevel.Information, logEntry.Level);
    Assert.Equal(1001, logEntry.EventId.Id);
    Assert.Contains("player/may", logEntry.Message);
    Assert.Contains("128x256px", logEntry.Message);
}
```

## Best Practices

### ✅ DO

- Use appropriate log levels based on frequency
- Include relevant context (IDs, counts, timings)
- Keep parameter names descriptive
- Use existing templates when available
- Test log templates in unit tests

### ❌ DON'T

- Log sensitive data (passwords, tokens)
- Use Information level for per-frame operations
- Create duplicate templates
- Log large objects or collections
- Skip error handling before logging

## Adding New Templates

See `docs/LogTemplate-EventIDs.md` for event ID ranges and registration process.

### Template Pattern

```csharp
/// <summary>Brief description of what this logs</summary>
[LoggerMessage(
    EventId = XXXX,  // Choose from appropriate range
    Level = LogLevel.YourLevel,
    Message = "[accent]{Param1}[/] | [color]{Param2}[/]")]
public static partial void LogYourTemplate(
    this ILogger logger,
    string param1,
    int param2);
```

## Support

- Event ID Registry: `docs/LogTemplate-EventIDs.md`
- LogTemplates Source: `PokeSharp.Engine.Common/Logging/LogTemplates.cs`
- Test Examples: `tests/LoggingTests/`
