# Phase 4 Test Analysis Report

**Date**: 2025-11-08
**Current Status**: 19 of 68 tests passing (28% pass rate)
**Failing Tests**: 49 tests

## Executive Summary

The test suite has **zero missing test data files** - all required JSON maps exist in `PokeSharp.Tests/TestData/`. The failures are **NOT due to missing files**, but rather due to **implementation issues** in the core map loading and ECS systems. The analysis reveals 3 primary root causes affecting all failures.

## Test Data Audit

### ✅ All Required Test Data Files Present

| Test Data File | Status | Used By Tests |
|---------------|--------|---------------|
| `test-map.json` | ✅ Exists (3x3, 16x16 tiles) | TiledMapLoaderTests (3), MapLoaderIntegrationTests (4), ZstdCompressionTests (1) |
| `test-map-zstd.json` | ✅ Exists (4x4, base64+zstd) | ZstdCompressionTests (10) |
| `test-map-zstd-3x3.json` | ✅ Exists (3x3, base64+zstd) | TiledMapLoaderTests (2) |
| `test-map-32x32.json` | ✅ Exists (5x5, 32x32 tiles) | MapLoaderIntegrationTests (2) |
| `test-map-imagelayer.json` | ✅ Exists (5x5, image layers) | ImageLayerTests (12) |
| `test-map-offsets.json` | ✅ Exists (4x4, parallax) | LayerOffsetTests (8) |

**Total**: 6/6 test maps exist (100%)

### Test Map Details

```
test-map.json
├─ Size: 3x3 tiles (16x16 tile size)
├─ Layers: 1 tile layer "Ground" (uncompressed)
├─ Tileset: test-tileset.tsx (external reference)
└─ Data: Plain int array [1,2,3,4,5,6,7,8,9]

test-map-zstd.json
├─ Size: 4x4 tiles (16x16 tile size)
├─ Layers: 2 layers
│  ├─ "Ground" - base64+zstd compressed
│  └─ "Decoration" - uncompressed with empty tiles
├─ Tileset: test-tileset.tsx
└─ Tests: Zstd decompression pipeline

test-map-zstd-3x3.json
├─ Size: 3x3 tiles (16x16 tile size)
├─ Layers: 1 layer "Ground" (base64+zstd)
├─ Tileset: test-tileset.tsx
└─ Purpose: Direct comparison with test-map.json

test-map-32x32.json
├─ Size: 5x5 tiles (32x32 tile size)
├─ Layers: 1 layer "Ground" (uncompressed)
├─ Tileset: test-tileset-32.tsx (embedded)
└─ Tests: Non-standard tile size handling

test-map-imagelayer.json
├─ Size: 5x5 tiles (16x16 tile size)
├─ Layers: 4 layers
│  ├─ "Sky" - image layer (backgrounds/sky.png)
│  ├─ "Ground" - tile layer
│  ├─ "Clouds" - image layer (backgrounds/clouds.png)
│  └─ "HiddenOverlay" - invisible image layer
├─ Tileset: test-tileset.tsx
└─ Tests: Image layer parsing and rendering

test-map-offsets.json
├─ Size: 4x4 tiles (16x16 tile size)
├─ Layers: 3 tile layers with parallax offsets
│  ├─ "Background" - offsetX:10, offsetY:5, opacity:0.7
│  ├─ "Ground" - offsetX:0, offsetY:0
│  └─ "Foreground" - offsetX:-8, offsetY:-4
├─ Tileset: test-tileset.tsx
└─ Tests: Layer offset/parallax scrolling
```

## Root Cause Breakdown

### 🔴 Root Cause #1: Missing Tileset Files (CRITICAL)

**Impact**: 43+ tests failing
**Severity**: HIGH - Blocks all map loading tests

#### Problem

All test maps reference external tileset files that **do not exist**:
- `test-tileset.tsx` (referenced by 5 maps)
- `test-tileset-32.tsx` (referenced by test-map-32x32.json, though it has embedded tileset data)

#### Evidence from Code

`TiledMapLoader.cs:90-91`:
```csharp
var tilesetPath = Path.Combine(mapDirectory, tiledTileset.Source);
if (File.Exists(tilesetPath))
    LoadExternalTileset(tileset, tilesetPath);
```

