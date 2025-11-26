using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.UI.Debug.Components.Base;
using PokeSharp.Engine.UI.Debug.Components.Controls;
using PokeSharp.Engine.UI.Debug.Components.Layout;
using PokeSharp.Engine.UI.Debug.Core;
using PokeSharp.Engine.UI.Debug.Layout;
using System.Collections.Generic;
using System.Linq;

namespace PokeSharp.Engine.UI.Debug.Components.Debug;

/// <summary>
/// Panel for viewing and filtering console logs.
/// </summary>
public class LogsPanel : Panel
{
    private readonly TextBuffer _logBuffer;
    private readonly List<LogEntry> _allLogs = new();
    private LogLevel _filterLevel = LogLevel.Trace; // Show all by default
    private string? _searchFilter = null;
    private readonly int _maxLogs;
    private readonly HashSet<string> _enabledCategories = new(); // Empty = show all

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
    }

    /// <summary>
    /// Creates a LogsPanel with the specified components.
    /// Use <see cref="LogsPanelBuilder"/> to construct instances.
    /// </summary>
    internal LogsPanel(TextBuffer logBuffer, int maxLogs, LogLevel filterLevel)
    {
        _logBuffer = logBuffer;
        _maxLogs = maxLogs;
        _filterLevel = filterLevel;

        Id = "logs_panel";
        BackgroundColor = UITheme.Dark.ConsoleBackground;
        BorderColor = UITheme.Dark.BorderPrimary;
        BorderThickness = 1;
        Constraint.Padding = UITheme.Dark.PaddingMedium;

        AddChild(_logBuffer);
        UpdateLogDisplay();
    }

    /// <summary>
    /// Adds a log entry with current timestamp.
    /// </summary>
    public void AddLog(LogLevel level, string message, string category = "General")
    {
        AddLog(level, message, category, DateTime.Now);
    }

    /// <summary>
    /// Adds a log entry with a specific timestamp (used for replaying buffered logs).
    /// </summary>
    public void AddLog(LogLevel level, string message, string category, DateTime timestamp)
    {
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            Category = category
        };

        _allLogs.Add(entry);

        // Trim if we have too many logs
        if (_allLogs.Count > _maxLogs)
        {
            _allLogs.RemoveAt(0);
        }

        // Update display if this log passes the filter
        if (PassesFilter(entry))
        {
            AppendLogToBuffer(entry);
        }
    }

    /// <summary>
    /// Sets the log level filter (only show logs at this level or higher).
    /// </summary>
    public void SetFilterLevel(LogLevel level)
    {
        _filterLevel = level;
        UpdateLogDisplay();
    }

    /// <summary>
    /// Sets a text search filter (only show logs containing this text).
    /// </summary>
    public void SetSearchFilter(string? filter)
    {
        _searchFilter = string.IsNullOrWhiteSpace(filter) ? null : filter;
        UpdateLogDisplay();
    }

    /// <summary>
    /// Sets the enabled categories. Pass null or empty to show all categories.
    /// </summary>
    public void SetCategoryFilter(IEnumerable<string>? categories)
    {
        _enabledCategories.Clear();
        if (categories != null)
        {
            foreach (var cat in categories)
            {
                _enabledCategories.Add(cat);
            }
        }
        UpdateLogDisplay();
    }

    /// <summary>
    /// Enables a single category for filtering.
    /// </summary>
    public void EnableCategory(string category)
    {
        _enabledCategories.Add(category);
        UpdateLogDisplay();
    }

    /// <summary>
    /// Disables a single category from filtering.
    /// </summary>
    public void DisableCategory(string category)
    {
        _enabledCategories.Remove(category);
        UpdateLogDisplay();
    }

    /// <summary>
    /// Clears category filter (shows all categories).
    /// </summary>
    public void ClearCategoryFilter()
    {
        _enabledCategories.Clear();
        UpdateLogDisplay();
    }

    /// <summary>
    /// Gets all unique categories from logged entries.
    /// </summary>
    public IEnumerable<string> GetAvailableCategories()
    {
        return _allLogs.Select(l => l.Category).Distinct().OrderBy(c => c);
    }

    /// <summary>
    /// Gets the currently enabled categories. Empty means all are shown.
    /// </summary>
    public IReadOnlySet<string> GetEnabledCategories() => _enabledCategories;

    /// <summary>
    /// Gets the count of logs per category.
    /// </summary>
    public Dictionary<string, int> GetCategoryCounts()
    {
        return _allLogs
            .GroupBy(l => l.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Clears all logs.
    /// </summary>
    public void ClearLogs()
    {
        _allLogs.Clear();
        _logBuffer.Clear();
        UpdateLogDisplay();
    }

    /// <summary>
    /// Gets the total number of logs (unfiltered).
    /// </summary>
    public int GetTotalLogCount() => _allLogs.Count;

    /// <summary>
    /// Gets the number of filtered logs currently displayed.
    /// </summary>
    public int GetFilteredLogCount()
    {
        return _allLogs.Count(PassesFilter);
    }

    /// <summary>
    /// Checks if a log entry passes the current filters.
    /// </summary>
    private bool PassesFilter(LogEntry entry)
    {
        // Check log level filter
        if (entry.Level < _filterLevel)
            return false;

        // Check category filter (empty set means show all)
        if (_enabledCategories.Count > 0 && !_enabledCategories.Contains(entry.Category))
            return false;

        // Check text search filter
        if (_searchFilter != null && !entry.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Rebuilds the log display with current filters.
    /// </summary>
    private void UpdateLogDisplay()
    {
        _logBuffer.Clear();

        // Display header
        var filteredCount = _allLogs.Count(PassesFilter);
        var hiddenCount = _allLogs.Count - filteredCount;

        _logBuffer.AppendLine("═══════════════════════════════════════════════════════════════════", UITheme.Dark.Info);
        _logBuffer.AppendLine($"  CONSOLE LOGS ({filteredCount} shown, {hiddenCount} hidden)", UITheme.Dark.Info);
        _logBuffer.AppendLine($"  Level: {GetFilterLevelName(_filterLevel)} and above", UITheme.Dark.TextSecondary);
        if (_enabledCategories.Count > 0)
        {
            _logBuffer.AppendLine($"  Categories: {string.Join(", ", _enabledCategories.OrderBy(c => c))}", UITheme.Dark.TextSecondary);
        }
        if (_searchFilter != null)
        {
            _logBuffer.AppendLine($"  Search: \"{_searchFilter}\"", UITheme.Dark.TextSecondary);
        }
        _logBuffer.AppendLine("═══════════════════════════════════════════════════════════════════", UITheme.Dark.Info);
        _logBuffer.AppendLine("", Color.White);

        // Display filtered logs
        if (filteredCount == 0)
        {
            _logBuffer.AppendLine("  No logs to display.", UITheme.Dark.TextDim);
            return;
        }

        foreach (var entry in _allLogs.Where(PassesFilter))
        {
            AppendLogToBuffer(entry);
        }

        // Footer
        _logBuffer.AppendLine("", Color.White);
        _logBuffer.AppendLine("─────────────────────────────────────────────────────────────────", UITheme.Dark.BorderPrimary);
        _logBuffer.AppendLine($"Total: {_allLogs.Count} logs | Filtered: {filteredCount}", UITheme.Dark.TextSecondary);
    }

    /// <summary>
    /// Appends a single log entry to the buffer.
    /// </summary>
    private void AppendLogToBuffer(LogEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
        var levelStr = GetLogLevelShortName(entry.Level).PadRight(5);
        var color = GetLogLevelColor(entry.Level);

        // Format: [12:34:56.789] [INFO ] Message
        var logLine = $"[{timestamp}] [{levelStr}] {entry.Message}";
        _logBuffer.AppendLine(logLine, color, entry.Category);
    }

    /// <summary>
    /// Gets the color for a log level.
    /// </summary>
    private Color GetLogLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => UITheme.Dark.TextDim,
            LogLevel.Debug => UITheme.Dark.Info,
            LogLevel.Information => UITheme.Dark.TextPrimary,
            LogLevel.Warning => UITheme.Dark.Warning,
            LogLevel.Error => UITheme.Dark.Error,
            LogLevel.Critical => UITheme.Dark.Error,
            _ => UITheme.Dark.TextPrimary
        };
    }

    /// <summary>
    /// Gets a short name for a log level (5 chars).
    /// </summary>
    private string GetLogLevelShortName(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "LOG"
        };
    }

    /// <summary>
    /// Gets a display name for a log level.
    /// </summary>
    private string GetFilterLevelName(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => "All"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Export Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exports all logs (respecting current filters) to a formatted string.
    /// </summary>
    public string ExportToString(bool includeTimestamp = true, bool includeLevel = true, bool includeCategory = false)
    {
        var sb = new System.Text.StringBuilder();
        var filtered = _allLogs.Where(PassesFilter).ToList();

        sb.AppendLine($"# Log Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# Total: {filtered.Count} entries (filtered from {_allLogs.Count})");
        sb.AppendLine();

        foreach (var entry in filtered)
        {
            var parts = new List<string>();

            if (includeTimestamp)
                parts.Add($"[{entry.Timestamp:HH:mm:ss.fff}]");

            if (includeLevel)
                parts.Add($"[{GetLogLevelShortName(entry.Level)}]");

            if (includeCategory)
                parts.Add($"[{entry.Category}]");

            parts.Add(entry.Message);

            sb.AppendLine(string.Join(" ", parts));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports logs to CSV format.
    /// </summary>
    public string ExportToCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Timestamp,Level,Category,Message");

        foreach (var entry in _allLogs.Where(PassesFilter))
        {
            // Escape message for CSV (handle quotes and newlines)
            var escapedMessage = entry.Message
                .Replace("\"", "\"\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");

            sb.AppendLine($"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",\"{entry.Level}\",\"{entry.Category}\",\"{escapedMessage}\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Copies filtered logs to clipboard.
    /// </summary>
    public void CopyToClipboard()
    {
        var text = ExportToString();
        Utilities.ClipboardManager.SetText(text);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Statistics Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets log counts grouped by level.
    /// </summary>
    public Dictionary<LogLevel, int> GetLevelCounts()
    {
        return _allLogs
            .GroupBy(l => l.Level)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets log statistics summary.
    /// </summary>
    public LogStatistics GetStatistics()
    {
        var now = DateTime.Now;
        var logs = _allLogs;

        return new LogStatistics
        {
            TotalCount = logs.Count,
            FilteredCount = logs.Count(PassesFilter),
            TraceCount = logs.Count(l => l.Level == LogLevel.Trace),
            DebugCount = logs.Count(l => l.Level == LogLevel.Debug),
            InfoCount = logs.Count(l => l.Level == LogLevel.Information),
            WarningCount = logs.Count(l => l.Level == LogLevel.Warning),
            ErrorCount = logs.Count(l => l.Level == LogLevel.Error),
            CriticalCount = logs.Count(l => l.Level == LogLevel.Critical),
            CategoryCount = logs.Select(l => l.Category).Distinct().Count(),
            LogsLastMinute = logs.Count(l => (now - l.Timestamp).TotalMinutes <= 1),
            LogsLastFiveMinutes = logs.Count(l => (now - l.Timestamp).TotalMinutes <= 5),
            OldestLog = logs.Count > 0 ? logs.Min(l => l.Timestamp) : (DateTime?)null,
            NewestLog = logs.Count > 0 ? logs.Max(l => l.Timestamp) : (DateTime?)null
        };
    }

    /// <summary>
    /// Log statistics summary.
    /// </summary>
    public class LogStatistics
    {
        public int TotalCount { get; init; }
        public int FilteredCount { get; init; }
        public int TraceCount { get; init; }
        public int DebugCount { get; init; }
        public int InfoCount { get; init; }
        public int WarningCount { get; init; }
        public int ErrorCount { get; init; }
        public int CriticalCount { get; init; }
        public int CategoryCount { get; init; }
        public int LogsLastMinute { get; init; }
        public int LogsLastFiveMinutes { get; init; }
        public DateTime? OldestLog { get; init; }
        public DateTime? NewestLog { get; init; }
    }
}

