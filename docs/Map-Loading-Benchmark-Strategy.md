# Map Loading Performance Benchmark & Validation Strategy

## Executive Summary

**Mission**: Design comprehensive benchmarking framework to measure and validate map loading performance improvements from current baseline (20s load time, 712 MB memory) to target (<5s, <300 MB).

**Current Performance Baseline**:
- Total map load time: **20 seconds**
- Peak memory usage: **712 MB**
- Entity count: Unknown (needs profiling)
- Texture memory: Unknown (needs profiling)

**Target Performance Goals**:
- Total map load time: **<5 seconds** (75% reduction)
- Peak memory usage: **<300 MB** (58% reduction)
- Maintain 60 FPS during loading
- No memory leaks or fragmentation

---

## 1. Benchmark Suite Design

### 1.1 Test Map Categories

Create standardized test maps of varying complexity:

```csharp
// Test Maps
public enum BenchmarkMapSize
{
    Tiny,      // 10x10 tiles  - baseline performance
    Small,     // 32x32 tiles  - typical room/building
    Medium,    // 64x64 tiles  - typical outdoor area
    Large,     // 128x128 tiles - full map like Littleroot Town
    Huge,      // 256x256 tiles - stress test
    Extreme    // 512x512 tiles - maximum scale test
}

public class BenchmarkMapProfile
{
    public string MapName { get; set; }
    public BenchmarkMapSize Size { get; set; }
    public int ExpectedTileCount { get; set; }
    public int ExpectedNpcCount { get; set; }
    public int ExpectedTilesetCount { get; set; }
    public int ExpectedAnimatedTiles { get; set; }
    public bool HasCompression { get; set; }
    public string CompressionType { get; set; } // "none", "gzip", "zlib", "zstd"
}
```

**Standard Test Maps**:
1. **Tiny_Empty** - 10x10, no NPCs, 1 tileset (baseline)
2. **Small_Simple** - 32x32, 2 NPCs, 1 tileset (simple room)
3. **Medium_Standard** - 64x64, 10 NPCs, 2 tilesets (typical map)
4. **Large_Complex** - 128x128, 30 NPCs, 3 tilesets, animations (Littleroot Town equivalent)
5. **Huge_Stress** - 256x256, 50 NPCs, 5 tilesets, many animations (stress test)
6. **Extreme_Max** - 512x512, 100 NPCs, 10 tilesets (maximum scale)

### 1.2 Benchmark Phases

```csharp
public enum LoadingPhase
{
    FileRead,              // File.ReadAllText
    JsonDeserialization,   // JsonSerializer.Deserialize
    Decompression,         // GZip/Zlib/Zstd decompression
    TilesetLoading,        // Texture loading and parsing
    ExternalTilesetLoad,   // External tileset file loading
    TileAnimationParsing,  // Animation frame parsing
    LayerProcessing,       // Layer iteration and decoding
    TileEntityCreation,    // Entity spawning (bulk operations)
    ComponentAddition,     // Adding components (Position, Sprite, etc.)
    PropertyMapping,       // Tile property processing
    NpcSpawning,           // Object group entity spawning
    SpatialHashRebuild,    // Spatial hash invalidation/rebuild
    TextureMemoryAlloc,    // GPU texture memory allocation
    Total                  // End-to-end loading time
}
```

---

## 2. Detailed Stage Profiling

### 2.1 Instrumentation Points

**File: TiledMapLoader.cs**
```csharp
// Line 60: File reading
var stopwatch = Stopwatch.StartNew();
var json = File.ReadAllText(mapPath);
metrics.FileReadTime = stopwatch.ElapsedMilliseconds;

// Line 62: JSON deserialization
stopwatch.Restart();
var tiledMap = JsonSerializer.Deserialize<TiledJsonMap>(json, JsonOptions);
metrics.JsonDeserializationTime = stopwatch.ElapsedMilliseconds;

// Line 313: Decompression
stopwatch.Restart();
bytes = DecompressBytes(bytes, layer.Compression);
metrics.DecompressionTime += stopwatch.ElapsedMilliseconds;
```

