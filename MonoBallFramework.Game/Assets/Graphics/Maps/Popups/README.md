# Map Popup Definitions

This folder contains the popup graphics used for displaying map/region names during transitions (pokeemerald-style).

## Folder Structure

```
Popups/
├── Outlines/          # Border/frame sprite sheets (9-slice)
│   ├── stone_outline.png
│   ├── stone2_outline.png
│   ├── wood_outline.png
│   ├── brick_outline.png
│   ├── marble_outline.png
│   └── underwater_outline.png
└── Backgrounds/       # Background fill textures
    ├── stone.png
    ├── stone2.png
    ├── wood.png
    ├── brick.png
    ├── marble.png
    └── underwater.png
```

## Definition Files

Background and outline definitions are stored separately in `Assets/Data/Maps/Popups/`:

```
Data/Maps/Popups/
├── Backgrounds/
│   ├── stone.json
│   ├── wood.json
│   └── ...
└── Outlines/
    ├── stone_outline.json
    ├── wood_outline.json
    └── ...
```

## Background Textures

Backgrounds are simple textures that get stretched or tiled to fill the popup area.

### Background Definition Format

Each background JSON file (`Backgrounds/*.json`):

```json
{
  "id": "wood",
  "displayName": "Wood",
  "texturePath": "Graphics/Maps/Popups/Backgrounds/wood.png",
  "defaultWidth": 200,
  "defaultHeight": 40
}
```

## Outline Sprite Sheets (9-Slice)

**IMPORTANT:** Outlines are **sprite sheets**, not simple textures!

They use **9-slice/9-patch rendering** to maintain pixel-perfect corners and edges without distortion. The sprite sheet is divided into 9 regions:

```
┌─────┬─────────┬─────┐
│ TL  │   Top   │ TR  │  TL/TR/BL/BR = Corners (never stretched)
├─────┼─────────┼─────┤  Top/Bottom = Horizontal edges (stretched horizontally)
│Left │ Center  │Right│  Left/Right = Vertical edges (stretched vertically)
├─────┼─────────┼─────┤  Center = Usually transparent for popups
│ BL  │ Bottom  │ BR  │
└─────┴─────────┴─────┘
```

### Outline Definition Format

Each outline JSON file (`Outlines/*.json`):

```json
{
  "id": "wood_outline",
  "displayName": "Wood Outline",
  "texturePath": "Graphics/Maps/Popups/Outlines/wood_outline.png",
  "cornerWidth": 8,
  "cornerHeight": 8,
  "borderWidth": 8,
  "borderHeight": 8
}
```

**Properties:**
- `cornerWidth`: Width in pixels of the left/right corner slices (how much of the sprite sheet is reserved for corners)
- `cornerHeight`: Height in pixels of the top/bottom corner slices
- `borderWidth`: How thick the border appears on screen (left/right)
- `borderHeight`: How thick the border appears on screen (top/bottom)

## Pokeemerald Behavior

In pokeemerald, backgrounds and outlines are **separate and can be mixed and matched** per map:
- A map can have a wood background with a stone outline
- Another map can have a brick background with a marble outline
- This allows for flexible customization per region/area

The current implementation defaults to wood background + wood outline for testing.

## How It Works

1. When a map transition occurs, `MapLifecycleManager` or `MapStreamingSystem` publishes a `MapTransitionEvent`
2. `MapPopupService` subscribes to this event
3. It gets the background and outline definitions from `PopupRegistry`
4. It creates a `MapPopupScene` with the region/map name and selected background/outline
5. The scene is pushed onto the scene stack as an overlay
6. **Pokeemerald-style**: The popup slides IN from the LEFT into the top-left corner
7. **Rendering**: Background is stretched, outline is rendered using 9-slice (corners stay sharp)
8. It displays for 2.5 seconds, then slides OUT to the LEFT
9. The scene is automatically popped from the stack when complete

## Animation Details (Pokeemerald-Accurate)

- **Position**: Top-left corner (no padding from edges)
- **Slide In**: 0.4 seconds from TOP (cubic ease-out)
- **Display**: 2.5 seconds
- **Slide Out**: 0.4 seconds to TOP (cubic ease-in)
- **Text Color**: White (pokeemerald standard)
- **Text Shadow**: Dark gray (1px down, 1px right offset)
- **Font**: 9pt Pokemon font (~8-9px tall, GBA accurate)
- **Rendering**: Tile-based border assembly (GBA accurate)

These settings match the original Pokémon Emerald behavior.

## Adding New Popup Styles

### Add a Background
1. Add your background texture to `Backgrounds/your_style.png`
2. Create `Data/Maps/Popups/Backgrounds/your_style.json`
3. The system will automatically load it on startup

### Add an Outline (Sprite Sheet)
1. Create a sprite sheet with 9-slice layout (corners, edges, center)
2. Add your outline sprite sheet to `Outlines/your_style_outline.png`
3. Create `Data/Maps/Popups/Outlines/your_style_outline.json`
4. Set `cornerWidth` and `cornerHeight` to match your sprite sheet's corner dimensions
5. The system will automatically load it on startup

### Mix and Match
In the future, each map will be able to specify which background and outline to use independently via map metadata.

## Technical Notes

- Backgrounds are rendered first (stretched to popup size)
- Outlines are rendered on top using 9-slice algorithm
- Center region of outline sprite sheet is typically transparent
- Corners maintain their exact pixel dimensions (never scaled)
- Edges are stretched along one axis only
- This prevents the "blurry corner" issue seen when stretching entire border sprites
