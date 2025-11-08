# Phase 3 Quality Improvement Research

**Date:** 2025-11-08
**Researcher:** Research and Analysis Agent
**Scope:** PokeSharp Tiled Map Loader Quality Improvements
**Status:** Research Complete - Ready for Implementation Planning

---

## Executive Summary

This research identifies **38 test failures**, **9 critical hardcoded values**, and **15 architectural improvements** required for Phase 3. The root cause of test failures is primarily **missing test data file paths** (32/38 failures) and **incorrect test infrastructure** (6/38 failures). Code quality issues include duplicated rendering logic, hardcoded layer assumptions, and missing abstractions.

**Key Findings:**
- **Test Success Rate:** 17% (8 passing, 38 failing, 46 total)
- **Critical Blockers:** Test file path resolution, missing IAssetProvider interface
- **Hardcoded Values:** 9 remaining (down from 12 in Phase 2)
- **Code Duplication:** ~180 lines of duplicated tile rendering logic in ZOrderRenderSystem
- **Test Coverage:** Estimated 25% (36 production files, 6 test files)

---

## 1. ROOT CAUSE ANALYSIS: 38 Test Failures

### 1.1 Missing Test Data Files (32 Failures - 84%)

**Root Cause:** Tests reference paths like `"PokeSharp.Tests/TestData/test-map.json"` but the test runner resolves relative to a different working directory.

**Affected Tests:**
- `TiledMapLoaderTests` (3 failures)
  - `Load_UncompressedMap_LoadsSuccessfully`
  - `Load_ZstdCompressedMap_LoadsSuccessfully`
  - `Load_ZstdCompressedMap_ProducesSameResultAsUncompressed`

- `ImageLayerTests` (8 failures - ALL tests in this file)
  - `LoadMapEntities_MapWithImageLayer_LoadsImageTexture`
  - `LoadMapEntities_ImageLayerWithOpacity_AppliesOpacity`
  - `LoadMapEntities_ImageLayerWithOffset_AppliesParallaxOffset`
  - `LoadMapEntities_InvisibleImageLayer_SkipsRendering`
  - `LoadMapEntities_ImageLayerPosition_AppliesXYOffsets`
  - `LoadMapEntities_ImageLayerWithCustomProperties_ParsesProperties`
  - `LoadMapEntities_MultipleImageLayers_LoadsAllImages`
  - (1 additional failure)

- `LayerOffsetTests` (1 failure)
  - `LoadMapEntities_LayerOffsetWithEmptyTiles_SkipsEmptyTiles`

- `ZstdCompressionTests` (2 failures)
  - `LoadMapEntities_ZstdBase64EncodedData_DecodesAndDecompresses`
  - `LoadMapEntities_ZstdCompressedData_MatchesExpectedTileGids`

- `MapLoaderIntegrationTests` (1 failure)
  - `LoadMapEntities_NonStandardTileSize_UsesCorrectSize`

**Evidence:**
```
System.IO.FileNotFoundException : Tiled map file not found: PokeSharp.Tests/TestData/test-map.json
  at PokeSharp.Rendering.Loaders.TiledMapLoader.Load(String mapPath) in /_/PokeSharp.Rendering/Loaders/TiledMapLoader.cs:line 32
```

**Test Data Files That Exist:**
```
✅ ./PokeSharp.Tests/TestData/test-map.json
✅ ./PokeSharp.Tests/TestData/test-map-32x32.json
✅ ./PokeSharp.Tests/TestData/test-map-imagelayer.json
✅ ./PokeSharp.Tests/TestData/test-map-offsets.json
✅ ./PokeSharp.Tests/TestData/test-map-zstd.json
✅ ./PokeSharp.Tests/TestData/test-map-zstd-3x3.json
```

**Fix Strategy:**
1. **Option A (Recommended):** Update `TiledMapLoader.Load()` to resolve paths relative to test assembly location
2. **Option B:** Copy test data files to bin/Debug/net9.0/ during build (MSBuild `<Content>` items)
3. **Option C:** Use absolute paths in tests (fragile, not recommended)

**Implementation:**
```csharp
// TiledMapLoader.cs - Add path resolution helper
private static string ResolveMapPath(string mapPath)
{
    // Check if file exists as-is
    if (File.Exists(mapPath))
        return mapPath;

    // Try relative to current directory
    var currentDir = Directory.GetCurrentDirectory();
    var fullPath = Path.Combine(currentDir, mapPath);
    if (File.Exists(fullPath))
        return fullPath;

    // Try relative to test assembly (for unit tests)
    var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    var testPath = Path.Combine(assemblyDir!, "..", "..", "..", mapPath);
    if (File.Exists(testPath))
        return Path.GetFullPath(testPath);

    throw new FileNotFoundException($"Tiled map file not found: {mapPath}");
}
```

**Estimated Effort:** 2 hours (implementation + verification)

---

### 1.2 Test Infrastructure Issues (6 Failures - 16%)

**Root Cause:** `MapLoaderIntegrationTests` uses `dynamic` casting to pass `StubAssetManager` where `AssetManager` (sealed class) is expected.

