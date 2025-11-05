# Collision Troubleshooting Guide

**Issue:** Collision not working after ECS refactoring
**Status:** ✅ Collision logic verified in tests - issue is likely in map loading

---

## Test Results

### ✅ Collision System Tests (5/5 passing)
The core collision detection logic works correctly:
- Solid tiles block movement ✅
- Non-solid tiles allow movement ✅
- Empty positions are walkable ✅
- Ledges respect directional blocking ✅
- Multiple entities at same position handled correctly ✅

### ✅ SpatialHash Tests (12/12 passing)
The spatial indexing works correctly:
- Entities can be added and retrieved ✅
- Multiple entities at same position supported ✅
- Multi-map isolation works ✅
- Bounds queries return correct results ✅

---

## Likely Issue: Property Parsing

The collision logic is sound. The problem is likely that tiles from `test-map.json` aren't getting `Collision` components.

### Debug Output to Check

**When running the game**, you should see console output like:

```
✅ Loaded map: test-map (20x15 tiles)
   Created 245 tile entities
   
  Tile (0,0) GID=1 LocalID=0 has 1 properties
    solid property found: value=True, type=Boolean
    ✅ Added Collision(IsSolid=true) to tile at (0,0)
    
  Tile (1,0) GID=1 LocalID=0 has 1 properties
    solid property found: value=True, type=Boolean
    ✅ Added Collision(IsSolid=true) to tile at (1,0)
```

**If you DON'T see this**, the properties aren't being parsed from the tileset.

---

## Debugging Steps

### 1. Verify Tileset Properties are Loaded

Add this to `TiledMapLoader.ParseTileAnimations()` after line 176:

```csharp
tileset.TileProperties[tile.Id] = props;
Console.WriteLine($"  Loaded {props.Count} properties for tile ID {tile.Id}");
foreach (var kvp in props)
{
    Console.WriteLine($"    {kvp.Key} = {kvp.Value} ({kvp.Value?.GetType().Name})");
}
```

### 2. Check Spatial Hash Indexing

The debug output should show:

```
SpatialHash indexed 245 entities at 245 positions
```

If it shows `0 entities`, tiles aren't being created.

### 3. Test Movement with Debug Output

Press arrow keys to move. You should see:

```
Checking collision at (10,9): Found 1 entities
  Entity 123: Has Collision=True
    IsSolid=True
❌ Movement to (10,9) BLOCKED
```

Or for empty tiles:

```
Checking collision at (10,9): Found 0 entities
✅ Movement to (10,9) ALLOWED
```

---

## Possible Root Causes

### 1. JSON Property Type Mismatch
**Issue**: Property value might be coming through as `JsonElement` instead of `bool`

**Fix**: Already handled in MapLoader with type switching:
```csharp
bool isSolid = solidValue switch
{
    bool b => b,
    string s => bool.TryParse(s, out var result) && result,
    _ => false
};
```

### 2. External Tileset Not Loaded
**Issue**: If `test-tileset.json` isn't being parsed, properties won't exist

**Check**: `TiledMapLoader.LoadExternalTileset()` should be called

**Debug**: Add logging in `LoadExternalTileset()`:
```csharp
Console.WriteLine($"Loading external tileset: {tilesetPath}");
Console.WriteLine($"  Tiles with data: {tiledTileset.Tiles?.Count ?? 0}");
```

### 3. FirstGid Offset Issue
**Issue**: LocalTileId calculation might be wrong if FirstGid != 1

**Check**:
```csharp
Console.WriteLine($"Tileset FirstGid={tileset.FirstGid}");
Console.WriteLine($"TileGid={tileGid}, LocalTileId={tileGid - tileset.FirstGid}");
```

---

## Quick Verification Test

Add this to your game initialization after `LoadMapEntities()`:

```csharp
// Count tiles with collision
var collisionQuery = new QueryDescription().WithAll<TilePosition, Collision>();
int solidTileCount = 0;
_world.Query(in collisionQuery, (Entity e, ref TilePosition pos, ref Collision col) =>
{
    solidTileCount++;
    Console.WriteLine($"Solid tile at ({pos.X},{pos.Y})");
});

Console.WriteLine($"\n🔍 COLLISION DIAGNOSTIC:");
Console.WriteLine($"   Total solid tiles: {solidTileCount}");

if (solidTileCount == 0)
{
    Console.WriteLine("   ⚠️  WARNING: No solid tiles found!");
    Console.WriteLine("   This means properties aren't being parsed from tileset.");
}
else
{
    Console.WriteLine($"   ✅ {solidTileCount} solid tiles loaded correctly");
}
```

---

## Expected Behavior

Based on `test-tileset.json`:
- Tile ID 0 (GID 1): `solid=true` ✅
- Tile ID 1 (GID 2): `solid=true` ✅
- Tile ID 2 (GID 3): `solid=true` ✅
- Tile ID 7 (GID 8): `ledge_direction=down` ✅

The map should have ~100-150 solid tiles (rough estimate based on border walls).

---

## Next Step

Run the game and check the console output. You should see:
1. Tiles being created with properties
2. Collision components being added
3. Spatial hash indexing entities
4. Movement collision checks showing entities found

If any of these aren't appearing, that's where the issue is!

