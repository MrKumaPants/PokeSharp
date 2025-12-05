# MapPopupService Naming Collision - Implementation Plan

**Date:** December 5, 2025
**Type:** Architecture Refactoring
**Status:** PROPOSED
**Severity:** Medium - Causes confusion and requires fully qualified names

---

## Executive Summary

Two classes named `MapPopupService` exist in different namespaces, causing naming conflicts and requiring fully qualified names throughout the codebase. This document proposes a clear naming strategy and step-by-step migration plan to resolve the collision.

---

## Current State Analysis

### 1. GameData.Services.MapPopupService
**Location:** `/MonoBallFramework.Game/GameData/Services/MapPopupService.cs`
**Namespace:** `MonoBallFramework.Game.GameData.Services`
**Responsibility:** Data access layer for popup themes and map sections

**Key Characteristics:**
- EF Core database query service
- Caching layer with `ConcurrentDictionary`
- O(1) lookups for hot paths (map transitions)
- Handles `PopupTheme` and `MapSection` entities
- Contains methods like:
  - `GetTheme(themeId)`
  - `GetSection(sectionId)`
  - `GetPopupDisplayInfo(sectionId)`
  - `PreloadAllAsync()`
  - `GetStatisticsAsync()`

**Design Pattern:** Repository + Cache Pattern

### 2. Engine.Scenes.Services.MapPopupService
**Location:** `/MonoBallFramework.Game/Engine/Scenes/Services/MapPopupService.cs`
**Namespace:** `MonoBallFramework.Game.Engine.Scenes.Services`
**Responsibility:** Orchestrates map popup display during transitions

**Key Characteristics:**
- Event-driven orchestration service
- Subscribes to `MapTransitionEvent` and `MapRenderReadyEvent`
- Creates and pushes `MapPopupScene` to scene stack
- Coordinates between data service, popup registry, and scene manager
- Dependencies on:
  - `World` (ECS)
  - `SceneManager`
  - `GraphicsDevice`
  - `AssetManager`
  - `PopupRegistry`
  - `GameData.Services.MapPopupService` (the other service!)
  - `IEventBus`

**Design Pattern:** Event Handler + Orchestrator Pattern

### 3. Current Collision Points

**Files requiring fully qualified names:**
1. `Engine/Scenes/Services/MapPopupService.cs` (line 29, 65)
   - Uses `GameData.Services.MapPopupService` as dependency
2. `Initialization/InitializationContext.cs` (line 138)
   - Stores `Engine.Scenes.Services.MapPopupService` as property
3. `MonoBallFrameworkGame.cs` (line 67)
   - Stores `Engine.Scenes.Services.MapPopupService` as field

**Documentation references:**
- `docs/features/map-popup-themes-sections.md`
- `docs/bugfixes/prevent-double-popups.md`
- `docs/bugfixes/popup-map-name-formatting.md`
- `MonoBallFramework.Game/Assets/Graphics/Maps/Popups/README.md`

---

## Proposed Solution

### Naming Strategy

Based on responsibilities and design patterns, the following names clearly distinguish the services:

#### Option A: Responsibility-Based Naming (RECOMMENDED)

| Current Name | New Name | Rationale |
|--------------|----------|-----------|
| `GameData.Services.MapPopupService` | `MapPopupDataService` | Emphasizes data access responsibility |
| `Engine.Scenes.Services.MapPopupService` | `MapPopupOrchestrator` | Emphasizes orchestration responsibility |

**Why this is best:**
- ✅ Clear separation of concerns (Data vs Orchestration)
- ✅ Follows common naming patterns (DataService, Orchestrator)
- ✅ Self-documenting - name indicates purpose
- ✅ No ambiguity in any context

#### Option B: Layer-Based Naming (ALTERNATIVE)

| Current Name | New Name | Rationale |
|--------------|----------|-----------|
| `GameData.Services.MapPopupService` | `MapPopupRepository` | Repository pattern for data access |
| `Engine.Scenes.Services.MapPopupService` | `MapPopupDisplayService` | Display coordination service |

**Considerations:**
- ✅ "Repository" is accurate for the EF Core service
- ⚠️ "Repository" might be too strict (it does caching too)
- ⚠️ "DisplayService" is less specific than "Orchestrator"

#### Option C: Minimal Change (NOT RECOMMENDED)