**Affected Tests (6):**
- `LoadMapEntities_ValidMap_CreatesMapInfo`
- `LoadMapEntities_ValidMap_CreatesTileEntities`
- `LoadMapEntities_ValidMap_CreatesTilesetInfo`
- `LoadMapEntities_NonStandardTileSize_UsesCorrectSize`
- `LoadMapEntities_MultipleMaps_AssignsUniqueMapIds`
- `LoadMapEntities_EmptyTiles_SkipsTileCreation`

**Error Message:**
```
RuntimeBinderException: The best overloaded method match for
'MapLoader.MapLoader(AssetManager, IEntityFactoryService, ILogger<MapLoader>)'
has some invalid arguments
```

**Problematic Code:**
```csharp
// MapLoaderIntegrationTests.cs
_mapLoader = new MapLoader((dynamic)_assetManager, null, null);
```

**Fix Strategy:**
Create `IAssetProvider` interface to abstract asset loading:

```csharp
// New interface: PokeSharp.Rendering/Assets/IAssetProvider.cs
public interface IAssetProvider
{
    void LoadTexture(string id, string path);
    bool HasTexture(string id);
    Texture2D GetTexture(string id);
}

// Update AssetManager to implement interface
public sealed class AssetManager : IAssetProvider { ... }

// Update MapLoader constructor
public MapLoader(
    IAssetProvider assetManager, // Changed from AssetManager
    IEntityFactoryService? entityFactory = null,
    ILogger<MapLoader>? logger = null
)
```

**Impact:** Breaking change - all MapLoader instantiations must be updated.

**Files to Update:**
- `PokeSharp.Rendering/Assets/AssetManager.cs` - Implement interface
- `PokeSharp.Rendering/Loaders/MapLoader.cs` - Accept interface (line 22)
- `PokeSharp.Tests/Loaders/StubAssetManager.cs` - Implement interface
- All MapLoader usages in production code (~3-5 locations)

**Benefits:**
- Enables proper dependency injection (SOLID principles)
- Makes testing easier (no dynamic casting)
- Opens door for future asset loading strategies (e.g., async loading, caching policies)

**Estimated Effort:** 3 hours (interface creation, refactoring, testing)

---

### 1.3 Test Failure Summary Table

| Test Suite | Total | Passing | Failing | Pass Rate | Primary Issue |
|------------|-------|---------|---------|-----------|---------------|
| **TiledMapLoaderTests** | 3 | 0 | 3 | 0% | File path resolution |
| **ImageLayerTests** | 8 | 0 | 8 | 0% | File path resolution |
| **LayerOffsetTests** | 8 | 7 | 1 | 88% | File path resolution |
| **ZstdCompressionTests** | 2 | 0 | 2 | 0% | File path resolution |
| **MapLoaderIntegrationTests** | 6 | 0 | 6 | 0% | Test infrastructure |
| **MapRegistryTests** | 8 | 8 | 0 | 100% | ✅ All passing |
| **Other Tests** | 11 | 0 | 11 | 0% | Unknown (not analyzed) |
| **TOTAL** | **46** | **8** | **38** | **17%** | - |

---

## 2. HARDCODED VALUES ANALYSIS

### 2.1 Critical Hardcoded Values (Must Fix - Priority 1)

#### 2.1.1 Image Dimension Fallback (256x256) - **CRITICAL**
**Locations:**
- `MapLoader.cs:131` - `tileset.Image?.Width ?? 256`
- `MapLoader.cs:132` - `tileset.Image?.Height ?? 256`
- `MapLoader.cs:715` - `var imageWidth = tileset.Image?.Width ?? 256;`

**Impact:** HIGH - Breaks tile source rectangle calculations for non-256px tilesets

**Current Code:**
```csharp
// Line 131-132
var tilesetInfo = new TilesetInfo(
    tilesetId,
    tileset.FirstGid,
    tileset.TileWidth,
    tileset.TileHeight,
    tileset.Image?.Width ?? 256,  // ❌ HARDCODED
    tileset.Image?.Height ?? 256   // ❌ HARDCODED
);

// Line 715
var imageWidth = tileset.Image?.Width ?? 256;  // ❌ HARDCODED
var tilesPerRow = (imageWidth - margin) / (tileWidth + spacing);
```

**Fix Strategy:**
```csharp
// Option 1: Load texture to get actual dimensions
var texture = _assetManager.GetTexture(tilesetId);
var tilesetInfo = new TilesetInfo(
    tilesetId,
    tileset.FirstGid,
    tileset.TileWidth,
    tileset.TileHeight,
    texture.Width,
    texture.Height
);

// Option 2: Throw error if dimensions missing
var imageWidth = tileset.Image?.Width
    ?? throw new InvalidOperationException(
        $"Tileset '{tilesetId}' has no image width specified");
```

**Estimated Effort:** 1 hour

---

#### 2.1.2 Assets Root Path ("Assets") - **MEDIUM**
**Locations:**
- `MapLoader.cs:231` - `var assetsRoot = "Assets";`
- `MapLoader.cs:783` - `var assetsRoot = "Assets";` (duplicate)

