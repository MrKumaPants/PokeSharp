using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;
using PokeSharp.Engine.Core.Events.Movement;
using PokeSharp.Engine.Core.Events.Tile;

/// <summary>
///     Ice tile behavior.
///     Forces sliding movement in the current direction.
/// </summary>
public class IceBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        // Handle sliding when movement completes on ice
        On<MovementCompletedEvent>((evt) =>
        {
            Context.Logger.LogDebug($"Ice tile: Movement completed with direction {evt.Direction}");

            // Continue sliding in current direction if valid (0-3 are valid directions)
            if (evt.Direction >= 0 && evt.Direction <= 3)
            {
                Context.Logger.LogDebug($"Ice tile: Forcing continued movement in direction {evt.Direction}");
                // The movement system will pick up the forced direction from tile metadata
            }
        });
    }
}

return new IceBehavior();