| Current Name | New Name | Rationale |
|--------------|----------|-----------|
| `GameData.Services.MapPopupService` | `MapPopupQueryService` | Just add "Query" |
| `Engine.Scenes.Services.MapPopupService` | `MapPopupSceneService` | Just add "Scene" |

**Why not recommended:**
- ❌ Less descriptive
- ❌ "QueryService" doesn't capture caching responsibility
- ❌ "SceneService" doesn't capture event handling

---

## Recommended Implementation: Option A

### Step 1: Create Interfaces (Foundation)

**Purpose:** Establish contracts before renaming to ensure compatibility

#### 1.1 Create IMapPopupDataService
```csharp
// File: MonoBallFramework.Game/GameData/Services/IMapPopupDataService.cs
namespace MonoBallFramework.Game.GameData.Services;

/// <summary>
/// Data access service for popup themes and map sections.
/// Provides cached O(1) lookups for hot paths (map transitions).
/// </summary>
public interface IMapPopupDataService
{
    // Theme operations
    PopupTheme? GetTheme(string themeId);
    Task<PopupTheme?> GetThemeAsync(string themeId, CancellationToken ct = default);
    Task<List<PopupTheme>> GetAllThemesAsync(CancellationToken ct = default);
    Task PreloadThemesAsync(CancellationToken ct = default);

    // Section operations
    MapSection? GetSection(string sectionId);
    Task<MapSection?> GetSectionAsync(string sectionId, CancellationToken ct = default);
    Task<List<MapSection>> GetAllSectionsAsync(CancellationToken ct = default);
    Task<List<MapSection>> GetSectionsByThemeAsync(string themeId, CancellationToken ct = default);
    Task PreloadSectionsAsync(CancellationToken ct = default);

    // Combined operations
    PopupTheme? GetThemeForSection(string sectionId);
    Task<PopupTheme?> GetThemeForSectionAsync(string sectionId, CancellationToken ct = default);
    PopupDisplayInfo? GetPopupDisplayInfo(string sectionId);
    Task<PopupDisplayInfo?> GetPopupDisplayInfoAsync(string sectionId, CancellationToken ct = default);
    Task PreloadAllAsync(CancellationToken ct = default);

    // Statistics and diagnostics
    Task<PopupDataStatistics> GetStatisticsAsync();
    Task LogStatisticsAsync();

    // Cache management
    void ClearCache();
}
```

#### 1.2 Create IMapPopupOrchestrator
```csharp
// File: MonoBallFramework.Game/Engine/Scenes/Services/IMapPopupOrchestrator.cs
namespace MonoBallFramework.Game.Engine.Scenes.Services;

/// <summary>
/// Orchestrates map popup display during map transitions.
/// Subscribes to map events and manages popup scene lifecycle.
/// </summary>
public interface IMapPopupOrchestrator : IDisposable
{
    // This interface is primarily marker-based since the service
    // works through event subscriptions. No public methods needed
    // beyond IDisposable for cleanup.
}
```

### Step 2: Rename Classes

#### 2.1 Rename GameData.Services.MapPopupService

**File Changes:**
1. Rename file: `MapPopupService.cs` → `MapPopupDataService.cs`
2. Update class declaration:
   ```csharp
   public class MapPopupDataService : IMapPopupDataService
   ```
3. Update constructor and logger references:
   ```csharp
   private readonly ILogger<MapPopupDataService> _logger;

   public MapPopupDataService(GameDataContext context, ILogger<MapPopupDataService> logger)
   ```

#### 2.2 Rename Engine.Scenes.Services.MapPopupService

**File Changes:**
1. Rename file: `MapPopupService.cs` → `MapPopupOrchestrator.cs`
2. Update class declaration:
   ```csharp
   public class MapPopupOrchestrator : IMapPopupOrchestrator
   ```
3. Update constructor, logger, and dependency references:
   ```csharp
   private readonly IMapPopupDataService _mapPopupDataService;
   private readonly ILogger<MapPopupOrchestrator> _logger;

   public MapPopupOrchestrator(
       World world,
       SceneManager sceneManager,
       GraphicsDevice graphicsDevice,
       IServiceProvider services,
       AssetManager assetManager,
       PopupRegistry popupRegistry,
       IEventBus eventBus,
       ILogger<MapPopupOrchestrator> logger
   )
   {
       // ...
       _mapPopupDataService = services.GetRequiredService<IMapPopupDataService>();
       _logger = logger;
   }
   ```

