using System.Text;
using Microsoft.Xna.Framework;
using PokeSharp.Engine.UI.Debug.Components.Base;
using PokeSharp.Engine.UI.Debug.Components.Controls;
using PokeSharp.Engine.UI.Debug.Core;
using PokeSharp.Engine.UI.Debug.Layout;
using PokeSharp.Engine.UI.Debug.Models;

namespace PokeSharp.Engine.UI.Debug.Components.Debug;

/// <summary>
///     Content area for the Event Inspector panel.
///     Displays event types, subscriptions, and performance metrics in a scrollable view.
/// </summary>
public class EventInspectorContent : TextBuffer
{
    private Func<EventInspectorData>? _dataProvider;
    private EventInspectorData? _cachedData;
    private int _frameCounter;
    private int _refreshInterval = 1; // Update every frame by default
    private int _selectedEventIndex = -1;
    private bool _showSubscriptions = true;

    public EventInspectorContent() : base("event_inspector_content")
    {
        LinePadding = DebugPanelBase.StandardLinePadding;
        // TextBuffer handles scrolling automatically
    }

    public bool HasProvider => _dataProvider != null;

    /// <summary>
    ///     Sets the data provider function.
    /// </summary>
    public void SetDataProvider(Func<EventInspectorData>? provider)
    {
        _dataProvider = provider;
        _cachedData = null;
        Refresh();
    }

    /// <summary>
    ///     Sets the refresh interval in frames.
    /// </summary>
    public void SetRefreshInterval(int frameInterval)
    {
        _refreshInterval = Math.Max(1, frameInterval);
    }

    /// <summary>
    ///     Gets the current refresh interval.
    /// </summary>
    public int GetRefreshInterval() => _refreshInterval;

    /// <summary>
    ///     Toggles subscription details visibility.
    /// </summary>
    public void ToggleSubscriptions()
    {
        _showSubscriptions = !_showSubscriptions;
        Refresh();
    }

    /// <summary>
    ///     Selects the next event in the list.
    /// </summary>
    public void SelectNextEvent()
    {
        if (_cachedData == null || _cachedData.Events.Count == 0) return;

        _selectedEventIndex = (_selectedEventIndex + 1) % _cachedData.Events.Count;
        _cachedData.SelectedEventType = _cachedData.Events[_selectedEventIndex].EventTypeName;
        Refresh();
    }

    /// <summary>
    ///     Selects the previous event in the list.
    /// </summary>
    public void SelectPreviousEvent()
    {
        if (_cachedData == null || _cachedData.Events.Count == 0) return;

        _selectedEventIndex = _selectedEventIndex <= 0
            ? _cachedData.Events.Count - 1
            : _selectedEventIndex - 1;
        _cachedData.SelectedEventType = _cachedData.Events[_selectedEventIndex].EventTypeName;
        Refresh();
    }

    /// <summary>
    ///     Forces an immediate refresh of the data and display.
    /// </summary>
    public void Refresh()
    {
        if (_dataProvider == null) return;

        _cachedData = _dataProvider();
        UpdateDisplay();
    }

    /// <summary>
    ///     Called every frame to auto-refresh based on interval.
    /// </summary>
    public void Update()
    {
        // Auto-refresh based on interval
        _frameCounter++;
        if (_frameCounter >= _refreshInterval)
        {
            _frameCounter = 0;
            Refresh();
        }
    }

