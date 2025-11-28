# PokeSharp Logging Standards

## Overview

This document defines the logging standards for the PokeSharp project. All logging should follow these guidelines to ensure consistency, maintainability, and optimal integration with Serilog.

---

## 1. Log Level Decision Tree

### LogDebug
**Use for:** Detailed diagnostic information useful during development
- Individual tile/entity creation details
- Frame-by-frame system execution timing
- Sprite ID collections
- Spatial hash invalidation
- Detailed asset loading information
- Animation frame details

**Examples:**
```csharp
logger.LogDebug("Spatial hash invalidated for map '{MapName}'", mapName);
logger.LogDebug("  - {SpriteId}", spriteId);
logger.LogDebug("Parsed animation for tile {TileId}: {FrameCount} frames", tileId, frameCount);
```

### LogInformation
**Use for:** Normal operational events that represent significant state changes
- System initialization completion
- Map loading completion (summary)
- NPC definition application
- Workflow completion
- Asset loading summary (not individual assets)
- Configuration changes

**Examples:**
```csharp
logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapId, displayName);
logger.LogInformation("Game data definitions loaded successfully");
logger.LogInformation("Applied NPC definition '{NpcId}' ({DisplayName})", npcId, displayName);
```

### LogWarning
**Use for:** Unexpected but recoverable situations
- Missing optional resources
- Performance issues (slow operations)
- Invalid but non-critical configuration
- Deprecated API usage
- Resource not found (when there's a fallback)

**Examples:**
```csharp
logger.LogWarning("NPC definition not found: '{NpcId}' (falling back to map properties)", npcId);
logger.LogWarning("Failed to parse layer of type {Type}", layerType);
```

### LogError
**Use for:** Errors that prevent an operation from completing
- File not found (no fallback)
- Parsing failures
- Invalid required configuration
- Exceptions during critical operations

**Examples:**
```csharp
logger.LogError(ex, "Failed to load external tileset from {Path}", tilesetPath);
logger.LogError("Invalid tile dimensions: {Width}x{Height}", tileWidth, tileHeight);
```

### LogCritical
**Use for:** Fatal errors that prevent the application from continuing
- Game initialization failures
- Critical system failures
- Unrecoverable exceptions

**Examples:**
```csharp
logger.LogCritical(ex, "Fatal error during game initialization");
logger.LogCritical("Game initialization failed - exiting");
```

---

## 2. Message Template Format

### Standard Format
```
{Verb} {Noun}: {Details}
```

**Good Examples:**
```csharp
logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapId, displayName);
logger.LogDebug("Spatial hash invalidated for map '{MapName}'", mapName);
logger.LogInformation("Applied NPC definition '{NpcId}' ({DisplayName}) with behavior={Behavior}", npcId, displayName, behavior);
```

**Bad Examples:**
```csharp
// ❌ Multi-line logs
logger.LogInformation("MapId: {MapId}", mapId);
logger.LogInformation("DisplayName: {DisplayName}", displayName);

// ❌ No context
logger.LogInformation("{Count}", count);

// ❌ Too verbose for LogInformation
logger.LogInformation("Tile created at {X}, {Y}", x, y);
```

### Combine Related Information
**Do this:**
```csharp
logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapId, displayName);
```

**Not this:**
```csharp
logger.LogInformation("Loading map from definition: {MapId}", mapId);
logger.LogInformation("DisplayName: {DisplayName}", displayName);
```

---

## 3. Parameter Naming

### Use PascalCase for All Parameters
Serilog uses PascalCase for structured logging parameters.

**Correct:**
```csharp
logger.LogInformation("Map loaded: {MapId} width={Width} height={Height}", mapId, width, height);
logger.LogDebug("Asset loaded: {AssetPath} time={LoadTimeMs}ms", assetPath, loadTimeMs);
```

**Incorrect:**
```csharp
logger.LogInformation("Map loaded: {mapId} width={width}", mapId, width); // ❌ camelCase
logger.LogDebug("Asset loaded: {asset_path}", assetPath); // ❌ snake_case
```

### Common Parameter Names
- `MapId`, `MapName`, `DisplayName`
- `EntityId`, `EntityType`
- `ComponentType`, `ComponentCount`
- `FilePath`, `AssetPath`, `TextureId`
- `Width`, `Height`, `TileWidth`, `TileHeight`
- `Count`, `Total`, `Index`
- `TimeMs`, `ElapsedMs`, `LoadTimeMs`
- `X`, `Y`, `Position`
- `NpcId`, `SpriteId`, `TemplateId`

---

## 4. No Color Markup

### Serilog Handles Formatting
**Don't use Spectre.Console markup in log messages:**

**Bad:**
```csharp
logger.LogInformation("[green]Map loaded[/]: [cyan]{MapId}[/]", mapId);
logger.LogDebug("[dim]MapId:[/] [grey]{MapId}[/]", mapId);
```

**Good:**
```csharp
logger.LogInformation("Map loaded: {MapId}", mapId);
logger.LogDebug("Spatial hash invalidated for map '{MapName}'", mapName);
```

**Rationale:** Serilog sinks (Console, File, Seq, etc.) handle their own formatting and colorization. Color markup in messages:
- Creates visual noise in structured log viewers
- Breaks JSON formatting
- Makes logs harder to parse
- Is sink-specific (not portable)

---

## 5. Structured Logging Best Practices

### Use Named Parameters
**Good:**
```csharp
logger.LogInformation("Entity spawned: {EntityType} at ({X}, {Y})", entityType, x, y);
```

**Bad:**
```csharp
logger.LogInformation($"Entity spawned: {entityType} at ({x}, {y})"); // ❌ String interpolation
logger.LogInformation("Entity spawned: " + entityType); // ❌ String concatenation
```

### Complex Objects
For complex objects, use `@` prefix to serialize:
```csharp
logger.LogDebug("NPC definition loaded: {@NpcDefinition}", npcDef);
```

### Use Scopes for Context
```csharp
using (logger.BeginScope("Loading:{MapId}", mapId))
{
    logger.LogDebug("Parsing layers");
    logger.LogDebug("Creating entities");
}
```

---

## 6. Extension Methods

### Prefer Extension Methods for Common Patterns
Instead of repeating templates, create extension methods:

**Good:**
```csharp
// In GameLoggingExtensions.cs
public static void LogMapLoaded(this ILogger logger, string mapId, string displayName)
{
    logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapId, displayName);
}

// Usage
logger.LogMapLoaded(mapId, displayName);
```

**When to create extensions:**
- Pattern used in 3+ places
- Complex formatting logic
- Domain-specific logging

---

## 7. Performance Considerations

### Use IsEnabled for Expensive Operations
```csharp
if (logger.IsEnabled(LogLevel.Debug))
{
    var details = ExpensiveCalculation();
    logger.LogDebug("Details: {Details}", details);
}
```

### Avoid String Allocation in Hot Paths
```csharp
// ❌ Bad (allocates strings every frame)
logger.LogDebug($"Frame {frameNumber} took {elapsed}ms");

// ✅ Good (structured logging, no allocation until needed)
logger.LogDebug("Frame {FrameNumber} took {ElapsedMs}ms", frameNumber, elapsed);
```

---

## 8. Common Patterns

### System Initialization
```csharp
logger.LogInformation("System initialized: {SystemName} with {Count} entities", systemName, count);
```

### Asset Loading
```csharp
// Summary (Information)
logger.LogInformation("Assets loaded: {Count} textures in {TimeMs}ms", count, timeMs);

// Individual (Debug)
logger.LogDebug("Asset loaded: {AssetPath} ({Width}x{Height}) in {TimeMs}ms", path, width, height, timeMs);
```

### Entity Operations
```csharp
// Creation (Debug)
logger.LogDebug("Entity created: {EntityType} #{EntityId}", entityType, entityId);

// Spawning from template (Information if significant, Debug otherwise)
logger.LogDebug("Spawned '{ObjectName}' ({TemplateId}) at ({X}, {Y})", objName, templateId, x, y);
```

### Errors with Context
```csharp
logger.LogError(ex, "Failed to load tileset from {Path}", tilesetPath);
```

---

## 9. Anti-Patterns to Avoid

### ❌ Don't Log Inside Loops (Unless Debug)
```csharp
// Bad
for (int i = 0; i < 1000; i++)
{
    logger.LogInformation("Processing item {Index}", i); // ❌ Floods logs
}

// Good
logger.LogInformation("Processing {Count} items", items.Count);
for (int i = 0; i < items.Count; i++)
{
    if (logger.IsEnabled(LogLevel.Debug))
        logger.LogDebug("Processing item {Index}", i); // ✅ Only if debug enabled
}
```

### ❌ Don't Use Multi-Line Logs for Single Concepts
```csharp
// Bad
logger.LogInformation("Map loaded");
logger.LogInformation("MapId: {MapId}", mapId);
logger.LogInformation("Size: {Width}x{Height}", width, height);

// Good
logger.LogInformation("Map loaded: {MapId} ({Width}x{Height})", mapId, width, height);
```

### ❌ Don't Log Redundant Information
```csharp
// Bad
logger.LogInformation("Starting to load map");
logger.LogInformation("Map loading started");
logger.LogInformation("Loading map now");

// Good
logger.LogDebug("Loading map: {MapId}", mapId); // Just once, with context
```

---

## 10. Migration Checklist

When updating logging in a file:

- [ ] Remove all Spectre.Console color markup (`[green]`, `[/]`, etc.)
- [ ] Change verbose `LogInformation` to `LogDebug`
- [ ] Combine multi-line logs into single statements
- [ ] Use PascalCase for all parameter names
- [ ] Use structured logging (no string interpolation)
- [ ] Add scopes for related operations
- [ ] Check log level appropriateness
- [ ] Remove redundant logs
- [ ] Add missing context to error logs

---

## 11. Examples by File Type

### MapLoader.cs
```csharp
// ✅ Good
logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapId, displayName);
logger.LogDebug("Spatial hash invalidated for map '{MapName}'", mapName);
logger.LogDebug("Parsed animation for tile {TileId}: {FrameCount} frames", tileId, frameCount);

// ❌ Bad
logger.LogInformation("[green]Map loaded[/]");
logger.LogInformation("MapId: {MapId}", mapId);
logger.LogInformation("DisplayName: {DisplayName}", displayName);
```

### PokeSharpGame.cs
```csharp
// ✅ Good
logger.LogInformation("Game data definitions loaded successfully");
logger.LogCritical(ex, "Fatal error during game initialization");

// ❌ Bad
logger.LogInformation("[green]Game initialized successfully[/]");
logger.LogError("Error: " + ex.Message);
```

### System Classes
```csharp
// ✅ Good
logger.LogDebug("System executed: {SystemName} in {TimeMs}ms", systemName, timeMs);
logger.LogWarning("System slow: {SystemName} took {TimeMs}ms (threshold: {ThresholdMs}ms)",
    systemName, timeMs, thresholdMs);

// ❌ Bad
logger.LogInformation("System: {SystemName}", systemName);
logger.LogInformation("Time: {Time}", timeMs);
```

---

## Summary

**Key Principles:**
1. **Use appropriate log levels** (Debug for details, Information for summaries)
2. **No color markup** (Serilog handles formatting)
3. **PascalCase parameters** (Serilog convention)
4. **Combine related information** (one event = one log call)
5. **Structured logging only** (no string interpolation)
6. **Add context** (who/what/where/when/why)
7. **Avoid log spam** (especially in loops)
8. **Use scopes** (for grouping related operations)

Following these standards will result in logs that are:
- **Searchable** (structured data)
- **Filterable** (appropriate levels)
- **Readable** (consistent format)
- **Performant** (minimal overhead)
- **Portable** (work with any Serilog sink)
