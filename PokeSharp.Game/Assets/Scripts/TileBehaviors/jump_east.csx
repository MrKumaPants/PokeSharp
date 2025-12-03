using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;
using PokeSharp.Engine.Core.Events.Movement;
using PokeSharp.Engine.Core.Events.Tile;

/// <summary>
///     Jump east behavior.
///     Allows jumping east but blocks west movement.
/// </summary>
public class JumpEastBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        // Block movement from west (can't climb up the ledge)
        // Direction values: 0=South, 1=West, 2=East, 3=North
        On<MovementStartedEvent>((evt) =>
        {
            // If on this tile trying to move west, block it (can't go back across the ledge from wrong side)
            if (evt.Direction == 1) // Moving west
            {
                Context.Logger.LogDebug("Jump east tile: Blocking movement to west - can't enter from east side");
                evt.PreventDefault("Can't move in that direction");
            }
        });

        // Handle jump effect when moving east onto this tile
        On<MovementCompletedEvent>((evt) =>
        {
            // If player just moved east onto this tile, trigger jump
            if (evt.Direction == 2) // Moved east
            {
                Context.Logger.LogDebug("Jump east tile: Player jumped east onto ledge");
                // Jump animation/effect would go here if API existed
            }
        });
    }
}

return new JumpEastBehavior();