**File: MapLoader.cs**
```csharp
// Line 272: Tileset loading
stopwatch.Restart();
var loadedTilesets = LoadTilesetsInternal(tmxDoc, mapPath);
metrics.TilesetLoadingTime = stopwatch.ElapsedMilliseconds;

// Line 591: Layer processing
stopwatch.Restart();
var tilesCreated = ProcessLayers(world, tmxDoc, mapId, tilesets);
metrics.LayerProcessingTime = stopwatch.ElapsedMilliseconds;

// Line 625: Tile entity creation (bulk operations)
stopwatch.Restart();
var tileDataList = new List<TileData>();
// ... collect tile data ...
var tileEntities = bulkOps.CreateEntities(...);
metrics.BulkEntityCreationTime = stopwatch.ElapsedMilliseconds;

// Line 707: Component addition
stopwatch.Restart();
for (var i = 0; i < tileEntities.Length; i++) {
    // ... add components ...
}
metrics.ComponentAdditionTime = stopwatch.ElapsedMilliseconds;

// Line 1530: NPC spawning
stopwatch.Restart();
var created = SpawnMapObjects(world, tmxDoc, mapId, tileHeight);
metrics.NpcSpawningTime = stopwatch.ElapsedMilliseconds;

// Line 187: Spatial hash rebuild
stopwatch.Restart();
spatialHashSystem.InvalidateStaticTiles();
metrics.SpatialHashRebuildTime = stopwatch.ElapsedMilliseconds;
```

### 2.2 Performance Metrics Class

```csharp
public class MapLoadingMetrics
{
    // Timing metrics (milliseconds)
    public long FileReadTime { get; set; }
    public long JsonDeserializationTime { get; set; }
    public long DecompressionTime { get; set; }
    public long TilesetLoadingTime { get; set; }
    public long ExternalTilesetLoadTime { get; set; }
    public long TileAnimationParsingTime { get; set; }
    public long LayerProcessingTime { get; set; }
    public long BulkEntityCreationTime { get; set; }
    public long ComponentAdditionTime { get; set; }
    public long PropertyMappingTime { get; set; }
    public long NpcSpawningTime { get; set; }
    public long SpatialHashRebuildTime { get; set; }
    public long TextureMemoryAllocTime { get; set; }
    public long TotalLoadingTime { get; set; }

    // Memory metrics (bytes)
    public long MemoryBeforeLoading { get; set; }
    public long MemoryAfterLoading { get; set; }
    public long MemoryPeakDuringLoading { get; set; }
    public long HeapAllocations { get; set; }
    public long Gen0Collections { get; set; }
    public long Gen1Collections { get; set; }
    public long Gen2Collections { get; set; }

    // Entity metrics
    public int TilesCreated { get; set; }
    public int NpcsCreated { get; set; }
    public int ImageLayersCreated { get; set; }
    public int AnimatedTilesCreated { get; set; }
    public int TotalEntities { get; set; }

    // Resource metrics
    public int TexturesLoaded { get; set; }
    public long TotalTextureMemoryBytes { get; set; }
    public int TilesetsLoaded { get; set; }
    public int AnimationsLoaded { get; set; }

    // Compression metrics
    public long CompressedDataSize { get; set; }
    public long DecompressedDataSize { get; set; }
    public double CompressionRatio => CompressedDataSize > 0
        ? (double)DecompressedDataSize / CompressedDataSize
        : 1.0;

    // Performance ratios
    public double TilesPerSecond => TotalLoadingTime > 0
        ? (TilesCreated * 1000.0) / TotalLoadingTime
        : 0;

    public double MemoryPerTile => TilesCreated > 0
        ? (MemoryAfterLoading - MemoryBeforeLoading) / (double)TilesCreated
        : 0;

    // Helper: Get top 5 slowest phases
    public IEnumerable<(string Phase, long TimeMs)> GetSlowestPhases()
    {
        var phases = new Dictionary<string, long>
        {
            { "File Read", FileReadTime },
            { "JSON Deserialization", JsonDeserializationTime },
            { "Decompression", DecompressionTime },
            { "Tileset Loading", TilesetLoadingTime },
            { "Layer Processing", LayerProcessingTime },
            { "Bulk Entity Creation", BulkEntityCreationTime },
            { "Component Addition", ComponentAdditionTime },
            { "NPC Spawning", NpcSpawningTime },
            { "Spatial Hash Rebuild", SpatialHashRebuildTime }
        };

        return phases
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp => (kvp.Key, kvp.Value));
    }
}
```

