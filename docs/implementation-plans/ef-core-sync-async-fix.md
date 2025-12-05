# EF Core Sync/Async Anti-Pattern Fix - Implementation Plan

## Executive Summary

The `GameData.Services.MapPopupService` has critical sync/async anti-patterns causing potential frame drops during map transitions. This plan eliminates synchronous database access in hot paths by enforcing cache-first architecture with mandatory preloading.

## Problem Analysis

### Current Issues

1. **Sync Database Queries in Hot Path** (Lines 90-111, 169-190, MapPopupService.cs)
   - `GetTheme()` uses `FirstOrDefault()` on cache miss → blocks game thread
   - `GetSection()` uses `FirstOrDefault()` on cache miss → blocks game thread
   - `GetPopupDisplayInfo()` (line 311) calls sync methods → hot path during map transitions

2. **Runtime Service Usage** (Line 152, Engine/Scenes/Services/MapPopupService.cs)
   - `_mapPopupDataService.GetPopupDisplayInfo(regionName)` called during map transition
   - Cache miss = synchronous database hit = frame drop

3. **Mixed API Surface**
   - Both sync and async methods exist for same operations
   - Developers might accidentally use sync methods
   - No compile-time enforcement of cache-only access

4. **Incomplete Preloading**
   - `PreloadAllAsync()` exists but not called during initialization
   - No validation that all data is cached before gameplay starts

## Solution Architecture

### Design Principles

1. **Cache-First Mandate**: Runtime code NEVER touches database directly
2. **Fail-Fast Validation**: Missing data = error at startup, not during gameplay
3. **Clear API Surface**: Separate "loading" vs "runtime" APIs
4. **Zero Database Access**: Hot paths only use in-memory dictionaries

### Implementation Strategy

```
┌─────────────────────────────────────────────────────────────┐
│ Phase 1: Mandatory Preloading at Startup                   │
│  - LoadGameDataStep calls PreloadAllAsync()                │
│  - Validates 100% cache coverage                           │
│  - Fails initialization if data missing                    │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 2: Cache-Only Runtime API                            │
│  - Remove sync database fallback from GetTheme/GetSection  │
│  - Return null on cache miss (indicates bad data)          │
│  - Log warnings for missing references                     │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 3: Deprecate/Remove Sync Methods                     │
│  - Mark GetTheme()/GetSection() [Obsolete]                 │
│  - Remove after verification period                        │
│  - Keep async methods for tools/editors only               │
└─────────────────────────────────────────────────────────────┘
```

## Detailed Implementation Plan

### Phase 1: Mandatory Preloading ⚡ CRITICAL

**File**: `MonoBallFramework.Game/Initialization/Pipeline/Steps/LoadGameDataStep.cs`

**Changes**:

```csharp
protected override async Task ExecuteStepAsync(
    InitializationContext context,
    LoadingProgress progress,
    CancellationToken cancellationToken)
{
    ILogger<LoadGameDataStep> logger = context.LoggerFactory.CreateLogger<LoadGameDataStep>();
    string dataPath = context.PathResolver.Resolve(
        context.Configuration.Initialization.DataPath
    );

    logger.LogDebug("Loading game data from: {Path}", dataPath);

    try
    {
        // Load all JSON data into database
        await context.DataLoader.LoadAllAsync(dataPath, cancellationToken);
        logger.LogInformation("Game data definitions loaded successfully");

        // ⚡ NEW: Mandatory preload into cache
        var mapPopupService = context.Services.GetRequiredService<MapPopupService>();
        await mapPopupService.PreloadAllAsync(cancellationToken);

        // ⚡ NEW: Validate cache coverage
        var stats = await mapPopupService.GetStatisticsAsync();
        if (stats.TotalThemes != stats.ThemesCached)
        {
            throw new InvalidOperationException(
                $"Theme cache incomplete: {stats.ThemesCached}/{stats.TotalThemes} cached. " +
                "All themes must be preloaded before gameplay starts.");
        }

        if (stats.TotalSections != stats.SectionsCached)
        {
            throw new InvalidOperationException(
                $"Section cache incomplete: {stats.SectionsCached}/{stats.TotalSections} cached. " +
                "All sections must be preloaded before gameplay starts.");
        }

        logger.LogInformation(
            "✓ Popup data fully cached: {ThemeCount} themes, {SectionCount} sections",
            stats.ThemesCached,
            stats.SectionsCached
        );

        await mapPopupService.LogStatisticsAsync();
    }
    catch (FileNotFoundException ex)
    {
        logger.LogWarning(ex,
            "Game data directory not found at {Path} - continuing with default templates",
            dataPath);
    }
    // ... rest of catch blocks
}
```

