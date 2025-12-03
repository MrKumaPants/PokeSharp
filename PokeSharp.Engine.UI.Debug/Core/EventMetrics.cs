using System.Collections.Concurrent;
using System.Diagnostics;
using PokeSharp.Engine.Core.Events;

namespace PokeSharp.Engine.UI.Debug.Core;

/// <summary>
///     Tracks performance metrics for event bus operations.
///     Only collects data when the Event Inspector is active to minimize overhead.
/// </summary>
public class EventMetrics : IEventMetrics
{
    private readonly ConcurrentDictionary<string, EventTypeMetrics> _eventMetrics = new();
    private readonly ConcurrentDictionary<string, SubscriptionMetrics> _subscriptionMetrics = new();
    private bool _isEnabled;

    /// <summary>
    ///     Gets or sets whether metrics collection is enabled.
    ///     When disabled, all tracking calls are no-ops for minimal performance impact.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    ///     Records a publish operation for an event type.
    /// </summary>
    public void RecordPublish(string eventTypeName, long elapsedMicroseconds)
    {
        if (!_isEnabled) return;

        var metrics = _eventMetrics.GetOrAdd(eventTypeName, _ => new EventTypeMetrics(eventTypeName));
        metrics.RecordPublish(elapsedMicroseconds);
    }

    /// <summary>
    ///     Records a handler invocation.
    /// </summary>
    public void RecordHandlerInvoke(string eventTypeName, int handlerId, long elapsedMicroseconds)
    {
        if (!_isEnabled) return;

        var metrics = _eventMetrics.GetOrAdd(eventTypeName, _ => new EventTypeMetrics(eventTypeName));
        metrics.RecordHandlerInvoke(elapsedMicroseconds);

        string key = $"{eventTypeName}:{handlerId}";
        var subMetrics = _subscriptionMetrics.GetOrAdd(key, _ => new SubscriptionMetrics(eventTypeName, handlerId));
        subMetrics.RecordInvoke(elapsedMicroseconds);
    }

    /// <summary>
    ///     Records a subscription being added.
    /// </summary>
    public void RecordSubscription(string eventTypeName, int handlerId, string? source = null, int priority = 0)
    {
        if (!_isEnabled) return;

        var metrics = _eventMetrics.GetOrAdd(eventTypeName, _ => new EventTypeMetrics(eventTypeName));
        metrics.IncrementSubscriberCount();

        string key = $"{eventTypeName}:{handlerId}";
        var subMetrics = _subscriptionMetrics.GetOrAdd(key, _ => new SubscriptionMetrics(eventTypeName, handlerId));
        subMetrics.Source = source;
        subMetrics.Priority = priority;
    }

    /// <summary>
    ///     Records a subscription being removed.
    /// </summary>
    public void RecordUnsubscription(string eventTypeName, int handlerId)
    {
        if (!_isEnabled) return;

        var metrics = _eventMetrics.GetOrAdd(eventTypeName, _ => new EventTypeMetrics(eventTypeName));
        metrics.DecrementSubscriberCount();

        string key = $"{eventTypeName}:{handlerId}";
        _subscriptionMetrics.TryRemove(key, out _);
    }

    /// <summary>
    ///     Gets all event type metrics.
    /// </summary>
    public IReadOnlyCollection<EventTypeMetrics> GetAllEventMetrics()
    {
        return _eventMetrics.Values.ToList();
    }

    /// <summary>
    ///     Gets metrics for a specific event type.
    /// </summary>
    public EventTypeMetrics? GetEventMetrics(string eventTypeName)
    {
        _eventMetrics.TryGetValue(eventTypeName, out var metrics);
        return metrics;
    }

    /// <summary>
    ///     Gets all subscription metrics for an event type.
    /// </summary>
    public IReadOnlyCollection<SubscriptionMetrics> GetSubscriptionMetrics(string eventTypeName)
    {
        return _subscriptionMetrics.Values
            .Where(s => s.EventTypeName == eventTypeName)
            .OrderByDescending(s => s.Priority)
            .ToList();
    }

    /// <summary>
    ///     Clears all collected metrics.
    /// </summary>
    public void Clear()
    {
        _eventMetrics.Clear();
        _subscriptionMetrics.Clear();
    }

