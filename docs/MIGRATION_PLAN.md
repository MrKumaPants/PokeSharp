# Logging Migration Plan

## Overview

This document provides a step-by-step plan to migrate PokeSharp's logging to the standardized format defined in `LOGGING_STANDARDS.md`.

---

## Migration Phases

### Phase 1: Remove Color Markup (Priority: HIGH)
**Goal:** Eliminate all Spectre.Console color markup from log messages.

**Impact:** High - Affects log parsing and structured logging.

**Files Affected:**
- `LogTemplates.cs` - Primary source of color markup
- `MapLoader.cs` - Uses color markup in some messages
- All files that use `LogTemplates` extension methods

**Steps:**

1. **Update LogTemplates.cs**
   - Remove all `WithAccent()` calls and formatting
   - Remove `AccentStyles` dictionary
   - Remove `FormatTemplate()` wrapper (if it only adds colors)
   - Convert all methods to plain structured logging

2. **Find and Replace Patterns:**
   ```csharp
   // Before
   logger.LogInformation("[green]Map loaded[/]: [cyan]{MapId}[/]", mapId);

   // After
   logger.LogInformation("Map loaded: {MapId}", mapId);
   ```

3. **Search for markup patterns:**
   ```bash
   grep -r "\[green\]" --include="*.cs"
   grep -r "\[cyan\]" --include="*.cs"
   grep -r "\[yellow\]" --include="*.cs"
   grep -r "\[red\]" --include="*.cs"
   grep -r "\[dim\]" --include="*.cs"
   grep -r "\[grey\]" --include="*.cs"
   grep -r "\[/\]" --include="*.cs"
   ```

4. **Update calls systematically:**
   - Remove all `[color]` and `[/]` tags
   - Keep the structured logging parameters
   - Preserve the semantic meaning

**Example Transformations:**

```csharp
// BEFORE
var body = $"[cyan]{EscapeMarkup(systemName)}[/] [dim]initialized[/]{detailsFormatted}";
logger.LogInformation(LogFormatting.FormatTemplate(WithAccent(LogAccent.Initialization, body)));

// AFTER
logger.LogInformation("System initialized: {SystemName}{Details}", systemName, detailsFormatted);
```

```csharp
// BEFORE
logger.LogDebug("[dim]MapId:[/] [grey]{MapId}[/] [dim]|[/] [dim]Animated:[/] [yellow]{AnimatedCount}[/]",
    mapId, animatedCount);

// AFTER
logger.LogDebug("Map details: MapId={MapId}, Animated={AnimatedCount}", mapId, animatedCount);
```

---

### Phase 2: Fix Log Levels (Priority: HIGH)
**Goal:** Ensure log levels are used appropriately.

**Impact:** Medium - Affects log filtering and noise.

**Common Changes:**
- `LogInformation` → `LogDebug` for verbose/detailed logs
- Keep `LogInformation` for significant state changes only

**Decision Matrix:**

| Current Level | New Level | Criteria |
|--------------|-----------|----------|
| LogInformation | **LogDebug** | Individual entity/tile creation |
| LogInformation | **LogDebug** | Frame-by-frame metrics |
| LogInformation | **LogDebug** | Detailed asset information |
| LogInformation | **Keep LogInformation** | System initialization complete |
| LogInformation | **Keep LogInformation** | Map loading summary |
| LogInformation | **Keep LogInformation** | Configuration changes |

**Files to Review:**
1. `MapLoader.cs` - Many `LogInformation` for detailed tile/sprite info → `LogDebug`
2. `GameLoggingExtensions.cs` - Check all extension methods
3. `LogTemplates.cs` - Review all template methods
4. System files - Frame metrics should be `LogDebug`

**Example Changes:**

```csharp
// BEFORE (Too verbose for Information)
logger.LogInformation("Collected {Count} unique sprite IDs for map {MapId}", count, mapId);
foreach (var spriteId in spriteIds)
{
    logger.LogInformation("  - {SpriteId}", spriteId); // ❌ Way too detailed
}

// AFTER
logger.LogDebug("Collected {Count} unique sprite IDs for map {MapId}", count, mapId);
if (logger.IsEnabled(LogLevel.Debug))
{
    foreach (var spriteId in spriteIds.OrderBy(x => x))
    {
        logger.LogDebug("  - {SpriteId}", spriteId); // ✅ Only if debug enabled
    }
}
```

---

### Phase 3: Combine Multi-Line Logs (Priority: MEDIUM)
**Goal:** Consolidate related information into single log statements.