---

## 3. Memory Profiling

### 3.1 Memory Tracking Points

```csharp
public class MemoryProfiler
{
    private long _baselineMemory;
    private long _peakMemory;
    private List<MemorySample> _samples = new();

    public void StartProfiling()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        _baselineMemory = GC.GetTotalMemory(false);
        _peakMemory = _baselineMemory;
        _samples.Clear();
    }

    public void Sample(string label)
    {
        var currentMemory = GC.GetTotalMemory(false);
        _peakMemory = Math.Max(_peakMemory, currentMemory);

        _samples.Add(new MemorySample
        {
            Label = label,
            Timestamp = DateTime.UtcNow,
            TotalMemory = currentMemory,
            DeltaFromBaseline = currentMemory - _baselineMemory,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        });
    }

    public MemoryProfile GetProfile()
    {
        return new MemoryProfile
        {
            BaselineMemory = _baselineMemory,
            PeakMemory = _peakMemory,
            FinalMemory = _samples.LastOrDefault()?.TotalMemory ?? _baselineMemory,
            Samples = _samples.ToList(),
            TotalIncrease = (_samples.LastOrDefault()?.TotalMemory ?? _baselineMemory) - _baselineMemory,
            PeakIncrease = _peakMemory - _baselineMemory
        };
    }
}

public class MemorySample
{
    public string Label { get; set; }
    public DateTime Timestamp { get; set; }
    public long TotalMemory { get; set; }
    public long DeltaFromBaseline { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}
```

### 3.2 Memory Leak Detection

```csharp
public class MemoryLeakDetector
{
    public void ValidateNoLeaks(int mapId, World world, IAssetProvider assets)
    {
        // 1. Load map
        var memBefore = GC.GetTotalMemory(false);
        LoadMap(mapId, world, assets);
        var memAfterLoad = GC.GetTotalMemory(false);

        // 2. Unload map
        UnloadMap(mapId, world, assets);

        // 3. Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memAfterUnload = GC.GetTotalMemory(false);

        // 4. Validate cleanup
        var leaked = memAfterUnload - memBefore;

        Assert.True(leaked < 1_000_000, // Allow <1MB variance
            $"Memory leak detected: {leaked:N0} bytes not freed");
    }
}
```

---

## 4. Performance Regression Tests

### 4.1 Benchmark Test Structure

