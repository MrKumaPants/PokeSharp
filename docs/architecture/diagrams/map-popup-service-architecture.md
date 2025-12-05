# Map Popup Service Architecture

**Date:** December 5, 2025
**Version:** 2.0 (Post-Refactoring)

---

## Current Architecture (Before Refactoring)

```
┌─────────────────────────────────────────────────────────────────────┐
│                         NAMING COLLISION ZONE                        │
│                                                                      │
│  MonoBallFramework.Game                                             │
│  ├── GameData.Services                                              │
│  │   └── MapPopupService ⚠️  (Data Access)                         │
│  │       ├── GetTheme()                                             │
│  │       ├── GetSection()                                           │
│  │       ├── GetPopupDisplayInfo()                                  │
│  │       └── Cache (ConcurrentDictionary)                           │
│  │                                                                   │
│  └── Engine.Scenes.Services                                         │
│      └── MapPopupService ⚠️  (Orchestrator)                        │
│          ├── OnMapTransition()                                      │
│          ├── OnMapRenderReady()                                     │
│          ├── ShowPopupForMap()                                      │
│          └── Uses: GameData.Services.MapPopupService ⬆️             │
│                                                                      │
│  Problem: Both named "MapPopupService" - Requires FQNs everywhere! │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Proposed Architecture (After Refactoring)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLEAR SEPARATION                              │
│                                                                      │
│  MonoBallFramework.Game                                             │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ DATA LAYER                                                   │   │
│  │                                                              │   │
│  │ GameData.Services                                           │   │
│  │ ┌────────────────────────────────────────────────────────┐  │   │
│  │ │ IMapPopupDataService (Interface)                       │  │   │
│  │ │  ├── GetTheme(themeId)                                 │  │   │
│  │ │  ├── GetSection(sectionId)                             │  │   │
│  │ │  ├── GetPopupDisplayInfo(sectionId)                    │  │   │
│  │ │  ├── PreloadAllAsync()                                 │  │   │
│  │ │  └── ClearCache()                                      │  │   │
│  │ └────────────────────────────────────────────────────────┘  │   │
│  │          ▲                                                   │   │
│  │          │ implements                                        │   │
│  │          │                                                    │   │
│  │ ┌────────────────────────────────────────────────────────┐  │   │
│  │ │ MapPopupDataService ✅  (Concrete)                     │  │   │
│  │ │  ├── EF Core: GameDataContext                          │  │   │
│  │ │  ├── Cache: ConcurrentDictionary<string, PopupTheme>   │  │   │
│  │ │  ├── Cache: ConcurrentDictionary<string, MapSection>   │  │   │
│  │ │  └── Pattern: Repository + Cache                       │  │   │
│  │ └────────────────────────────────────────────────────────┘  │   │
│  │                                                              │   │
│  │ Dependencies:                                                │   │
│  │  - GameDataContext (EF Core)                                │   │
│  │  - ILogger<MapPopupDataService>                             │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│                              │ uses                                  │
│                              │                                       │
│  ┌──────────────────────────▼──────────────────────────────────┐   │
│  │ ORCHESTRATION LAYER                                          │   │
│  │                                                              │   │
│  │ Engine.Scenes.Services                                      │   │
│  │ ┌────────────────────────────────────────────────────────┐  │   │
│  │ │ IMapPopupOrchestrator (Interface)                      │  │   │
│  │ │  └── IDisposable (event cleanup)                       │  │   │
│  │ └────────────────────────────────────────────────────────┘  │   │
│  │          ▲                                                   │   │
│  │          │ implements                                        │   │
│  │          │                                                    │   │
│  │ ┌────────────────────────────────────────────────────────┐  │   │
│  │ │ MapPopupOrchestrator ✅  (Concrete)                    │  │   │
│  │ │  ├── Subscribes: MapTransitionEvent                    │  │   │
│  │ │  ├── Subscribes: MapRenderReadyEvent                   │  │   │
│  │ │  ├── Creates: MapPopupScene                            │  │   │
│  │ │  ├── Manages: Scene Stack (via SceneManager)           │  │   │
│  │ │  └── Pattern: Event Handler + Orchestrator             │  │   │
│  │ └────────────────────────────────────────────────────────┘  │   │
│  │                                                              │   │
│  │ Dependencies:                                                │   │
│  │  - IMapPopupDataService (for data) ⬆️                       │   │
│  │  - World (ECS)                                               │   │
│  │  - SceneManager (scene lifecycle)                            │   │
│  │  - PopupRegistry (asset definitions)                         │   │
│  │  - IEventBus (event subscriptions)                           │   │
│  │  - ILogger<MapPopupOrchestrator>                             │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  Benefits: ✅ No collision ✅ Clear roles ✅ Clean code             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Component Interaction Flow

### Data Flow: Map Transition → Popup Display

```
┌──────────────────────────────────────────────────────────────────────┐
│                        EVENT-DRIVEN FLOW                              │
└──────────────────────────────────────────────────────────────────────┘

