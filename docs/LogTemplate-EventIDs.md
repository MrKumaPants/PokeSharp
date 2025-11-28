# LogTemplates Event ID Registry

**Version:** 1.0.0
**Last Updated:** 2025-11-16

## Event ID Ranges

| Range | Category | Description |
|-------|----------|-------------|
| 1000-1999 | Rendering | Sprite loading, animation, texture management, render passes |
| 2000-2999 | Systems | ECS systems, spatial hash, pooling, pathfinding |
| 3000-3999 | Map Loading | Tiled map parsing, tileset loading, object spawning |
| 4000-4999 | Data Loading | Templates, assets, database, deserialization |
| 5000-5999 | Scripting | Hot-reload, compilation, NPC behaviors |

## Rendering Templates (1000-1999)

| Event ID | Method | Level | Description |
|----------|--------|-------|-------------|
| 1000 | LogSpriteAnimationUpdated | Debug | Sprite animation frame update |
| 1001 | LogSpriteTextureLoaded | Information | Sprite texture loaded successfully |
| 1002 | LogSpriteManifestLoaded | Debug | Sprite manifest loaded with animation count |
| 1003 | LogSpriteNotFound | Warning | Sprite not found in loader cache |
| 1004 | LogRenderPassCompleted | Debug | Render pass completed with draw statistics |
| 1005 | LogElevationLayerRendered | Debug | Elevation layer rendering statistics |

## Systems Templates (2000-2999)

| Event ID | Method | Level | Description |
|----------|--------|-------|-------------|
| 2000 | LogSystemRegistered | Information | ECS system registered with priority |
| 2001 | LogSystemLifecycle | Information | System lifecycle event (enable/disable) |
| 2002 | LogSystemUpdateCompleted | Debug | System update loop completed with timing |
| 2003 | LogSpatialHashRebuilt | Debug | Spatial hash rebuilt with statistics |
| 2004 | LogComponentPoolCreated | Debug | Component pool created with capacity |
| 2005 | LogComponentPoolStats | Debug | Component pooling hit rate statistics |
| 2006 | LogSystemDependencyNotFound | Warning | System dependency not found |
| 2007 | LogPathfindingCompleted | Debug | Pathfinding computation completed |

## Map Loading Templates (3000-3999)

| Event ID | Method | Level | Description |
|----------|--------|-------|-------------|
| 3000 | LogMapLoadingStarted | Information | Map loading started |
| 3001 | LogMapDefinitionLoaded | Debug | Map definition loaded from database |
| 3002 | LogTilesetLoaded | Information | Tileset loaded successfully |
| 3003 | LogExternalTilesetLoaded | Debug | External tileset file loaded |
| 3004 | LogTileLayerParsed | Debug | Tile layer parsed with dimensions |
| 3005 | LogAnimatedTilesCreated | Debug | Animated tile entities created |
| 3006 | LogImageLayerCreated | Debug | Image layer created |
| 3007 | LogMapObjectSpawned | Debug | Map object spawned from template |
| 3008 | LogNpcDefinitionApplied | Information | NPC definition applied to entity |
| 3009 | LogSpriteCollectionCompleted | Information | Sprite collection for lazy loading |
| 3010 | LogMapTexturesTracked | Debug | Map texture tracking initialized |

## Data Loading Templates (4000-4999)

| Event ID | Method | Level | Description |
|----------|--------|-------|-------------|
| 4000 | LogGameDataLoaderInitialized | Information | Game data loader initialized |
| 4001 | LogDatabaseMigrated | Information | Database migration completed |
| 4002 | LogTemplateLoaded | Debug | Entity template loaded from JSON |
| 4003 | LogDeserializerRegistered | Debug | Component deserializer registered |
| 4004 | LogAssetLoadedWithType | Debug | Asset loaded with type and timing |
| 4005 | LogAssetCacheHit | Debug | Asset cache hit |
| 4006 | LogAssetCacheMiss | Debug | Asset cache miss with load required |
| 4007 | LogAssetEvicted | Debug | Asset evicted from LRU cache |
| 4008 | LogTypeRegistered | Debug | Type registered in type registry |

## Scripting Templates (5000-5999)

| Event ID | Method | Level | Description |
|----------|--------|-------|-------------|
| 5000 | LogScriptCompilationStarted | Information | Script compilation started |
| 5001 | LogScriptCompilationSucceeded | Information | Script compilation succeeded |
| 5002 | LogScriptCompilationFailed | Error | Script compilation failed |
| 5003 | LogScriptDiagnosticError | Error | Script compilation diagnostic error |
| 5004 | LogScriptRollback | Warning | Script hot-reload rollback performed |
| 5005 | LogHotReloadStarted | Information | Hot-reload service started |
| 5006 | LogScriptChangeDebounced | Debug | Script change event debounced |
| 5007 | LogScriptBackupCreated | Debug | Script backup created before compilation |
| 5008 | LogNpcBehaviorAttached | Information | NPC behavior script attached |
| 5009 | LogScriptExecutionError | Error | Script execution error |
| 5010 | LogScriptWatcherError | Error | Script watcher error occurred |

## Color Scheme

- **Success**: `[green]✓[/]`
- **Names/IDs**: `[cyan]`
- **Values/Counts**: `[yellow]`
- **Warnings**: `[orange1]⚠[/]`
- **Errors**: `[red]✗[/]`
- **Debug info**: `[grey]`
- **Dimensions/Secondary**: `[dim]`

## Usage Notes

1. All templates use LoggerMessage source generator for zero-allocation logging
2. Event IDs are immutable - never reuse or change assigned IDs
3. Keep log levels consistent with performance impact:
   - **Debug**: Per-frame or high-frequency operations
   - **Information**: Lifecycle events, successful operations
   - **Warning**: Recoverable issues, missing optional data
   - **Error**: Failures, critical issues

## Adding New Templates

1. Choose appropriate event ID range based on category
2. Use next available ID in the range
3. Follow naming convention: `Log{Action}{Subject}` (e.g., `LogMapLoaded`)
4. Include Spectre color markup for readability
5. Update this registry document
6. Add unit test in `LoggingTests`
