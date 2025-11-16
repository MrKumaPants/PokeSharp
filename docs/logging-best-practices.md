# PokeSharp Logging Best Practices and Standards

**Version:** 1.0
**Last Updated:** 2025-11-15
**Research Date:** 2025-11-15

---

## Table of Contents

1. [Log Level Standards](#log-level-standards)
2. [Message Template Standards](#message-template-standards)
3. [Structured Logging Best Practices](#structured-logging-best-practices)
4. [Color/Formatting Standards](#colorformatting-standards)
5. [Performance Patterns](#performance-patterns)
6. [What to Log (Game Engine Context)](#what-to-log-game-engine-context)
7. [PokeSharp Recommendations](#pokesharp-recommendations)
8. [Examples: Good vs Bad](#examples-good-vs-bad)

---

## Log Level Standards

### Microsoft/Serilog Log Level Guidelines

Based on official Microsoft documentation and Serilog best practices:

#### **Trace (Serilog: Verbose)**
- **When to use:** Detailed flow tracing, execution paths, per-frame diagnostics
- **Purpose:** Captures the journey between snapshots, including all small steps in between
- **Production:** Should be DISABLED in production (compile away or filter)
- **Game Engine Context:** Per-frame updates, render calls, input polling
- **Performance:** Zero-cost when disabled; constant-time boolean check when enabled

**Examples:**
```csharp
// ✅ GOOD: Per-frame low-level diagnostics
logger.LogTrace("Frame {FrameNumber} rendering {EntityCount} entities", frame, count);

// ✅ GOOD: Detailed execution flow
logger.LogTrace("Entity {EntityId} position updated to ({X}, {Y})", id, x, y);

// ❌ BAD: Don't use Trace for important state changes
logger.LogTrace("Map loaded successfully"); // Use Information instead
```

#### **Debug**
- **When to use:** Developer troubleshooting, state snapshots, non-critical system details
- **Purpose:** Tells you what current values and conditions are at specific points
- **Production:** Typically DISABLED in production builds
- **Game Engine Context:** System state, component values, pathfinding decisions

**Examples:**
```csharp
// ✅ GOOD: Developer diagnostics
logger.LogDebug("Collision check: entity {EntityId} at ({X}, {Y}) blocked by {Blocker}",
    entityId, x, y, blockerType);

// ✅ GOOD: System state snapshots
logger.LogDebug("Processing {EntityCount} entities in {SystemName}", count, systemName);

// ❌ BAD: Don't use Debug for production-critical events
logger.LogDebug("User authentication failed"); // Use Warning/Error instead
```

#### **Information**
- **When to use:** System lifecycle events, important state changes, successful operations
- **Purpose:** Production-level visibility into running state and correctness
- **Production:** ENABLED - critical for operations and monitoring
- **Game Engine Context:** Map loading, system initialization, major game state changes

**Examples:**
```csharp
// ✅ GOOD: System lifecycle
logger.LogInformation("Initializing {Count} systems", systemCount);
logger.LogInformation("Map {MapName} loaded successfully", mapName);

// ✅ GOOD: Important state changes
logger.LogInformation("Player entered {MapName}", mapName);

// ❌ BAD: Don't use Information for every small operation
logger.LogInformation("Moved entity 5 pixels"); // Use Debug or Trace
```

#### **Warning**
- **When to use:** Recoverable errors, degraded performance, unexpected but handled situations
- **Purpose:** Indicates something unusual that should be investigated but doesn't stop execution
- **Production:** ENABLED - review warnings regularly
- **Game Engine Context:** Missing optional resources, slow operations, fallback behaviors

**Examples:**
```csharp
// ✅ GOOD: Performance degradation
logger.LogWarning("System {SystemName} took {TimeMs:F2}ms (threshold: {ThresholdMs}ms)",
    systemName, timeMs, threshold);

// ✅ GOOD: Recoverable resource issues
logger.LogWarning("Sprite {SpriteName} not found in manifest, using fallback", spriteName);

// ❌ BAD: Don't use Warning for expected behavior
logger.LogWarning("Cache miss for {Key}"); // Use Debug instead
```

#### **Error**
- **When to use:** Operation failures, exceptions, unrecoverable errors
- **Purpose:** Something went wrong that prevents normal operation
- **Production:** ENABLED - requires immediate attention
- **Game Engine Context:** Failed asset loads, system crashes, corrupted data

**Examples:**
```csharp
// ✅ GOOD: Operation failure with context
logger.LogError(ex, "Failed to load map {MapId} from {Path}", mapId, path);

// ✅ GOOD: Unrecoverable state
logger.LogError("Entity {EntityId} has invalid position ({X}, {Y})", entityId, x, y);
```

#### **Critical**
- **When to use:** Application-threatening failures, data corruption, system crashes
- **Purpose:** The application cannot continue or data integrity is at risk
- **Production:** ENABLED - requires URGENT response
- **Game Engine Context:** Renderer failure, save file corruption, catastrophic errors

**Examples:**
```csharp
// ✅ GOOD: Application-level failure
logger.LogCritical(ex, "Renderer initialization failed - cannot continue");

// ✅ GOOD: Data integrity threat
logger.LogCritical("Save file corrupted at {Path}, backup restoration required", path);
```

---

## Message Template Standards

### Parameter Naming Conventions

**Rule:** Use **PascalCase** for all named placeholders.

```csharp
// ✅ GOOD: PascalCase parameters
logger.LogInformation("Entity {EntityId} moved to ({PositionX}, {PositionY})", id, x, y);

// ❌ BAD: camelCase parameters
logger.LogInformation("Entity {entityId} moved to ({positionX}, {positionY})", id, x, y);

// ❌ BAD: snake_case parameters
logger.LogInformation("Entity {entity_id} moved to ({position_x}, {position_y})", id, x, y);
```

### Message Style

**Rule:** Messages are fragments, not sentences. Avoid trailing periods.

```csharp
// ✅ GOOD: Fragment without period
logger.LogInformation("System initialized successfully");

// ❌ BAD: Sentence with period
logger.LogInformation("The system has been initialized successfully.");
```

**Rule:** Use property names as content within the message.

```csharp
// ✅ GOOD: Property name integrated into message
logger.LogInformation("Loading {AssetType} from {AssetPath}", assetType, path);

// ❌ BAD: Generic placeholders
logger.LogInformation("Loading {0} from {1}", assetType, path);
```

### Verb-First vs Noun-First

**PokeSharp Standard:** Use **verb-first** (action-oriented) for active operations, **noun-first** for state descriptions.

```csharp
// ✅ GOOD: Verb-first for actions
logger.LogInformation("Loading map {MapId}", mapId);
logger.LogInformation("Processing {EntityCount} entities", count);

// ✅ GOOD: Noun-first for state
logger.LogInformation("Map {MapName} loaded successfully", mapName);
logger.LogInformation("System {SystemName} ready", systemName);

// ❌ BAD: Inconsistent style
logger.LogInformation("Map loading {MapId}", mapId); // Unclear gerund form
```

### Punctuation and Separators

**PokeSharp Standard:** Use pipe `|` for primary separators, colon `:` for key-value pairs.

```csharp
// ✅ GOOD: Consistent separator usage
logger.LogInformation("System {SystemName} | entities: {Count} | time: {TimeMs:F2}ms",
    systemName, count, timeMs);

// ✅ GOOD (via templates): Pre-formatted with separators
logger.LogAssetLoaded(assetPath, assetType, loadTimeMs);
// Outputs: "A   Asset loaded | path: textures/sprite.png, type: Texture, time: 45.2ms"
```

---

## Structured Logging Best Practices

### Parameter Count

**Rule:** Limit to **5 parameters or fewer** per log statement. Use structured objects for complex data.

```csharp
// ✅ GOOD: Reasonable parameter count
logger.LogInformation(
    "Entity spawned | type: {EntityType}, id: {EntityId}, position: ({X}, {Y})",
    entityType, entityId, x, y);

// ❌ BAD: Too many parameters (hard to read)
logger.LogInformation(
    "Entity created | type: {Type}, id: {Id}, x: {X}, y: {Y}, map: {Map}, layer: {Layer}, sprite: {Sprite}",
    type, id, x, y, map, layer, sprite);

// ✅ BETTER: Use structured object
logger.LogInformation(
    "Entity created | {@EntityData}",
    new { Type = type, Id = id, Position = (x, y), MapId = map, Layer = layer });
```

### Structure Capturing Operators

**`@` Operator:** Preserves structure (serializes objects)
**`$` Operator:** Forces string conversion

```csharp
// ✅ GOOD: Capture structure with @
logger.LogInformation("Loaded configuration | {@Config}", config);
// Outputs full object structure

// ✅ GOOD: Force string conversion with $
logger.LogInformation("Entity state | {$State}", complexObject);
// Outputs: Entity state | ComplexObject.ToString()

// ❌ BAD: Missing @ for complex objects
logger.LogInformation("Configuration loaded | {Config}", config);
// Outputs: Configuration loaded | ConfigClass (just type name)
```

### String Interpolation vs Templates

**Rule:** NEVER use string interpolation for log messages. Always use message templates.

```csharp
// ❌ VERY BAD: String interpolation (loses structure, allocates strings)
logger.LogInformation($"Entity {entityId} moved to ({x}, {y})");

// ✅ GOOD: Message template (structured, deferred allocation)
logger.LogInformation("Entity {EntityId} moved to ({X}, {Y})", entityId, x, y);
```

**Why?** String interpolation:
- Allocates strings even when logging is disabled
- Loses property structure (cannot query by EntityId)
- Cannot be analyzed by log aggregation tools
- Executes expensive ToString() calls unconditionally

---

## Color/Formatting Standards

### Console vs File Logging

#### Console Logging (Development)
- **Use:** Colored output with Spectre.Console markup
- **Purpose:** Enhanced readability during development
- **Format:** Rich formatting with color-coded levels, categories, and glyphs

```csharp
// PokeSharp's LogTemplates use rich console formatting
logger.LogSystemInitialized("RenderSystem", ("entities", 1000));
// Console output: ▶   [cyan]RenderSystem[/] [dim]initialized[/] | [dim]entities:[/] [aqua]1000[/]
```

#### File Logging (Production)
- **Use:** Plain text without markup
- **Purpose:** Machine-parseable, log aggregation compatibility
- **Format:** Structured text with timestamps and levels

```csharp
// FileLogger automatically strips markup
// File output: [2025-11-15 10:30:45.123] [INFO ] RenderSystem initialized | entities: 1000
```

#### Environment Detection

**PokeSharp Standard:** Automatically detects terminal capabilities.

```csharp
// LogFormatting.cs automatically handles this
if (LogFormatting.SupportsMarkup)
    AnsiConsole.MarkupLine(formatted); // Rich formatting
else
    AnsiConsole.WriteLine(StripMarkup(formatted)); // Plain text
```

**Override:** Set environment variable `POKESHARP_LOG_PLAIN=1` to force plain text.

### Serilog Console Themes

**Recommended Themes:**
- **Development:** `AnsiConsoleTheme.Code` (VS Code-inspired) or `SystemConsoleTheme.Literate`
- **CI/CD:** Plain text (no theme) or JSON formatting
- **Production:** File-based logging with plain text format

---

## Performance Patterns

### 1. LoggerMessage Source Generators

**Use:** `LoggerMessageAttribute` for high-frequency logging (hot paths).

**Benefits:**
- **Zero boxing:** Eliminates value type conversions
- **Zero allocations:** Pre-compiles message templates
- **Compile-time safety:** Catches format errors at build time
- **10-100x faster** than traditional logging

```csharp
// ✅ EXCELLENT: Source generator (zero-allocation)
public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Trace,
        Message = "Frame {FrameNumber} rendering {EntityCount} entities")]
    public static partial void LogFrameRender(this ILogger logger, int frameNumber, int entityCount);
}

// Usage (hot path)
logger.LogFrameRender(frameNumber, entityCount);

// ❌ BAD: Traditional logging (allocates every call)
logger.LogTrace("Frame {FrameNumber} rendering {EntityCount} entities", frameNumber, entityCount);
```

**Measurement:** Source generators reduce per-log overhead from **2-10ms to <0.1ms**.

### 2. IsEnabled Checks

**Rule:** Always check `IsEnabled()` before expensive operations.

```csharp
// ✅ GOOD: Guard expensive operations
if (logger.IsEnabled(LogLevel.Debug))
{
    var diagnostics = CalculateExpensiveDiagnostics(); // Only runs if Debug enabled
    logger.LogDebug("System diagnostics | {@Diagnostics}", diagnostics);
}

// ❌ BAD: Always calculates even if logging disabled
logger.LogDebug("System diagnostics | {@Diagnostics}", CalculateExpensiveDiagnostics());
```

### 3. Buffered File Writes

**PokeSharp Implementation:** FileLogger uses buffered writes with deferred flushing.

```csharp
// FileLogger.cs
_currentWriter = new StreamWriter(_currentLogFile, true, Encoding.UTF8)
{
    AutoFlush = false, // 95%+ reduction in disk I/O
};

// Manual flush only on Dispose()
_currentWriter?.Flush();
```

**Result:** Reduces per-log overhead from **2-10ms to <0.1ms**.

### 4. Hot Path Logging

**Rule:** Minimize or eliminate logging in hot paths (>60Hz execution).

```csharp
// ❌ BAD: Per-frame logging in 60 FPS game (generates 3,600 logs/minute)
void Update()
{
    logger.LogDebug("Frame update | entities: {Count}", entities.Count);
}

// ✅ GOOD: Aggregate logging (every second)
private int _frameCount = 0;
void Update()
{
    if (++_frameCount % 60 == 0)
    {
        logger.LogDebug("Frame stats | last 60 frames: {AverageFps:F1} FPS", 60.0);
    }
}

// ✅ BETTER: Use Trace level (compile away in release)
void Update()
{
    logger.LogTrace("Frame {FrameNumber} update", _frameCount++);
}
```

### 5. Tactical Logging

**Rule:** Add logs temporarily for debugging, then remove once issue is resolved.

```csharp
// During debugging
logger.LogDebug("Pathfinding iteration {Iteration} | nodes: {Nodes}", i, nodeCount);

// After fix: Remove or downgrade to Trace
// (Trace compiles away in release builds)
logger.LogTrace("Pathfinding iteration {Iteration} | nodes: {Nodes}", i, nodeCount);
```

---

## What to Log (Game Engine Context)

### ✅ ALWAYS LOG

#### 1. System Lifecycle
```csharp
logger.LogInformation("Initializing {Count} systems", systemCount);
logger.LogInformation("All systems initialized successfully");
logger.LogInformation("Game shutting down gracefully");
```

#### 2. Critical Operations
```csharp
logger.LogInformation("Loading map {MapName}", mapName);
logger.LogInformation("Saving game state to {Path}", savePath);
logger.LogInformation("Connecting to server {ServerUrl}", url);
```

#### 3. State Changes
```csharp
logger.LogInformation("Player entered {MapName}", mapName);
logger.LogInformation("Battle started | opponent: {OpponentName}", opponent);
```

#### 4. Errors and Exceptions
```csharp
logger.LogError(ex, "Failed to load asset {AssetPath}", path);
logger.LogError("Entity {EntityId} has invalid state", entityId);
```

#### 5. Performance Warnings
```csharp
logger.LogWarning("Slow system | {SystemName} took {TimeMs:F2}ms (>{ThresholdMs}ms)",
    systemName, timeMs, threshold);
logger.LogWarning("High memory usage | {MemoryMb:F2}MB (threshold: {ThresholdMb}MB)",
    memoryMb, thresholdMb);
```

### ⚠️ CONDITIONALLY LOG (Debug/Trace only)

#### 1. Per-Frame Operations
```csharp
// Use Trace (compiles away in release)
logger.LogTrace("Frame {FrameNumber} | FPS: {Fps:F1}", frameNum, fps);
```

#### 2. Component Operations
```csharp
// Use Debug (disabled in production)
logger.LogDebug("Component added | entity: {EntityId}, component: {ComponentType}",
    entityId, componentType);
```

#### 3. Pathfinding Details
```csharp
logger.LogDebug("Pathfinding | start: ({StartX}, {StartY}), goal: ({GoalX}, {GoalY})",
    startX, startY, goalX, goalY);
```

### ❌ NEVER LOG

#### 1. Secrets and Sensitive Data
```csharp
// ❌ NEVER: API keys, passwords, tokens
logger.LogInformation("Connecting with API key {ApiKey}", apiKey);

// ✅ GOOD: Redact sensitive data
logger.LogInformation("Connecting to server {ServerUrl} (credentials redacted)", url);
```

#### 2. Personally Identifiable Information (PII)
```csharp
// ❌ NEVER: User emails, passwords, personal data
logger.LogInformation("User {Email} logged in", userEmail);

// ✅ GOOD: Use user ID instead
logger.LogInformation("User {UserId} logged in", userId);
```

#### 3. Excessive Detail in Hot Paths
```csharp
// ❌ NEVER: Per-entity updates (thousands per frame)
foreach (var entity in entities)
{
    logger.LogDebug("Entity {EntityId} updated", entity.Id);
}

// ✅ GOOD: Aggregate statistics
logger.LogDebug("Updated {EntityCount} entities in {TimeMs:F2}ms", entities.Count, timeMs);
```

#### 4. Binary/Large Data
```csharp
// ❌ NEVER: Full byte arrays, large objects
logger.LogDebug("Loaded texture data | {@TextureData}", textureBytes);

// ✅ GOOD: Metadata only
logger.LogDebug("Loaded texture | size: {Width}x{Height}, format: {Format}",
    width, height, format);
```

---

## PokeSharp Recommendations

### Standard Log Levels per Component

| Component | Console Min Level | File Min Level | Production Min Level |
|-----------|------------------|----------------|---------------------|
| **Development** | Debug | Debug | Information |
| **Testing** | Information | Debug | Information |
| **Production** | Warning | Information | Warning |

### Log Categories by Domain

```csharp
// Initialization (Information)
logger.LogSystemInitialized("RenderSystem", ("version", "1.0"));

// Asset Loading (Information for success, Debug for details)
logger.LogInformation("Loaded {AssetType} | path: {Path}", type, path);
logger.LogDebug("Asset metadata | width: {Width}, height: {Height}", w, h);

// Performance (Debug for stats, Warning for threshold violations)
logger.LogDebug("Frame time | {FrameTimeMs:F2}ms ({Fps:F1} FPS)", frameMs, fps);
logger.LogWarning("Slow frame | {FrameTimeMs:F2}ms (target: {TargetMs}ms)", frameMs, target);

// Gameplay (Information for major events, Debug for details)
logger.LogInformation("Player entered {MapName}", mapName);
logger.LogDebug("Player position | ({X}, {Y})", x, y);
```

### Template Usage

**Prefer LogTemplates for common patterns:**

```csharp
// ✅ GOOD: Use pre-defined templates
logger.LogEntitySpawned("NPC", entityId, "oak_template", x, y);
logger.LogSlowSystem(systemName, timeMs, percent);

// ✅ ALSO GOOD: Direct logging for unique cases
logger.LogInformation("Custom event | {EventData}", eventData);
```

### Source Generators for Hot Paths

**Create LogMessages.cs for game-specific high-frequency logs:**

```csharp
public static partial class GameLogMessages
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Trace,
        Message = "Entity {EntityId} updated | position: ({X}, {Y})")]
    public static partial void LogEntityUpdate(this ILogger logger, int entityId, float x, float y);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug,
        Message = "Collision detected | entities: {Entity1} <-> {Entity2}")]
    public static partial void LogCollision(this ILogger logger, int entity1, int entity2);
}
```

---

## Examples: Good vs Bad

### Example 1: Asset Loading

```csharp
// ❌ BAD: String interpolation, missing structure, poor level choice
logger.LogDebug($"Loading sprite from {path}");
var sprite = LoadSprite(path);
logger.LogDebug($"Sprite loaded: {sprite.Width}x{sprite.Height}");

// ✅ GOOD: Structured logging, appropriate levels, rich context
logger.LogInformation("Loading sprite | path: {SpritePath}", path);
var sprite = logger.LogTimed("SpriteLoad", () => LoadSprite(path), warnThresholdMs: 50);
logger.LogDebug("Sprite loaded | size: {Width}x{Height}, format: {Format}",
    sprite.Width, sprite.Height, sprite.Format);
```

### Example 2: System Performance

```csharp
// ❌ BAD: No level check, expensive computation, poor formatting
logger.LogDebug($"System performance: {CalculateComplexStats()}");

// ✅ GOOD: IsEnabled check, deferred computation, source generator
if (logger.IsEnabled(LogLevel.Debug))
{
    var stats = CalculateComplexStats();
    logger.LogSystemPerformance(systemName, stats.AvgMs, stats.MaxMs, stats.Calls);
}
```

### Example 3: Error Handling

```csharp
// ❌ BAD: Missing exception, vague message, no context
try
{
    LoadMap(mapId);
}
catch (Exception)
{
    logger.LogError("Map load failed");
}

// ✅ GOOD: Exception included, specific context, recovery action
try
{
    LoadMap(mapId);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to load map {MapId} from {Path}, falling back to default",
        mapId, mapPath);
    LoadDefaultMap();
}
```

### Example 4: Hot Path Logging

```csharp
// ❌ BAD: Debug logging in 60 FPS loop (generates 3,600 logs/minute)
void Update(float deltaTime)
{
    logger.LogDebug("Update | deltaTime: {DeltaTime}", deltaTime);
    // ... update logic
}

// ✅ GOOD: Trace level (compiles away in release)
void Update(float deltaTime)
{
    logger.LogTrace("Update | deltaTime: {DeltaTime}", deltaTime);
    // ... update logic
}

// ✅ BETTER: Source generator for zero-cost when disabled
void Update(float deltaTime)
{
    logger.LogFrameUpdate(deltaTime); // LoggerMessage source-generated
    // ... update logic
}
```

### Example 5: Structured Data

```csharp
// ❌ BAD: Too many parameters, hard to read
logger.LogInformation("Entity created | type: {Type}, id: {Id}, x: {X}, y: {Y}, map: {Map}, layer: {Layer}, sprite: {Sprite}",
    type, id, x, y, map, layer, sprite);

// ✅ GOOD: Structured object with @ operator
logger.LogInformation("Entity created | {@EntityData}",
    new EntityCreationData(type, id, new Position(x, y), map, layer, sprite));

// ✅ ALSO GOOD: Use template with reasonable parameter count
logger.LogEntitySpawned(type, id, templateId, x, y);
```

---

## Summary of Key Decisions

### Log Levels
- **Trace:** Per-frame, hot path (compile away in release)
- **Debug:** Developer diagnostics (disabled in production)
- **Information:** System lifecycle, important events (production default)
- **Warning:** Performance issues, recoverable errors
- **Error:** Operation failures, exceptions
- **Critical:** Application-threatening failures

### Message Templates
- **PascalCase** for parameter names
- **Verb-first** for actions, **noun-first** for state
- **Pipe `|`** for separators, **colon `:`** for key-value
- **5 parameters max** per log statement
- **No string interpolation** - always use templates

### Performance
- **LoggerMessage source generators** for hot paths
- **IsEnabled checks** before expensive operations
- **Buffered file writes** (95%+ I/O reduction)
- **Tactical logging** (add temporarily, remove after debugging)
- **Aggregate statistics** instead of per-entity logs

### Color/Formatting
- **Console:** Colored output for development (Spectre.Console)
- **File:** Plain text for production/aggregation
- **Auto-detect:** Terminal capabilities (POKESHARP_LOG_PLAIN override)

### What to Log
- **✅ Always:** Lifecycle, critical ops, state changes, errors, performance warnings
- **⚠️ Conditional:** Per-frame ops (Trace), component details (Debug)
- **❌ Never:** Secrets, PII, excessive hot path logs, binary data

---

## References

- [Microsoft Logging Guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Writing-Log-Events)
- [Message Templates](https://messagetemplates.org/)
- [LoggerMessage Source Generators](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- [PokeSharp Logging Implementation](/PokeSharp.Engine.Common/Logging/)

---

**End of Document**
