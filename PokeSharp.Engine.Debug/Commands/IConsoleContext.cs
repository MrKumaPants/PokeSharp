using Microsoft.Xna.Framework;
using PokeSharp.Engine.UI.Debug.Core;
using PokeSharp.Engine.UI.Debug.Interfaces;

namespace PokeSharp.Engine.Debug.Commands;

/// <summary>
/// Provides context and services for console command execution.
/// This interface exposes core console operations and panel interfaces as properties.
/// </summary>
/// <remarks>
/// <para>
/// Core operations are exposed directly on this interface:
/// - <see cref="IConsoleOutput"/> - Basic output operations
/// - <see cref="IConsoleLogging"/> - Logging control
/// - <see cref="IConsoleCommands"/> - Command registry
/// - <see cref="IConsoleHistory"/> - Command history
/// - <see cref="IConsoleAliases"/> - Alias management
/// - <see cref="IConsoleScripts"/> - Script operations
/// - <see cref="IConsoleBookmarks"/> - Bookmark management
/// - <see cref="IConsoleNavigation"/> - Tab navigation
/// - <see cref="IConsoleExport"/> - Console output export
/// </para>
/// <para>
/// Panel operations are exposed as properties to reduce boilerplate delegation:
/// - <see cref="Entities"/> - Entity browser operations
/// - <see cref="Watches"/> - Watch panel operations
/// - <see cref="Variables"/> - Variables panel operations
/// - <see cref="Logs"/> - Logs panel operations
/// </para>
/// <para>
/// Commands use panel properties directly: <c>context.Entities.Refresh()</c> instead of <c>context.RefreshEntities()</c>
/// </para>
/// </remarks>
public interface IConsoleContext :
    IConsoleOutput,
    IConsoleLogging,
    IConsoleCommands,
    IConsoleHistory,
    IConsoleAliases,
    IConsoleScripts,
    IConsoleBookmarks,
    IConsoleNavigation,
    IConsoleExport
{
    /// <summary>
    /// Gets the entity browser operations.
    /// Use for entity inspection, filtering, and management.
    /// </summary>
    /// <example>
    /// <code>
    /// context.Entities.Refresh();
    /// context.Entities.SetTagFilter("Player");
    /// var stats = context.Entities.GetStatistics();
    /// </code>
    /// </example>
    IEntityOperations Entities { get; }

    /// <summary>
    /// Gets the watch panel operations.
    /// Use for adding, removing, and managing watch expressions.
    /// </summary>
    /// <example>
    /// <code>
    /// context.Watches.Add("playerPos", "player.Position", () => player.Position);
    /// context.Watches.Pin("playerPos");
    /// </code>
    /// </example>
    IWatchOperations Watches { get; }

    /// <summary>
    /// Gets the variables panel operations.
    /// Use for viewing and managing script variables.
    /// </summary>
    /// <example>
    /// <code>
    /// context.Variables.SetSearchFilter("position");
    /// context.Variables.Expand("player");
    /// </code>
    /// </example>
    IVariableOperations Variables { get; }

    /// <summary>
    /// Gets the logs panel operations.
    /// Use for filtering and exporting logs.
    /// </summary>
    /// <example>
    /// <code>
    /// context.Logs.SetFilterLevel(LogLevel.Warning);
    /// context.Logs.SetSearch("error");
    /// </code>
    /// </example>
    ILogOperations Logs { get; }

    // ═══════════════════════════════════════════════════════════════════════════
    // Expression-based Watch Operations (require script evaluation)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a watch using an expression string that will be evaluated.
    /// Use this from commands. For direct panel access with value getters, use <see cref="Watches"/>.
    /// </summary>
    bool AddWatch(string name, string expression);

    /// <summary>
    /// Adds a watch with expression, group, and condition.
    /// The expression will be evaluated using the script engine.
    /// </summary>
    bool AddWatch(string name, string expression, string? group, string? condition);

    // ═══════════════════════════════════════════════════════════════════════════
    // Watch Preset Operations (managed by WatchPresetManager)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Saves current watches as a preset.</summary>
    bool SaveWatchPreset(string name, string description);

    /// <summary>Loads a watch preset by name.</summary>
    bool LoadWatchPreset(string name);

    /// <summary>Lists all watch presets.</summary>
    IEnumerable<(string Name, string Description, int WatchCount, DateTime CreatedAt)> ListWatchPresets();

    /// <summary>Deletes a watch preset.</summary>
    bool DeleteWatchPreset(string name);

    /// <summary>Checks if a preset exists.</summary>
    bool WatchPresetExists(string name);

    /// <summary>Creates built-in watch presets.</summary>
    void CreateBuiltInWatchPresets();
}
