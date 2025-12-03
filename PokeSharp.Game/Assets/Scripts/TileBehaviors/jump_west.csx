using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;
using PokeSharp.Engine.Core.Events.Movement;
using PokeSharp.Engine.Core.Events.Tile;

/// <summary>
///     Jump west behavior.
///     Allows jumping west but blocks east movement.
/// </summary>
public class JumpWestBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        // Block movement from east (can't climb up the ledge)
        // Direction values: 0=South, 1=West, 2=East, 3=North
        On<MovementStartedEvent>((evt) =>
        {
            // If on this tile trying to move east, block it (can't go back across the ledge from wrong side)
            if (evt.Direction == 2) // Moving east
            {
                Context.Logger.LogDebug("Jump west tile: Blocking movement to east - can't enter from west side");
                evt.PreventDefault("Can't move in that direction");
            }
        });

        // Handle jump effect when moving west onto this tile
        On<MovementCompletedEvent>((evt) =>
        {
            // If player just moved west onto this tile, trigger jump
            if (evt.Direction == 1) // Moved west
            {
                Context.Logger.LogDebug("Jump west tile: Player jumped west onto ledge");
                // Jump animation/effect would go here if API existed
            }
        });
    }
}

return new JumpWestBehavior();
