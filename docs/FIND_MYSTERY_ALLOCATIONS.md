# Finding the 10.5 KB/frame Mystery Allocations

**Quick Reference Guide for Memory Profiling**

---

## The Problem

```
Current GC Pressure: 46.8 Gen0 GC/sec (23x higher than normal)
Known sources:       ~2 KB/frame (SystemManager + SpriteAnimation LINQ)
Rendering:           ~0.8 KB/frame (optimized with static vectors)
MISSING:             ~10.5 KB/frame (84% of the problem!)
```

---

## Method 1: dotnet-trace (Recommended)

### Setup

```bash
# Install if not already available
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-counters
```

### Collect Allocation Data

```bash
# Terminal 1: Start the game
cd PokeSharp.Game
dotnet run --configuration Release

# Terminal 2: Find process ID
ps aux | grep PokeSharp
# Or on Windows WSL:
ps -W | grep PokeSharp

# Monitor live allocations
dotnet-counters monitor --process-id <pid> \
  --counters System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate]

# Collect detailed trace (10 seconds)
dotnet-trace collect --process-id <pid> \
  --providers Microsoft-DotNETCore-SampleProfiler \
  --duration 00:00:10 \
  --output trace.nettrace

# Analyze allocations
dotnet-trace analyze trace.nettrace --top-allocations
```

### What to Look For

- **Functions allocating >50 KB/sec** (red flag!)
- **Allocations in game loop** (Update/Render paths)
- **String allocations** (especially in logging)
- **Collection allocations** (`List<>`, `Dictionary<>`, arrays)

---

## Method 2: Visual Studio Profiler (Windows)

### Steps

1. Open PokeSharp solution in Visual Studio 2022
2. Go to **Debug → Performance Profiler**
3. Select **.NET Object Allocation Tracking**
4. Click **Start**
5. Let game run for 10 seconds
6. Click **Stop Collection**

### Analysis

1. Switch to **Allocation** view
2. Sort by **Total Allocations (Bytes)**
3. Look for:
   - Hot paths allocating >100 KB
   - Repeated allocations in loops
   - Large collection allocations

---

## Method 3: JetBrains dotMemory (Best Detail)

### Setup

```bash
# Install dotMemory command-line tool
dotnet tool install --global JetBrains.dotMemory.Console
```

### Collect Memory Snapshot

```bash
# Attach to running process
dotmemory attach <process-id> --save-to-dir=./memory-snapshots

# Wait 10 seconds, then take snapshot
# Press Enter to detach and save

# Analyze in dotMemory UI
dotmemory open ./memory-snapshots/snapshot.dmw
```

### What to Look For

1. **Allocation hot spots** (red indicators)
2. **Surviving objects** (Gen1/Gen2 promotions)
3. **String allocations** from logging
4. **Collection growth** (List, Dictionary capacity)

---

## Method 4: Manual Code Search (Quick Wins)

### Search for Common Allocation Patterns

```bash
# 1. LINQ in hot paths
grep -rn "\.Where\|\.Select\|\.ToArray\|\.ToList\|\.FirstOrDefault\|\.Any" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/ \
  PokeSharp.Game.Systems/

# 2. Collection allocations in loops
grep -rn "new List\|new Dictionary\|new HashSet\|new \[\]" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/ \
  PokeSharp.Game.Systems/

# 3. String concatenation/interpolation
grep -rn '\$"\|+ "' \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/

# 4. Entity creation/destruction
grep -rn "world\.Create\|world\.Destroy\|entity\.Destroy" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/

# 5. Logging in hot paths
grep -rn "LogInformation\|LogDebug\|LogTrace" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/ | \
  grep -v "LogRender\|LogMemory"  # Exclude periodic logging
```

---

## Method 5: Add Instrumentation

### Modify PerformanceMonitor for Detailed Tracking

Add this to `PokeSharp.Game/Diagnostics/PerformanceMonitor.cs`:

```csharp
private long _lastTotalMemory;
private long _lastGen0Count;
private long _peakAllocationRate;

private void LogMemoryStats()
{
    var totalMemoryBytes = GC.GetTotalMemory(false);
    var totalMemoryMb = totalMemoryBytes / 1024.0 / 1024.0;

    var gen0 = GC.CollectionCount(0);
    var gen1 = GC.CollectionCount(1);
    var gen2 = GC.CollectionCount(2);

    // Calculate allocation rate (approximate)
    var allocatedBytes = totalMemoryBytes - _lastTotalMemory;
    var allocatedKB = Math.Max(0, allocatedBytes / 1024.0);
    var allocRatePerSec = allocatedKB / 5.0;  // 5-second interval

    // Track peak
    if (allocatedBytes > _peakAllocationRate)
    {
        _peakAllocationRate = allocatedBytes;
        _logger.LogWarning(
            "NEW PEAK allocation rate: {AllocKB:F1} KB in 5 seconds ({PerSec:F1} KB/sec)",
            allocatedKB, allocRatePerSec
        );
    }

    // Log detailed stats
    _logger.LogInformation(
        "Memory: {Memory:F1} MB | Alloc: {Alloc:F1} KB/sec | Peak: {Peak:F1} KB/sec | " +
        "Gen0: {Gen0} ({Gen0Delta}/5s), Gen1: {Gen1}, Gen2: {Gen2}",
        totalMemoryMb,
        allocRatePerSec,
        _peakAllocationRate / 5120.0,  // Convert to KB/sec
        gen0,
        gen0 - _lastGen0Count,
        gen1,
        gen2
    );

    _lastTotalMemory = totalMemoryBytes;
    _lastGen0Count = gen0;

    // ... rest of existing code
}
```