```csharp
[Collection("Performance")]
public class MapLoadingBenchmarks
{
    [Theory]
    [InlineData("Tiny_Empty", 100)]      // 10x10, target <100ms
    [InlineData("Small_Simple", 500)]    // 32x32, target <500ms
    [InlineData("Medium_Standard", 2000)] // 64x64, target <2s
    [InlineData("Large_Complex", 5000)]  // 128x128, target <5s
    [InlineData("Huge_Stress", 15000)]   // 256x256, target <15s
    public void MapLoadingPerformance_MeetsTargets(string mapName, int maxLoadTimeMs)
    {
        // Arrange
        var world = World.Create();
        var assets = new AssetManager("Assets");
        var loader = new MapLoader(assets, ...);
        var profiler = new PerformanceProfiler();

        // Act
        profiler.Start();
        var mapEntity = loader.LoadMap(world, mapName);
        var metrics = profiler.Stop();

        // Assert
        Assert.True(metrics.TotalLoadingTime <= maxLoadTimeMs,
            $"Map {mapName} took {metrics.TotalLoadingTime}ms (max: {maxLoadTimeMs}ms)");

        // Log detailed breakdown
        Console.WriteLine($"\n=== {mapName} Performance ===");
        foreach (var (phase, time) in metrics.GetSlowestPhases())
        {
            var percentage = (time * 100.0) / metrics.TotalLoadingTime;
            Console.WriteLine($"{phase}: {time}ms ({percentage:F1}%)");
        }
    }

    [Theory]
    [InlineData("Tiny_Empty", 10_000_000)]      // <10 MB
    [InlineData("Small_Simple", 50_000_000)]    // <50 MB
    [InlineData("Medium_Standard", 150_000_000)] // <150 MB
    [InlineData("Large_Complex", 300_000_000)]  // <300 MB
    public void MapLoadingMemory_MeetsTargets(string mapName, long maxMemoryBytes)
    {
        // Arrange
        var world = World.Create();
        var assets = new AssetManager("Assets");
        var loader = new MapLoader(assets, ...);
        var memProfiler = new MemoryProfiler();

        // Act
        memProfiler.StartProfiling();
        var mapEntity = loader.LoadMap(world, mapName);
        memProfiler.Sample("After Loading");
        var profile = memProfiler.GetProfile();

        // Assert
        Assert.True(profile.PeakIncrease <= maxMemoryBytes,
            $"Map {mapName} used {profile.PeakIncrease:N0} bytes (max: {maxMemoryBytes:N0})");

        // Memory efficiency metrics
        var bytesPerTile = profile.TotalIncrease / (double)GetTileCount(mapName);
        Console.WriteLine($"\n=== {mapName} Memory ===");
        Console.WriteLine($"Total increase: {profile.TotalIncrease:N0} bytes");
        Console.WriteLine($"Peak increase: {profile.PeakIncrease:N0} bytes");
        Console.WriteLine($"Bytes per tile: {bytesPerTile:F2}");
    }
}
```

### 4.2 Compression Algorithm Comparison

```csharp
[Theory]
[InlineData("none")]
[InlineData("gzip")]
[InlineData("zlib")]
[InlineData("zstd")]
public void CompressionAlgorithm_PerformanceComparison(string compressionType)
{
    // Test same map with different compression algorithms
    var results = new List<CompressionResult>();

    foreach (var algo in new[] { "none", "gzip", "zlib", "zstd" })
    {
        var map = CreateTestMapWithCompression("Medium_Standard", algo);
        var profiler = new PerformanceProfiler();

        profiler.Start();
        LoadMap(map);
        var metrics = profiler.Stop();

        results.Add(new CompressionResult
        {
            Algorithm = algo,
            LoadTime = metrics.TotalLoadingTime,
            DecompressionTime = metrics.DecompressionTime,
            FileSize = GetFileSize(map),
            MemoryUsage = metrics.MemoryAfterLoading - metrics.MemoryBeforeLoading
        });
    }

    // Print comparison table
    PrintCompressionComparison(results);
}
```

---

## 5. Optimization Validation

### 5.1 Before/After Framework