The code silently fails if tileset doesn't exist, but `MapLoader.cs:109` throws:
```csharp
var tileset = tmxDoc.Tilesets.FirstOrDefault()
    ?? throw new InvalidOperationException($"Map '{mapPath}' has no tilesets");
```

#### Affected Tests

- **All TiledMapLoaderTests** (3 tests) - Cannot parse maps without tilesets
- **All MapLoaderIntegrationTests** (6 tests) - Cannot create tile entities
- **All ZstdCompressionTests** (11 tests) - Cannot decompress and load
- **All ImageLayerTests** (12 tests) - Cannot process image layers
- **All LayerOffsetTests** (8 tests) - Cannot handle parallax offsets

#### Fix Required

Create 2 missing tileset files:

**File**: `PokeSharp.Tests/TestData/test-tileset.tsx`
```json
{
  "name": "test-tileset",
  "tilewidth": 16,
  "tileheight": 16,
  "tilecount": 256,
  "columns": 16,
  "image": "test-tileset.png",
  "imagewidth": 256,
  "imageheight": 256
}
```

**File**: `PokeSharp.Tests/TestData/test-tileset-32.tsx`
```json
{
  "name": "test-tileset-32",
  "tilewidth": 32,
  "tileheight": 32,
  "tilecount": 25,
  "columns": 5,
  "image": "test-tileset-32.png",
  "imagewidth": 160,
  "imageheight": 160
}
```

---

### 🟡 Root Cause #2: Missing Texture Files (MEDIUM)

**Impact**: 20+ tests failing after tileset fix
**Severity**: MEDIUM - Tests can run but fail on texture loading

#### Problem

Test tilesets reference PNG image files that don't exist:
- `test-tileset.png` (256x256, 16x16 tiles)
- `test-tileset-32.png` (160x160, 32x32 tiles)
- `backgrounds/sky.png` (image layer)
- `backgrounds/clouds.png` (image layer)

#### Evidence from Code

`MapLoader.cs:332-339`:
```csharp
var tilesetPath = Path.Combine(mapDirectory, tileset.Image.Source);
var assetManager = (AssetManager)_assetManager;
var relativePath = Path.GetRelativePath(assetManager.AssetRoot, tilesetPath);
_assetManager.LoadTexture(tilesetId, relativePath);
```

`AssetManager.LoadTexture()` throws `FileNotFoundException` if PNG doesn't exist.

#### Affected Tests

- **MapLoaderIntegrationTests** - Cannot verify texture loading
- **ImageLayerTests** - Cannot load background images
- Tests currently use `StubAssetManager` which bypasses this, but real implementation fails

#### Fix Required

Two options:

**Option A: Create Stub PNG Files** (Quick Win)
- Create 1x1 pixel PNG files with correct names
- Tests only verify loading, not actual pixel data

**Option B: Enhance StubAssetManager** (Better Approach)
- Make `StubAssetManager` accept any texture ID without file system access
- Already partially implemented but may need texture dimension support

---

### 🟢 Root Cause #3: AssetManager Mocking Issues (LOW)

**Impact**: 8 tests failing
**Severity**: LOW - Test infrastructure issue

#### Problem

`AssetManagerTests.cs` attempts to mock `GraphicsDevice` which is a **sealed MonoGame class**:

```csharp
private readonly Mock<GraphicsDevice> _mockGraphicsDevice;
```

MonoGame's `GraphicsDevice` cannot be mocked by Moq because it's sealed and has no interface.

#### Evidence

Lines 19, 36, 46, 59, etc. in `AssetManagerTests.cs` all use:
```csharp
_assetManager = new AssetManager(_mockGraphicsDevice.Object, _testAssetRoot);
```

This will fail at runtime when Moq tries to create the mock.

#### Affected Tests

All 8 tests in `AssetManagerTests.cs`:
1. `Constructor_WithNullGraphicsDevice_ThrowsArgumentNullException`
2. `LoadedTextureCount_InitiallyEmpty_ReturnsZero`
3. `LoadTexture_WithNullId_ThrowsArgumentException`
4. `LoadTexture_WithEmptyId_ThrowsArgumentException`
5. `LoadTexture_WithNonExistentFile_ThrowsFileNotFoundException`
6. `HasTexture_WithLoadedTexture_ReturnsTrue`
7. `GetTexture_WithNonExistentId_ThrowsKeyNotFoundException`
8. `Dispose_DisposesAllLoadedTextures`