1. TRIGGER: Player crosses map boundary
   │
   ├──> MapTransitionSystem detects boundary cross
   │
   └──> EventBus.Publish<MapTransitionEvent>
           │
           ├─ FromMapId: 1
           ├─ ToMapId: 2
           ├─ ToMapName: "LITTLEROOT TOWN"
           └─ RegionName: "MAPSEC_LITTLEROOT_TOWN"

2. ORCHESTRATOR: Receives event
   │
   └──> MapPopupOrchestrator.OnMapTransition(evt)
           │
           ├─ Check: ShouldShowPopupForMap(mapId)?
           │  └─> ECS Query: Does map have ShowMapNameOnEntry component?
           │
           └─ YES ──> ShowPopupForMap(mapId, displayName, regionName)

3. DATA RETRIEVAL: Get popup info
   │
   └──> MapPopupDataService.GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN")
           │
           ├─ Cache lookup: _sectionCache["MAPSEC_LITTLEROOT_TOWN"]
           │  └─> MISS ──> EF Core query ──> Add to cache
           │
           ├─ Cache lookup: _themeCache["wood"]
           │  └─> HIT ──> Return cached theme
           │
           └──> Return: PopupDisplayInfo
                  ├─ SectionName: "LITTLEROOT TOWN"
                  ├─ ThemeId: "wood"
                  ├─ BackgroundAssetId: "wood"
                  └─ OutlineAssetId: "wood_outline"

4. ASSET RESOLUTION: Get renderable assets
   │
   └──> PopupRegistry.GetBackground("wood")
        PopupRegistry.GetOutline("wood_outline")
           │
           └──> Return: PopupBackgroundDefinition, PopupOutlineDefinition

5. SCENE CREATION: Build popup scene
   │
   └──> new MapPopupScene(
           graphicsDevice,
           services,
           logger,
           assetProvider,
           backgroundDef,
           outlineDef,
           "LITTLEROOT TOWN"
        )

6. SCENE MANAGEMENT: Show popup
   │
   └──> SceneManager.PushScene(popupScene)
           │
           └─> Scene renders on top of game
               (Fades in, displays text, fades out, pops itself)

┌──────────────────────────────────────────────────────────────────────┐
│ RESULT: Popup displays "LITTLEROOT TOWN" with wood theme             │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Dependency Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      DEPENDENCY HIERARCHY                        │
└─────────────────────────────────────────────────────────────────┘

                            ┌─────────────────┐
                            │  EventBus       │
                            │  (IEventBus)    │
                            └────────┬────────┘
                                     │ publishes events
                                     │
                     ┌───────────────▼───────────────┐
                     │  MapPopupOrchestrator         │
                     │  (IMapPopupOrchestrator)      │
                     └───────────────┬───────────────┘
                                     │
                     ┌───────────────┼───────────────┐
                     │               │               │
         ┌───────────▼─────┐  ┌─────▼──────┐  ┌────▼────────┐
         │ MapPopupData    │  │ Popup      │  │ Scene       │
         │ Service         │  │ Registry   │  │ Manager     │
         │ (Data)          │  │ (Assets)   │  │ (Scenes)    │
         └───────┬─────────┘  └────────────┘  └─────────────┘
                 │
     ┌───────────┼───────────┐
     │           │           │
