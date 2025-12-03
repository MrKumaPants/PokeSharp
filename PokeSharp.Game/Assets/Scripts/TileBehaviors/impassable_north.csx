using PokeSharp.Engine.Core.Events.Collision;
using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;

/// <summary>
///     Impassable north behavior.
///     Blocks movement from north.
/// </summary>
public class ImpassableNorthBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        On<CollisionCheckEvent>(evt =>
        {
            // Block if moving from north (Direction.North = 3)
            if (evt.FromDirection == 3)
            {
                evt.PreventDefault("Cannot pass from north");
            }
        });
    }
}

return new ImpassableNorthBehavior();