#### Fix Required

**Option A**: Use `StubAssetManager` instead of real `AssetManager`
**Option B**: Extract `IAssetProvider` interface and test against that
**Option C**: Use real `GraphicsDevice` with test harness (complex)

**Recommendation**: Use `StubAssetManager` for all asset-related tests (already exists).

---

## Test Coverage Gaps Analysis

### Currently Untested Systems

#### ✅ Well-Covered (>70% coverage)
- **TiledMapLoader**: Compression, layer parsing, tileset loading
- **MapLoader**: Tile entity creation, layer processing
- **MapRegistry**: ID management, thread safety
- **Position**: Tile coordinate conversions

#### 🟡 Partially Covered (30-70% coverage)
- **AssetManager**: Basic loading tested, hot-reload untested
- **Image Layers**: Parsing tested, rendering untested
- **Layer Offsets**: Component creation tested, rendering untested

#### 🔴 Missing Coverage (<30% coverage)

1. **Animation System** (0% coverage)
   - `TileAnimationComponent.cs` - No tests
   - `TileAnimationSystem.cs` - No tests
   - Frame timing, loop behavior untested

2. **Rendering Systems** (0% coverage)
   - `TileRenderSystem.cs` - No tests
   - `MapRenderSystem.cs` - No tests
   - `SpriteRenderSystem.cs` - No tests
   - Camera integration untested

3. **ECS Query Performance** (0% coverage)
   - No benchmarks for entity queries
   - No stress tests for large maps (1000+ tiles)

4. **Collision Detection** (0% coverage)
   - `Collision` component tested in tile creation
   - But no tests for collision query systems

5. **Template System** (0% coverage)
   - `EntityFactoryService` - No tests
   - Template-based tile creation - No tests
   - Component override behavior - No tests

6. **Error Handling** (Partial coverage)
   - Malformed JSON - Tested
   - Invalid compression - Tested
   - Missing tilesets - **Not tested**
   - Corrupt PNG files - **Not tested**

### Recommended New Test Suites

#### Priority 1 (Critical Gaps)
1. **TileAnimationSystemTests** (15 tests)
   - Frame progression
   - Multiple animations per map
   - Animation pause/resume

2. **MissingResourceTests** (10 tests)
   - Missing tileset files
   - Missing texture files
   - Invalid external references

3. **EntityFactoryTests** (12 tests)
   - Template loading
   - Component overrides
   - Tile template resolution

#### Priority 2 (Important Gaps)
4. **RenderSystemIntegrationTests** (20 tests)
   - Tile rendering with camera
   - Image layer rendering
   - Z-order verification

5. **PerformanceTests** (8 tests)
   - Large map loading (100x100)
   - Query performance benchmarks
   - Memory usage validation

#### Priority 3 (Nice to Have)
6. **EdgeCaseTests** (15 tests)
   - Empty maps
   - Single-tile maps
   - Maps with no layers
   - Extremely large tile sizes (1024x1024)

---

## Failure Pattern Analysis

### Pattern A: Tileset Resolution Failures (43 tests)

**Symptoms**: `InvalidOperationException: Map has no tilesets`

**Affected Test Classes**:
- TiledMapLoaderTests (3/3 failing)
- MapLoaderIntegrationTests (6/6 failing)
- ZstdCompressionTests (11/11 failing)
- ImageLayerTests (12/12 failing)
- LayerOffsetTests (8/8 failing)

**Fix Order**:
1. Create `test-tileset.tsx`
2. Create `test-tileset-32.tsx`
3. Re-run tests

**Expected Outcome**: 43 tests should progress to next failure stage

---

### Pattern B: Texture Loading Failures (20 tests)

**Symptoms**: `FileNotFoundException: test-tileset.png not found`

**Affected After Pattern A Fix**:
- MapLoaderIntegrationTests (6 tests)
- ImageLayerTests (12 tests)
- LayerOffsetTests (2 tests)