```csharp
public class OptimizationValidator
{
    public ValidationResult ValidateOptimization(
        string optimizationName,
        Action beforeAction,
        Action afterAction,
        PerformanceBudget budget)
    {
        // Baseline (before optimization)
        var baselineMetrics = MeasurePerformance(beforeAction);

        // Optimized version
        var optimizedMetrics = MeasurePerformance(afterAction);

        // Calculate improvements
        var timeSavings = baselineMetrics.TotalLoadingTime - optimizedMetrics.TotalLoadingTime;
        var timeImprovement = (timeSavings * 100.0) / baselineMetrics.TotalLoadingTime;

        var memorySavings = baselineMetrics.PeakMemory - optimizedMetrics.PeakMemory;
        var memoryImprovement = (memorySavings * 100.0) / baselineMetrics.PeakMemory;

        return new ValidationResult
        {
            OptimizationName = optimizationName,
            BaselineTime = baselineMetrics.TotalLoadingTime,
            OptimizedTime = optimizedMetrics.TotalLoadingTime,
            TimeSavingsMs = timeSavings,
            TimeImprovementPercent = timeImprovement,
            BaselineMemory = baselineMetrics.PeakMemory,
            OptimizedMemory = optimizedMetrics.PeakMemory,
            MemorySavingsBytes = memorySavings,
            MemoryImprovementPercent = memoryImprovement,
            MeetsBudget = optimizedMetrics.TotalLoadingTime <= budget.MaxLoadTimeMs &&
                         optimizedMetrics.PeakMemory <= budget.MaxMemoryBytes,
            IsStatisticallySignificant = IsSignificant(baselineMetrics, optimizedMetrics)
        };
    }

    private bool IsSignificant(MapLoadingMetrics baseline, MapLoadingMetrics optimized)
    {
        // Require at least 5% improvement to be considered significant
        var improvement = ((baseline.TotalLoadingTime - optimized.TotalLoadingTime) * 100.0)
                         / baseline.TotalLoadingTime;
        return improvement >= 5.0;
    }
}

public class PerformanceBudget
{
    public long MaxLoadTimeMs { get; set; }
    public long MaxMemoryBytes { get; set; }
    public double MaxTilesPerSecond { get; set; }
    public double MaxMemoryPerTile { get; set; }
}
```

### 5.2 Statistical Testing

```csharp
public class StatisticalValidator
{
    public void ValidateConsistency(string mapName, int iterations = 10)
    {
        var samples = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            var metrics = MeasureMapLoad(mapName);
            samples.Add(metrics.TotalLoadingTime);
        }

        var mean = samples.Average();
        var stdDev = CalculateStdDev(samples, mean);
        var coefficientOfVariation = (stdDev / mean) * 100;

        // Performance should be consistent (<10% coefficient of variation)
        Assert.True(coefficientOfVariation < 10.0,
            $"Performance too variable: CV={coefficientOfVariation:F2}% (max 10%)");

        Console.WriteLine($"\n=== Consistency Analysis ({iterations} runs) ===");
        Console.WriteLine($"Mean: {mean:F2}ms");
        Console.WriteLine($"Std Dev: {stdDev:F2}ms");
        Console.WriteLine($"CV: {coefficientOfVariation:F2}%");
        Console.WriteLine($"Min: {samples.Min()}ms");
        Console.WriteLine($"Max: {samples.Max()}ms");
    }
}
```

---

## 6. Instrumentation Implementation Plan

### 6.1 Performance Monitoring Service

```csharp
public interface IMapLoadingMonitor
{
    void StartMonitoring(string mapName);
    void RecordPhase(LoadingPhase phase, long durationMs);
    void RecordMemory(string label);
    void RecordEntityCount(string entityType, int count);
    void RecordTextureLoad(string textureId, long sizeBytes);
    MapLoadingMetrics FinishMonitoring();
}

public class MapLoadingMonitor : IMapLoadingMonitor
{
    private readonly Stopwatch _totalTimer = new();
    private readonly MemoryProfiler _memoryProfiler = new();
    private readonly Dictionary<LoadingPhase, long> _phaseTimes = new();
    private readonly MapLoadingMetrics _metrics = new();

    public void StartMonitoring(string mapName)
    {
        _metrics.MapName = mapName;
        _memoryProfiler.StartProfiling();
        _totalTimer.Restart();
        _metrics.MemoryBeforeLoading = GC.GetTotalMemory(false);
    }

    public void RecordPhase(LoadingPhase phase, long durationMs)
    {
        _phaseTimes[phase] = durationMs;

        // Update metrics based on phase
        switch (phase)
        {
            case LoadingPhase.FileRead:
                _metrics.FileReadTime = durationMs;
                break;
            case LoadingPhase.JsonDeserialization:
                _metrics.JsonDeserializationTime = durationMs;
                break;
            // ... etc for all phases
        }
    }

    public MapLoadingMetrics FinishMonitoring()
    {
        _totalTimer.Stop();
        _metrics.TotalLoadingTime = _totalTimer.ElapsedMilliseconds;
        _metrics.MemoryAfterLoading = GC.GetTotalMemory(false);

        var memProfile = _memoryProfiler.GetProfile();
        _metrics.MemoryPeakDuringLoading = memProfile.PeakMemory;

        return _metrics;
    }
}
```

