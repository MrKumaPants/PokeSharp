using PokeSharp.Engine.Core.Events.Collision;
using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;

/// <summary>
///     Impassable west behavior.
///     Blocks movement from west.
/// </summary>
public class ImpassableWestBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        On<CollisionCheckEvent>(evt =>
        {
            // Block if moving from west (Direction.West = 1)
            if (evt.FromDirection == 1)
            {
                evt.PreventDefault("Cannot pass from west");
            }
        });
    }
}

return new ImpassableWestBehavior();