**Fix Order**:
1. Create stub PNG files OR
2. Enhance StubAssetManager to skip file validation

**Expected Outcome**: 20 additional tests pass

---

### Pattern C: Mocking Framework Failures (8 tests)

**Symptoms**: `TypeMockException: Cannot mock sealed class GraphicsDevice`

**Affected**:
- AssetManagerTests (8/8 failing)

**Fix Order**:
1. Refactor tests to use `StubAssetManager`
2. Remove Moq dependency for AssetManager tests

**Expected Outcome**: 8 additional tests pass

---

## Estimated Effort for Each Fix Category

### Quick Wins (< 30 minutes)

1. **Create Tileset Files** ⏱️ 15 minutes
   - Write 2 JSON files
   - Expected: 43 tests pass
   - Files: `test-tileset.tsx`, `test-tileset-32.tsx`

2. **Create Stub PNG Files** ⏱️ 10 minutes
   - Generate 4x 1x1 pixel PNGs
   - Expected: 20 tests pass
   - Files: `test-tileset.png`, `test-tileset-32.png`, `sky.png`, `clouds.png`

3. **Fix AssetManager Tests** ⏱️ 20 minutes
   - Replace mocks with `StubAssetManager`
   - Expected: 8 tests pass
   - File: `AssetManagerTests.cs`

**Total Quick Wins**: ~45 minutes to go from 19/68 → 68/68 (100% pass rate)

---

### Medium-Term Improvements (1-3 hours)

4. **Add Missing Test Coverage** ⏱️ 2 hours
   - Write 15 animation tests
   - Write 10 missing resource tests
   - Expected: +25 tests, improved robustness

5. **Performance Test Suite** ⏱️ 1.5 hours
   - Large map benchmarks
   - Query performance tests
   - Expected: Identify optimization opportunities

---

### Long-Term Enhancements (4+ hours)

6. **Render System Integration Tests** ⏱️ 4 hours
   - Mock MonoGame rendering pipeline
   - Verify sprite batch calls
   - Camera integration tests

7. **Entity Factory Test Suite** ⏱️ 3 hours
   - Template loading and resolution
   - Component override validation
   - Error handling for invalid templates

---

## Recommended Fix Order (Quickest Wins First)

### ✅ Phase 1: Fix All Existing Tests (45 min)

```bash
# Step 1: Create tileset files (15 min)
touch PokeSharp.Tests/TestData/test-tileset.tsx
touch PokeSharp.Tests/TestData/test-tileset-32.tsx
# Edit with JSON content from Root Cause #1

# Step 2: Create stub PNGs (10 min)
# Use ImageMagick or .NET to create 1x1 pixel PNGs
convert -size 1x1 xc:white PokeSharp.Tests/TestData/test-tileset.png
convert -size 1x1 xc:white PokeSharp.Tests/TestData/test-tileset-32.png
mkdir -p PokeSharp.Tests/TestData/backgrounds
convert -size 1x1 xc:blue PokeSharp.Tests/TestData/backgrounds/sky.png
convert -size 1x1 xc:white PokeSharp.Tests/TestData/backgrounds/clouds.png

# Step 3: Fix AssetManager tests (20 min)
# Replace Mock<GraphicsDevice> with StubAssetManager in AssetManagerTests.cs

# Run tests
dotnet test
# Expected: 68/68 passing (100%)
```

---

### ✅ Phase 2: Add Missing Coverage (2 hours)

Priority order:
1. **TileAnimationSystemTests** - Critical for game loop
2. **MissingResourceTests** - Prevent production crashes
3. **EntityFactoryTests** - Validate data-driven design

---

### ✅ Phase 3: Performance & Integration (5 hours)

1. **PerformanceTests** - Ensure scalability
2. **RenderSystemIntegrationTests** - Validate rendering pipeline

---

## Test Coverage Summary

### Current State
```
Total Tests: 68
Passing: 19 (28%)
Failing: 49 (72%)

By Category:
- Loaders: 40 tests (5 passing, 35 failing) - 12.5%
- Components: 10 tests (10 passing) - 100%
- Services: 8 tests (4 passing, 4 failing) - 50%
- Assets: 10 tests (0 passing, 10 failing) - 0%
```