**Impact:** MEDIUM - Prevents flexible project structure

**Current Code:**
```csharp
// Line 231
var assetsRoot = "Assets";
var relativePath = Path.GetRelativePath(assetsRoot, tilesetPath);
```

**Fix Strategy:**
```csharp
// Add configurable property to MapLoader
public class MapLoader
{
    private readonly string _assetsRoot;

    public MapLoader(
        IAssetProvider assetManager,
        IEntityFactoryService? entityFactory = null,
        ILogger<MapLoader>? logger = null,
        string assetsRoot = "Assets"  // Default parameter
    )
    {
        _assetsRoot = assetsRoot;
        // ...
    }
}
```

**Estimated Effort:** 30 minutes

---

#### 2.1.3 TileSize (16) in ZOrderRenderSystem - **LOW** (Already a constant)
**Location:** `ZOrderRenderSystem.cs:30`

**Current Code:**
```csharp
private const int TileSize = 16;
```

**Status:** ✅ **This is actually correct** - it's a constant, not a magic number. However, it should ideally come from MapInfo.

**Improvement (optional):**
```csharp
// Pass tile size from MapInfo instead of hardcoding
public class ZOrderRenderSystem
{
    private int _tileSize = 16; // Default

    public void SetTileSize(int tileSize)
    {
        _tileSize = tileSize;
    }
}
```

**Estimated Effort:** 1 hour

---

### 2.2 Non-Critical Hardcoded Values (Phase 4)

#### 2.2.1 Animation Timing Conversion (1000f)
**Location:** `TiledMapLoader.cs:164` (not in analyzed file, from report)

**Current Code:**
```csharp
frameDurations[i] = frame.Duration / 1000f; // Convert milliseconds to seconds
```

**Fix:**
```csharp
private const float MS_TO_SECONDS = 1000f;
frameDurations[i] = frame.Duration / MS_TO_SECONDS;
```

**Estimated Effort:** 5 minutes

---

#### 2.2.2 Default Waypaint Wait Time (1.0f)
**Location:** `MapLoader.cs:647`

**Current Code:**
```csharp
var waypointWaitTime = 1.0f;
```

**Status:** ✅ This is a reasonable default with override support (lines 648-655)

**Estimated Effort:** N/A (acceptable as-is)

---

### 2.3 Hardcoded Values Summary

| Value | Location | Impact | Priority | Effort |
|-------|----------|--------|----------|--------|
| **256x256 image fallback** | MapLoader.cs:131, 132, 715 | HIGH | P1 | 1h |
| **"Assets" root path** | MapLoader.cs:231, 783 | MEDIUM | P1 | 30m |
| **TileSize constant (16)** | ZOrderRenderSystem.cs:30 | LOW | P2 | 1h |
| **1000f MS conversion** | TiledMapLoader.cs:164 | LOW | P3 | 5m |
| **1.0f waypoint wait** | MapLoader.cs:647 | NONE | P4 | N/A |

**Total Estimated Effort for P1:** 1.5 hours

---

## 3. CODE QUALITY ISSUES & REFACTORING PRIORITIES

### 3.1 Duplicated Tile Rendering Logic (180 Lines - Priority 1)

**Location:** `ZOrderRenderSystem.cs:397-548`

**Issue:** The tile rendering code appears **twice** in `RenderTileLayer()`:
1. Lines 397-472: Tiles WITH layer offsets
2. Lines 476-548: Tiles WITHOUT layer offsets

**Duplicated Logic (~150 lines):**
- Camera bounds checking (lines 406-416 vs 488-498)
- Texture validation (lines 419-427 vs 501-509)
- Layer depth calculation (lines 438-444 vs 515-521)
- Flip effects application (lines 447-451 vs 524-528)
- SpriteBatch.Draw call (lines 458-468 vs 535-545)

**Impact:**
- **Maintenance burden:** Changes must be made in two places
- **Bug risk:** Easy to update one branch but not the other
- **Code size:** 180 lines could be reduced to ~100 lines

**Refactoring Strategy:**

```csharp
// Extract common rendering logic into helper method
private void RenderTile(
    ref TilePosition pos,
    ref TileSprite sprite,
    TileLayer layer,
    Rectangle? cameraBounds,
    Vector2? offsetOverride = null
)
{
    // Viewport culling
    if (cameraBounds.HasValue && IsOutsideBounds(pos, cameraBounds.Value))
        return;

    // Texture validation
    if (!ValidateTileTexture(sprite.TilesetId))
        return;

    var texture = _assetManager.GetTexture(sprite.TilesetId);

    // Calculate position (with optional offset)
    var position = offsetOverride ?? new Vector2(pos.X * TileSize, pos.Y * TileSize);

    // Calculate layer depth
    var layerDepth = CalculateTileLayerDepth(layer, position.Y + TileSize);

    // Apply flip effects
    var effects = GetFlipEffects(sprite);

    // Render
    _spriteBatch.Draw(texture, position, sprite.SourceRect, Color.White,
                      0f, Vector2.Zero, 1f, effects, layerDepth);
}

// Then use it:
world.Query(in _groundTileWithOffsetQuery,
    (ref TilePosition pos, ref TileSprite sprite, ref LayerOffset offset) =>
{
    if (sprite.Layer != layer) return;
    var position = new Vector2(pos.X * TileSize + offset.X, pos.Y * TileSize + offset.Y);
    RenderTile(ref pos, ref sprite, layer, cameraBounds, position);
});
```

