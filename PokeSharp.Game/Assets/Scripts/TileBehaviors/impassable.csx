using PokeSharp.Engine.Core.Events.Collision;
using PokeSharp.Game.Scripting.Runtime;

/// <summary>
///     Impassable tile behavior.
///     Blocks all movement in any direction.
/// </summary>
public class ImpassableBehavior : ScriptBase
{
    public override void RegisterEventHandlers(ScriptContext ctx)
    {
        On<CollisionCheckEvent>(evt =>
        {
            evt.PreventDefault("Tile is impassable");
        });
    }
}

return new ImpassableBehavior();