**Impact**:
- Ensures 100% cache coverage before gameplay
- Fast-fail if data is missing
- Zero risk of database access during gameplay

---

### Phase 2: Cache-Only Runtime Methods

**File**: `MonoBallFramework.Game/GameData/Services/MapPopupService.cs`

#### 2.1 Update GetTheme() - Remove Database Fallback

**BEFORE** (Lines 90-111):
```csharp
public PopupTheme? GetTheme(string themeId)
{
    if (string.IsNullOrWhiteSpace(themeId))
        return null;

    // Check cache first
    if (_themeCache.TryGetValue(themeId, out PopupTheme? cached))
        return cached;

    // ❌ BAD: Query database and cache result
    PopupTheme? theme = _context.PopupThemes.FirstOrDefault(t => t.Id == themeId);
    if (theme != null)
        _themeCache[themeId] = theme;

    return theme;
}
```

**AFTER**:
```csharp
/// <summary>
/// Get popup theme by ID from cache (O(1), runtime-safe).
/// ⚠️ IMPORTANT: This method only reads from cache. All themes must be preloaded
/// during initialization. Returns null if theme not found in cache.
/// </summary>
/// <param name="themeId">The theme ID to lookup</param>
/// <returns>Theme from cache, or null if not cached</returns>
public PopupTheme? GetTheme(string themeId)
{
    if (string.IsNullOrWhiteSpace(themeId))
        return null;

    // ✅ GOOD: Cache-only lookup (zero database access)
    if (_themeCache.TryGetValue(themeId, out PopupTheme? cached))
        return cached;

    // ⚠️ Cache miss indicates missing data - log and return null
    _logger.LogWarning(
        "Theme '{ThemeId}' not found in cache. This indicates incomplete preloading " +
        "or a reference to non-existent theme. Total themes cached: {Count}",
        themeId,
        _themeCache.Count
    );

    return null;
}
```

#### 2.2 Update GetSection() - Remove Database Fallback

**BEFORE** (Lines 169-190):
```csharp
public MapSection? GetSection(string sectionId)
{
    if (string.IsNullOrWhiteSpace(sectionId))
        return null;

    // Check cache first
    if (_sectionCache.TryGetValue(sectionId, out MapSection? cached))
        return cached;

    // ❌ BAD: Query database and cache result
    MapSection? section = _context.MapSections.FirstOrDefault(s => s.Id == sectionId);
    if (section != null)
        _sectionCache[sectionId] = section;

    return section;
}
```

**AFTER**:
```csharp
/// <summary>
/// Get map section by ID from cache (O(1), runtime-safe).
/// ⚠️ IMPORTANT: This method only reads from cache. All sections must be preloaded
/// during initialization. Returns null if section not found in cache.
/// </summary>
/// <param name="sectionId">The section ID to lookup</param>
/// <returns>Section from cache, or null if not cached</returns>
public MapSection? GetSection(string sectionId)
{
    if (string.IsNullOrWhiteSpace(sectionId))
        return null;

    // ✅ GOOD: Cache-only lookup (zero database access)
    if (_sectionCache.TryGetValue(sectionId, out MapSection? cached))
        return cached;

    // ⚠️ Cache miss indicates missing data - log and return null
    _logger.LogWarning(
        "Section '{SectionId}' not found in cache. This indicates incomplete preloading " +
        "or a reference to non-existent section. Total sections cached: {Count}",
        sectionId,
        _sectionCache.Count
    );

    return null;
}
```

#### 2.3 GetPopupDisplayInfo() - Already Cache-Only ✅

**File**: Line 311-369 - Already uses cache-only methods!