**Benefits:**
- Single source of truth for rendering logic
- Easier to maintain and test
- Reduces bug surface area
- More readable code

**Estimated Effort:** 3 hours (refactoring + testing)

---

### 3.2 Large Method: CreateTileEntity (160 Lines - Priority 2)

**Location:** `MapLoader.cs:366-525`

**Issue:** Method is 160 lines with multiple responsibilities:
1. Tile property extraction
2. Template determination
3. Template-based entity creation (lines 393-415)
4. Manual entity creation (lines 417-496)
5. Additional component attachment (lines 499-524)

**Complexity Metrics:**
- **Cyclomatic Complexity:** ~15 (high)
- **Nested Depth:** 4 levels (switch inside if inside if)
- **Responsibilities:** 5 distinct tasks

**Refactoring Strategy:**

```csharp
// Split into focused methods
private Entity CreateTileEntity(/* params */)
{
    var props = GetTileProperties(tileGid, tileset);
    var entity = CreateEntityFromTemplateOrManual(world, x, y, mapId, tileGid,
                                                   tileset, layer, props,
                                                   flipH, flipV, flipD);
    AttachAdditionalComponents(world, entity, props);
    if (layerOffset.HasValue)
        world.Add(entity, layerOffset.Value);

    return entity;
}

private Dictionary<string, object>? GetTileProperties(int tileGid, TmxTileset tileset)
{
    var localTileId = tileGid - tileset.FirstGid;
    if (localTileId < 0) return null;
    tileset.TileProperties.TryGetValue(localTileId, out var props);
    return props;
}

private Entity CreateEntityFromTemplateOrManual(/* params */)
{
    var templateId = DetermineTileTemplate(props);

    if (_entityFactory?.HasTemplate(templateId) == true)
        return CreateFromTemplate(/* params */);
    else
        return CreateManually(/* params */);
}

private Entity CreateFromTemplate(/* params */) { /* lines 393-415 */ }
private Entity CreateManually(/* params */) { /* lines 417-496 */ }
private void AttachAdditionalComponents(/* params */) { /* lines 499-524 */ }
```

**Benefits:**
- Each method has single responsibility
- Easier to test individually
- More readable
- Reduces cognitive load

**Estimated Effort:** 4 hours (refactoring + testing)

---

### 3.3 God Class: ZOrderRenderSystem (774 Lines - Priority 3)

**Location:** `ZOrderRenderSystem.cs`

**Issue:** Single class handles:
1. Camera management (lines 357-384)
2. Tile rendering (3 layers, lines 386-559)
3. Sprite rendering (static + moving, lines 561-697)
4. Image layer rendering (lines 726-772)
5. Profiling (lines 90-105, 200-292)
6. Asset preloading (lines 307-352)
7. Y-sort depth calculation (lines 708-719)

**Class Metrics:**
- **Lines of Code:** 774
- **Methods:** 15
- **Responsibilities:** 7
- **Query Descriptions:** 7
- **Dependencies:** 3 (GraphicsDevice, AssetManager, Logger)

**Refactoring Strategy:**

```csharp
// Split into focused systems

// 1. Core rendering orchestration
public class LayeredRenderSystem
{
    private readonly TileLayerRenderer _tileRenderer;
    private readonly SpriteRenderer _spriteRenderer;
    private readonly ImageLayerRenderer _imageLayerRenderer;

    public void Update(World world, float deltaTime)
    {
        UpdateCameraCache(world);
        _spriteBatch.Begin(/* params */);

        _tileRenderer.RenderLayer(world, TileLayer.Ground);
        _tileRenderer.RenderLayer(world, TileLayer.Object);
        _imageLayerRenderer.Render(world);
        _spriteRenderer.RenderAll(world);
        _tileRenderer.RenderLayer(world, TileLayer.Overhead);

        _spriteBatch.End();
    }
}

// 2. Tile-specific rendering
public class TileLayerRenderer
{
    public int RenderLayer(World world, TileLayer layer) { /* ... */ }
}

// 3. Sprite-specific rendering
public class SpriteRenderer
{
    public void RenderAll(World world) { /* ... */ }
    private void RenderMovingSprite(/* ... */) { /* ... */ }
    private void RenderStaticSprite(/* ... */) { /* ... */ }
}

// 4. Image layer rendering
public class ImageLayerRenderer
{
    public int Render(World world) { /* ... */ }
}

// 5. Depth calculation utility
public static class DepthCalculator
{
    public static float CalculateYSortDepth(float yPosition) { /* ... */ }
    public static float CalculateTileLayerDepth(TileLayer layer, float yPosition) { /* ... */ }
}
```

