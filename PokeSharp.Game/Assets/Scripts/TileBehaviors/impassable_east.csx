using PokeSharp.Engine.Core.Events.Collision;
using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;

/// <summary>
///     Impassable east behavior.
///     Blocks movement from east.
/// </summary>
public class ImpassableEastBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        On<CollisionCheckEvent>(evt =>
        {
            // Block if moving from east (Direction.East = 2)
            if (evt.FromDirection == 2)
            {
                evt.PreventDefault("Cannot pass from east");
            }
        });
    }
}

return new ImpassableEastBehavior();
