using System;
using Microsoft.Xna.Framework;
using Microsoft.Extensions.DependencyInjection;
using PokeSharp.Engine.UI.Debug.Core;
using PokeSharp.Engine.UI.Debug.Interfaces;
using PokeSharp.Engine.UI.Debug.Scenes;
using PokeSharp.Engine.Debug.Console.Features;
using PokeSharp.Engine.Debug.Console.Scripting;
using PokeSharp.Engine.Debug.Scripting;
using PokeSharp.Engine.Debug.Features;

namespace PokeSharp.Engine.Debug.Commands;

/// <summary>
/// Implementation of IConsoleContext that provides services to command execution.
/// </summary>
public class ConsoleContext : IConsoleContext
{
    private readonly ConsoleScene _consoleScene;
    private readonly Action _closeAction;
    private readonly ConsoleLoggingCallbacks _loggingCallbacks;
    private readonly ConsoleServices _services;

    /// <summary>
    /// Creates a new ConsoleContext with aggregated services.
    /// This is the preferred constructor for new code.
    /// </summary>
    public ConsoleContext(
        ConsoleScene consoleScene,
        Action closeAction,
        ConsoleLoggingCallbacks loggingCallbacks,
        ConsoleServices services)
    {
        _consoleScene = consoleScene ?? throw new ArgumentNullException(nameof(consoleScene));
        _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));
        _loggingCallbacks = loggingCallbacks ?? throw new ArgumentNullException(nameof(loggingCallbacks));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Creates a new ConsoleContext with individual parameters.
    /// This constructor is maintained for backward compatibility.
    /// Prefer using the constructor with ConsoleServices and ConsoleLoggingCallbacks.
    /// </summary>
    [Obsolete("Use the constructor with ConsoleServices and ConsoleLoggingCallbacks instead")]
    public ConsoleContext(
        ConsoleScene consoleScene,
        Action closeAction,
        Func<bool> isLoggingEnabledFunc,
        Action<bool> setLoggingEnabledAction,
        Func<Microsoft.Extensions.Logging.LogLevel> getLogLevelFunc,
        Action<Microsoft.Extensions.Logging.LogLevel> setLogLevelAction,
        ConsoleCommandRegistry commandRegistry,
        AliasMacroManager aliasManager,
        ScriptManager scriptManager,
        ConsoleScriptEvaluator scriptEvaluator,
        ConsoleGlobals consoleGlobals,
        BookmarkedCommandsManager bookmarkManager,
        WatchPresetManager watchPresetManager)
        : this(
            consoleScene,
            closeAction,
            new ConsoleLoggingCallbacks(isLoggingEnabledFunc, setLoggingEnabledAction, getLogLevelFunc, setLogLevelAction),
            new ConsoleServices(commandRegistry, aliasManager, scriptManager, scriptEvaluator, consoleGlobals, bookmarkManager, watchPresetManager))
    {
    }

    public UITheme Theme => UITheme.Dark;

    // Panel operation interfaces - expose panels directly for commands
    public IEntityOperations Entities => _consoleScene.EntityOperations
        ?? throw new InvalidOperationException("Entities panel not initialized");
    public IWatchOperations Watches => _consoleScene.WatchOperations
        ?? throw new InvalidOperationException("Watches panel not initialized");
    public IVariableOperations Variables => _consoleScene.VariableOperations
        ?? throw new InvalidOperationException("Variables panel not initialized");
    public ILogOperations Logs => _consoleScene.LogOperations
        ?? throw new InvalidOperationException("Logs panel not initialized");

    public void WriteLine(string text)
    {
        _consoleScene.AppendOutput(text, Theme.TextPrimary);
    }

    public void WriteLine(string text, Color color)
    {
        _consoleScene.AppendOutput(text, color);
    }

    public void Clear()
    {
        _consoleScene.ClearOutput();
    }

    public bool IsLoggingEnabled => _loggingCallbacks.IsLoggingEnabled();

    public void SetLoggingEnabled(bool enabled)
    {
        _loggingCallbacks.SetLoggingEnabled(enabled);
    }

    public Microsoft.Extensions.Logging.LogLevel MinimumLogLevel => _loggingCallbacks.GetLogLevel();

    public void SetMinimumLogLevel(Microsoft.Extensions.Logging.LogLevel level)
    {
        _loggingCallbacks.SetLogLevel(level);
    }

    public void Close()
    {
        _closeAction();
    }

    public IEnumerable<IConsoleCommand> GetAllCommands()
    {
        return _services.CommandRegistry.GetAllCommands();
    }

    public IConsoleCommand? GetCommand(string name)
    {
        return _services.CommandRegistry.GetCommand(name);
    }

    public IReadOnlyList<string> GetCommandHistory()
    {
        return _consoleScene.GetCommandHistory();
    }

    public void ClearCommandHistory()
    {
        _consoleScene.ClearCommandHistory();
    }

    public void SaveCommandHistory()
    {
        _consoleScene.SaveCommandHistory();
    }

    public void LoadCommandHistory()
    {
        _consoleScene.LoadCommandHistory();
    }

    public bool DefineAlias(string name, string command)
    {
        var result = _services.AliasManager.DefineAlias(name, command);
        if (result)
        {
            _services.AliasManager.SaveAliases();
        }
        return result;
    }

    public bool RemoveAlias(string name)
    {
        var result = _services.AliasManager.RemoveAlias(name);
        if (result)
        {
            _services.AliasManager.SaveAliases();
        }
        return result;
    }

    public IReadOnlyDictionary<string, string> GetAllAliases()
    {
        return _services.AliasManager.GetAllAliases();
    }

    public List<string> ListScripts()
    {
        return _services.ScriptManager.ListScripts();
    }

    public string GetScriptsDirectory()
    {
        return _services.ScriptManager.ScriptsDirectory;
    }

    public string? LoadScript(string filename)
    {
        var result = _services.ScriptManager.LoadScript(filename);
        return result.IsSuccess ? result.Value : null;
    }

    public bool SaveScript(string filename, string content)
    {
        var result = _services.ScriptManager.SaveScript(filename, content);
        return result.IsSuccess;
    }

    public async Task ExecuteScriptAsync(string scriptContent)
    {
        var result = await _services.ScriptEvaluator.EvaluateAsync(scriptContent, _services.ScriptGlobals);

        // Handle compilation errors
        if (result.IsCompilationError && result.Errors != null)
        {
            WriteLine("Compilation Error:", Theme.Error);
            foreach (var error in result.Errors)
            {
                WriteLine($"  {error.Message}", Theme.Error);
            }
            return;
        }

        // Handle runtime errors
        if (result.IsRuntimeError)
        {
            WriteLine($"Runtime Error: {result.RuntimeException?.Message ?? "Unknown error"}", Theme.Error);
            if (result.RuntimeException != null)
            {
                WriteLine($"  {result.RuntimeException.GetType().Name}", Theme.TextSecondary);
            }
            return;
        }

        // Display result if there is one
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            WriteLine(result.Output, Theme.Success);
        }
    }

    public void ResetScriptState()
    {
        _services.ScriptEvaluator.Reset();
    }

    public IReadOnlyDictionary<int, string> GetAllBookmarks()
    {
        return _services.BookmarkManager.GetAllBookmarks();
    }

    public string? GetBookmark(int fkeyNumber)
    {
        return _services.BookmarkManager.GetBookmark(fkeyNumber);
    }

    public bool SetBookmark(int fkeyNumber, string command)
    {
        var result = _services.BookmarkManager.BookmarkCommand(fkeyNumber, command);
        if (result) _services.BookmarkManager.SaveBookmarks(); // Auto-save on change
        return result;
    }

    public bool RemoveBookmark(int fkeyNumber)
    {
        var result = _services.BookmarkManager.RemoveBookmark(fkeyNumber);
        if (result) _services.BookmarkManager.SaveBookmarks(); // Auto-save on change
        return result;
    }

    public void ClearAllBookmarks()
    {
        _services.BookmarkManager.ClearAll();
        _services.BookmarkManager.SaveBookmarks(); // Auto-save on change
    }

    public bool SaveBookmarks()
    {
        return _services.BookmarkManager.SaveBookmarks();
    }

    public int LoadBookmarks()
    {
        return _services.BookmarkManager.LoadBookmarks();
    }

    public bool AddWatch(string name, string expression)
    {
        return AddWatch(name, expression, null, null);
    }

    public bool AddWatch(string name, string expression, string? group, string? condition)
    {
        // Create value evaluator lambda
        Func<object?> valueGetter = () =>
        {
            try
            {
                var task = _services.ScriptEvaluator.EvaluateAsync(expression, _services.ScriptGlobals);
                task.Wait();
                var result = task.Result;

                if (result.IsSuccess)
                {
                    return string.IsNullOrWhiteSpace(result.Output) ? "<null>" : result.Output;
                }
                else
                {
                    if (result.Errors != null && result.Errors.Count > 0)
                    {
                        return $"<error: {result.Errors[0].Message}>";
                    }
                    return "<error: evaluation failed>";
                }
            }
            catch (Exception ex)
            {
                return $"<error: {ex.Message}>";
            }
        };

        // Create condition evaluator lambda if condition provided
        Func<bool>? conditionEvaluator = null;
        if (!string.IsNullOrWhiteSpace(condition))
        {
            conditionEvaluator = () =>
            {
                try
                {
                    var task = _services.ScriptEvaluator.EvaluateAsync(condition, _services.ScriptGlobals);
                    task.Wait();
                    var result = task.Result;

                    if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Output))
                    {
                        // Try to parse as boolean
                        if (bool.TryParse(result.Output.Trim(), out var boolResult))
                        {
                            return boolResult;
                        }
                        // Non-empty result treated as true
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            };
        }

        return _consoleScene.AddWatch(name, expression, valueGetter, group, condition, conditionEvaluator);
    }

    public bool RemoveWatch(string name)
    {
        return _consoleScene.RemoveWatch(name);
    }

    public void ClearWatches()
    {
        _consoleScene.ClearWatches();
    }

    public bool ToggleWatchAutoUpdate()
    {
        return _consoleScene.ToggleWatchAutoUpdate();
    }

    public int GetWatchCount()
    {
        return _consoleScene.GetWatchCount();
    }

    public bool PinWatch(string name)
    {
        return _consoleScene.PinWatch(name);
    }

    public bool UnpinWatch(string name)
    {
        return _consoleScene.UnpinWatch(name);
    }

    public bool IsWatchPinned(string name)
    {
        return _consoleScene.IsWatchPinned(name);
    }

    public bool SetWatchInterval(double intervalSeconds)
    {
        return _consoleScene.SetWatchInterval(intervalSeconds);
    }

    public bool CollapseWatchGroup(string groupName)
    {
        return _consoleScene.CollapseWatchGroup(groupName);
    }

    public bool ExpandWatchGroup(string groupName)
    {
        return _consoleScene.ExpandWatchGroup(groupName);
    }

    public bool ToggleWatchGroup(string groupName)
    {
        return _consoleScene.ToggleWatchGroup(groupName);
    }

    public IEnumerable<string> GetWatchGroups()
    {
        return _consoleScene.GetWatchGroups();
    }

    public bool SetWatchAlert(string name, string alertType, object? threshold)
    {
        return _consoleScene.SetWatchAlert(name, alertType, threshold);
    }

    public bool RemoveWatchAlert(string name)
    {
        return _consoleScene.RemoveWatchAlert(name);
    }

    public IEnumerable<(string Name, string AlertType, bool Triggered)> GetWatchesWithAlerts()
    {
        return _consoleScene.GetWatchesWithAlerts();
    }

    public bool ClearWatchAlertStatus(string name)
    {
        return _consoleScene.ClearWatchAlertStatus(name);
    }

    public bool SetWatchComparison(string watchName, string compareWithName, string comparisonLabel = "Expected")
    {
        return _consoleScene.SetWatchComparison(watchName, compareWithName, comparisonLabel);
    }

    public bool RemoveWatchComparison(string name)
    {
        return _consoleScene.RemoveWatchComparison(name);
    }

    public IEnumerable<(string Name, string ComparedWith)> GetWatchesWithComparisons()
    {
        return _consoleScene.GetWatchesWithComparisons();
    }

    public void SetLogFilter(Microsoft.Extensions.Logging.LogLevel level)
    {
        _consoleScene.SetLogFilter(level);
    }

    public void SetLogSearch(string? searchText)
    {
        _consoleScene.SetLogSearch(searchText);
    }

    public void AddLog(Microsoft.Extensions.Logging.LogLevel level, string message, string category = "General")
    {
        _consoleScene.AddLog(level, message, category);
    }

    public void ClearLogs()
    {
        _consoleScene.ClearLogs();
    }

    public int GetLogCount()
    {
        return _consoleScene.GetLogCount();
    }

    public void SetLogCategoryFilter(IEnumerable<string>? categories)
    {
        _consoleScene.SetLogCategoryFilter(categories);
    }

    public void ClearLogCategoryFilter()
    {
        _consoleScene.ClearLogCategoryFilter();
    }

    public IEnumerable<string> GetLogCategories()
    {
        return _consoleScene.GetLogCategories();
    }

    public Dictionary<string, int> GetLogCategoryCounts()
    {
        return _consoleScene.GetLogCategoryCounts();
    }

    public string ExportLogs(bool includeTimestamp = true, bool includeLevel = true, bool includeCategory = false)
    {
        return _consoleScene.ExportLogs(includeTimestamp, includeLevel, includeCategory);
    }

    public string ExportLogsToCsv()
    {
        return _consoleScene.ExportLogsToCsv();
    }

    public void CopyLogsToClipboard()
    {
        _consoleScene.CopyLogsToClipboard();
    }

    public (int Total, int Filtered, int Errors, int Warnings, int LastMinute, int Categories) GetLogStatistics()
    {
        return _consoleScene.GetLogStatistics();
    }

    public Dictionary<Microsoft.Extensions.Logging.LogLevel, int> GetLogLevelCounts()
    {
        return _consoleScene.GetLogLevelCounts();
    }

    public bool SaveWatchPreset(string name, string description)
    {
        try
        {
            var config = _consoleScene.ExportWatchConfiguration();
            if (config == null)
                return false;

            var (watches, updateInterval, autoUpdateEnabled) = config.Value;

            var preset = new WatchPreset
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                UpdateInterval = updateInterval,
                AutoUpdateEnabled = autoUpdateEnabled,
                Watches = watches.Select(w => new WatchPresetEntry
                {
                    Name = w.Name,
                    Expression = w.Expression,
                    Group = w.Group,
                    Condition = w.Condition,
                    IsPinned = w.IsPinned,
                    Alert = w.AlertType != null ? new WatchAlertConfig
                    {
                        Type = w.AlertType,
                        Threshold = w.AlertThreshold?.ToString()
                    } : null,
                    Comparison = w.ComparisonWith != null ? new WatchComparisonConfig
                    {
                        CompareWith = w.ComparisonWith,
                        Label = w.ComparisonLabel ?? "Expected"
                    } : null
                }).ToList()
            };

            return _services.WatchPresetManager.SavePreset(preset);
        }
        catch
        {
            return false;
        }
    }

    public bool LoadWatchPreset(string name)
    {
        try
        {
            var preset = _services.WatchPresetManager.LoadPreset(name);
            if (preset == null)
                return false;

            // Import configuration (clears watches and sets update settings)
            _consoleScene.ImportWatchConfiguration(preset.UpdateInterval, preset.AutoUpdateEnabled);

            // Add watches from preset
            foreach (var watch in preset.Watches)
            {
                // Create value getter for the expression
                System.Func<object?> valueGetter = () =>
                {
                    try
                    {
                        var result = _services.ScriptEvaluator.EvaluateAsync(watch.Expression, _services.ScriptGlobals).Result;
                        return result.IsSuccess ? result.Output : $"<error: {result.Errors?[0].Message ?? "evaluation failed"}>";
                    }
                    catch (Exception ex)
                    {
                        return $"<error: {ex.Message}>";
                    }
                };

                // Create condition evaluator if condition exists
                System.Func<bool>? conditionEvaluator = null;
                if (!string.IsNullOrEmpty(watch.Condition))
                {
                    conditionEvaluator = () =>
                    {
                        try
                        {
                            var result = _services.ScriptEvaluator.EvaluateAsync(watch.Condition, _services.ScriptGlobals).Result;
                            if (!result.IsSuccess) return false;
                            // Output is a string representation, parse it as boolean
                            if (string.IsNullOrEmpty(result.Output)) return false;
                            if (bool.TryParse(result.Output, out var boolValue)) return boolValue;
                            // Consider "true" (case-insensitive) or non-empty strings as true
                            return result.Output.Equals("true", StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    };
                }

                // Add the watch
                AddWatch(watch.Name, watch.Expression, watch.Group, watch.Condition);

                // Pin if needed
                if (watch.IsPinned)
                {
                    PinWatch(watch.Name);
                }

                // Set up alert if configured
                if (watch.Alert != null)
                {
                    object? alertThreshold = null;
                    if (watch.Alert.Threshold != null)
                    {
                        if (double.TryParse(watch.Alert.Threshold, out var numThreshold))
                        {
                            alertThreshold = numThreshold;
                        }
                        else
                        {
                            alertThreshold = watch.Alert.Threshold;
                        }
                    }

                    SetWatchAlert(watch.Name, watch.Alert.Type, alertThreshold);
                }

                // Set up comparison if configured
                if (watch.Comparison != null)
                {
                    SetWatchComparison(watch.Name, watch.Comparison.CompareWith, watch.Comparison.Label);
                }
            }

            WriteLine($"Loaded preset '{name}' ({preset.Watches.Count} watches)", Theme.Success);
            return true;
        }
        catch (Exception ex)
        {
            WriteLine($"Failed to load preset '{name}': {ex.Message}", Theme.Error);
            return false;
        }
    }

    public IEnumerable<(string Name, string Description, int WatchCount, DateTime CreatedAt)> ListWatchPresets()
    {
        return _services.WatchPresetManager.ListPresets();
    }

    public bool DeleteWatchPreset(string name)
    {
        return _services.WatchPresetManager.DeletePreset(name);
    }

    public bool WatchPresetExists(string name)
    {
        return _services.WatchPresetManager.PresetExists(name);
    }

    public void CreateBuiltInWatchPresets()
    {
        _services.WatchPresetManager.CreateBuiltInPresets();
    }

    public void SwitchToTab(int tabIndex)
    {
        _consoleScene.SetActiveTab(tabIndex);
    }

    public int GetActiveTab()
    {
        return _consoleScene.GetActiveTab();
    }

    public void SetConsoleHeight(float heightPercent)
    {
        _consoleScene.SetHeightPercent(heightPercent);
    }

    public string ExportConsoleOutput()
    {
        return _consoleScene.ExportConsoleOutput();
    }

    public void CopyConsoleOutputToClipboard()
    {
        _consoleScene.CopyConsoleOutputToClipboard();
    }

    public (int TotalLines, int FilteredLines) GetConsoleOutputStats()
    {
        return _consoleScene.GetConsoleOutputStats();
    }

    public string ExportWatchesToCsv()
    {
        return _consoleScene.ExportWatchesToCsv();
    }

    public void CopyWatchesToClipboard(bool asCsv = false)
    {
        _consoleScene.CopyWatchesToClipboard(asCsv);
    }

    public (int Total, int Pinned, int WithErrors, int WithAlerts, int Groups) GetWatchStatistics()
    {
        return _consoleScene.GetWatchStatistics();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Variables Tab
    // ═══════════════════════════════════════════════════════════════════════════

    public (int Variables, int Globals, int Pinned, int Expanded) GetVariableStatistics()
    {
        return _consoleScene.GetVariableStatistics();
    }

    public IEnumerable<string> GetVariableNames()
    {
        return _consoleScene.GetVariableNames();
    }

    public object? GetVariableValue(string name)
    {
        return _consoleScene.GetVariableValue(name);
    }

    public void SetVariableSearchFilter(string filter)
    {
        _consoleScene.SetVariableSearchFilter(filter);
    }

    public void ClearVariableSearchFilter()
    {
        _consoleScene.ClearVariableSearchFilter();
    }

    public bool ExpandVariable(string path)
    {
        return _consoleScene.ExpandVariable(path);
    }

    public void CollapseVariable(string path)
    {
        _consoleScene.CollapseVariable(path);
    }

    public void ExpandAllVariables()
    {
        _consoleScene.ExpandAllVariables();
    }

    public void CollapseAllVariables()
    {
        _consoleScene.CollapseAllVariables();
    }

    public void PinVariable(string name)
    {
        _consoleScene.PinVariable(name);
    }

    public void UnpinVariable(string name)
    {
        _consoleScene.UnpinVariable(name);
    }

    public void ClearVariables()
    {
        _consoleScene.ClearVariables();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Entities Tab
    // ═══════════════════════════════════════════════════════════════════════════

    public void RefreshEntities()
    {
        _consoleScene.RefreshEntities();
    }

    public void SetEntityTagFilter(string tag)
    {
        _consoleScene.SetEntityTagFilter(tag);
    }

    public void SetEntitySearchFilter(string search)
    {
        _consoleScene.SetEntitySearchFilter(search);
    }

    public void SetEntityComponentFilter(string componentName)
    {
        _consoleScene.SetEntityComponentFilter(componentName);
    }

    public void ClearEntityFilters()
    {
        _consoleScene.ClearEntityFilters();
    }

    public (string Tag, string Search, string Component) GetEntityFilters()
    {
        return _consoleScene.GetEntityFilters();
    }

    public void SelectEntity(int entityId)
    {
        _consoleScene.SelectEntity(entityId);
    }

    public void ExpandEntity(int entityId)
    {
        _consoleScene.ExpandEntity(entityId);
    }

    public void CollapseEntity(int entityId)
    {
        _consoleScene.CollapseEntity(entityId);
    }

    public bool ToggleEntity(int entityId)
    {
        return _consoleScene.ToggleEntity(entityId);
    }

    public void ExpandAllEntities()
    {
        _consoleScene.ExpandAllEntities();
    }

    public void CollapseAllEntities()
    {
        _consoleScene.CollapseAllEntities();
    }

    public void PinEntity(int entityId)
    {
        _consoleScene.PinEntity(entityId);
    }

    public void UnpinEntity(int entityId)
    {
        _consoleScene.UnpinEntity(entityId);
    }

    public (int Total, int Filtered, int Pinned, int Expanded) GetEntityStatistics()
    {
        return _consoleScene.GetEntityStatistics();
    }

    public Dictionary<string, int> GetEntityTagCounts()
    {
        return _consoleScene.GetEntityTagCounts();
    }

    public IEnumerable<string> GetEntityComponentNames()
    {
        return _consoleScene.GetEntityComponentNames();
    }

    public IEnumerable<string> GetEntityTags()
    {
        return _consoleScene.GetEntityTags();
    }

    public PokeSharp.Engine.UI.Debug.Models.EntityInfo? FindEntity(int entityId)
    {
        return _consoleScene.FindEntity(entityId);
    }

    public IEnumerable<PokeSharp.Engine.UI.Debug.Models.EntityInfo> FindEntitiesByName(string name)
    {
        return _consoleScene.FindEntitiesByName(name);
    }

    public (int Spawned, int Removed, int CurrentlyHighlighted) GetEntitySessionStats()
    {
        return _consoleScene.GetEntitySessionStats();
    }

    public void ClearEntitySessionStats()
    {
        _consoleScene.ClearEntitySessionStats();
    }

    public bool EntityAutoRefresh
    {
        get => _consoleScene.EntityAutoRefresh;
        set => _consoleScene.EntityAutoRefresh = value;
    }

    public float EntityRefreshInterval
    {
        get => _consoleScene.EntityRefreshInterval;
        set => _consoleScene.EntityRefreshInterval = value;
    }

    public float EntityHighlightDuration
    {
        get => _consoleScene.EntityHighlightDuration;
        set => _consoleScene.EntityHighlightDuration = value;
    }

    public IEnumerable<int> GetNewEntityIds()
    {
        return _consoleScene.GetNewEntityIds();
    }

    public string ExportEntitiesToText(bool includeComponents = true, bool includeProperties = true)
    {
        return _consoleScene.ExportEntitiesToText(includeComponents, includeProperties);
    }

    public string ExportEntitiesToCsv()
    {
        return _consoleScene.ExportEntitiesToCsv();
    }

    public string? ExportSelectedEntity()
    {
        return _consoleScene.ExportSelectedEntity();
    }

    public void CopyEntitiesToClipboard(bool asCsv = false)
    {
        _consoleScene.CopyEntitiesToClipboard(asCsv);
    }

    public int? SelectedEntityId => _consoleScene.SelectedEntityId;
}