```csharp
public PopupDisplayInfo? GetPopupDisplayInfo(string sectionId)
{
    // ✅ Already cache-only - calls GetSection() and GetTheme()
    MapSection? section = GetSection(sectionId);
    if (section == null)
    {
        _logger.LogWarning(
            "MapSection '{SectionId}' not found in database. Total sections cached: {Count}",
            sectionId,
            _sectionCache.Count
        );
        return null;
    }

    PopupTheme? theme = GetTheme(section.ThemeId);
    if (theme == null)
    {
        _logger.LogWarning(
            "PopupTheme '{ThemeId}' not found for section '{SectionId}'. Total themes cached: {Count}",
            section.ThemeId,
            sectionId,
            _themeCache.Count
        );
        return null;
    }

    // ... rest of method
}
```

**Impact**:
- Zero database access during map transitions
- Clear warning logs if data is missing
- No performance degradation

---

### Phase 3: Async Method Guidance

#### 3.1 Keep Async Methods for Tools/Editors

The async methods (`GetThemeAsync`, `GetSectionAsync`, etc.) should be kept but documented as "initialization/editor only":

```csharp
/// <summary>
/// Get popup theme by ID asynchronously.
/// ⚠️ FOR INITIALIZATION AND TOOLS ONLY - Do NOT call during gameplay!
/// Use GetTheme() for runtime access after preloading.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public async Task<PopupTheme?> GetThemeAsync(string themeId, CancellationToken ct = default)
{
    // ... existing implementation with database access
}
```

#### 3.2 Document PreloadAllAsync() Requirement

```csharp
/// <summary>
/// Preload all themes and sections into cache.
/// ⚡ REQUIRED: Must be called during initialization before gameplay starts.
/// This ensures zero database access during map transitions.
/// </summary>
/// <exception cref="InvalidOperationException">Thrown if database is unavailable</exception>
public async Task PreloadAllAsync(CancellationToken ct = default)
{
    _logger.LogInformation("Preloading all popup themes and sections into cache...");

    await PreloadThemesAsync(ct);
    await PreloadSectionsAsync(ct);

    var stats = await GetStatisticsAsync();

    _logger.LogInformation(
        "✓ Preloaded {ThemeCount} themes and {SectionCount} sections. " +
        "Runtime lookups are now cache-only (O(1)).",
        stats.ThemesCached,
        stats.SectionsCached
    );
}
```

---

### Phase 4: Fallback Handling Strategy

#### 4.1 Design Decision: Error vs Default

**Question**: What should happen if cache miss occurs during gameplay?

**Options**:

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| **A) Return null, log warning** | Simple, explicit failure | UI shows nothing | ✅ **RECOMMENDED** |
| **B) Return default theme** | Graceful degradation | Hides data errors | ❌ Risky |
| **C) Throw exception** | Immediate failure detection | Crashes game | ❌ Too harsh |

**Selected Approach**: **Option A** - Return null with warning

**Rationale**:
- Cache misses during gameplay indicate bugs (bad data references)
- Warnings in logs help debugging without crashing
- Engine can handle null gracefully (fallback to default in UI layer)

#### 4.2 Implementation

Already implemented in Phase 2 - GetTheme() and GetSection() return null with warnings.

**UI Layer Fallback** (Engine/Scenes/Services/MapPopupService.cs, Line 198):
```csharp
// Fallback to default if no theme found
if (backgroundDef == null || outlineDef == null)
{
    backgroundDef = _popupRegistry.GetDefaultBackground();
    outlineDef = _popupRegistry.GetDefaultOutline();
    usedThemeId = "wood"; // Default theme

    // ✅ This already handles cache miss gracefully
}
```

---

## Performance Validation

### Pre-Implementation Benchmarks

**Metrics to Capture**:
1. Map transition time (ms) - should be < 16ms (60 FPS)
2. Database query count during gameplay - should be 0
3. Cache hit rate - should be 100%

**Measurement Tools**:

```csharp
// Add diagnostic telemetry
public class MapPopupService
{
    private int _cacheHits;
    private int _cacheMisses;
    private int _databaseQueries;

    public PopupTheme? GetTheme(string themeId)
    {
        if (_themeCache.TryGetValue(themeId, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            return cached;
        }

        Interlocked.Increment(ref _cacheMisses);
        _logger.LogWarning(/* ... */);
        return null;
    }

    public DiagnosticStats GetDiagnostics() => new()
    {
        CacheHits = _cacheHits,
        CacheMisses = _cacheMisses,
        DatabaseQueries = _databaseQueries
    };
}
```