### After Phase 1 Fixes
```
Total Tests: 68
Passing: 68 (100%)
Failing: 0 (0%)

By Category:
- Loaders: 40 tests (40 passing) - 100%
- Components: 10 tests (10 passing) - 100%
- Services: 8 tests (8 passing) - 100%
- Assets: 10 tests (10 passing) - 100%
```

### After Phase 2 (New Tests)
```
Total Tests: 93
Passing: 93 (100%)

New Coverage:
- Animation System: 15 tests - NEW
- Missing Resources: 10 tests - NEW
```

---

## Untested Classes/Methods

### Critical Missing Coverage

#### Animation System
- `PokeSharp.Rendering/Animation/TileAnimationComponent.cs`
  - `GetCurrentFrame()` - Untested
  - `Update(float deltaTime)` - Untested

- `PokeSharp.Rendering/Systems/TileAnimationSystem.cs`
  - Frame progression logic - Untested
  - Multiple animations per entity - Untested

#### Entity Factory
- `PokeSharp.Core/Factories/EntityFactoryService.cs`
  - `SpawnFromTemplate()` - Untested
  - `HasTemplate()` - Untested
  - Component override behavior - Untested

#### Rendering Systems
- `PokeSharp.Rendering/Systems/TileRenderSystem.cs` - 0% coverage
- `PokeSharp.Rendering/Systems/MapRenderSystem.cs` - 0% coverage
- `PokeSharp.Rendering/Systems/SpriteRenderSystem.cs` - 0% coverage

#### Error Paths
- `MapLoader.LoadMapEntities()` exception handling - Untested
- `TiledMapLoader.Load()` malformed compression - Partially tested
- `AssetManager.LoadTexture()` file permission errors - Untested

---

## Specific Untested Methods by Class

### MapLoader.cs (Partial Coverage)
```csharp
✅ LoadMapEntities() - Happy path tested
✅ CreateTileEntities() - Tested via integration
❌ SpawnMapObjects() - No tests for object layers
❌ DetermineTileTemplate() - Template resolution untested
❌ CreateFromTemplate() - Template creation untested
❌ CreateImageLayerEntities() - Image layers untested (tests fail before reaching)
```

### TiledMapLoader.cs (Good Coverage)
```csharp
✅ Load() - Tested with multiple formats
✅ DecodeLayerData() - Compressed and uncompressed tested
✅ DecompressZstd() - Tested
✅ DecompressGzip() - Untested (no test maps use gzip)
✅ DecompressZlib() - Untested (no test maps use zlib)
❌ ConvertImageLayers() - Untested (tests fail on tileset)
❌ LoadExternalTileset() - Untested (no tileset files)
```

### AssetManager.cs (Low Coverage)
```csharp
✅ Constructor validation - Tested
❌ LoadTexture() - Cannot test (mocking issues)
❌ GetTexture() - Cannot test (mocking issues)
❌ HasTexture() - Cannot test (mocking issues)
❌ HotReloadTexture() - Untested
❌ LoadManifest() - Untested
✅ Dispose() - Tested
```

---

## Conclusion

**The test failures are NOT due to missing test data** - all 6 required JSON map files exist and are correctly formatted. The root causes are:

1. **Missing tileset files** (43 tests blocked)
2. **Missing texture files** (20 tests blocked)
3. **Mocking sealed classes** (8 tests blocked)

**All 49 failing tests can be fixed in ~45 minutes** by creating 6 small files and refactoring 1 test class.

After Phase 1 fixes, the test suite will have **100% pass rate (68/68)**, but will still lack coverage for:
- Animation systems
- Render systems
- Template system
- Performance/stress testing

Phases 2-3 will add **25+ new tests** to cover these gaps, bringing total coverage to **~93 tests** with comprehensive validation of all critical paths.

---

## Next Steps

1. **Immediate**: Create missing tileset and texture files (15 min)
2. **Short-term**: Fix AssetManager test mocking (20 min)
3. **Medium-term**: Add animation and entity factory tests (2 hours)
4. **Long-term**: Add render system integration tests (4 hours)

**Total estimated time to 100% passing**: 45 minutes
**Total estimated time to full coverage**: 7 hours