### Step 3: Update Service Registration

**File:** `Infrastructure/ServiceRegistration/CoreServicesExtensions.cs`

**Changes:**
```csharp
// Before (line 137):
services.AddSingleton<MapPopupService>();

// After:
services.AddSingleton<IMapPopupDataService, MapPopupDataService>();
```

**Note:** The orchestrator is not registered in DI - it's created directly in InitializeMapPopupStep.

### Step 4: Update Initialization Context

**File:** `Initialization/InitializationContext.cs`

**Changes:**
```csharp
// Before (line 137-138):
/// <summary>
///     Gets or sets the map popup service (created during map popup initialization).
/// </summary>
public Engine.Scenes.Services.MapPopupService? MapPopupService { get; set; }

// After:
/// <summary>
///     Gets or sets the map popup orchestrator (created during map popup initialization).
/// </summary>
public IMapPopupOrchestrator? MapPopupOrchestrator { get; set; }
```

### Step 5: Update Initialization Step

**File:** `Initialization/Pipeline/Steps/InitializeMapPopupStep.cs`

**Changes:**
```csharp
// Update logger type (line 65-66)
ILogger<MapPopupOrchestrator> mapPopupLogger =
    context.LoggerFactory.CreateLogger<MapPopupOrchestrator>();

// Update instantiation (line 68-77)
var mapPopupOrchestrator = new MapPopupOrchestrator(
    context.World,
    context.SceneManager,
    context.GraphicsDevice,
    context.Services,
    context.GameInitializer.RenderSystem.AssetManager,
    popupRegistry,
    eventBus,
    mapPopupLogger
);

// Update context assignment (line 80)
context.MapPopupOrchestrator = mapPopupOrchestrator;
```

### Step 6: Update Game Class

**File:** `MonoBallFrameworkGame.cs`

**Changes:**
```csharp
// Field declaration (line 67):
private IMapPopupOrchestrator? _mapPopupOrchestrator;

// Assignment (line 375):
_mapPopupOrchestrator = context.MapPopupOrchestrator;

// Disposal (line 266):
_mapPopupOrchestrator?.Dispose();
```

### Step 7: Update LoadGameDataStep

**File:** `Initialization/Pipeline/Steps/LoadGameDataStep.cs`

**Changes:**
```csharp
// Update service retrieval (line 45-48):
var mapPopupDataService = context.Services.GetService<IMapPopupDataService>();
if (mapPopupDataService != null)
{
    await mapPopupDataService.LogStatisticsAsync();
}
```

### Step 8: Update Documentation

**Files to update:**

1. **docs/features/map-popup-themes-sections.md**
   - Update service name references
   - Update code examples
   - Add migration note

2. **docs/bugfixes/prevent-double-popups.md**
   - Update `MapPopupService` → `MapPopupOrchestrator` references

3. **docs/bugfixes/popup-map-name-formatting.md**
   - Update service name references

4. **MonoBallFramework.Game/Assets/Graphics/Maps/Popups/README.md**
   - Update service name references

5. **Create migration guide:**
   ```markdown
   # File: docs/architecture/migrations/map-popup-service-rename.md

   ## MapPopupService Refactoring

   **Date:** December 5, 2025

   ### Summary
   Resolved naming collision between two MapPopupService classes by renaming:
   - `GameData.Services.MapPopupService` → `MapPopupDataService`
   - `Engine.Scenes.Services.MapPopupService` → `MapPopupOrchestrator`

   ### Migration Guide for Developers

   If you have code that references these services:

   #### Old Code:
   ```csharp
   using MonoBallFramework.Game.GameData.Services;

   var service = serviceProvider.GetRequiredService<MapPopupService>();
   var info = service.GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN");
   ```

   #### New Code:
   ```csharp
   using MonoBallFramework.Game.GameData.Services;

   var service = serviceProvider.GetRequiredService<IMapPopupDataService>();
   var info = service.GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN");
   ```

   ### Breaking Changes
   - Class names changed
   - Interfaces introduced
   - Fully qualified names no longer needed
   ```

---

## File Change Checklist

### New Files (2)
- [ ] `MonoBallFramework.Game/GameData/Services/IMapPopupDataService.cs`
- [ ] `MonoBallFramework.Game/Engine/Scenes/Services/IMapPopupOrchestrator.cs`