### Post-Implementation Validation

**Success Criteria**:
- ✅ Zero database queries during map transitions
- ✅ 100% cache hit rate after preloading
- ✅ Map transition < 1ms (cache lookup)
- ✅ No frame drops during gameplay
- ✅ All tests passing

**Test Scenarios**:
1. **Cold start** - Verify preloading works
2. **Map transition** - Verify no database access
3. **Invalid section ID** - Verify graceful fallback
4. **Missing theme** - Verify warning logs
5. **Performance** - Verify < 16ms frame time

---

## Code Changes Summary

### Files Modified

1. **LoadGameDataStep.cs** (8 lines added)
   - Add mandatory preloading call
   - Add cache validation
   - Fail fast if incomplete

2. **MapPopupService.cs** (20 lines modified)
   - Remove database fallback from GetTheme()
   - Remove database fallback from GetSection()
   - Update XML docs
   - Add warnings for cache misses

3. **Documentation** (optional)
   - Update architecture docs to reflect cache-first design

### Files NOT Modified

- ✅ Engine/Scenes/Services/MapPopupService.cs - Already cache-only
- ✅ GetPopupDisplayInfo() - Already uses cache-only methods
- ✅ PreloadAllAsync() - Already exists

---

## Migration Guide

### For Developers

**OLD (Risky)**:
```csharp
// ❌ Might hit database during gameplay
var theme = _mapPopupService.GetTheme(themeId);
```

**NEW (Safe)**:
```csharp
// ✅ Always cache-only after preloading
var theme = _mapPopupService.GetTheme(themeId);
if (theme == null)
{
    // Use default fallback
    theme = GetDefaultTheme();
}
```

### For Modders

**Requirements**:
- All custom themes must be in `Assets/Data/Maps/Popups/Themes/`
- All custom sections must be in `Assets/Data/Maps/Sections/`
- JSON files loaded during initialization
- Invalid references will log warnings

---

## Rollback Plan

If issues arise:

1. **Revert LoadGameDataStep.cs changes**
   - Remove mandatory preloading
   - Remove cache validation

2. **Revert MapPopupService.cs changes**
   - Restore database fallback in GetTheme()
   - Restore database fallback in GetSection()

3. **Test with old behavior**
   - Verify game still loads
   - Verify map transitions work

**Risk**: Low - Changes are isolated and reversible

---

## Timeline Estimate

| Phase | Task | Time | Priority |
|-------|------|------|----------|
| 1 | Update LoadGameDataStep.cs | 30 min | P0 🔴 |
| 2 | Update GetTheme() and GetSection() | 30 min | P0 🔴 |
| 3 | Add diagnostic telemetry | 30 min | P1 🟡 |
| 4 | Write unit tests | 1 hour | P1 🟡 |
| 5 | Performance validation | 1 hour | P1 🟡 |
| 6 | Update documentation | 30 min | P2 🟢 |

**Total**: ~4 hours