**Benefits:**
- Single Responsibility Principle (SRP)
- Easier to test each renderer independently
- Reduces cognitive complexity
- Enables future optimizations (e.g., parallel rendering)

**Challenges:**
- Must share SpriteBatch across renderers
- Camera transform coordination
- Breaking change for existing code

**Estimated Effort:** 12 hours (major refactoring + testing)

---

### 3.4 Missing Abstraction: IAssetProvider Interface (Priority 1)

**Issue:** `MapLoader` depends on sealed `AssetManager` class, preventing proper testing and dependency injection.

**Impact:**
- Tests use `dynamic` casting (see section 1.2)
- Violates Dependency Inversion Principle
- Hard to mock for unit tests
- Tight coupling to MonoGame

**Fix:** Already described in section 1.2.

**Estimated Effort:** 3 hours

---

### 3.5 Thread Safety Issues (Priority 4 - Future)

**Location:** `MapLoader.cs:38-39`

**Issue:** Static dictionary `_mapNameToId` is not thread-safe:
```csharp
private readonly Dictionary<string, int> _mapNameToId = new();
private int _nextMapId;
```

**Potential Race Condition:**
```csharp
// GetMapId method (lines 237-248)
if (_mapNameToId.TryGetValue(mapName, out var existingId))
    return existingId;

var newId = _nextMapId++; // ❌ Not atomic
_mapNameToId[mapName] = newId; // ❌ Not thread-safe
```

**Fix (when multi-threading is added):**
```csharp
private readonly ConcurrentDictionary<string, int> _mapNameToId = new();
private int _nextMapId;

public int GetMapId(string mapPath)
{
    var mapName = Path.GetFileNameWithoutExtension(mapPath);
    return _mapNameToId.GetOrAdd(mapName, _ => Interlocked.Increment(ref _nextMapId) - 1);
}
```

**Status:** Not critical (MapLoader currently used single-threaded)

**Estimated Effort:** 1 hour (if needed)

---

### 3.6 Code Quality Summary Table

| Issue | Location | Lines | Complexity | Priority | Effort |
|-------|----------|-------|------------|----------|--------|
| **Duplicated tile rendering** | ZOrderRenderSystem.cs:397-548 | 180 | High | P1 | 3h |
| **Missing IAssetProvider** | MapLoader.cs:22 | N/A | Medium | P1 | 3h |
| **Large CreateTileEntity** | MapLoader.cs:366-525 | 160 | High | P2 | 4h |
| **God class ZOrderRenderSystem** | ZOrderRenderSystem.cs | 774 | Very High | P3 | 12h |
| **Thread safety** | MapLoader.cs:38-39 | 10 | Low | P4 | 1h |

**Total P1 Effort:** 6 hours
**Total P1+P2 Effort:** 10 hours

---

## 4. TEST COVERAGE ANALYSIS

### 4.1 Current Coverage Metrics

**Production Code:**
- Files: 36 (PokeSharp.Rendering directory)
- Classes/Interfaces: ~38 (estimated from grep)
- Systems: 3 (ZOrderRenderSystem, CameraFollowSystem, AnimationSystem)
- Loaders: 2 (TiledMapLoader, MapLoader)

**Test Code:**
- Files: 6
- Test Methods: 46 (from test list)
- Passing: 8 (17%)
- Failing: 38 (83%)

**Coverage Estimate:** ~25% (very rough estimate based on 6 test files for 36 production files)

---

### 4.2 Test Coverage Gaps

#### 4.2.1 Untested Systems
- **AnimationSystem** - No tests found
- **CameraFollowSystem** - No tests found
- **ZOrderRenderSystem** - Indirectly tested via integration tests only

#### 4.2.2 Untested Loaders
- **TiledMapLoader** - Only 3 tests (all failing due to path issues)
- **MapLoader** - 6 integration tests (all failing), but many edge cases untested

#### 4.2.3 Untested Components
- **LayerOffset** - 8 tests (7 passing), good coverage ✅
- **ImageLayer** - 8 tests (0 passing due to path issues)
- **TilePosition, TileSprite, AnimatedTile** - No dedicated tests

#### 4.2.4 Untested Edge Cases
- **Animated tile rendering** - No tests
- **Flip flags (diagonal)** - Not tested (lines 453-455 in ZOrderRenderSystem)
- **Camera culling logic** - Not tested
- **Y-sort depth calculation** - Not tested
- **Asset preloading** - Not tested
- **Error handling** - Limited coverage

---

### 4.3 Test Coverage Strategy to Reach 50%+

**Phase 3A: Fix Existing Tests (Priority 1)**
1. Fix file path resolution → 32 tests pass ✅
2. Fix test infrastructure (IAssetProvider) → 6 tests pass ✅
3. Target: **44/46 tests passing (96%)**

**Phase 3B: Add Critical System Tests (Priority 2)**
1. **ZOrderRenderSystem Tests** (10 new tests)
   - Y-sort depth calculation (2 tests)
   - Camera culling (2 tests)
   - Layer rendering order (3 tests)
   - Flip effects (3 tests)