### 6.2 Integration with Existing Code

**Modify MapLoader.cs**:
```csharp
public class MapLoader
{
    private readonly IMapLoadingMonitor? _monitor;

    public Entity LoadMap(World world, string mapId)
    {
        _monitor?.StartMonitoring(mapId);

        try
        {
            // Existing loading code with instrumentation...
            var sw = Stopwatch.StartNew();

            // Phase 1: Parse JSON
            var tmxDoc = ParseMapDocument(mapDef);
            _monitor?.RecordPhase(LoadingPhase.JsonDeserialization, sw.ElapsedMilliseconds);

            // Phase 2: Load tilesets
            sw.Restart();
            var tilesets = LoadTilesets(tmxDoc);
            _monitor?.RecordPhase(LoadingPhase.TilesetLoading, sw.ElapsedMilliseconds);

            // ... etc for all phases

            return mapEntity;
        }
        finally
        {
            var finalMetrics = _monitor?.FinishMonitoring();
            if (finalMetrics != null)
            {
                LogPerformanceMetrics(finalMetrics);
            }
        }
    }
}
```

---

## 7. Test Execution Plan

### 7.1 Automated Test Suite

**File**: `tests/PerformanceBenchmarks/MapLoadingBenchmarks.cs`

```csharp
[Collection("MapLoading")]
public class MapLoadingBenchmarks : IDisposable
{
    private readonly World _world;
    private readonly MapLoader _loader;
    private readonly TestOutputHelper _output;

    public MapLoadingBenchmarks(ITestOutputHelper output)
    {
        _output = output;
        _world = World.Create();
        _loader = CreateMapLoader();
    }

    [Fact]
    public void Baseline_CurrentPerformance()
    {
        // Measure current performance as baseline
        var metrics = MeasureMapLoad("littleroot_town");

        _output.WriteLine("=== CURRENT BASELINE ===");
        _output.WriteLine($"Total time: {metrics.TotalLoadingTime}ms");
        _output.WriteLine($"Memory: {metrics.MemoryAfterLoading - metrics.MemoryBeforeLoading:N0} bytes");
        _output.WriteLine($"Tiles: {metrics.TilesCreated}");

        // Save baseline for comparison
        SaveBaseline("littleroot_town", metrics);
    }

    [Theory]
    [MemberData(nameof(GetOptimizationScenarios))]
    public void Optimization_ImprovesPerformance(
        string optimizationName,
        Func<MapLoader> createLoader,
        double minImprovementPercent)
    {
        var baseline = LoadBaseline("littleroot_town");
        var optimizedLoader = createLoader();
        var optimizedMetrics = MeasureMapLoadWithLoader(optimizedLoader, "littleroot_town");

        var improvement = ((baseline.TotalLoadingTime - optimizedMetrics.TotalLoadingTime) * 100.0)
                         / baseline.TotalLoadingTime;

        Assert.True(improvement >= minImprovementPercent,
            $"{optimizationName} improvement {improvement:F2}% < target {minImprovementPercent}%");

        _output.WriteLine($"\n=== {optimizationName} ===");
        _output.WriteLine($"Baseline: {baseline.TotalLoadingTime}ms");
        _output.WriteLine($"Optimized: {optimizedMetrics.TotalLoadingTime}ms");
        _output.WriteLine($"Improvement: {improvement:F2}%");
    }

    public static IEnumerable<object[]> GetOptimizationScenarios()
    {
        yield return new object[]
        {
            "Parallel Tileset Loading",
            (Func<MapLoader>)(() => CreateLoaderWithParallelTilesets()),
            10.0 // Expect at least 10% improvement
        };

        yield return new object[]
        {
            "Lazy Entity Creation",
            (Func<MapLoader>)(() => CreateLoaderWithLazyEntities()),
            15.0
        };

        yield return new object[]
        {
            "Texture Pooling",
            (Func<MapLoader>)(() => CreateLoaderWithTexturePool()),
            5.0
        };
    }
}
```