    /// <summary>
    ///     Resets timing statistics while keeping subscriber counts.
    /// </summary>
    public void ResetTimings()
    {
        foreach (var metrics in _eventMetrics.Values)
        {
            metrics.ResetTimings();
        }
        foreach (var metrics in _subscriptionMetrics.Values)
        {
            metrics.ResetTimings();
        }
    }
}

/// <summary>
///     Performance metrics for a specific event type.
/// </summary>
public class EventTypeMetrics
{
    private readonly object _lock = new();
    private long _totalPublishCount;
    private long _totalHandlerInvocations;
    private long _totalPublishTimeMicroseconds;
    private long _totalHandlerTimeMicroseconds;
    private long _maxPublishTimeMicroseconds;
    private long _maxHandlerTimeMicroseconds;
    private int _subscriberCount;

    public EventTypeMetrics(string eventTypeName)
    {
        EventTypeName = eventTypeName;
    }

    public string EventTypeName { get; }
    public long PublishCount => _totalPublishCount;
    public long HandlerInvocations => _totalHandlerInvocations;
    public int SubscriberCount => _subscriberCount;

    public double AveragePublishTimeMs =>
        _totalPublishCount > 0 ? (_totalPublishTimeMicroseconds / (double)_totalPublishCount) / 1000.0 : 0.0;

    public double MaxPublishTimeMs => _maxPublishTimeMicroseconds / 1000.0;

    public double AverageHandlerTimeMs =>
        _totalHandlerInvocations > 0 ? (_totalHandlerTimeMicroseconds / (double)_totalHandlerInvocations) / 1000.0 : 0.0;

    public double MaxHandlerTimeMs => _maxHandlerTimeMicroseconds / 1000.0;

    public void RecordPublish(long elapsedMicroseconds)
    {
        lock (_lock)
        {
            _totalPublishCount++;
            _totalPublishTimeMicroseconds += elapsedMicroseconds;
            if (elapsedMicroseconds > _maxPublishTimeMicroseconds)
                _maxPublishTimeMicroseconds = elapsedMicroseconds;
        }
    }

    public void RecordHandlerInvoke(long elapsedMicroseconds)
    {
        lock (_lock)
        {
            _totalHandlerInvocations++;
            _totalHandlerTimeMicroseconds += elapsedMicroseconds;
            if (elapsedMicroseconds > _maxHandlerTimeMicroseconds)
                _maxHandlerTimeMicroseconds = elapsedMicroseconds;
        }
    }

    public void IncrementSubscriberCount()
    {
        Interlocked.Increment(ref _subscriberCount);
    }

    public void DecrementSubscriberCount()
    {
        Interlocked.Decrement(ref _subscriberCount);
    }

    public void ResetTimings()
    {
        lock (_lock)
        {
            _totalPublishCount = 0;
            _totalHandlerInvocations = 0;
            _totalPublishTimeMicroseconds = 0;
            _totalHandlerTimeMicroseconds = 0;
            _maxPublishTimeMicroseconds = 0;
            _maxHandlerTimeMicroseconds = 0;
        }
    }
}

/// <summary>
///     Performance metrics for a specific subscription.
/// </summary>
public class SubscriptionMetrics
{
    private readonly object _lock = new();
    private long _totalInvocations;
    private long _totalTimeMicroseconds;
    private long _maxTimeMicroseconds;

    public SubscriptionMetrics(string eventTypeName, int handlerId)
    {
        EventTypeName = eventTypeName;
        HandlerId = handlerId;
    }

    public string EventTypeName { get; }
    public int HandlerId { get; }
    public string? Source { get; set; }
    public int Priority { get; set; }

    public long InvocationCount => _totalInvocations;

    public double AverageTimeMs =>
        _totalInvocations > 0 ? (_totalTimeMicroseconds / (double)_totalInvocations) / 1000.0 : 0.0;

    public double MaxTimeMs => _maxTimeMicroseconds / 1000.0;

    public void RecordInvoke(long elapsedMicroseconds)
    {
        lock (_lock)
        {
            _totalInvocations++;
            _totalTimeMicroseconds += elapsedMicroseconds;
            if (elapsedMicroseconds > _maxTimeMicroseconds)
                _maxTimeMicroseconds = elapsedMicroseconds;
        }
    }

    public void ResetTimings()
    {
        lock (_lock)
        {
            _totalInvocations = 0;
            _totalTimeMicroseconds = 0;
            _maxTimeMicroseconds = 0;
        }
    }
}