---

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class MapPopupServiceTests
{
    [TestMethod]
    public async Task GetTheme_AfterPreload_ReturnsFromCache()
    {
        // Arrange
        var service = CreateService();
        await service.PreloadAllAsync();

        // Act
        var theme = service.GetTheme("wood");

        // Assert
        Assert.IsNotNull(theme);
        Assert.AreEqual("wood", theme.Id);

        // Verify no database access (check via telemetry)
        var stats = service.GetDiagnostics();
        Assert.AreEqual(0, stats.DatabaseQueries);
    }

    [TestMethod]
    public void GetTheme_WithoutPreload_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        // Don't call PreloadAllAsync()

        // Act
        var theme = service.GetTheme("wood");

        // Assert
        Assert.IsNull(theme, "Should return null if not preloaded");

        // Verify warning was logged
        // (check via mock logger)
    }

    [TestMethod]
    public void GetTheme_InvalidId_ReturnsNullWithWarning()
    {
        // Arrange
        var service = CreateService();
        await service.PreloadAllAsync();

        // Act
        var theme = service.GetTheme("nonexistent");

        // Assert
        Assert.IsNull(theme);

        // Verify cache miss logged
        var stats = service.GetDiagnostics();
        Assert.AreEqual(1, stats.CacheMisses);
    }
}
```

### Integration Tests

```csharp
[TestMethod]
public async Task MapTransition_WithPreloadedData_NoFrameDrop()
{
    // Arrange
    var game = CreateTestGame();
    await game.InitializeAsync();

    // Act
    var stopwatch = Stopwatch.StartNew();
    await game.TransitionToMap("route101");
    stopwatch.Stop();

    // Assert
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 16,
        $"Map transition took {stopwatch.ElapsedMilliseconds}ms (should be < 16ms for 60 FPS)");

    // Verify no database queries during transition
    var diagnostics = game.GetDiagnostics();
    Assert.AreEqual(0, diagnostics.RuntimeDatabaseQueries);
}
```

---

## Monitoring & Observability

### Logging Strategy

**Startup Logs**:
```
[INFO] MapPopupService initialized
[INFO] Preloading all popup themes and sections into cache...
[INFO] Preloaded 153 map sections into cache
[INFO] Preloaded 12 popup themes into cache
[INFO] ✓ Preloaded 12 themes and 153 sections. Runtime lookups are now cache-only (O(1)).
[INFO] MapPopup Data: 12 themes, 153 sections loaded (Cached: 12 themes, 153 sections)
```

**Runtime Warnings** (should be rare):
```
[WARN] Section 'invalid_section' not found in cache. This indicates incomplete preloading or a reference to non-existent section. Total sections cached: 153
[WARN] Theme 'missing_theme' not found in cache. This indicates incomplete preloading or a reference to non-existent theme. Total themes cached: 12
```

**Error Logs** (should never happen in production):
```
[ERROR] Theme cache incomplete: 11/12 cached. All themes must be preloaded before gameplay starts.
[ERROR] Section cache incomplete: 150/153 cached. All sections must be preloaded before gameplay starts.
```

---

## Success Metrics

### Before Fix
- ⚠️ Database queries during map transitions: **Variable (0-2 per transition)**
- ⚠️ Map transition time: **Unpredictable (1-50ms)**
- ⚠️ Frame drops: **Possible on cache miss**

### After Fix
- ✅ Database queries during map transitions: **0 (guaranteed)**
- ✅ Map transition time: **< 1ms (cache lookup)**
- ✅ Frame drops: **Eliminated**
- ✅ Cache hit rate: **100% after preload**

---

## Appendix: Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    GAME INITIALIZATION                           │
│                                                                  │
│  LoadGameDataStep                                               │
│    │                                                            │
│    ├─► GameDataLoader.LoadAllAsync()                           │
│    │     └─► Load JSON → EF Core InMemory DB                   │
│    │                                                            │
│    └─► MapPopupService.PreloadAllAsync() ⚡ REQUIRED           │
│          ├─► PreloadThemesAsync() → _themeCache               │
│          ├─► PreloadSectionsAsync() → _sectionCache           │
│          └─► Validate 100% cache coverage                     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                           │
                           │ All data cached
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                     RUNTIME (GAMEPLAY)                           │
│                                                                  │
│  Map Transition Event                                           │
│    │                                                            │
│    └─► Engine.MapPopupService.ShowPopupForMap()               │
│          │                                                      │
│          └─► GameData.MapPopupService.GetPopupDisplayInfo()    │
│                │                                                │
│                ├─► GetSection(id) ✅ Cache-only O(1)           │
│                │     └─► _sectionCache[id]                     │
│                │                                                │
│                └─► GetTheme(id) ✅ Cache-only O(1)             │
│                      └─► _themeCache[id]                       │
│                                                                  │
│  🚫 ZERO DATABASE ACCESS                                        │
│  ⚡ < 1ms lookup time                                          │
│  ✅ 60 FPS guaranteed                                          │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Conclusion

This implementation plan eliminates EF Core sync/async anti-patterns by:

1. **Mandatory preloading** at startup (fail-fast validation)
2. **Cache-only runtime API** (zero database access)
3. **Clear separation** of loading vs runtime code
4. **Graceful fallbacks** for missing data
5. **Performance validation** via telemetry

**Risk**: Low - Changes are isolated and reversible
**Impact**: High - Eliminates frame drops during map transitions
**Effort**: ~4 hours implementation + testing

---

**Next Steps**:
1. Review this plan with team
2. Create feature branch
3. Implement Phase 1 + 2
4. Write tests
5. Validate performance
6. Merge to main