    private void UpdateDisplay()
    {
        if (_cachedData == null)
        {
            Clear();
            AppendLine("No event data available", Color.Gray);
            return;
        }

        Clear();

        var lines = new List<string>();
        UITheme theme = ThemeManager.Current;

        // Header
        lines.Add("Event Inspector");
        lines.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        lines.Add("");

        // Active Events Section
        lines.Add($"📊 Active Events ({_cachedData.Events.Count})");
        lines.Add("");

        if (_cachedData.Events.Count == 0)
        {
            lines.Add("  No events registered");
        }
        else
        {
            var sortedEvents = _cachedData.Events
                .OrderByDescending(e => e.SubscriberCount)
                .ThenBy(e => e.EventTypeName)
                .ToList();

            foreach (var eventInfo in sortedEvents)
            {
                bool isSelected = eventInfo.EventTypeName == _cachedData.SelectedEventType;
                string indicator = isSelected ? "►" : " ";
                string customTag = eventInfo.IsCustom ? " [Custom]" : "";
                string statusColor = eventInfo.SubscriberCount > 0 ? "green" : "gray";

                lines.Add(
                    $"  {indicator} [{statusColor}]✓[/] {eventInfo.EventTypeName} " +
                    $"[cyan]({eventInfo.SubscriberCount} subscribers)[/]{customTag}"
                );

                // Show basic metrics
                if (eventInfo.PublishCount > 0)
                {
                    string perfColor = GetPerformanceColor(eventInfo.AverageTimeMs);
                    lines.Add(
                        $"      [{perfColor}]avg: {eventInfo.AverageTimeMs:F3}ms, " +
                        $"max: {eventInfo.MaxTimeMs:F3}ms, " +
                        $"count: {eventInfo.PublishCount}[/]"
                    );
                }
            }
        }

        lines.Add("");

        // Subscriptions for Selected Event
        if (!string.IsNullOrEmpty(_cachedData.SelectedEventType) && _showSubscriptions)
        {
            var selectedEvent = _cachedData.Events
                .FirstOrDefault(e => e.EventTypeName == _cachedData.SelectedEventType);

            if (selectedEvent != null && selectedEvent.Subscriptions.Count > 0)
            {
                lines.Add($"📝 Subscriptions for: [cyan]{selectedEvent.EventTypeName}[/]");
                lines.Add("");

                foreach (var sub in selectedEvent.Subscriptions.OrderByDescending(s => s.Priority))
                {
                    string source = string.IsNullOrEmpty(sub.Source)
                        ? $"Handler #{sub.HandlerId}"
                        : sub.Source;

                    lines.Add($"  [Priority {sub.Priority}] {source}");

                    if (sub.InvocationCount > 0)
                    {
                        string perfColor = GetPerformanceColor(sub.AverageTimeMs);
                        lines.Add(
                            $"    [{perfColor}]avg: {sub.AverageTimeMs:F3}ms, " +
                            $"max: {sub.MaxTimeMs:F3}ms, " +
                            $"calls: {sub.InvocationCount}[/]"
                        );
                    }
                }

                lines.Add("");
            }
        }

        // Performance Summary
        lines.Add("📈 Performance Summary");
        lines.Add("");

        if (_cachedData.Events.Any(e => e.PublishCount > 0))
        {
            var activeEvents = _cachedData.Events
                .Where(e => e.PublishCount > 0)
                .OrderByDescending(e => e.AverageTimeMs)
                .Take(5)
                .ToList();

            lines.Add("  Slowest Events:");
            foreach (var evt in activeEvents)
            {
                string perfColor = GetPerformanceColor(evt.AverageTimeMs);
                lines.Add(
                    $"    [{perfColor}]{evt.EventTypeName,-40}[/] " +
                    $"[{perfColor}]{evt.AverageTimeMs:F3}ms avg, {evt.MaxTimeMs:F3}ms max[/]"
                );
            }
        }
        else
        {
            lines.Add("  No events published yet");
        }

        lines.Add("");

        // Recent Event Log (if available)
        if (_cachedData.RecentEvents.Count > 0)
        {
            lines.Add("📋 Recent Events (last 10)");
            lines.Add("");

            foreach (var entry in _cachedData.RecentEvents.TakeLast(10))
            {
                string timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
                string operation = entry.Operation == "Publish" ? "→" : "←";
                string perfColor = GetPerformanceColor(entry.DurationMs);
                string handler = entry.HandlerId.HasValue ? $" [#{entry.HandlerId}]" : "";

                lines.Add(
                    $"  [{perfColor}]{timestamp}[/] {operation} " +
                    $"[cyan]{entry.EventType}[/]{handler} " +
                    $"[{perfColor}]({entry.DurationMs:F3}ms)[/]"
                );
            }
        }

        // Add all lines to TextBuffer
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    private string GetPerformanceColor(double timeMs)
    {
        // Color coding based on performance thresholds
        return timeMs switch
        {
            < 0.1 => "green",
            < 0.5 => "yellow",
            < 1.0 => "orange",
            _ => "red"
        };
    }
}