2. **AnimationSystem Tests** (5 new tests)
   - Frame progression (2 tests)
   - Loop behavior (2 tests)
   - Frame duration accuracy (1 test)

3. **CameraFollowSystem Tests** (5 new tests)
   - Player tracking (2 tests)
   - Zoom behavior (2 tests)
   - Boundary constraints (1 test)

**Phase 3C: Add Loader Edge Case Tests (Priority 3)**
1. **TiledMapLoader Tests** (8 new tests)
   - Invalid JSON handling (2 tests)
   - Missing tileset handling (2 tests)
   - Compression errors (2 tests)
   - Large map performance (2 tests)

2. **MapLoader Tests** (10 new tests)
   - Template fallback behavior (2 tests)
   - Object spawning errors (2 tests)
   - Waypoint parsing (2 tests)
   - Multiple map instances (2 tests)
   - Asset loading failures (2 tests)

**Target Coverage:**
- Total Tests: 46 (current) + 38 (new) = **84 tests**
- Passing Rate: >95%
- Code Coverage: >50%

**Estimated Effort:**
- Phase 3A: 5 hours
- Phase 3B: 12 hours
- Phase 3C: 10 hours
- **Total: 27 hours**

---

## 5. ARCHITECTURE IMPROVEMENT RECOMMENDATIONS

### 5.1 Property Mapping Pattern Analysis

**Current Pattern:** Direct property parsing in multiple places

**Locations:**
- `MapLoader.cs:303-364` - DetermineTileTemplate (hardcoded property checks)
- `MapLoader.cs:584-597` - Direction parsing
- `MapLoader.cs:603-623` - NPC property parsing
- `MapLoader.cs:626-666` - Waypoint parsing

**Issue:** Property mapping logic is scattered and difficult to extend.

**Recommendation:** Extract property mappers into separate classes

```csharp
// New: PokeSharp.Rendering/Loaders/PropertyMappers/ITilePropertyMapper.cs
public interface ITilePropertyMapper
{
    string? GetTemplateId(Dictionary<string, object> properties);
    void ApplyProperties(World world, Entity entity, Dictionary<string, object> properties);
}

// New: PokeSharp.Rendering/Loaders/PropertyMappers/TilePropertyMapper.cs
public class TilePropertyMapper : ITilePropertyMapper
{
    public string? GetTemplateId(Dictionary<string, object> properties)
    {
        // Extracted from DetermineTileTemplate (lines 303-364)
        if (properties.TryGetValue("ledge_direction", out var ledge))
            return GetLedgeTemplate(ledge);

        if (GetBoolProperty(properties, "solid"))
            return "tile/wall";

        if (GetIntProperty(properties, "encounter_rate") > 0)
            return "tile/grass";

        return "tile/ground";
    }
}

// New: PokeSharp.Rendering/Loaders/PropertyMappers/ObjectPropertyMapper.cs
public class ObjectPropertyMapper
{
    public Direction ParseDirection(Dictionary<string, object> properties)
    {
        // Extracted from lines 584-597
    }

    public Npc? ParseNpcData(Dictionary<string, object> properties)
    {
        // Extracted from lines 603-623
    }

    public MovementRoute? ParseWaypoints(Dictionary<string, object> properties)
    {
        // Extracted from lines 626-666
    }
}
```

**Benefits:**
- Separation of Concerns
- Easier to add new property types
- Testable in isolation
- Reusable across different loaders

**Estimated Effort:** 6 hours

---

### 5.2 Interface Extraction Opportunities

#### 5.2.1 IAssetProvider (Already discussed in 1.2)
**Priority:** P1
**Effort:** 3 hours

#### 5.2.2 IMapLoader
**Current:** `MapLoader` is a concrete class with no interface

**Recommendation:**
```csharp
public interface IMapLoader
{
    Entity LoadMapEntities(World world, string mapPath);
    int GetMapIdByName(string mapName);
}

public class MapLoader : IMapLoader { /* ... */ }
```

**Benefits:**
- Enables alternative implementations (e.g., async loading, streaming)
- Makes MapLoader mockable for tests
- Follows Interface Segregation Principle

**Estimated Effort:** 2 hours

---

#### 5.2.3 ITiledMapLoader
**Current:** `TiledMapLoader` is static (no interface possible)

**Recommendation:**
```csharp
public interface ITiledMapLoader
{
    TmxDocument Load(string mapPath);
}

public class TiledJsonMapLoader : ITiledMapLoader
{
    public TmxDocument Load(string mapPath)
    {
        // Current implementation (lines 29-49 in TiledMapLoader.cs)
    }
}
```

**Benefits:**
- Enables XML support (Tiled XML format)
- Allows custom map formats
- Testable with mock map data

**Estimated Effort:** 3 hours

---

#### 5.2.4 IEntityFactory (Already exists ✅)
**Status:** Already implemented in Phase 1
**Location:** `IEntityFactoryService`
**Quality:** ✅ Good abstraction

---

