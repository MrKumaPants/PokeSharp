namespace PokeSharp.Engine.Core.Events.System;

/// <summary>
/// Event published every frame for time-based updates.
/// Used by scripts that need frame-by-frame logic (NPC AI, animations, etc.)
/// </summary>
public sealed record TickEvent : IGameEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Time elapsed since the last frame (in seconds)
    /// </summary>
    public float DeltaTime { get; init; }

    /// <summary>
    /// Total elapsed time since game start (in seconds)
    /// </summary>
    public float TotalTime { get; init; }
}