**Impact:** Medium - Reduces log volume, improves readability.

**Pattern to Find:**
```csharp
// Bad pattern (multiple sequential logs about same operation)
logger.LogInformation("Map loaded");
logger.LogInformation("MapId: {MapId}", mapId);
logger.LogInformation("Size: {Width}x{Height}", width, height);
```

**How to Fix:**
```csharp
// Good (single combined log)
logger.LogInformation("Map loaded: {MapId} ({Width}x{Height})", mapId, width, height);
```

**Search Strategy:**
1. Look for sequential `LogInformation`/`LogDebug` calls (within 3 lines)
2. Check if they're logging related information
3. Combine into single statement with multiple parameters

**Example Transformations:**

```csharp
// BEFORE (MapLoader.cs lines 95-99)
logger.LogInformation("Loading map from definition: {MapId} ({DisplayName})", mapDef.MapId, mapDef.DisplayName);
// ... later ...
logger.LogInformation("Collected {Count} unique sprite IDs for map {MapId}", _requiredSpriteIds.Count, mapDef.MapId);

// AFTER (if they're in same scope and closely related, combine context)
using (logger.BeginScope("Map:{MapId}", mapDef.MapId))
{
    logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapDef.MapId, mapDef.DisplayName);
    logger.LogDebug("Collected {Count} sprite IDs", _requiredSpriteIds.Count);
}
```

---

### Phase 4: Add Missing Logging (Priority: LOW)
**Goal:** Ensure key operations have appropriate logging.

**Impact:** Low - Improves observability.

**Areas to Add Logging:**

1. **System Updates** (if missing)
   ```csharp
   public void Update(World world, float deltaTime)
   {
       if (_logger.IsEnabled(LogLevel.Debug))
       {
           var sw = Stopwatch.StartNew();
           // ... system logic ...
           sw.Stop();
           _logger.LogDebug("System executed: {SystemName} in {TimeMs}ms",
               GetType().Name, sw.Elapsed.TotalMilliseconds);
       }
   }
   ```

2. **Error Boundaries** (catch blocks without logging)
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError(ex, "Failed to load {ResourceType} from {Path}", resourceType, path);
       throw; // or handle gracefully
   }
   ```

3. **Configuration Changes**
   ```csharp
   public void SetZoom(float zoom)
   {
       _zoom = zoom;
       _logger.LogInformation("Zoom changed: {Zoom}", zoom);
   }
   ```

**Files to Review:**
- System classes in `PokeSharp.Game.Systems/`
- Service classes without logging
- Exception handlers without logging

---

### Phase 5: Standardize Message Formats (Priority: MEDIUM)
**Goal:** Ensure all messages follow `{Verb} {Noun}: {Details}` pattern.

**Impact:** Medium - Improves log consistency and searchability.

**Common Verbs:**
- Loaded, Created, Initialized, Updated, Executed
- Failed, Skipped, Missing, Invalid
- Started, Completed, Finished

**Template Patterns:**

```csharp
// System lifecycle
logger.LogInformation("System initialized: {SystemName}", systemName);
logger.LogInformation("System disposed: {SystemName}", systemName);

// Resource operations
logger.LogInformation("Asset loaded: {AssetPath} in {TimeMs}ms", path, timeMs);
logger.LogError(ex, "Failed to load asset: {AssetPath}", path);

// Entity operations
logger.LogDebug("Entity created: {EntityType} #{EntityId}", entityType, entityId);
logger.LogDebug("Component added: {ComponentType} to entity #{EntityId}", componentType, entityId);

// State changes
logger.LogInformation("Map loaded: {MapId} ({DisplayName})", mapId, displayName);
logger.LogInformation("Game mode changed: {OldMode} → {NewMode}", oldMode, newMode);
```

**Review Strategy:**
1. Find logs that don't start with a verb
2. Add appropriate verb based on operation type
3. Ensure subject (noun) is clear
4. Add relevant details as parameters

---

## Automated Search Patterns

### Find Color Markup
```bash
# Find all color tags
rg "\[(?:green|red|yellow|cyan|grey|dim|bold|magenta|aqua|orange)\]" --type cs

# Find closing tags
rg "\[/\]" --type cs
```

### Find Inappropriate Log Levels
```bash
# Find LogInformation in loops (likely should be Debug)
rg "for.*\{[\s\S]{0,200}LogInformation" --type cs

