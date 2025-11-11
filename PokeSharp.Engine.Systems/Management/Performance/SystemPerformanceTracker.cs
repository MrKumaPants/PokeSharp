using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Configuration;
using PokeSharp.Engine.Common.Logging;

namespace PokeSharp.Engine.Systems.Management;

/// <summary>
///     Tracks and monitors system performance metrics.
///     Provides warnings for slow systems and aggregates performance statistics.
/// </summary>
/// <remarks>
/// <para>
/// This class is responsible for collecting execution time metrics for all systems,
/// detecting slow systems, and logging performance statistics periodically.
/// </para>
/// <para>
/// <b>Features:</b>
/// </para>
/// <list type="bullet">
///     <item>Per-system metrics tracking (avg, max, last execution time)</item>
///     <item>Configurable performance thresholds via PerformanceConfiguration</item>
///     <item>Throttled slow system warnings to avoid log spam</item>
///     <item>Periodic performance statistics logging</item>
///     <item>Thread-safe metric collection</item>
/// </list>
/// <para>
/// <b>Example Usage:</b>
/// </para>
/// <code>
/// var config = PerformanceConfiguration.Development;
/// var tracker = new SystemPerformanceTracker(logger, config);
///
/// // In game loop
/// tracker.IncrementFrame();
///
/// // Track system execution
/// var sw = Stopwatch.StartNew();
/// mySystem.Update(world, deltaTime);
/// sw.Stop();
/// tracker.TrackSystemPerformance("MySystem", sw.Elapsed.TotalMilliseconds);
///
/// // Log stats periodically
/// if (tracker.FrameCount % 300 == 0)
///     tracker.LogPerformanceStats();
/// </code>
/// </remarks>
public class SystemPerformanceTracker
{
    private readonly PerformanceConfiguration _config;
    private readonly ILogger? _logger;
    private readonly Dictionary<string, SystemMetrics> _metrics = new();
    private readonly Dictionary<string, ulong> _lastSlowWarningFrame = new();
    private ulong _frameCounter;

    /// <summary>
    ///     Creates a new performance tracker.
    /// </summary>
    /// <param name="logger">Optional logger for performance warnings.</param>
    /// <param name="config">Optional performance configuration. Uses default if not specified.</param>
    public SystemPerformanceTracker(ILogger? logger = null, PerformanceConfiguration? config = null)
    {
        _logger = logger;
        _config = config ?? PerformanceConfiguration.Default;
    }

    /// <summary>
    ///     Tracks execution time for a system and issues warnings if slow.
    /// </summary>
    /// <param name="systemName">The name of the system that executed.</param>
    /// <param name="elapsedMs">Execution time in milliseconds.</param>
    public void TrackSystemPerformance(string systemName, double elapsedMs)
    {
        ArgumentNullException.ThrowIfNull(systemName);

        // Update metrics
        lock (_metrics)
        {
            if (!_metrics.TryGetValue(systemName, out var metrics))
            {
                metrics = new SystemMetrics();
                _metrics[systemName] = metrics;
            }

            metrics.UpdateCount++;
            metrics.TotalTimeMs += elapsedMs;
            metrics.LastUpdateMs = elapsedMs;

            if (elapsedMs > metrics.MaxUpdateMs)
                metrics.MaxUpdateMs = elapsedMs;
        }

        // Check for slow systems and log warnings (throttled to avoid spam)
        if (elapsedMs > _config.TargetFrameTimeMs * _config.SlowSystemThresholdPercent)
            lock (_lastSlowWarningFrame)
            {
                // Only warn if cooldown period has passed since last warning for this system
                if (
                    !_lastSlowWarningFrame.TryGetValue(systemName, out var lastWarning)
                    || _frameCounter - lastWarning >= _config.SlowSystemWarningCooldownFrames
                )
                {
                    _lastSlowWarningFrame[systemName] = _frameCounter;
                    var percentOfFrame = elapsedMs / _config.TargetFrameTimeMs * 100;
                    _logger?.LogSlowSystem(systemName, elapsedMs, percentOfFrame);
                }
            }
    }

    /// <summary>
    ///     Increments the frame counter. Should be called once per frame.
    /// </summary>
    public void IncrementFrame()
    {
        _frameCounter++;
    }

    /// <summary>
    ///     Gets the current frame count.
    /// </summary>
    public ulong FrameCount => _frameCounter;

    /// <summary>
    ///     Gets metrics for a specific system.
    /// </summary>
    /// <param name="systemName">The name of the system to query.</param>
    /// <returns>Metrics for the system, or null if not tracked.</returns>
    public SystemMetrics? GetMetrics(string systemName)
    {
        ArgumentNullException.ThrowIfNull(systemName);

        lock (_metrics)
        {
            return _metrics.TryGetValue(systemName, out var metrics) ? metrics : null;
        }
    }

    /// <summary>
    ///     Gets all tracked system metrics.
    /// </summary>
    /// <returns>Dictionary of system name to metrics.</returns>
    public IReadOnlyDictionary<string, SystemMetrics> GetAllMetrics()
    {
        lock (_metrics)
        {
            return new Dictionary<string, SystemMetrics>(_metrics);
        }
    }

    /// <summary>
    ///     Logs performance statistics for all systems.
    /// </summary>
    public void LogPerformanceStats()
    {
        if (_logger == null)
            return;

        lock (_metrics)
        {
            if (_metrics.Count == 0)
                return;

            var sortedMetrics = _metrics
                .OrderByDescending(kvp => kvp.Value.AverageUpdateMs)
                .ToList();

            // Log all systems using the custom template
            foreach (var kvp in sortedMetrics)
            {
                var systemName = kvp.Key;
                var metrics = kvp.Value;
                _logger.LogSystemPerformance(
                    systemName,
                    metrics.AverageUpdateMs,
                    metrics.MaxUpdateMs,
                    metrics.UpdateCount
                );
            }
        }
    }

    /// <summary>
    ///     Resets all metrics. Useful for benchmarking specific scenarios.
    /// </summary>
    public void ResetMetrics()
    {
        lock (_metrics)
        {
            _metrics.Clear();
        }

        lock (_lastSlowWarningFrame)
        {
            _lastSlowWarningFrame.Clear();
        }
    }
}

