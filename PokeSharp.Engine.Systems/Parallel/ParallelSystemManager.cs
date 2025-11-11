using PokeSharp.Engine.Core.Systems;
using System.Diagnostics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Configuration;
using PokeSharp.Engine.Common.Logging;
using PokeSharp.Engine.Systems.Management;

namespace PokeSharp.Engine.Systems.Parallel;

/// <summary>
///     Extended SystemManager that supports parallel system execution.
///     Automatically determines which systems can run in parallel based on component access patterns.
/// </summary>
public class ParallelSystemManager : SystemManager
{
    private readonly SystemDependencyGraph _dependencyGraph;
    private readonly ParallelQueryExecutor _parallelExecutor;
    private readonly bool _parallelEnabled;
    private readonly ILogger<ParallelSystemManager>? _logger;

    private List<List<ISystem>>? _executionStages;
    private bool _executionPlanBuilt;

    /// <summary>
    ///     Creates a new parallel system manager.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="enableParallel">Enable parallel execution (default: true).</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="config">Optional performance configuration.</param>
    public ParallelSystemManager(
        World world,
        bool enableParallel = true,
        ILogger<ParallelSystemManager>? logger = null,
        PerformanceConfiguration? config = null
    )
        : base(logger, config)
    {
        ArgumentNullException.ThrowIfNull(world);

        _dependencyGraph = new SystemDependencyGraph();
        _parallelExecutor = new ParallelQueryExecutor(world);
        _parallelEnabled = enableParallel;
        _logger = logger;
    }

    /// <summary>
    ///     Gets the parallel query executor for this manager.
    /// </summary>
    public ParallelQueryExecutor ParallelExecutor => _parallelExecutor;

    /// <summary>
    ///     Gets the system dependency graph.
    /// </summary>
    public SystemDependencyGraph DependencyGraph => _dependencyGraph;

    /// <summary>
    ///     Gets whether parallel execution is enabled.
    /// </summary>
    public bool IsParallelEnabled => _parallelEnabled;

    /// <summary>
    ///     Gets the execution stages for logging and debugging.
    ///     Returns null if execution plan has not been built yet.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<ISystem>>? ExecutionStages =>
        _executionStages?.Select(stage => (IReadOnlyList<ISystem>)stage.AsReadOnly()).ToList();


    /// <summary>
    ///     Register an update system instance.
    /// </summary>
    public override void RegisterUpdateSystem(IUpdateSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        // Extract and register metadata before calling base
        // IUpdateSystem inherits from ISystem, so Priority is always available
        int priority = system.Priority;
        RegisterSystemMetadata(system, priority);

        base.RegisterUpdateSystem(system);
        _executionPlanBuilt = false;
    }


    /// <summary>
    ///     Register a render system instance.
    /// </summary>
    public override void RegisterRenderSystem(IRenderSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        // Extract and register metadata before calling base
        int priority = system is ISystem s ? s.Priority : system.RenderOrder;
        RegisterSystemMetadata(system, priority);

        base.RegisterRenderSystem(system);
        _executionPlanBuilt = false;
    }

    /// <summary>
    ///     Helper method to extract and register system metadata.
    /// </summary>
    private void RegisterSystemMetadata(object system, int priority)
    {
        ArgumentNullException.ThrowIfNull(system);

        // Create metadata
        SystemMetadata metadata;

        if (system is IParallelSystemMetadata parallelSystem)
        {
            // Use detailed metadata from ParallelSystemBase
            metadata = new SystemMetadata
            {
                SystemType = system.GetType(),
                ReadsComponents = parallelSystem.GetReadComponents(),
                WritesComponents = parallelSystem.GetWriteComponents(),
                Priority = priority,
                AllowsParallelExecution = parallelSystem.AllowsParallelExecution
            };
        }
        else
        {
            // For systems without parallel metadata, assume they allow parallel execution
            // but have no component dependencies (conservative approach)
            metadata = new SystemMetadata
            {
                SystemType = system.GetType(),
                ReadsComponents = new List<Type>(),
                WritesComponents = new List<Type>(),
                Priority = priority,
                AllowsParallelExecution = true // Conservative: allow parallel unless proven otherwise
            };

            _logger?.LogWorkflowStatus(
                $"Registered {system.GetType().Name} (legacy mode)",
                ("parallel", "assumed safe"),
                ("dependencies", "none")
            );
        }

        // Register metadata with dependency graph
        try
        {
            var method = _dependencyGraph.GetType().GetMethod(nameof(_dependencyGraph.RegisterSystem));
            var genericMethod = method!.MakeGenericMethod(system.GetType());
            genericMethod.Invoke(_dependencyGraph, new object[] { metadata });

            _logger?.LogWorkflowStatus(
                $"Registered {system.GetType().Name}",
                ("reads", metadata.ReadsComponents.Count),
                ("writes", metadata.WritesComponents.Count),
                ("parallel", metadata.AllowsParallelExecution)
            );
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to register system metadata for {SystemName}", system.GetType().Name);
        }
    }

