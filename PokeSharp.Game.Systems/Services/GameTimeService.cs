namespace PokeSharp.Game.Systems.Services;

/// <summary>
///     Default implementation of IGameTimeService.
///     Tracks game time for timestamps and time-based game logic.
/// </summary>
public class GameTimeService : IGameTimeService
{
    /// <inheritdoc />
    public float TotalSeconds { get; private set; }

    /// <inheritdoc />
    public double TotalMilliseconds => TotalSeconds * 1000.0;

    /// <inheritdoc />
    public float DeltaTime { get; private set; }

    /// <inheritdoc />
    public void Update(float totalSeconds, float deltaTime)
    {
        TotalSeconds = totalSeconds;
        DeltaTime = deltaTime;
    }
}
