using Arch.Core;
using PokeSharp.Core.Components;
using PokeSharp.Rendering.Assets;

namespace PokeSharp.Rendering.Loaders;

/// <summary>
///     Loads Tiled maps and converts them to ECS components.
/// </summary>
public class MapLoader
{
    private readonly AssetManager _assetManager;
    private readonly Dictionary<string, int> _mapNameToId = new();
    private int _nextMapId = 0;

    /// <summary>
    ///     Initializes a new instance of the MapLoader class.
    /// </summary>
    /// <param name="assetManager">Asset manager for texture loading.</param>
    public MapLoader(AssetManager assetManager)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
    }

    /// <summary>
    ///     Loads a complete map by creating tile entities for each non-empty tile.
    ///     This is the new ECS-based approach where every tile is an entity with components.
    ///     Also creates a MapInfo entity to store map metadata.
    /// </summary>
    /// <param name="world">The ECS world to create entities in.</param>
    /// <param name="mapPath">Path to the Tiled JSON map file.</param>
    /// <returns>The MapInfo entity containing map metadata.</returns>
    public Entity LoadMapEntities(World world, string mapPath)
    {
        var tmxDoc = TiledMapLoader.Load(mapPath);
        var mapId = GetMapId(mapPath);
        var tileset =
            tmxDoc.Tilesets.FirstOrDefault()
            ?? throw new InvalidOperationException($"Map '{mapPath}' has no tilesets");

        var tilesetId = ExtractTilesetId(tileset, mapPath);

        // Ensure tileset texture is loaded
        if (!_assetManager.HasTexture(tilesetId))
            LoadTilesetTexture(tileset, mapPath, tilesetId);

        int tilesCreated = 0;

        // Create entity for each non-empty tile across all layers
        for (int layerIndex = 0; layerIndex < 3; layerIndex++)
        {
            var layerData = GetLayerData(tmxDoc, layerIndex);
            if (layerData == null)
                continue;

            var tileLayer = (TileLayer)layerIndex;

            for (int y = 0; y < tmxDoc.Height; y++)
            {
                for (int x = 0; x < tmxDoc.Width; x++)
                {
                    int tileGid = layerData[y, x];
                    if (tileGid == 0)
                        continue; // Skip empty tiles

                    CreateTileEntity(world, x, y, mapId, tileGid, tileset, tileLayer);
                    tilesCreated++;
                }
            }
        }

        // Create MapInfo entity for map metadata
        var mapName = Path.GetFileNameWithoutExtension(mapPath);
        var mapInfo = new MapInfo(mapId, mapName, tmxDoc.Width, tmxDoc.Height, tmxDoc.TileWidth);
        var mapInfoEntity = world.Create(mapInfo);

        // Create TilesetInfo entity for tileset metadata
        var tilesetInfo = new TilesetInfo(
            tilesetId,
            tileset.FirstGid,
            tileset.TileWidth,
            tileset.TileHeight,
            tileset.Image?.Width ?? 256,
            tileset.Image?.Height ?? 256
        );
        var tilesetEntity = world.Create(tilesetInfo);

        // Create animated tile entities
        var animatedTilesCreated = CreateAnimatedTileEntities(world, tmxDoc, tileset);

        Console.WriteLine($"✅ Loaded map: {mapName} ({tmxDoc.Width}x{tmxDoc.Height} tiles)");
        Console.WriteLine($"   MapId: {mapId}");
        Console.WriteLine($"   Created {tilesCreated} tile entities");
        Console.WriteLine($"   Created {animatedTilesCreated} animated tile entities");
        Console.WriteLine($"   MapInfo entity: {mapInfoEntity}");
        Console.WriteLine($"   TilesetInfo entity: {tilesetEntity}");
        Console.WriteLine(
            $"   Tileset: {tilesetId} ({tilesetInfo.TilesPerRow}x{tilesetInfo.TilesPerColumn} tiles)"
        );

        return mapInfoEntity;
    }

    private int CreateAnimatedTileEntities(World world, TmxDocument tmxDoc, TmxTileset tileset)
    {
        if (tileset.Animations.Count == 0)
            return 0;

        int created = 0;

        foreach (var kvp in tileset.Animations)
        {
            var localTileId = kvp.Key;
            var animation = kvp.Value;

            // Convert local tile ID to global tile ID
            var globalTileId = tileset.FirstGid + localTileId;

            // Convert frame local IDs to global IDs
            var globalFrameIds = animation
                .FrameTileIds.Select(id => tileset.FirstGid + id)
                .ToArray();

            // Create AnimatedTile component
            var animatedTile = new AnimatedTile(
                globalTileId,
                globalFrameIds,
                animation.FrameDurations
            );

            // Find all tile entities with this tile ID and add AnimatedTile component
            var tileQuery = new QueryDescription().WithAll<TileSprite>();
            world.Query(
                in tileQuery,
                (Entity entity, ref TileSprite sprite) =>
                {
                    if (sprite.TileGid == globalTileId)
                    {
                        world.Add(entity, animatedTile);
                        created++;
                    }
                }
            );
        }

        return created;
    }

    // Obsolete methods removed - they referenced TileMap and TileProperties components which no longer exist
    // Use LoadMapEntities() instead

    private static string ExtractTilesetId(TmxTileset tileset, string mapPath)
    {
        // If tileset has an image, use the image filename as ID
        if (tileset.Image != null && !string.IsNullOrEmpty(tileset.Image.Source))
            return Path.GetFileNameWithoutExtension(tileset.Image.Source);

        // Fallback to tileset name
        return tileset.Name ?? "default-tileset";
    }

    private void LoadTilesetTexture(TmxTileset tileset, string mapPath, string tilesetId)
    {
        if (tileset.Image == null || string.IsNullOrEmpty(tileset.Image.Source))
            throw new InvalidOperationException("Tileset has no image source");

        // Resolve relative path from map directory
        var mapDirectory = Path.GetDirectoryName(mapPath) ?? string.Empty;
        var tilesetPath = Path.Combine(mapDirectory, tileset.Image.Source);

        // Make path relative to Assets root for AssetManager
        var assetsRoot = "Assets";
        var relativePath = Path.GetRelativePath(assetsRoot, tilesetPath);

        _assetManager.LoadTexture(tilesetId, relativePath);
    }

    private int GetMapId(string mapPath)
    {
        var mapName = Path.GetFileNameWithoutExtension(mapPath);

        // Get or create unique map ID
        if (_mapNameToId.TryGetValue(mapName, out var existingId))
            return existingId;

        var newId = _nextMapId++;
        _mapNameToId[mapName] = newId;
        return newId;
    }

    /// <summary>
    ///     Gets the map ID for a map name without loading it.
    /// </summary>
    /// <param name="mapName">The map name (without extension).</param>
    /// <returns>Map ID if the map has been loaded, -1 otherwise.</returns>
    public int GetMapIdByName(string mapName)
    {
        return _mapNameToId.TryGetValue(mapName, out var id) ? id : -1;
    }

    private int[,]? GetLayerData(TmxDocument tmxDoc, int layerIndex)
    {
        var layerName = layerIndex switch
        {
            0 => "Ground",
            1 => "Objects",
            2 => "Overhead",
            _ => null,
        };

        if (layerName == null)
            return null;

        var layer = tmxDoc.Layers.FirstOrDefault(l =>
            l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)
        );

        return layer?.Data;
    }

    private void CreateTileEntity(
        World world,
        int x,
        int y,
        int mapId,
        int tileGid,
        TmxTileset tileset,
        TileLayer layer
    )
    {
        // Always add position and sprite
        var position = new TilePosition(x, y, mapId);
        var sprite = new TileSprite(
            tileset.Name ?? "default",
            tileGid,
            layer,
            CalculateSourceRect(tileGid, tileset)
        );

        // Create entity with base components
        var entity = world.Create(position, sprite);

        // Get tile properties from tileset (convert global ID to local ID)
        int localTileId = tileGid - tileset.FirstGid;
        if (localTileId >= 0 && tileset.TileProperties.TryGetValue(localTileId, out var props))
        {
            // Check if this is a ledge tile (needs special handling)
            bool isLedge = props.ContainsKey("ledge_direction");

            // Add Collision component if tile is solid OR is a ledge (ledges are always solid with directional exceptions)
            if (props.TryGetValue("solid", out var solidValue) || isLedge)
            {
                bool isSolid = false;

                if (solidValue != null)
                {
                    // Handle different value types from JSON
                    isSolid = solidValue switch
                    {
                        bool b => b,
                        string s => bool.TryParse(s, out var result) && result,
                        _ => false,
                    };
                }
                else if (isLedge)
                {
                    // Ledges are always solid (just with directional exceptions)
                    isSolid = true;
                }

                if (isSolid)
                {
                    world.Add(entity, new Collision(true));
                }
            }

            // Add TileLedge component for ledges
            if (props.TryGetValue("ledge_direction", out var ledgeValue))
            {
                // Handle different value types from JSON
                string? ledgeDir = ledgeValue switch
                {
                    string s => s,
                    _ => ledgeValue?.ToString(),
                };

                if (!string.IsNullOrEmpty(ledgeDir))
                {
                    var jumpDirection = ledgeDir.ToLower() switch
                    {
                        "down" => Direction.Down,
                        "up" => Direction.Up,
                        "left" => Direction.Left,
                        "right" => Direction.Right,
                        _ => Direction.None,
                    };

                    if (jumpDirection != Direction.None)
                    {
                        world.Add(entity, new TileLedge(jumpDirection));
                    }
                }
            }

            // Add EncounterZone component if encounter rate exists
            if (
                props.TryGetValue("encounter_rate", out var encounterRateValue)
                && encounterRateValue is int encounterRate
                && encounterRate > 0
            )
            {
                var encounterTableId = props.TryGetValue("encounter_table", out var tableValue)
                    ? tableValue.ToString() ?? ""
                    : "";

                world.Add(entity, new EncounterZone(encounterTableId, encounterRate));
            }

            // Add TerrainType component if terrain type exists
            if (
                props.TryGetValue("terrain_type", out var terrainValue)
                && terrainValue is string terrainType
            )
            {
                var footstepSound = props.TryGetValue("footstep_sound", out var soundValue)
                    ? soundValue.ToString() ?? ""
                    : "";

                world.Add(entity, new TerrainType(terrainType, footstepSound));
            }

            // Add TileScript component if script path exists
            if (
                props.TryGetValue("script", out var scriptValue) && scriptValue is string scriptPath
            )
            {
                world.Add(entity, new TileScript(scriptPath));
            }
        }
    }

    private Microsoft.Xna.Framework.Rectangle CalculateSourceRect(int tileGid, TmxTileset tileset)
    {
        // Convert global ID to local ID
        int localTileId = tileGid - tileset.FirstGid;

        // Get tileset dimensions
        int tileWidth = tileset.TileWidth;
        int tileHeight = tileset.TileHeight;
        int tilesPerRow = tileset.Image?.Width / tileWidth ?? 1;

        // Calculate source rectangle
        int tileX = localTileId % tilesPerRow;
        int tileY = localTileId / tilesPerRow;

        return new Microsoft.Xna.Framework.Rectangle(
            tileX * tileWidth,
            tileY * tileHeight,
            tileWidth,
            tileHeight
        );
    }
}
