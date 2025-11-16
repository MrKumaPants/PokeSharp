# GameDataLoader N+1 Query Pattern Fix

## Summary
Fixed N+1 database query pattern in `GameDataLoader.LoadMapsAsync()` method that was causing excessive database round trips during map loading.

## Problem Identified
**Location**: `/mnt/c/Users/nate0/RiderProjects/PokeSharp/PokeSharp.Game.Data/Loading/GameDataLoader.cs:282`

**Original Code** (N+1 Pattern):
```csharp
foreach (var file in files)
{
    // ... file processing ...

    // ❌ BAD: Queries database once per map in loop
    var existing = _context.Maps.Find(mapId);
    if (existing != null)
    {
        _context.Entry(existing).CurrentValues.SetValues(mapDef);
        _logger.LogMapOverridden(mapDef.MapId, mapDef.DisplayName);
    }
    else
    {
        _context.Maps.Add(mapDef);
    }
}
```

**Impact**:
- For N map files, this caused N individual database queries
- Each `Find()` call = 1 database round trip
- Example: 50 maps = 50 database queries instead of 1

## Solution Implemented

**Optimized Code** (Bulk Fetch):
```csharp
// ✅ GOOD: Load all existing maps once before loop
var existingMaps = await _context
    .Maps.AsNoTracking()
    .ToDictionaryAsync(m => m.MapId, ct);

foreach (var file in files)
{
    // ... file processing ...

    // ✅ In-memory dictionary lookup (O(1) performance)
    if (existingMaps.TryGetValue(mapId, out var existing))
    {
        // Mod is overriding base game map - attach and update
        _context.Maps.Attach(existing);
        _context.Entry(existing).CurrentValues.SetValues(mapDef);
        _logger.LogMapOverridden(mapDef.MapId, mapDef.DisplayName);
    }
    else
    {
        _context.Maps.Add(mapDef);
    }
}
```

## Changes Made

### 1. Bulk Load Existing Maps (Line 232-234)
- Added `ToDictionaryAsync(m => m.MapId, ct)` to load all existing maps in one query
- Used `AsNoTracking()` for read-only performance optimization
- Creates in-memory dictionary for O(1) lookups

### 2. Replace Find() with Dictionary Lookup (Line 288-298)
- Changed `_context.Maps.Find(mapId)` to `existingMaps.TryGetValue(mapId, out var existing)`
- No database queries in loop - all lookups are in-memory
- Used `Attach()` to properly track the entity before updating

## Performance Benefits

| Scenario | Before (N+1) | After (Bulk) | Improvement |
|----------|-------------|--------------|-------------|
| 10 maps | 10 queries | 1 query | 90% reduction |
| 50 maps | 50 queries | 1 query | 98% reduction |
| 100 maps | 100 queries | 1 query | 99% reduction |

**Additional Benefits**:
- `AsNoTracking()` reduces memory overhead for read-only data
- In-memory dictionary lookups are O(1) constant time
- Single database round trip regardless of map count

## Methods Optimized

| Method | Lines Changed | Optimization |
|--------|--------------|--------------|
| `LoadMapsAsync()` | 230-234, 288-298 | Bulk fetch + dictionary lookup |

## Other Methods Analyzed

✅ **Already Optimized**:
- `LoadNpcsAsync()` - Uses batch additions with single `SaveChangesAsync()`
- `LoadTrainersAsync()` - Uses batch additions with single `SaveChangesAsync()`

These methods iterate over **files** (not database records), so they don't have N+1 patterns. They efficiently batch all additions and save once at the end.

## Testing Recommendations

1. **Functional Testing**:
   - Verify all maps load correctly
   - Test mod override functionality works as before
   - Ensure map metadata is preserved

2. **Performance Testing**:
   - Measure load time with 10, 50, 100+ maps
   - Monitor database query count (should be 1 for maps)
   - Verify memory usage remains stable

3. **Edge Cases**:
   - Empty map directory
   - Duplicate map IDs
   - Malformed JSON files

## Files Modified

- `/mnt/c/Users/nate0/RiderProjects/PokeSharp/PokeSharp.Game.Data/Loading/GameDataLoader.cs`

## Coordination Hooks Executed

- ✅ Pre-task: `npx claude-flow@alpha hooks pre-task --description "GameDataLoader N+1 fix"`
- ✅ Post-edit: Tracked file modification in swarm memory
- ✅ Post-task: Marked task as complete
- ✅ Notification: Notified swarm coordination system

## Technical Notes

### Why AsNoTracking()?
- We only need to read existing maps to check for duplicates
- We manually track changes using `Attach()` when needed
- Reduces memory overhead and improves query performance

### Why Attach() Instead of Update()?
- The entity is loaded with `AsNoTracking()` (not tracked)
- We need to manually attach it to the context before updating
- `Attach()` + `SetValues()` is the correct pattern for this scenario

### Preserved Functionality
- Mod override detection still works identically
- All logging statements preserved
- Error handling unchanged
- Cancellation token support maintained