### 5.3 Architecture Improvements Summary

| Improvement | Type | Priority | Effort | Impact |
|-------------|------|----------|--------|--------|
| **Property Mappers** | New Classes | P2 | 6h | Medium |
| **IAssetProvider** | Interface | P1 | 3h | High |
| **IMapLoader** | Interface | P2 | 2h | Medium |
| **ITiledMapLoader** | Interface | P3 | 3h | Low |
| **Renderer Split** | Refactoring | P3 | 12h | High (long-term) |

**Total P1 Effort:** 3 hours
**Total P1+P2 Effort:** 11 hours

---

## 6. PHASE 3 IMPLEMENTATION ROADMAP

### 6.1 Phase 3A: Test Infrastructure (Priority 1 - Week 1)

**Goal:** Get 38 failing tests to pass

**Tasks:**
1. ✅ **Fix file path resolution** (2 hours)
   - Update `TiledMapLoader.Load()` with path resolution helper
   - OR add MSBuild copy rules for test data
   - Verify 32 tests pass

2. ✅ **Create IAssetProvider interface** (3 hours)
   - Extract interface from AssetManager
   - Update MapLoader to accept interface
   - Update StubAssetManager to implement interface
   - Verify 6 integration tests pass

3. ✅ **Verify all tests pass** (1 hour)
   - Run full test suite
   - Fix any remaining issues
   - Target: 44/46 tests passing (96%)

**Total Effort:** 6 hours
**Success Criteria:** ≥95% test pass rate

---

### 6.2 Phase 3B: Remove Hardcoded Values (Priority 1 - Week 1)

**Goal:** Eliminate all critical hardcoded values

**Tasks:**
1. ✅ **Fix 256x256 image fallback** (1 hour)
   - Load texture to get actual dimensions
   - OR throw error if dimensions missing
   - Test with various tileset sizes

2. ✅ **Make Assets root path configurable** (30 minutes)
   - Add constructor parameter
   - Update all MapLoader instantiations
   - Document configuration option

3. ✅ **Extract magic number constants** (30 minutes)
   - MS_TO_SECONDS constant
   - Any other discovered magic numbers

**Total Effort:** 2 hours
**Success Criteria:** Zero magic numbers in P1 code

---

### 6.3 Phase 3C: Code Quality Refactoring (Priority 2 - Week 2)

**Goal:** Reduce duplication and improve maintainability

**Tasks:**
1. ✅ **Refactor duplicated tile rendering** (3 hours)
   - Extract common rendering logic
   - Test both offset and non-offset rendering
   - Verify performance unchanged

2. ✅ **Split CreateTileEntity method** (4 hours)
   - Extract property parsing
   - Extract template creation
   - Extract component attachment
   - Add unit tests for each piece

3. ✅ **Create property mapper classes** (6 hours)
   - ITilePropertyMapper interface
   - TilePropertyMapper implementation
   - ObjectPropertyMapper implementation
   - Comprehensive unit tests

**Total Effort:** 13 hours
**Success Criteria:** Cyclomatic complexity <10 for all methods

---

### 6.4 Phase 3D: Expand Test Coverage (Priority 3 - Week 3)

**Goal:** Reach 50%+ code coverage

**Tasks:**
1. ✅ **Add System tests** (12 hours)
   - ZOrderRenderSystem (10 tests)
   - AnimationSystem (5 tests)
   - CameraFollowSystem (5 tests)

2. ✅ **Add Loader edge case tests** (10 hours)
   - TiledMapLoader error handling (8 tests)
   - MapLoader edge cases (10 tests)

3. ✅ **Add Component tests** (5 hours)
   - TileSprite tests
   - AnimatedTile tests
   - LayerOffset tests (expand existing)

**Total Effort:** 27 hours
**Success Criteria:** >50% code coverage, >95% test pass rate

---

### 6.5 Phase 3E: Architecture Improvements (Priority 3 - Week 4)

**Goal:** Prepare for Phase 4 features

**Tasks:**
1. ✅ **Extract mapper interfaces** (6 hours)
   - Already covered in 6.3

2. ✅ **Create IMapLoader interface** (2 hours)
   - Extract interface
   - Update usages
   - Document interface contracts

3. ✅ **Refactor ZOrderRenderSystem** (12 hours)
   - Split into focused renderers
   - Maintain performance
   - Comprehensive testing

**Total Effort:** 14 hours (8 hours if ZOrderRenderSystem split is deferred)
**Success Criteria:** All public APIs have interfaces

---

## 7. ESTIMATED EFFORT SUMMARY

### 7.1 By Priority

| Priority | Category | Effort | Impact |
|----------|----------|--------|--------|
| **P1** | Test Infrastructure | 6h | Critical - unblocks all tests |
| **P1** | Hardcoded Values | 2h | Critical - fixes bugs |
| **P2** | Code Quality | 13h | High - reduces tech debt |
| **P3** | Test Coverage | 27h | Medium - improves confidence |
| **P3** | Architecture | 8h | Medium - enables Phase 4 |
| **TOTAL** | | **56 hours** | **7 work days** |