### 7.2 Continuous Monitoring

**GitHub Actions Integration**:
```yaml
name: Performance Regression Tests

on: [push, pull_request]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2

      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'

      - name: Run Performance Benchmarks
        run: |
          cd tests/PerformanceBenchmarks
          dotnet test --logger "console;verbosity=detailed" \
                      --results-directory ./BenchmarkResults

      - name: Upload Results
        uses: actions/upload-artifact@v2
        with:
          name: benchmark-results
          path: tests/PerformanceBenchmarks/BenchmarkResults/

      - name: Check Performance Regression
        run: |
          python scripts/check-performance-regression.py \
            --baseline baseline-metrics.json \
            --current BenchmarkResults/latest-metrics.json \
            --threshold 5.0  # Fail if >5% regression
```

---

## 8. Reporting & Visualization

### 8.1 Performance Report Format

```csharp
public class PerformanceReportGenerator
{
    public string GenerateReport(MapLoadingMetrics metrics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔═══════════════════════════════════════════════════════╗");
        sb.AppendLine("║        MAP LOADING PERFORMANCE REPORT                 ║");
        sb.AppendLine("╚═══════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"Map: {metrics.MapName}");
        sb.AppendLine($"Total Time: {metrics.TotalLoadingTime}ms");
        sb.AppendLine($"Tiles Created: {metrics.TilesCreated:N0}");
        sb.AppendLine($"Tiles/sec: {metrics.TilesPerSecond:F2}");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────────────────┐");
        sb.AppendLine("│ PHASE BREAKDOWN                                     │");
        sb.AppendLine("├─────────────────────────────────────────────────────┤");

        foreach (var (phase, time) in metrics.GetSlowestPhases())
        {
            var percent = (time * 100.0) / metrics.TotalLoadingTime;
            var bar = GenerateBar(percent, 30);
            sb.AppendLine($"│ {phase,-25} {time,6}ms {bar} {percent,5:F1}% │");
        }

        sb.AppendLine("└─────────────────────────────────────────────────────┘");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────────────────┐");
        sb.AppendLine("│ MEMORY ANALYSIS                                     │");
        sb.AppendLine("├─────────────────────────────────────────────────────┤");
        sb.AppendLine($"│ Before:     {FormatBytes(metrics.MemoryBeforeLoading),-36} │");
        sb.AppendLine($"│ After:      {FormatBytes(metrics.MemoryAfterLoading),-36} │");
        sb.AppendLine($"│ Peak:       {FormatBytes(metrics.MemoryPeakDuringLoading),-36} │");
        sb.AppendLine($"│ Increase:   {FormatBytes(metrics.MemoryAfterLoading - metrics.MemoryBeforeLoading),-36} │");
        sb.AppendLine($"│ Per Tile:   {FormatBytes((long)metrics.MemoryPerTile),-36} │");
        sb.AppendLine("└─────────────────────────────────────────────────────┘");

        return sb.ToString();
    }

    private string GenerateBar(double percent, int width)
    {
        var filled = (int)((percent / 100.0) * width);
        return new string('█', filled) + new string('░', width - filled);
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }
}
```

---

## 9. Success Criteria

### 9.1 Performance Targets

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Total Load Time (Large Map) | 20s | <5s | ❌ Not Met |
| Memory Usage | 712 MB | <300 MB | ❌ Not Met |
| Tiles/Second | Unknown | >10,000 | ⚠️ Needs Measurement |
| Memory per Tile | Unknown | <50 KB | ⚠️ Needs Measurement |