┌────▼────┐ ┌───▼─────┐ ┌───▼────────┐
│ EF Core │ │ Logger  │ │ Cache      │
│ Context │ │         │ │ (Concurrent│
│         │ │         │ │ Dictionary)│
└─────────┘ └─────────┘ └────────────┘

Legend:
────>  Dependency (uses)
═══>  Event flow
```

---

## Layer Responsibilities

### Data Layer (MapPopupDataService)

**Purpose:** Abstract database operations and provide fast lookups

**Responsibilities:**
- ✅ Query EF Core database for themes and sections
- ✅ Cache frequently accessed data (O(1) lookups)
- ✅ Provide async and sync query methods
- ✅ Manage cache lifecycle (preload, clear)
- ✅ Return aggregated display info

**Does NOT:**
- ❌ Handle events
- ❌ Manage UI/scenes
- ❌ Know about game logic
- ❌ Depend on ECS

**Pattern:** Repository + Cache

---

### Orchestration Layer (MapPopupOrchestrator)

**Purpose:** Coordinate popup display during gameplay

**Responsibilities:**
- ✅ Subscribe to map transition events
- ✅ Query ECS for ShowMapNameOnEntry component
- ✅ Retrieve popup data from MapPopupDataService
- ✅ Resolve asset definitions from PopupRegistry
- ✅ Create MapPopupScene instances
- ✅ Push scenes to SceneManager stack
- ✅ Prevent double popups (check existing scenes)

**Does NOT:**
- ❌ Query database directly
- ❌ Cache data
- ❌ Render graphics
- ❌ Handle player input

**Pattern:** Event Handler + Orchestrator

---

## Service Lifecycle

### MapPopupDataService

```
1. REGISTRATION (CoreServicesExtensions)
   └─> services.AddSingleton<IMapPopupDataService, MapPopupDataService>()

2. CONSTRUCTION
   ├─> DI injects: GameDataContext, ILogger<MapPopupDataService>
   └─> Initialize empty caches

3. DATA LOADING (LoadGameDataStep)
   ├─> GameDataLoader.LoadAllAsync()
   │   ├─ Load themes from JSON
   │   └─ Load sections from JSON
   └─> EF Core InMemory database populated

4. USAGE (Throughout Game)
   ├─> GetPopupDisplayInfo() [Hot path - map transitions]
   ├─> GetTheme() [Asset loading]
   └─> GetSection() [Editor/tools]

5. SHUTDOWN
   └─> Garbage collection (no explicit disposal needed)
```

### MapPopupOrchestrator

```
1. CONSTRUCTION (InitializeMapPopupStep)
   ├─> Created directly (not in DI container)
   ├─> DI provides dependencies via IServiceProvider
   └─> Subscribes to EventBus

2. EVENT SUBSCRIPTIONS
   ├─> MapTransitionEvent → OnMapTransition()
   └─> MapRenderReadyEvent → OnMapRenderReady()

3. USAGE (Event-Driven)
   ├─> Game triggers map transition
   ├─> EventBus publishes event
   └─> Orchestrator handles event → Shows popup

4. SHUTDOWN (MonoBallFrameworkGame.DisposeAsync)
   ├─> Dispose() called
   ├─> Event subscriptions released
   └─> Garbage collection