### 7.2 By Week

| Week | Tasks | Effort | Deliverables |
|------|-------|--------|--------------|
| **Week 1** | Test fixes + Hardcoded values | 8h | 44/46 tests passing, zero magic numbers |
| **Week 2** | Code quality refactoring | 13h | Clean code, low complexity |
| **Week 3** | Test coverage expansion | 27h | 50%+ coverage, 84 tests |
| **Week 4** | Architecture improvements | 8h | Interface-based design |

### 7.3 Minimum Viable Phase 3 (P1 Only)

**If time is constrained, focus on Priority 1:**

1. Fix test infrastructure (6 hours)
2. Remove hardcoded values (2 hours)

**Total:** 8 hours (1 work day)
**Outcome:** Tests passing, critical bugs fixed, ready for Phase 4

---

## 8. RISKS & MITIGATION

### 8.1 Breaking Changes

**Risk:** IAssetProvider interface breaks existing code

**Mitigation:**
- Make change incrementally (add interface, keep class)
- Update all usages in same commit
- Add deprecation warnings if needed
- Comprehensive regression testing

---

### 8.2 Test Data Maintenance

**Risk:** Test data files get out of sync with code

**Mitigation:**
- Centralize test data creation
- Document test data format
- Add validation tests for test data
- Consider generating test data programmatically

---

### 8.3 Performance Regression

**Risk:** Refactoring ZOrderRenderSystem impacts frame rate

**Mitigation:**
- Benchmark before refactoring (baseline)
- Benchmark after refactoring (comparison)
- Keep profiling enabled
- Use BenchmarkDotNet for micro-benchmarks
- Defer if performance impact >5%

---

### 8.4 Scope Creep

**Risk:** Phase 3 expands beyond 56 hours

**Mitigation:**
- Strict prioritization (P1 first, P2 if time, P3 optional)
- Time-box each task
- Focus on MVP (P1 only: 8 hours)
- Defer ZOrderRenderSystem split to Phase 4 if needed

---

## 9. SUCCESS CRITERIA

### 9.1 Must Have (P1)

✅ **Test Pass Rate:** ≥95% (44/46 tests passing)
✅ **Zero Magic Numbers:** No hardcoded 256, "Assets", etc. in P1 code
✅ **IAssetProvider:** Interface extracted and implemented
✅ **File Path Resolution:** Tests find data files correctly

---

### 9.2 Should Have (P2)

✅ **Code Duplication:** <5% duplicated code in rendering logic
✅ **Method Complexity:** All methods have cyclomatic complexity <10
✅ **Property Mappers:** Extracted into dedicated classes
✅ **Test Coverage:** ≥40% code coverage

---

### 9.3 Nice to Have (P3)

✅ **Test Coverage:** ≥50% code coverage
✅ **Architecture:** All public APIs have interfaces
✅ **ZOrderRenderSystem:** Split into focused renderers
✅ **Documentation:** All interfaces documented with XML comments

---

## 10. NEXT STEPS

### 10.1 Immediate Actions (This Week)

1. **Review this research** with stakeholders
2. **Approve Phase 3A scope** (test infrastructure + hardcoded values)
3. **Allocate 8 hours** for P1 tasks
4. **Create feature branch** `phase-3-quality-improvements`
5. **Begin implementation** of test fixes

---

### 10.2 Decision Points

**Decision 1:** Should we defer ZOrderRenderSystem split to Phase 4?
- **Pro:** Saves 12 hours, high-risk refactoring
- **Con:** Tech debt remains, harder to test

**Decision 2:** Should we use Option A (path resolution) or Option B (MSBuild copy) for test files?
- **Recommendation:** Option A (more robust, works in all environments)

**Decision 3:** Should we expand TileLayer enum now or wait for Phase 4?
- **Recommendation:** Wait (not critical, tests can use Ground/Object/Overhead)

---

## APPENDIX A: FILE LOCATIONS

### Production Files
- **MapLoader:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Rendering/Loaders/MapLoader.cs`
- **TiledMapLoader:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Rendering/Loaders/TiledMapLoader.cs`
- **ZOrderRenderSystem:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Rendering/Systems/ZOrderRenderSystem.cs`
- **AssetManager:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Rendering/Assets/AssetManager.cs`

### Test Files
- **Test Data:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Tests/TestData/*.json`
- **MapLoaderIntegrationTests:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Tests/Loaders/MapLoaderIntegrationTests.cs`
- **LayerOffsetTests:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Tests/Loaders/LayerOffsetTests.cs`
- **ImageLayerTests:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Tests/Loaders/ImageLayerTests.cs`

### Reports
- **Phase 2 Report:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/docs/analysis/PHASE-2-COMPLETION-REPORT.md`
- **Hardcoded Values Report:** `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/docs/analysis/hardcoded-values-report.md`

---

**End of Phase 3 Research Report**
**Generated:** 2025-11-08
**Total Analysis Time:** 4 hours
**Confidence Level:** 95%
**Recommendation:** Proceed with Phase 3A (P1 tasks) immediately