### 9.2 Validation Checklist

- [ ] All benchmark tests passing
- [ ] No memory leaks detected
- [ ] Performance consistent across runs (CV <10%)
- [ ] All optimization improvements statistically significant (>5%)
- [ ] Compression algorithm selected (best balance of speed/size)
- [ ] Texture memory under budget
- [ ] No GC pressure during loading (Gen2 collections <3)
- [ ] Spatial hash rebuild time <100ms

---

## 10. Implementation Priority

### Phase 1: Baseline Measurement (Week 1)
1. Implement `IMapLoadingMonitor` interface
2. Add instrumentation to all loading phases
3. Create baseline benchmark suite
4. Run baseline tests and document current performance

### Phase 2: Memory Profiling (Week 1-2)
1. Implement `MemoryProfiler` class
2. Add memory sampling at key points
3. Create memory leak detection tests
4. Profile texture memory usage

### Phase 3: Optimization Testing (Week 2-3)
1. Create before/after comparison framework
2. Implement statistical validation
3. Test each optimization candidate
4. Document improvements

### Phase 4: Continuous Monitoring (Week 3-4)
1. Set up automated benchmark runs
2. Integrate with CI/CD pipeline
3. Create performance dashboards
4. Establish regression alerts

---

## 11. Coordination with Optimizer Agent

**Store key measurement points in memory**:
```bash
npx claude-flow@alpha hooks post-task \
  --task-id "tester/benchmark_strategy" \
  --memory-key "hive/tester/measurement_points"
```

**Critical data for optimizer**:
- Slowest phases (top optimization targets)
- Memory allocation hotspots
- Entity creation bottlenecks
- Compression algorithm comparison
- Before/after validation framework

**Next steps**:
1. TESTER provides instrumentation points
2. OPTIMIZER implements targeted improvements
3. TESTER validates improvements with benchmarks
4. ARCHITECT reviews and approves changes

---

## Appendix: Example Benchmark Output

```
╔═══════════════════════════════════════════════════════╗
║        MAP LOADING PERFORMANCE REPORT                 ║
╚═══════════════════════════════════════════════════════╝

Map: littleroot_town
Total Time: 20,482ms
Tiles Created: 16,384
Tiles/sec: 799.30

┌─────────────────────────────────────────────────────┐
│ PHASE BREAKDOWN                                     │
├─────────────────────────────────────────────────────┤
│ Tileset Loading          8,234ms ███████████████░░░░░░░░░░░░░░░  40.2% │
│ Layer Processing         5,123ms █████████░░░░░░░░░░░░░░░░░░░░░  25.0% │
│ Component Addition       3,456ms ██████░░░░░░░░░░░░░░░░░░░░░░░░  16.9% │
│ Decompression           2,111ms ████░░░░░░░░░░░░░░░░░░░░░░░░░░  10.3% │
│ JSON Deserialization    1,558ms ███░░░░░░░░░░░░░░░░░░░░░░░░░░░   7.6% │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ MEMORY ANALYSIS                                     │
├─────────────────────────────────────────────────────┤
│ Before:     100.00 MB                               │
│ After:      812.00 MB                               │
│ Peak:       856.00 MB                               │
│ Increase:   712.00 MB                               │
│ Per Tile:   43.46 KB                                │
└─────────────────────────────────────────────────────┘

❌ PERFORMANCE: Does NOT meet target (<5s)
❌ MEMORY: Does NOT meet target (<300 MB)

TOP OPTIMIZATION TARGETS:
1. Tileset Loading (40.2% of time) - Implement parallel loading
2. Layer Processing (25.0% of time) - Optimize bulk operations
3. Component Addition (16.9% of time) - Reduce allocations
```

---

**END OF BENCHMARK STRATEGY DOCUMENT**

This comprehensive strategy provides the OPTIMIZER agent with exact measurement points, validation criteria, and success metrics for targeted performance improvements.
