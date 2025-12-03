using PokeSharp.Engine.Core.Events;
using PokeSharp.Engine.UI.Debug.Models;

namespace PokeSharp.Engine.UI.Debug.Core;

/// <summary>
///     Adapter that bridges EventBus and EventMetrics to provide data for the Event Inspector UI.
/// </summary>
public class EventInspectorAdapter
{
    private readonly EventBus _eventBus;
    private readonly EventMetrics _metrics;
    private readonly Queue<EventLogEntry> _eventLog;
    private readonly int _maxLogEntries;

    public EventInspectorAdapter(EventBus eventBus, EventMetrics metrics, int maxLogEntries = 100)
    {
        _eventBus = eventBus;
        _metrics = metrics;
        _maxLogEntries = maxLogEntries;
        _eventLog = new Queue<EventLogEntry>(maxLogEntries);

        // Attach metrics to event bus
        _eventBus.Metrics = _metrics;
    }

    /// <summary>
    ///     Gets whether metrics collection is currently enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _metrics.IsEnabled;
        set => _metrics.IsEnabled = value;
    }

    /// <summary>
    ///     Generates event inspector data from current metrics and bus state.
    /// </summary>
    public EventInspectorData GetInspectorData()
    {
        var data = new EventInspectorData
        {
            Events = new List<EventTypeInfo>(),
            RecentEvents = _eventLog.ToList(),
            Filters = new EventFilterOptions()
        };

        // Get all registered event types from the bus
        var registeredTypes = _eventBus.GetRegisteredEventTypes();

        foreach (var eventType in registeredTypes)
        {
            string eventTypeName = eventType.Name;
            var eventMetrics = _metrics.GetEventMetrics(eventTypeName);

            if (eventMetrics == null)
                continue;

            var eventInfo = new EventTypeInfo
            {
                EventTypeName = eventTypeName,
                SubscriberCount = eventMetrics.SubscriberCount,
                PublishCount = eventMetrics.PublishCount,
                AverageTimeMs = eventMetrics.AveragePublishTimeMs,
                MaxTimeMs = eventMetrics.MaxPublishTimeMs,
                IsCustom = IsCustomEvent(eventType),
                Subscriptions = new List<SubscriptionInfo>()
            };

            // Get subscription details
            var subMetrics = _metrics.GetSubscriptionMetrics(eventTypeName);
            foreach (var sub in subMetrics)
            {
                eventInfo.Subscriptions.Add(new SubscriptionInfo
                {
                    HandlerId = sub.HandlerId,
                    Priority = sub.Priority,
                    Source = sub.Source,
                    InvocationCount = sub.InvocationCount,
                    AverageTimeMs = sub.AverageTimeMs,
                    MaxTimeMs = sub.MaxTimeMs
                });
            }

            data.Events.Add(eventInfo);
        }

        return data;
    }

    /// <summary>
    ///     Logs an event publish operation.
    /// </summary>
    public void LogPublish(string eventTypeName, double durationMs, string? details = null)
    {
        if (!_metrics.IsEnabled)
            return;

        AddLogEntry(new EventLogEntry
        {
            Timestamp = DateTime.Now,
            EventType = eventTypeName,
            Operation = "Publish",
            DurationMs = durationMs,
            Details = details
        });
    }

    /// <summary>
    ///     Logs a handler invocation.
    /// </summary>
    public void LogHandlerInvoke(string eventTypeName, int handlerId, double durationMs, string? details = null)
    {
        if (!_metrics.IsEnabled)
            return;

        AddLogEntry(new EventLogEntry
        {
            Timestamp = DateTime.Now,
            EventType = eventTypeName,
            Operation = "Handle",
            HandlerId = handlerId,
            DurationMs = durationMs,
            Details = details
        });
    }

    /// <summary>
    ///     Clears all metrics and logs.
    /// </summary>
    public void Clear()
    {
        _metrics.Clear();
        _eventLog.Clear();
    }

    /// <summary>
    ///     Resets timing statistics while keeping subscriber counts.
    /// </summary>
    public void ResetTimings()
    {
        _metrics.ResetTimings();
    }

    private void AddLogEntry(EventLogEntry entry)
    {
        if (_eventLog.Count >= _maxLogEntries)
        {
            _eventLog.Dequeue();
        }
        _eventLog.Enqueue(entry);
    }

    private bool IsCustomEvent(Type eventType)
    {
        // Consider an event "custom" if it's not in the core engine namespaces
        return !eventType.Namespace?.StartsWith("PokeSharp.Engine.Core.Types.Events") ?? false;
    }
}