### Renamed Files (2)
- [ ] `GameData/Services/MapPopupService.cs` → `MapPopupDataService.cs`
- [ ] `Engine/Scenes/Services/MapPopupService.cs` → `MapPopupOrchestrator.cs`

### Modified Files (7)
- [ ] `GameData/Services/MapPopupDataService.cs` (implement interface, update class name)
- [ ] `Engine/Scenes/Services/MapPopupOrchestrator.cs` (implement interface, update class name, update dependency)
- [ ] `Infrastructure/ServiceRegistration/CoreServicesExtensions.cs` (update registration)
- [ ] `Initialization/InitializationContext.cs` (update property name and type)
- [ ] `Initialization/Pipeline/Steps/InitializeMapPopupStep.cs` (update instantiation)
- [ ] `Initialization/Pipeline/Steps/LoadGameDataStep.cs` (update service retrieval)
- [ ] `MonoBallFrameworkGame.cs` (update field name and type)

### Documentation Files (5)
- [ ] `docs/features/map-popup-themes-sections.md`
- [ ] `docs/bugfixes/prevent-double-popups.md`
- [ ] `docs/bugfixes/popup-map-name-formatting.md`
- [ ] `MonoBallFramework.Game/Assets/Graphics/Maps/Popups/README.md`
- [ ] `docs/architecture/migrations/map-popup-service-rename.md` (new)

---

## Testing Considerations

### 1. Compilation Testing
**Verify:**
- All files compile without errors
- No ambiguous reference warnings
- Interfaces properly implemented

**Command:**
```bash
dotnet build MonoBallFramework.Game/MonoBallFramework.Game.csproj
```

### 2. Functional Testing

**Test Scenarios:**

#### Test 1: Data Service Functionality
```csharp
// Verify MapPopupDataService still works correctly
var dataService = serviceProvider.GetRequiredService<IMapPopupDataService>();

// Test theme retrieval
var theme = dataService.GetTheme("wood");
Assert.NotNull(theme);
Assert.Equal("wood", theme.Id);

// Test section retrieval
var section = dataService.GetSection("MAPSEC_LITTLEROOT_TOWN");
Assert.NotNull(section);

// Test popup display info
var info = dataService.GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN");
Assert.NotNull(info);
Assert.Equal("LITTLEROOT TOWN", info.SectionName);
```

#### Test 2: Orchestrator Event Handling
```csharp
// Verify MapPopupOrchestrator still subscribes to events
// 1. Start game
// 2. Load initial map
// 3. Verify popup appears (MapRenderReadyEvent)
// 4. Trigger map transition
// 5. Verify popup appears (MapTransitionEvent)
// 6. Verify no double popups
```

#### Test 3: Service Integration
```csharp
// Verify orchestrator can retrieve data from data service
// This is tested implicitly by Test 2, but can be verified by:
// - Checking logs for successful data retrieval
// - Verifying correct theme is displayed
// - Verifying correct section name is shown
```

### 3. Regression Testing

**Critical Paths:**
1. ✅ Game startup (loads data correctly)
2. ✅ Initial map load (shows popup after first render)
3. ✅ Map transitions via warp (shows popup)
4. ✅ Map transitions via boundary crossing (shows popup)
5. ✅ No double popups when transitioning
6. ✅ Correct theme for each region section
7. ✅ Correct section name formatting (UPPERCASE)

### 4. Performance Testing

**Verify no performance degradation:**
- O(1) cache lookups still work
- No extra allocations from interface dispatch
- Event subscription/disposal works correctly

**Benchmark:**
```csharp
// Before and after metrics
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 10000; i++)
{
    var info = dataService.GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN");
}
stopwatch.Stop();
Console.WriteLine($"10k lookups: {stopwatch.ElapsedMilliseconds}ms");
// Should be < 5ms (O(1) cache hits)
```

---

## Migration Steps (Execution Order)

### Phase 1: Preparation (Non-Breaking)
1. ✅ Create `IMapPopupDataService` interface
2. ✅ Create `IMapPopupOrchestrator` interface
3. ✅ Update `MapPopupService` (GameData) to implement `IMapPopupDataService`
4. ✅ Update `MapPopupService` (Engine) to implement `IMapPopupOrchestrator`
5. ✅ Update DI registration to include interface
6. ✅ Test compilation and functionality