---

## Likely Culprits (Ranked by Probability)

### 🔴 Priority 1: Update Loop Allocations (60x per second)

**Check these systems for allocations:**

```bash
# Systems running every update (60fps)
find . -name "*System.cs" -type f -exec grep -l "IUpdateSystem\|Update(" {} \;
```

**Common issues:**
- Creating temporary lists/arrays
- LINQ queries on collections
- String formatting in loops
- Entity queries without caching

---

### 🔴 Priority 2: Render Loop Allocations (60x per second)

**Check these systems:**

```bash
# Systems running every render (60fps)
find . -name "*System.cs" -type f -exec grep -l "IRenderSystem\|Render(" {} \;
```

**Common issues:**
- Creating sprite batch parameters
- Allocating transformation matrices
- String formatting for debug rendering
- Temporary color/vector allocations

---

### 🟠 Priority 3: Entity/Component Management

**Search for:**

```bash
# Entity lifecycle operations
grep -rn "world\.Create\|entity\.Destroy\|world\.Destroy" \
  --include="*.cs" \
  PokeSharp.Game/Systems/

# Component addition/removal
grep -rn "entity\.Add\|entity\.Remove\|world\.Add\|world\.Remove" \
  --include="*.cs" \
  PokeSharp.Game/Systems/
```

**Common issues:**
- Creating/destroying entities in update loop
- Temporary entities for calculations
- Component churn (add/remove repeatedly)

---

### 🟠 Priority 4: Query Results Not Cached

**Search for queries in loops:**

```bash
# Find world.Query calls
grep -rn "world\.Query" \
  --include="*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/
```

**Common issues:**
- Running same query multiple times
- Not caching query results
- Creating enumerators in tight loops

---

### 🟡 Priority 5: Logging Allocations

**Search for logging in hot paths:**

```bash
# Find logging that might run frequently
grep -rn "Log\(Information\|Debug\|Trace\)" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ | \
  wc -l  # Count total logging calls

# Check for expensive string formatting
grep -rn '\$".*{.*}.*{.*}.*{' \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/
```

**Common issues:**
- Debug logging in release builds
- String interpolation without guards
- Structured logging with complex objects

---

## Quick Diagnostic Checklist

Run these commands and note any output:

```bash
# 1. Count LINQ usage in systems
echo "LINQ in Update/Render systems:"
grep -r "\.Where\|\.Select\|\.ToArray\|\.ToList" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/ | wc -l

# 2. Count new allocations in systems
echo "Collection allocations in systems:"
grep -r "new List\|new Dictionary\|new HashSet\|new \[\]" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ \
  PokeSharp.Engine.Systems/ | wc -l

# 3. Find world.Query calls
echo "ECS queries:"
grep -rn "world\.Query" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ | wc -l

# 4. Find entity creation/destruction
echo "Entity lifecycle operations:"
grep -rn "world\.Create\|Destroy" \
  --include="*System*.cs" \
  PokeSharp.Game/Systems/ | wc -l
```

---

## Expected Results

### After finding the mystery source, you should see:

```
Known Allocations:
├─ SystemManager LINQ:        360 bytes/frame (fixable)
├─ SpriteAnimation LINQ:       800 bytes/frame (fixable)
├─ Rendering optimized:        833 bytes/frame (already fixed)
└─ Mystery source:             ~10,500 bytes/frame (TO BE FOUND)

Expected outcome after fixes:
├─ Total allocations:          ~1,500 bytes/frame
├─ Gen0 GC rate:               5-8 collections/sec
├─ Gen2 GC rate:               0-1 collections/sec
└─ Status:                     🟢 NORMAL
```

---

## Next Steps After Finding Source

1. **Document the finding** in this file
2. **Estimate fix effort** (lines of code, complexity)
3. **Implement fix** (caching, pooling, or elimination)
4. **Measure improvement** (run game, check logs)
5. **Verify no regression** (run tests, check frame rate)

---

## Pro Tips

### Memory Profiling Best Practices

1. **Profile Release builds** (Debug has extra allocations)
2. **Let game stabilize** (first 30 seconds has startup allocations)
3. **Take multiple samples** (verify consistency)
4. **Compare before/after** (verify fixes work)

### Common False Positives

These are NORMAL and NOT the problem:

- ✅ Startup allocations (loading, initialization)
- ✅ Periodic logging (every 5 seconds)
- ✅ One-time caches (dictionary creation)
- ✅ UI interactions (user input events)

Focus on allocations that happen **every frame** in the **game loop**.

---

**Last Updated:** 2025-11-16
**Status:** Ready to profile
**Expected Time:** 30-60 minutes to find mystery source
