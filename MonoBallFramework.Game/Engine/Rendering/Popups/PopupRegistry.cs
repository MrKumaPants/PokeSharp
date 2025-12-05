using System.Collections.Concurrent;
using System.Text.Json;

namespace MonoBallFramework.Game.Engine.Rendering.Popups;

/// <summary>
///     Registry for popup definitions (backgrounds and outlines).
///     Manages available backgrounds and outlines for region/map popups.
///     Based on pokeemerald's region popup system where backgrounds and outlines
///     are separate and can be mixed and matched per map.
/// </summary>
/// <remarks>
///     <para>
///         <b>Phase 5 Optimizations:</b>
///         - Thread-safe concurrent dictionaries for registration during parallel loading
///         - Async loading with parallel file I/O for backgrounds and outlines
///         - SemaphoreSlim to prevent concurrent LoadDefinitionsAsync calls
///         - CancellationToken support for graceful shutdown
///     </para>
/// </remarks>
public class PopupRegistry
{
    private readonly ConcurrentDictionary<string, PopupBackgroundDefinition> _backgrounds = new();
    private readonly ConcurrentDictionary<string, PopupOutlineDefinition> _outlines = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private string _defaultBackgroundId = "wood";
    private string _defaultOutlineId = "wood_outline";
    private volatile bool _isLoaded = false;

    /// <summary>
    /// Gets whether definitions have been loaded.
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    ///     Registers a background definition.
    ///     Thread-safe for parallel loading.
    /// </summary>
    public void RegisterBackground(PopupBackgroundDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrEmpty(definition.Id);
        _backgrounds.TryAdd(definition.Id, definition);
    }

    /// <summary>
    ///     Registers an outline definition.
    ///     Thread-safe for parallel loading.
    /// </summary>
    public void RegisterOutline(PopupOutlineDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrEmpty(definition.Id);
        _outlines.TryAdd(definition.Id, definition);
    }

    /// <summary>
    ///     Gets a background definition by ID.
    /// </summary>
    public PopupBackgroundDefinition? GetBackground(string backgroundId)
    {
        return _backgrounds.TryGetValue(backgroundId, out PopupBackgroundDefinition? definition) ? definition : null;
    }

    /// <summary>
    ///     Gets an outline definition by ID.
    /// </summary>
    public PopupOutlineDefinition? GetOutline(string outlineId)
    {
        return _outlines.TryGetValue(outlineId, out PopupOutlineDefinition? definition) ? definition : null;
    }

    /// <summary>
    ///     Gets the default background definition.
    /// </summary>
    public PopupBackgroundDefinition? GetDefaultBackground()
    {
        return GetBackground(_defaultBackgroundId);
    }

    /// <summary>
    ///     Gets the default outline definition.
    /// </summary>
    public PopupOutlineDefinition? GetDefaultOutline()
    {
        return GetOutline(_defaultOutlineId);
    }

    /// <summary>
    ///     Sets the default background and outline IDs.
    /// </summary>
    public void SetDefaults(string backgroundId, string outlineId)
    {
        ArgumentException.ThrowIfNullOrEmpty(backgroundId);
        ArgumentException.ThrowIfNullOrEmpty(outlineId);
        _defaultBackgroundId = backgroundId;
        _defaultOutlineId = outlineId;
    }

    /// <summary>
    ///     Gets all registered background IDs.
    /// </summary>
    public IEnumerable<string> GetAllBackgroundIds()
    {
        return _backgrounds.Keys;
    }

    /// <summary>
    ///     Gets all registered outline IDs.
    /// </summary>
    public IEnumerable<string> GetAllOutlineIds()
    {
        return _outlines.Keys;
    }

    /// <summary>
    ///     Loads all popup definitions from the Data/Maps/Popups folder.
    /// </summary>
    public void LoadDefinitions(bool loadFromJson = true)
    {
        if (loadFromJson)
        {
            LoadDefinitionsFromJson();
        }
        else
        {
            LoadDefinitionsHardcoded();
        }
        _isLoaded = true;
    }

    /// <summary>
    ///     Loads popup definitions asynchronously with parallel file I/O.
    ///     Thread-safe - concurrent calls will wait for the first load to complete.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Performance:</b> Backgrounds and outlines are loaded in parallel using Task.WhenAll,
    ///         reducing total load time by ~50% compared to sequential loading.
    ///     </para>
    /// </remarks>
    public async Task LoadDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: already loaded
        if (_isLoaded) return;