### Phase 2: Rename Data Service
1. ✅ Rename `GameData/Services/MapPopupService.cs` → `MapPopupDataService.cs`
2. ✅ Update class name and logger references
3. ✅ Update service registration in `CoreServicesExtensions.cs`
4. ✅ Update usages in `LoadGameDataStep.cs`
5. ✅ Test compilation and functionality

### Phase 3: Rename Orchestrator
1. ✅ Rename `Engine/Scenes/Services/MapPopupService.cs` → `MapPopupOrchestrator.cs`
2. ✅ Update class name, logger, and dependency injection
3. ✅ Update `InitializationContext.cs` property
4. ✅ Update `InitializeMapPopupStep.cs` instantiation
5. ✅ Update `MonoBallFrameworkGame.cs` field
6. ✅ Test compilation and functionality

### Phase 4: Documentation
1. ✅ Update technical documentation
2. ✅ Update code examples
3. ✅ Create migration guide
4. ✅ Update inline comments

### Phase 5: Validation
1. ✅ Run full test suite
2. ✅ Perform manual regression testing
3. ✅ Verify no fully qualified names remain
4. ✅ Verify performance unchanged

---

## Risk Assessment

### Low Risk
- ✅ Rename is straightforward
- ✅ Compiler will catch all references
- ✅ No runtime behavior changes
- ✅ Interfaces provide abstraction safety

### Medium Risk
- ⚠️ Documentation might be out of sync temporarily
- ⚠️ External mods might reference old names

**Mitigation:**
- Create comprehensive documentation updates
- Add `[Obsolete]` attributes to old names with redirect message (if we keep them temporarily)
- Clear migration guide for mod developers

### No Risk
- ✅ No database schema changes
- ✅ No asset changes
- ✅ No gameplay logic changes

---

## Alternative Approaches Considered

### Approach 1: Use Namespaces to Disambiguate
**Pros:** No renaming needed
**Cons:** Requires fully qualified names everywhere, confusing

**Verdict:** ❌ Not recommended - doesn't solve the problem

### Approach 2: Move Classes to Different Namespaces
**Pros:** Keeps class names
**Cons:** Breaks namespace organization, still confusing

**Verdict:** ❌ Not recommended - namespace structure is correct

### Approach 3: Merge into Single Service
**Pros:** No naming collision
**Cons:** Violates Single Responsibility Principle, tight coupling

**Verdict:** ❌ Not recommended - current separation is architecturally sound

---

## Post-Refactoring Benefits

### Developer Experience
1. ✅ **No ambiguity** - Service names clearly indicate purpose
2. ✅ **No fully qualified names** - Clean, readable code
3. ✅ **Self-documenting** - Name tells you what it does
4. ✅ **Better IntelliSense** - Clear suggestions

### Code Quality
1. ✅ **Interface-based design** - Easier to mock for testing
2. ✅ **Clear contracts** - Explicit service boundaries
3. ✅ **Better separation** - Data vs Orchestration clearly separated
4. ✅ **Maintainability** - Easier to understand and modify

### Architecture
1. ✅ **Follows SOLID** - Single Responsibility, Interface Segregation
2. ✅ **Clear layers** - Data layer vs Service layer vs Presentation layer
3. ✅ **Testability** - Interfaces enable unit testing
4. ✅ **Extensibility** - Easy to add new implementations

---

## Appendix: Complete Interface Definitions

