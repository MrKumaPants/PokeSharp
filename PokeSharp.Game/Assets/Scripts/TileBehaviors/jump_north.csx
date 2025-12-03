using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;
using PokeSharp.Engine.Core.Events.Movement;
using PokeSharp.Engine.Core.Events.Tile;

/// <summary>
///     Jump north behavior.
///     Allows jumping north but blocks south movement.
/// </summary>
public class JumpNorthBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        // Block movement from south (can't climb up the ledge)
        // Direction values: 0=South, 1=West, 2=East, 3=North
        On<MovementStartedEvent>((evt) =>
        {
            // If on this tile trying to move south, block it (can't go back down the ledge from wrong side)
            if (evt.Direction == 0) // Moving south
            {
                Context.Logger.LogDebug("Jump north tile: Blocking movement to south - can't enter from north side");
                evt.PreventDefault("Can't move in that direction");
            }
        });

        // Handle jump effect when moving north onto this tile
        On<MovementCompletedEvent>((evt) =>
        {
            // If player just moved north onto this tile, trigger jump
            if (evt.Direction == 3) // Moved north
            {
                Context.Logger.LogDebug("Jump north tile: Player jumped north onto ledge");
                // Jump animation/effect would go here if API existed
            }
        });
    }
}

return new JumpNorthBehavior();
