# Map Popup Text Rendering Fix

## Date
2024-12-05

## Issue
Map popup text was rendering with incorrect colors and appeared too large, causing text to overflow the popup borders.

## Problems Identified

### 1. **Font Size Adjustment**
- **Original**: 16pt font (way too large!)
- **First fix**: 9pt font (too small when scaled)
- **Final**: 12pt font (correct size for modern resolution)
- **Reason**: GBA fonts are 8px tall at native 240x160 resolution. When rendering at higher resolutions, we need to scale appropriately. 12pt provides the right visual weight.

### 2. **Wrong Text Color**
- **Before**: Dark navy blue `new Color(32, 56, 144)`
- **After**: Opaque white `new Color(255, 255, 255, 255)`
- **Reason**: Pokeemerald uses white text (TEXT_COLOR_WHITE) for map location popups. Must be fully opaque with no transparency.

### 3. **Wrong Shadow Color**
- **Before**: Semi-transparent black `new Color(0, 0, 0, 128)` (alpha = 128)
- **After**: Opaque dark gray `new Color(72, 72, 80, 255)` (alpha = 255)
- **Reason**: Pokeemerald uses DARK_GRAY from its text palette. GBA has no alpha blending - all pixels are fully opaque.

### 4. **Shadow Offset** ✓
- **Correct**: 1 pixel down, 1 pixel right
- This was already correct in the original implementation.

### 5. **Missing Text Truncation**
- Added binary search algorithm to truncate text that exceeds the available width
- Prevents text from overflowing the 80-pixel-wide background

### 6. **No Alpha Blending** (GBA Hardware Limitation)
- **Issue**: Original code used semi-transparent shadow (alpha = 128)
- **Fix**: All colors now use alpha = 255 (fully opaque)
- **Reason**: The GBA has no alpha blending capability. All pixels are either fully opaque or fully transparent. Text rendering on GBA uses solid colors only.

## The Fix

### File Changed
`MonoBallFramework.Game/Engine/Scenes/Scenes/MapPopupScene.cs`

### Changes Made

1. **Font Size (Lines 178, 202)**:
```csharp
// Before:
_font = _fontSystem.GetFont(16); // Too large!

// After:
_font = _fontSystem.GetFont(12); // Correct size for scaled rendering
```

2. **Text Colors (Lines 464-477)** - **Fully Opaque**:
```csharp
// Before:
_spriteBatch.DrawString(_font, displayText, 
    new Vector2(textX + 1, textY + 1),
    new Color(0, 0, 0, 128)); // Semi-transparent black (wrong!)

_spriteBatch.DrawString(_font, displayText, 
    new Vector2(textX, textY),
    new Color(32, 56, 144)); // Dark navy blue (wrong!)

// After:
_spriteBatch.DrawString(_font, displayText, 
    new Vector2(textX + 1, textY + 1),
    new Color(72, 72, 80, 255)); // Dark gray shadow - FULLY OPAQUE

_spriteBatch.DrawString(_font, displayText, 
    new Vector2(textX, textY),
    new Color(255, 255, 255, 255)); // White text - FULLY OPAQUE
```

**Critical**: Both colors have alpha = 255 (fully opaque). GBA has no alpha blending!

3. **Text Truncation (Lines 413-453)**:
Added binary search algorithm to find the optimal truncation point if text exceeds 72 pixels width (80 pixels minus 4-pixel padding on each side).

## Pokeemerald Text Rendering Standard

### Text Appearance
- **Main Text**: White color (255, 255, 255, 255) - TEXT_COLOR_WHITE
- **Shadow**: Dark gray color (72, 72, 80, 255) - TEXT_COLOR_DARK_GRAY
- **Shadow Offset**: 1 pixel down and 1 pixel to the right
- **Rendering Order**: Shadow drawn first, then main text on top
- **Alpha Channel**: Both colors use alpha = 255 (fully opaque, no transparency)

### Font Specifications
- **Font Type**: Bitmap font (GBA hardware limitation)
- **Native Size**: 8 pixels tall at GBA resolution (240x160)
- **Rendered Size**: 12pt (appropriate for modern higher resolutions)
- **Character Width**: Variable (proportional font)
- **Rendering**: Fully opaque, no alpha blending (GBA limitation)

### Layout Constraints
- **Background Size**: 80×24 pixels (fixed)
- **Text Area Width**: 72 pixels (80 - 4px left padding - 4px right padding)
- **Text Area Height**: ~18 pixels (24 - 3px top offset - 3px bottom margin)
- **Text Position**: Centered horizontally, 3 pixels from top
- **Truncation**: Text that exceeds 72 pixels width is truncated

## Color Values Reference

### Pokeemerald Text Palette Colors
Based on pokeemerald's text rendering system:

| Color Name    | RGB Values     | Hex       | Usage                |
|---------------|----------------|-----------|----------------------|
| WHITE         | (248, 248, 248)| #F8F8F8   | Main text            |
| DARK_GRAY     | (80, 80, 88)   | #505058   | Shadow               |
| LIGHT_GRAY    | (184, 184, 184)| #B8B8B8   | Alternate text       |
| BLACK         | (0, 0, 0)      | #000000   | High contrast shadow |

### Why White Text?
In pokeemerald, map location popups use light-colored backgrounds (beige, wood, stone textures). White text with a dark gray shadow provides:
1. Maximum readability against light backgrounds
2. Clear contrast without being harsh
3. Consistent with GBA Pokemon games visual style

## Visual Comparison

### Before Fix
```
┌─────────────────────┐
│  ╔═══════════════╗  │
│  ║ ROUTE 101      ║  │  ← Dark blue text (wrong)
│  ║               ║  │     Too large, wrong color
│  ╚═══════════════╝  │     Overflow issues
└─────────────────────┘
```

### After Fix
```
┌─────────────────────┐
│  ╔═══════════════╗  │
│  ║ ROUTE 101     ║  │  ← White text (correct!)
│  ║               ║  │     Small, centered, readable
│  ╚═══════════════╝  │     Fits perfectly
└─────────────────────┘
```

## Testing

To verify the fix:
1. Trigger a map transition popup
2. Verify text is **white** (not blue)
3. Verify text has **dark gray shadow** (1px down-right)
4. Verify text is **small enough** to fit within the 24-pixel height
5. Verify text is **centered** within the 80-pixel width
6. Test with **long map names** to verify truncation
7. Verify text is **readable** against the background

## Related Issues
- Border rendering coordinate fix (see `popup-coordinate-fix.md`)
- Font size adjustment
- Text truncation algorithm

## References
- Pokeemerald `src/map_name_popup.c` - Text rendering implementation
- Pokeemerald text palette definitions
- GBA hardware text rendering limitations