```

---

## Performance Characteristics

### MapPopupDataService

**Cache Hit Rate:** ~99% (after preload)
**Lookup Complexity:** O(1) (dictionary lookup)
**Memory Usage:** ~10KB (100 themes + 300 sections)

**Benchmark:**
```csharp
// 10,000 lookups with cache hits
GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN") × 10,000
Expected: < 5ms total (< 0.0005ms per call)
```

### MapPopupOrchestrator

**Event Handling:** ~0.1ms per event
**Scene Creation:** ~2-3ms (asset loading)
**Total Popup Display:** ~300-500ms (includes fade animation)

**Allocation:** Minimal (event pooling reduces GC pressure)

---

## Testing Strategy

### Unit Tests

#### MapPopupDataService Tests
```csharp
[TestFixture]
public class MapPopupDataServiceTests
{
    [Test]
    public async Task GetPopupDisplayInfo_ValidSection_ReturnsCorrectData()
    {
        // Arrange
        var service = CreateService(); // with mock context

        // Act
        var info = service.GetPopupDisplayInfo("MAPSEC_LITTLEROOT_TOWN");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("LITTLEROOT TOWN", info.SectionName);
        Assert.Equal("wood", info.ThemeId);
    }

    [Test]
    public void GetTheme_CacheMiss_QueriesDatabase()
    {
        // Verify EF Core query on cache miss
    }

    [Test]
    public void GetTheme_CacheHit_SkipsDatabase()
    {
        // Verify cache lookup on subsequent calls
    }
}
```

#### MapPopupOrchestrator Tests
```csharp
[TestFixture]
public class MapPopupOrchestratorTests
{
    [Test]
    public void OnMapTransition_MapWithPopup_CreatesScene()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(); // with mocks
        var evt = new MapTransitionEvent { /* ... */ };

        // Act
        // Trigger event

        // Assert
        // Verify SceneManager.PushScene called
    }

    [Test]
    public void OnMapTransition_MapWithoutPopup_SkipsScene()
    {
        // Verify no scene created for maps without ShowMapNameOnEntry
    }
}
```

### Integration Tests

```csharp
[TestFixture]
public class MapPopupIntegrationTests
{
    [Test]
    public async Task FullWorkflow_MapTransition_ShowsPopup()
    {
        // Arrange: Full game initialization
        // Act: Trigger map transition
        // Assert: Popup scene appears with correct theme
    }
}
```

---

## Migration Checklist

### Code Changes
- [ ] Create `IMapPopupDataService` interface
- [ ] Create `IMapPopupOrchestrator` interface
- [ ] Rename `GameData.Services.MapPopupService` → `MapPopupDataService`
- [ ] Rename `Engine.Scenes.Services.MapPopupService` → `MapPopupOrchestrator`
- [ ] Update service registration in `CoreServicesExtensions`
- [ ] Update `InitializationContext` property
- [ ] Update `InitializeMapPopupStep` instantiation
- [ ] Update `LoadGameDataStep` service retrieval
- [ ] Update `MonoBallFrameworkGame` field

### Documentation Changes
- [ ] Update feature documentation
- [ ] Update bug fix documentation
- [ ] Update README files
- [ ] Create migration guide
- [ ] Update architecture diagrams

### Validation
- [ ] Compile without errors
- [ ] Run unit tests
- [ ] Run integration tests
- [ ] Manual regression testing
- [ ] Performance benchmarks

---

## Related Files

### Implementation Files
- `/MonoBallFramework.Game/GameData/Services/MapPopupDataService.cs`
- `/MonoBallFramework.Game/Engine/Scenes/Services/MapPopupOrchestrator.cs`
- `/MonoBallFramework.Game/Infrastructure/ServiceRegistration/CoreServicesExtensions.cs`

### Documentation Files
- `/docs/architecture/ADR-001-map-popup-service-naming.md`
- `/docs/architecture/map-popup-service-refactoring-plan.md`
- `/docs/features/map-popup-themes-sections.md`

---

**Document Version:** 2.0 (Post-Refactoring)
**Last Updated:** December 5, 2025
**Status:** PROPOSED - Pending Implementation
