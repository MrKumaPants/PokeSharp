# Collision System Setup

## Overview
The test map now has comprehensive collision data that integrates with the ECS collision system.

## Map Collision Data Structure

### File: `/Assets/Maps/test-map.json`

The map contains a **Collision** object group layer with 8 collision rectangles:

### Outer Walls (Map Boundaries)
1. **top-wall**: Full width (320px) × 16px at (0, 0)
2. **bottom-wall**: Full width (320px) × 16px at (0, 224)
3. **left-wall**: 16px × full height (240px) at (0, 0)
4. **right-wall**: 16px × full height (240px) at (304, 0)

### Inner Border (Decorative Boundary)
5. **inner-border-top**: 256px × 16px at (32, 32)
6. **inner-border-bottom**: 256px × 16px at (32, 192)
7. **inner-border-left**: 16px × 160px at (32, 32)
8. **inner-border-right**: 16px × 160px at (272, 32)

## Collision Object Format

Each collision object follows the Tiled format:

```json
{
  "height": 16,
  "id": 1,
  "name": "descriptive-name",
  "properties": [
    {
      "name": "solid",
      "type": "bool",
      "value": true
    }
  ],
  "type": "collision",
  "visible": true,
  "width": 320,
  "x": 0,
  "y": 0
}
```

## Integration with MapLoader

The `MapLoader.LoadCollision()` method:

1. Finds the "Collision" object group
2. Iterates through all objects
3. Checks for the "solid" property
4. Converts pixel coordinates to tile coordinates
5. Marks tiles as solid in the `TileCollider` component

### Coordinate Conversion
```csharp
int tileX = (int)(pixelX / tileWidth);  // tileWidth = 16
int tileY = (int)(pixelY / tileHeight); // tileHeight = 16
```

## Walkable Area

The center area (tiles 3,3 to 16,11) is **walkable** for player testing:
- Inner area: approximately 13×9 tiles
- Player can freely move within this space
- Collisions prevent movement outside boundaries

## Map Dimensions

- **Total Size**: 20×15 tiles (320×240 pixels)
- **Tile Size**: 16×16 pixels
- **Walkable Area**: ~13×9 tiles (center region)
- **Collision Coverage**: Outer perimeter + inner decorative border

## Visual Layout

```
┌────────────────────┐  ← Top wall (solid)
│┌──────────────────┐│  ← Outer border tiles
││                  ││
││   WALKABLE       ││  ← Center area (no collision)
││   AREA           ││
││                  ││
│└──────────────────┘│  ← Inner border (solid)
└────────────────────┘  ← Bottom wall (solid)
   ↑             ↑
 Left          Right
 wall          wall
(solid)       (solid)
```

## Testing Collision

To test the collision system:

1. **Movement**: Player should move freely in the center area
2. **Boundaries**: Player should not pass through outer walls
3. **Inner Border**: Player should not pass through decorative border
4. **Debug Rendering**: Enable collision debug overlay to visualize solid tiles

## Next Steps

- ✅ Collision data added to test map
- ✅ MapLoader parses collision objects
- ✅ TileCollider component receives collision data
- 🔄 CollisionSystem applies collision during movement
- 🔄 Debug rendering shows collision boundaries