        // Prevent concurrent loads
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_isLoaded) return;

            string dataPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "Maps", "Popups");

            if (!Directory.Exists(dataPath))
            {
                LoadDefinitionsHardcoded();
                _isLoaded = true;
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // Load backgrounds and outlines IN PARALLEL
            string backgroundsPath = Path.Combine(dataPath, "Backgrounds");
            string outlinesPath = Path.Combine(dataPath, "Outlines");

            Task backgroundsTask = LoadBackgroundsAsync(backgroundsPath, options, cancellationToken);
            Task outlinesTask = LoadOutlinesAsync(outlinesPath, options, cancellationToken);

            await Task.WhenAll(backgroundsTask, outlinesTask);

            // Fallback if nothing loaded
            if (_backgrounds.IsEmpty || _outlines.IsEmpty)
            {
                LoadDefinitionsHardcoded();
            }

            _isLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    ///     Loads background definitions from JSON files asynchronously.
    /// </summary>
    private async Task LoadBackgroundsAsync(string path, JsonSerializerOptions options, CancellationToken ct)
    {
        if (!Directory.Exists(path)) return;

        string[] jsonFiles = Directory.GetFiles(path, "*.json");

        // Load all background files in parallel
        var tasks = jsonFiles.Select(async jsonFile =>
        {
            try
            {
                string json = await File.ReadAllTextAsync(jsonFile, ct);
                var definition = JsonSerializer.Deserialize<PopupBackgroundDefinition>(json, options);
                if (definition != null)
                {
                    RegisterBackground(definition);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { /* Skip invalid files */ }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    ///     Loads outline definitions from JSON files asynchronously.
    /// </summary>
    private async Task LoadOutlinesAsync(string path, JsonSerializerOptions options, CancellationToken ct)
    {
        if (!Directory.Exists(path)) return;

        string[] jsonFiles = Directory.GetFiles(path, "*.json");

        // Load all outline files in parallel
        var tasks = jsonFiles.Select(async jsonFile =>
        {
            try
            {
                string json = await File.ReadAllTextAsync(jsonFile, ct);
                var definition = JsonSerializer.Deserialize<PopupOutlineDefinition>(json, options);
                if (definition != null)
                {
                    RegisterOutline(definition);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { /* Skip invalid files */ }
        });

        await Task.WhenAll(tasks);
    }

    private void LoadDefinitionsFromJson()
    {
        string dataPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Data",
            "Maps",
            "Popups"
        );

        if (!Directory.Exists(dataPath))
        {
            LoadDefinitionsHardcoded();
            return;
        }

        // Load background definitions
        string backgroundsPath = Path.Combine(dataPath, "Backgrounds");
        if (Directory.Exists(backgroundsPath))
        {
            foreach (string jsonFile in Directory.GetFiles(backgroundsPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(jsonFile);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    PopupBackgroundDefinition? definition = JsonSerializer.Deserialize<PopupBackgroundDefinition>(
                        json,
                        options
                    );

                    if (definition != null)
                    {
                        RegisterBackground(definition);
                    }
                }
                catch (Exception)
                {
                    // Skip invalid files
                }
            }
        }

        // Load outline definitions
        string outlinesPath = Path.Combine(dataPath, "Outlines");
        if (Directory.Exists(outlinesPath))
        {
            foreach (string jsonFile in Directory.GetFiles(outlinesPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(jsonFile);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    PopupOutlineDefinition? definition = JsonSerializer.Deserialize<PopupOutlineDefinition>(
                        json,
                        options
                    );

                    if (definition != null)
                    {
                        RegisterOutline(definition);
                    }
                }
                catch (Exception)
                {
                    // Skip invalid files
                }
            }
        }

        // Fallback to hardcoded if nothing was loaded
        if (_backgrounds.Count == 0 || _outlines.Count == 0)
        {
            LoadDefinitionsHardcoded();
        }
    }

    private void LoadDefinitionsHardcoded()
    {
        // Register backgrounds
        RegisterBackground(new PopupBackgroundDefinition
        {
            Id = "stone",
            DisplayName = "Stone",
            TexturePath = "Graphics/Maps/Popups/Backgrounds/stone.png"
        });
        RegisterBackground(new PopupBackgroundDefinition
        {
            Id = "stone2",
            DisplayName = "Stone 2",
            TexturePath = "Graphics/Maps/Popups/Backgrounds/stone2.png"
        });
        RegisterBackground(new PopupBackgroundDefinition
        {
            Id = "wood",
            DisplayName = "Wood",
            TexturePath = "Graphics/Maps/Popups/Backgrounds/wood.png"
        });
        RegisterBackground(new PopupBackgroundDefinition
        {
            Id = "brick",
            DisplayName = "Brick",
            TexturePath = "Graphics/Maps/Popups/Backgrounds/brick.png"
        });
        RegisterBackground(new PopupBackgroundDefinition
        {
            Id = "marble",
            DisplayName = "Marble",
            TexturePath = "Graphics/Maps/Popups/Backgrounds/marble.png"
        });
        RegisterBackground(new PopupBackgroundDefinition
        {
            Id = "underwater",
            DisplayName = "Underwater",
            TexturePath = "Graphics/Maps/Popups/Backgrounds/underwater.png"
        });

        // Register outlines
        RegisterOutline(new PopupOutlineDefinition
        {
            Id = "stone_outline",
            DisplayName = "Stone Outline",
            TexturePath = "Graphics/Maps/Popups/Outlines/stone_outline.png"
        });
        RegisterOutline(new PopupOutlineDefinition
        {
            Id = "stone2_outline",
            DisplayName = "Stone 2 Outline",
            TexturePath = "Graphics/Maps/Popups/Outlines/stone2_outline.png"
        });
        RegisterOutline(new PopupOutlineDefinition
        {
            Id = "wood_outline",
            DisplayName = "Wood Outline",
            TexturePath = "Graphics/Maps/Popups/Outlines/wood_outline.png"
        });
        RegisterOutline(new PopupOutlineDefinition
        {
            Id = "brick_outline",
            DisplayName = "Brick Outline",
            TexturePath = "Graphics/Maps/Popups/Outlines/brick_outline.png"
        });
        RegisterOutline(new PopupOutlineDefinition
        {
            Id = "marble_outline",
            DisplayName = "Marble Outline",
            TexturePath = "Graphics/Maps/Popups/Outlines/marble_outline.png"
        });
        RegisterOutline(new PopupOutlineDefinition
        {
            Id = "underwater_outline",
            DisplayName = "Underwater Outline",
            TexturePath = "Graphics/Maps/Popups/Outlines/underwater_outline.png"
        });
    }
}