### IMapPopupDataService (Full)
```csharp
namespace MonoBallFramework.Game.GameData.Services;

/// <summary>
/// Data access service for popup themes and map sections.
/// Provides cached O(1) lookups for hot paths (map transitions).
/// </summary>
public interface IMapPopupDataService
{
    #region Theme Queries

    /// <summary>
    /// Get popup theme by ID (O(1) cached).
    /// </summary>
    PopupTheme? GetTheme(string themeId);

    /// <summary>
    /// Get popup theme by ID asynchronously.
    /// </summary>
    Task<PopupTheme?> GetThemeAsync(string themeId, CancellationToken ct = default);

    /// <summary>
    /// Get all popup themes (use for tools/editors, not hot paths).
    /// </summary>
    Task<List<PopupTheme>> GetAllThemesAsync(CancellationToken ct = default);

    /// <summary>
    /// Preload all themes into cache for faster access.
    /// </summary>
    Task PreloadThemesAsync(CancellationToken ct = default);

    #endregion

    #region Section Queries

    /// <summary>
    /// Get map section by ID (O(1) cached).
    /// </summary>
    MapSection? GetSection(string sectionId);

    /// <summary>
    /// Get map section by ID asynchronously (includes Theme navigation property).
    /// </summary>
    Task<MapSection?> GetSectionAsync(string sectionId, CancellationToken ct = default);

    /// <summary>
    /// Get all map sections (use for tools/editors, not hot paths).
    /// </summary>
    Task<List<MapSection>> GetAllSectionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all map sections for a specific theme.
    /// </summary>
    Task<List<MapSection>> GetSectionsByThemeAsync(string themeId, CancellationToken ct = default);

    /// <summary>
    /// Preload all sections into cache for faster access.
    /// </summary>
    Task PreloadSectionsAsync(CancellationToken ct = default);

    #endregion

    #region Combined Queries

    /// <summary>
    /// Get popup theme for a map section ID.
    /// This is the primary method for map transitions.
    /// </summary>
    PopupTheme? GetThemeForSection(string sectionId);

    /// <summary>
    /// Get popup theme for a map section ID asynchronously.
    /// </summary>
    Task<PopupTheme?> GetThemeForSectionAsync(string sectionId, CancellationToken ct = default);

    /// <summary>
    /// Get popup display information (theme + section name) for a map section.
    /// Returns all data needed to show the map popup.
    /// </summary>
    PopupDisplayInfo? GetPopupDisplayInfo(string sectionId);

    /// <summary>
    /// Get popup display information asynchronously.
    /// </summary>
    Task<PopupDisplayInfo?> GetPopupDisplayInfoAsync(string sectionId, CancellationToken ct = default);

    /// <summary>
    /// Preload all themes and sections into cache.
    /// </summary>
    Task PreloadAllAsync(CancellationToken ct = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Get statistics about loaded data.
    /// </summary>
    Task<PopupDataStatistics> GetStatisticsAsync();

    /// <summary>
    /// Log statistics about loaded data (useful for debugging).
    /// </summary>
    Task LogStatisticsAsync();

    #endregion

    #region Cache Management

    /// <summary>
    /// Clear all caches.
    /// </summary>
    void ClearCache();

    #endregion
}
```

### IMapPopupOrchestrator (Full)
```csharp
namespace MonoBallFramework.Game.Engine.Scenes.Services;

/// <summary>
/// Orchestrates map popup display during map transitions.
/// Subscribes to MapTransitionEvent and MapRenderReadyEvent,
/// then pushes MapPopupScene onto the scene stack with appropriate theme.
/// </summary>
/// <remarks>
/// This service is event-driven and works through subscriptions.
/// It coordinates between:
/// - IMapPopupDataService (for theme/section data)
/// - PopupRegistry (for asset definitions)
/// - SceneManager (for scene lifecycle)
/// - EventBus (for map events)
/// </remarks>
public interface IMapPopupOrchestrator : IDisposable
{
    // No public methods needed - service works through event subscriptions.
    // IDisposable is required for cleanup of event subscriptions.
}
```

---

## Timeline Estimate

### Total Effort: ~4-6 hours

**Breakdown:**
- Phase 1 (Preparation): 1 hour
- Phase 2 (Data Service Rename): 30 minutes
- Phase 3 (Orchestrator Rename): 30 minutes
- Phase 4 (Documentation): 1.5 hours
- Phase 5 (Testing): 1-2 hours
- Buffer for unexpected issues: 30 minutes

**Recommended Execution:**
- Single PR with all changes
- Comprehensive testing before merge
- Clear commit messages for each phase

---

## Success Criteria

### ✅ Refactoring Complete When:
1. No compilation errors or warnings
2. All fully qualified names removed from codebase
3. All tests passing (if tests exist)
4. Manual regression testing passed
5. Documentation updated and accurate
6. Migration guide created
7. No performance degradation
8. Clean git history with clear commit messages

---

## Approval Checklist

- [ ] Architecture team review
- [ ] Code review by senior developer
- [ ] QA testing plan approved
- [ ] Documentation review completed
- [ ] Performance benchmarks validated
- [ ] Breaking change communication sent (if needed)

---

**Document Version:** 1.0
**Last Updated:** December 5, 2025
**Authors:** Claude (Architecture Specialist)
**Reviewers:** TBD