# Find LogInformation with very detailed info (tile IDs, coordinates, etc.)
rg "LogInformation.*\{X.*\{Y" --type cs
```

### Find Multi-Line Log Candidates
```bash
# Find sequential LogInformation calls
rg "LogInformation.*\n\s*.*LogInformation" --type cs
```

### Find Missing Error Logging
```bash
# Find catch blocks without logging
rg "catch.*\{[\s\S]{0,100}\}" --type cs | grep -v "Log"
```

---

## Regex Patterns for Find/Replace

### Remove Simple Color Markup
```regex
# Find
\[(green|red|yellow|cyan|grey|dim|bold|magenta|aqua|orange[0-9]*|blue|skyblue[0-9]*|deepskyblue[0-9]*|steelblue[0-9]*|lightsteelblue[0-9]*|plum[0-9]*|gold[0-9]*|springgreen[0-9]*)\](.*?)\[/\]

# Replace
$2
```

### Fix Parameter Casing (camelCase → PascalCase)
```regex
# Find (in LogInformation/LogDebug/etc.)
\{([a-z][a-zA-Z0-9]*)\}

# Replace (manual review needed)
{$1} → {PascalCaseVersion}
```

---

## File-by-File Migration Order

### Priority 1 (Core Infrastructure)
1. `LogTemplates.cs` - Remove all color markup, fix levels
2. `GameLoggingExtensions.cs` - Review and fix extension methods
3. `LoggerExtensions.cs` - Ensure base extensions are correct

### Priority 2 (High-Traffic Files)
4. `MapLoader.cs` - Heavy logging, needs level fixes and consolidation
5. `PokeSharpGame.cs` - Initialization logging
6. `GameInitializer.cs` - Startup logging
7. System classes (`SpatialHashSystem`, `RenderSystem`, etc.)

### Priority 3 (Supporting Files)
8. Service classes (`SpriteLoader`, `AssetManager`, etc.)
9. API services (`MapApiService`, `NpcApiService`, etc.)
10. Component deserializers and factories

---

## Testing Strategy

### Before Migration
1. Run application with `LogLevel.Debug`
2. Capture sample log output (save to file)
3. Note any issues with log formatting

### After Each Phase
1. Build and run application
2. Compare log output with baseline
3. Verify:
   - No missing information
   - Proper log levels (check with filters)
   - No color markup artifacts
   - Structured data is preserved
   - Performance is not degraded

### Acceptance Criteria
- [ ] All color markup removed from logs
- [ ] No `LogInformation` in tight loops
- [ ] Multi-line logs for same event combined
- [ ] All parameters use PascalCase
- [ ] Error logs include exception details
- [ ] Log levels are appropriate
- [ ] Performance metrics not degraded
- [ ] Logs are parseable by Serilog sinks

---

## Rollback Plan

If issues are found:
1. Use Git to revert specific files: `git checkout HEAD -- <file>`
2. Keep migration in feature branch until validated
3. Test with different Serilog sinks (Console, File, Seq) before merging

---

## Estimated Effort

| Phase | Files | Estimated Time |
|-------|-------|----------------|
| Phase 1: Remove Color Markup | ~15 files | 4-6 hours |
| Phase 2: Fix Log Levels | ~30 files | 6-8 hours |
| Phase 3: Combine Multi-Line | ~20 files | 3-4 hours |
| Phase 4: Add Missing Logging | ~10 files | 2-3 hours |
| Phase 5: Standardize Formats | ~40 files | 4-5 hours |
| **Total** | **~68 files** | **19-26 hours** |

---

## Tooling Support

### Roslyn Analyzer (Future Enhancement)
Create custom analyzer to detect:
- Color markup in log messages
- `LogInformation` in loops
- Missing structured logging parameters
- camelCase parameters in log messages

### Pre-Commit Hook (Future Enhancement)
```bash
#!/bin/bash
# .git/hooks/pre-commit

# Check for color markup in staged files
if git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' | xargs grep -E "\[(green|red|yellow|cyan)\]"; then
    echo "Error: Color markup found in log messages"
    exit 1
fi
```

---

## Post-Migration

After migration is complete:

1. **Update Documentation**
   - Add logging guidelines to CONTRIBUTING.md
   - Link to LOGGING_STANDARDS.md

2. **Team Training**
   - Review session on new standards
   - Code review checklist for logging

3. **Monitoring**
   - Set up Seq or similar for log aggregation
   - Create dashboards for common queries
   - Set up alerts for error patterns

4. **Continuous Improvement**
   - Regular review of log quality
   - Feedback loop from operations team
   - Update standards based on learnings