    /// <summary>
    ///     Rebuild execution plan based on current registered systems.
    ///     Must be called after all systems are registered and before Update.
    /// </summary>
    public void RebuildExecutionPlan()
    {
        // Collect all registered systems (Update, Render, and legacy Systems)
        var allSystemObjects = new List<object>();

        // Add all update systems
        foreach (var updateSystem in RegisteredUpdateSystems)
        {
            allSystemObjects.Add(updateSystem);
        }

        // Add all render systems
        foreach (var renderSystem in RegisteredRenderSystems)
        {
            allSystemObjects.Add(renderSystem);
        }

        var systemTypes = allSystemObjects.Select(s => s.GetType()).Distinct().ToList();

        _logger?.LogWorkflowStatus(
            "Building execution plan",
            ("total", systemTypes.Count),
            ("update", RegisteredUpdateSystems.Count),
            ("render", RegisteredRenderSystems.Count)
        );

        foreach (var type in systemTypes)
        {
            _logger?.LogDebug("  System registered: {SystemName}", type.Name);
        }

        var stages = _dependencyGraph.ComputeExecutionStages(systemTypes);

        _logger?.LogWorkflowStatus(
            "Computed execution stages",
            ("stages", stages.Count)
        );

        for (int i = 0; i < stages.Count; i++)
        {
            _logger?.LogDebug("  Stage {Index}: {SystemCount} systems", i + 1, stages[i].Count);
        }

        // Map types back to instances
        _executionStages = stages.Select(stage =>
        {
            return stage
                .Select(type => allSystemObjects.FirstOrDefault(s => s.GetType() == type))
                .Where(s => s != null)
                .Cast<ISystem>() // Cast to ISystem for execution
                .ToList();
        }).ToList();

        _executionPlanBuilt = true;

        if (_executionStages.Count == 0 || _executionStages.All(s => s.Count == 0))
        {
            _logger?.LogWarning("Execution plan is empty, parallel execution will be disabled");
            _executionPlanBuilt = false;
            return;
        }

        _logger?.LogWorkflowStatus(
            "Execution plan ready",
            ("stages", _executionStages.Count),
            ("max parallel", _executionStages.Max(s => s.Count))
        );

        LogExecutionPlan();
    }

    /// <summary>
    ///     Update all systems with parallel execution where safe.
    /// </summary>
    public new void Update(World world, float deltaTime)
    {
        ArgumentNullException.ThrowIfNull(world);

        // Lazy rebuild: If execution plan was invalidated by late system registration, rebuild now
        if (_parallelEnabled && !_executionPlanBuilt)
        {
            _logger?.LogWorkflowStatus("Rebuilding execution plan", ("reason", "late system registration"));
            RebuildExecutionPlan();
        }

        if (!_parallelEnabled || !_executionPlanBuilt || _executionStages == null || _executionStages.Count == 0)
        {
            // Fall back to sequential execution
            base.Update(world, deltaTime);
            return;
        }

        // Execute each stage
        foreach (var stage in _executionStages)
        {
            if (stage.Count == 1)
            {
                // Single system in stage, run sequentially
                var system = stage[0];
                if (system.Enabled)
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        system.Update(world, deltaTime);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "System {SystemName} threw exception during update", system.GetType().Name);
                    }
                    stopwatch.Stop();

                    // Track performance metrics
                    TrackSystemPerformance(system.GetType().Name, stopwatch.Elapsed.TotalMilliseconds);
                }
            }
            else
            {
                // Multiple systems in stage, run in parallel
                System.Threading.Tasks.Parallel.ForEach(
                    stage.Where(s => s.Enabled),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    system =>
                    {
                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            system.Update(world, deltaTime);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "System {SystemName} threw exception during parallel update", system.GetType().Name);
                        }
                        stopwatch.Stop();

                        // Track performance metrics (thread-safe)
                        TrackSystemPerformance(system.GetType().Name, stopwatch.Elapsed.TotalMilliseconds);
                    }
                );
            }
        }
    }

    /// <summary>
    ///     Enable or disable parallel execution at runtime.
    /// </summary>
    public void SetParallelExecution(bool enabled)
    {
        if (enabled && !_executionPlanBuilt)
        {
            throw new InvalidOperationException(
                "Cannot enable parallel execution without an execution plan. Call RebuildExecutionPlan() first."
            );
        }

        _logger?.LogWorkflowStatus(
            $"Parallel execution {(enabled ? "enabled" : "disabled")}"
        );
    }

    /// <summary>
    ///     Get execution plan as a formatted string.
    /// </summary>
    public string GetExecutionPlan()
    {
        if (!_executionPlanBuilt || _executionStages == null)
        {
            return "Execution plan not built. Call RebuildExecutionPlan() first.";
        }

        var stages = _executionStages.Select(stage =>
            stage.Select(s => s.GetType()).ToList()
        ).ToList();

        return _dependencyGraph.GenerateExecutionPlan(stages);
    }

    /// <summary>
    ///     Get dependency graph visualization.
    /// </summary>
    public string GetDependencyGraph()
    {
        return _dependencyGraph.GenerateDependencyGraph();
    }

    /// <summary>
    ///     Get parallel execution statistics.
    /// </summary>
    public ParallelExecutionStats GetParallelStats()
    {
        return _parallelExecutor.GetStats();
    }

    private void LogExecutionPlan()
    {
        if (_logger == null || _executionStages == null)
            return;

        _logger.LogDebug("=== Parallel Execution Plan ===");
        for (int i = 0; i < _executionStages.Count; i++)
        {
            var stage = _executionStages[i];
            var systemNames = string.Join(", ", stage.Select(s => s.GetType().Name));
            _logger.LogDebug("Stage {StageNumber}: {SystemCount} systems - {Systems}",
                i + 1, stage.Count, systemNames);
        }
    }
}

/// <summary>
///     Interface for systems that can provide metadata for parallel execution.
/// </summary>
public interface IParallelSystemMetadata
{
    /// <summary>
    ///     Get components this system reads.
    /// </summary>
    List<Type> GetReadComponents();

    /// <summary>
    ///     Get components this system writes.
    /// </summary>
    List<Type> GetWriteComponents();

    /// <summary>
    ///     Whether this system allows parallel execution.
    /// </summary>
    bool AllowsParallelExecution { get; }
}
