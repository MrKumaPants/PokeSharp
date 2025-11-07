namespace PokeSharp.Core.Components.Player;

/// <summary>
///     Tag component identifying an entity as the player.
///     Used for entity queries to find the player entity.
/// </summary>
public struct Player
{
    /// <summary>
    ///     The player's current money/currency.
    /// </summary>
    public int Money;

    // Future fields can be added here: Name, PlayTime, etc.
}
