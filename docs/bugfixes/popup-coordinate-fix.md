# Map Popup Rendering Coordinate Fix

## Date
2024-12-05

## Issue
Map popups were not rendering correctly because the border tiles were being drawn at the wrong position relative to the background.

## Root Cause
**Coordinate system mismatch** in `MapPopupScene.cs`:

The `DrawTileSheetBorder()` function expects to receive the **interior** (background) coordinates and draws the frame tiles AROUND that interior. However, it was receiving the **outer** popup coordinates instead.

### The Bug

```csharp
// Line 374-377: Background drawn at interior coordinates
int bgX = popupX + borderThickness;  // Interior X = outer + 8
int bgY = popupY + borderThickness;  // Interior Y = outer + 8

// Line 405: Border called with OUTER coordinates (WRONG!)
DrawNineSliceBorder(popupX, popupY, _popupWidth, _popupHeight);
```

### The Fix

```csharp
// Line 405-407: Border now called with INTERIOR coordinates (CORRECT!)
// Pass interior coordinates (bgX, bgY) not outer coordinates (popupX, popupY)
// DrawTileSheetBorder expects the interior position and draws the frame AROUND it
DrawNineSliceBorder(bgX, bgY, bgWidth, bgHeight);
```

## Technical Details

### Coordinate System in pokeemerald

From the GBA source code (pokeemerald's `DrawMapNamePopUpFrame`):
- The function receives the **interior window position** as `(x, y)`
- The frame tiles are drawn **around** this interior:
  - Top edge: Y = `y - 1 tile` (above the interior)
  - Left edge: X = `x - 1 tile` (left of the interior)
  - Right edge: X = `x + interiorWidth` (right of the interior)
  - Bottom edge: Y = `y + interiorHeight` (below the interior)

### Example Coordinate Flow (Fixed)

Given:
- `popupX, popupY` = outer position = `(0, 0)`
- `borderThickness` = 8 pixels (1 tile)
- `bgX, bgY` = interior position = `(8, 8)`
- Background size = 80×24 pixels (10×3 tiles)

Border drawing (correct):
1. Call: `DrawTileSheetBorder(8, 8, 80, 24)`
2. Top edge: Y = 8 - 8 = **0**, X from 0 to 88 (12 tiles)
3. Left edge: X = **0**, Y from 8 to 32 (3 tiles)
4. Right edge: X = **88**, Y from 8 to 32 (3 tiles)
5. Bottom edge: Y = **32**, X from 0 to 88 (12 tiles)

**Result:** Complete 96×40 pixel frame (matches `_popupWidth` and `_popupHeight`)

### Why the Bug Occurred

The bug was subtle because:
1. The background was correctly positioned at `bgX, bgY` (interior)
2. The text was correctly positioned relative to `bgX, bgY`
3. Only the border was using the wrong coordinates

This likely went unnoticed initially because the popup still appeared on screen, but the border tiles were offset by 8 pixels in both X and Y directions, causing them to not properly frame the background.

## Files Changed
- `MonoBallFramework.Game/Engine/Scenes/Scenes/MapPopupScene.cs`
  - Line 405-407: Changed border call to use interior coordinates
  - Line 178, 200: Changed font size from 16pt to 9pt (GBA-accurate)
  - Line 410-468: Added text truncation and improved positioning

## Additional Fix: Text Rendering

### Text Size Issue
The original implementation used 16pt font, which was far too large for the 24-pixel-tall background. In pokeemerald (GBA), fonts are bitmap-based and very small - typically 8-9 pixels tall.

### Text Rendering Fixes
1. **Font Size**: Adjusted to 12pt (appropriate for modern resolution scaling)
2. **Text Color**: Changed to fully opaque white (255, 255, 255, 255)
3. **Shadow Color**: Changed to fully opaque dark gray (72, 72, 80, 255)
4. **No Transparency**: All colors use alpha = 255 (GBA has no alpha blending)
5. **Shadow Offset**: Kept at 1 pixel down and 1 pixel right (correct)
6. **Text Truncation**: Added binary search algorithm to truncate text that exceeds 72 pixels width
7. **Text Padding**: Added 4-pixel padding from edges (usable width: 72 of 80 pixels)
8. **Vertical Constraints**: Text positioned at Y+3 with ~18 pixels available height

### pokeemerald Text Appearance
- **Text Color**: White (standard for map popups)
- **Shadow Color**: Dark gray (DARK_GRAY from pokeemerald text palette)
- **Shadow Offset**: 1 pixel down, 1 pixel right
- **Font Height**: ~8-9 pixels (GBA small font)

### pokeemerald Text Constraints
- Background: 80×24 pixels (fixed size)
- Text usable width: ~72 pixels (with 4px padding on each side)
- Text usable height: ~18 pixels (3px top offset, 3px bottom margin)
- Text is centered horizontally within the 80-pixel width
- Text is positioned 3 pixels from the top of the background

## Testing
To verify the fix:
1. Trigger a map transition that shows a popup
2. Verify the border tiles properly frame the background texture
3. Verify corners align correctly at all four corners
4. Verify edge tiles connect seamlessly with corners
5. Verify text is visible and fits within the popup borders
6. Verify text doesn't overflow the 80-pixel width
7. Test with long map names to verify truncation works

## Related Documentation
- `MonoBallFramework.Game/Engine/Rendering/Popups/README.md` - Popup rendering system overview
- `docs/analysis/map-popup-tiles.md` - Tile structure documentation
- `docs/analysis/map-popup-rendering-updates.md` - Rendering algorithm details

