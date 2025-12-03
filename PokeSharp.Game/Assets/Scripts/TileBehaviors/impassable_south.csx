using PokeSharp.Engine.Core.Events.Collision;
using PokeSharp.Game.Components.Movement;
using PokeSharp.Game.Scripting.Runtime;

/// <summary>
///     Impassable south behavior.
///     Blocks movement from south.
/// </summary>
public class ImpassableSouthBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        On<CollisionCheckEvent>(evt =>
        {
            // Block if moving from south (Direction.South = 0)
            if (evt.FromDirection == 0)
            {
                evt.PreventDefault("Cannot pass from south");
            }
        });
    }
}

return new ImpassableSouthBehavior();
