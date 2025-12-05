# Map Popup Name Formatting Fix

## Date
2024-12-05

## Issue
Map popup text needs to properly display the map's name by:
1. Using the correct name source (RegionSection or DisplayName)
2. Stripping the "MAPSEC_" prefix from region sections
3. Formatting underscored names properly (e.g., "ROUTE_101" → "Route 101")

## How Map Names Work

### Two Name Types

1. **DisplayName** (Component: `DisplayName`)
   - Human-readable name for the map
   - Examples: "Littleroot Town", "Route 101", "Oldale Town"
   - Set in Tiled map custom property: `displayName`
   - **This is what shows in the map popup**

2. **RegionMapSection** (Component: `RegionSection`)
   - Region section identifier for Town Map highlighting
   - Examples: "MAPSEC_LITTLEROOT_TOWN", "ROUTE_101", "MAPSEC_OLDALE_TOWN"
   - Set in Tiled map custom property: `regionMapSection`
   - Optional - not all maps have this
   - Used for Town Map system, NOT for popup display

### What Shows in the Popup

The map popup displays the **DisplayName** (`evt.ToMapName`):
- This is the `displayName` custom property from your Tiled map
- Should be properly formatted (e.g., "Littleroot Town", "Route 101")
- Always human-readable, no prefixes or underscores

## The Fix

### File Changed
`MonoBallFramework.Game/Engine/Scenes/Services/MapPopupService.cs`

### Changes Made

**Simplified to use DisplayName directly** (Line 83):
```csharp
// Use the map's DisplayName (evt.ToMapName) for the popup
// This is the human-readable map name set in Tiled's displayName property
string displayName = evt.ToMapName;
```

**Why the simplification:**
- The popup should show the map's **DisplayName**, not the RegionSection
- DisplayName is already properly formatted in Tiled
- RegionSection is for the Town Map system, not popup display
- No need for prefix stripping or formatting - DisplayName is clean

## Examples

### Example 1: Standard Map
**Map Properties:**
- `displayName`: "Littleroot Town"
- `regionMapSection`: "MAPSEC_LITTLEROOT_TOWN" (for Town Map)

**Popup shows:** "Littleroot Town" ✅

### Example 2: Route Map
**Map Properties:**
- `displayName`: "Route 101"
- `regionMapSection`: "ROUTE_101" (for Town Map)

**Popup shows:** "Route 101" ✅

### Example 3: Indoor Map
**Map Properties:**
- `displayName`: "Petalburg Woods"
- `regionMapSection`: (not set - indoor locations don't need Town Map highlighting)

**Popup shows:** "Petalburg Woods" ✅

## How to Configure Map Names in Tiled

### Required Property
Always set the `displayName` custom property with a properly formatted name:

```
displayName: "Littleroot Town"
```
The popup will show: **"Littleroot Town"**

### Optional Property (For Town Map System)
Optionally set `regionMapSection` for Town Map highlighting:

```
displayName: "Littleroot Town"
regionMapSection: "MAPSEC_LITTLEROOT_TOWN"
```
- Popup shows: **"Littleroot Town"** (uses displayName)
- Town Map uses: `MAPSEC_LITTLEROOT_TOWN` (for highlighting)

### Formatting Rules for DisplayName
Format your `displayName` properly in Tiled:
- ✅ Use Title Case: "Littleroot Town"
- ✅ Use spaces: "Route 101"
- ✅ Use proper punctuation: "Mt. Pyre Summit"
- ❌ Don't use underscores: "LITTLEROOT_TOWN"
- ❌ Don't use all caps: "LITTLEROOT TOWN"
- ❌ Don't use prefixes: "MAPSEC_LITTLEROOT_TOWN"

## Name Display Logic

```
1. Get evt.ToMapName (from DisplayName component)
   ↓
2. Use directly - no formatting needed
   (DisplayName is already properly formatted in Tiled)
   ↓
3. Display in popup
```

**Simple and straightforward:**
- The popup shows whatever you put in `displayName` in Tiled
- Format it nicely in Tiled, it shows nicely in game
- RegionSection is ignored for popup display (used only for Town Map)

## Testing

To verify the fix works:

1. **Set displayName in Tiled:**
   - Set `displayName: "Littleroot Town"`
   - Trigger map transition
   - Verify popup shows "Littleroot Town"

2. **Test with Route:**
   - Set `displayName: "Route 101"`
   - Trigger map transition
   - Verify popup shows "Route 101"

3. **Test with special characters:**
   - Set `displayName: "Mt. Pyre Summit"`
   - Trigger map transition
   - Verify popup shows "Mt. Pyre Summit"

4. **Check logs:**
   - Look for log message: "Displayed map popup: '...' (Map: ..., Region: ...)"
   - Verify the displayed name matches your displayName property

## Related Components

### ECS Components
- `DisplayName` (`MonoBallFramework.Game/Ecs/Components/Maps/DisplayName.cs`)
  - Human-readable map name
- `RegionSection` (`MonoBallFramework.Game/Ecs/Components/Maps/RegionSection.cs`)
  - Region map section identifier

### Event
- `MapTransitionEvent` (`MonoBallFramework.Game/Engine/Core/Events/Map/MapTransitionEvent.cs`)
  - `ToMapName`: Map's display name
  - `RegionName`: Map's region section

### Services
- `MapPopupService` (`MonoBallFramework.Game/Engine/Scenes/Services/MapPopupService.cs`)
  - Handles the name formatting and popup display

## Best Practices

1. **Always set `displayName`** - this is the human-readable fallback
2. **Set `regionMapSection` for main maps** - routes, towns, cities, dungeons
3. **Use consistent naming:**
   - DisplayName: Title Case with proper spacing ("Route 101")
   - RegionSection: Either "MAPSEC_NAME" or "NAME" format
4. **Omit `regionMapSection` for:** Indoor maps, minor locations, buildings
5. **The popup will handle formatting** - you don't need to format region sections

## Summary

This implementation correctly displays the map's DisplayName:
- ✅ Shows the `displayName` property from Tiled maps
- ✅ No unnecessary formatting or processing
- ✅ Clean and straightforward
- ✅ RegionSection is kept separate for Town Map functionality

The result is map popups that display exactly what you configure in Tiled's `displayName` property!

