using Arch.Core;
using MonoBallFramework.Game.Engine.Rendering.Context;

namespace MonoBallFramework.Game.Engine.Core.Systems;

/// <summary>
///     Interface for systems that perform rendering operations.
///     Render systems read component data and draw to the screen.
///     These systems execute during the Draw() phase of the game loop.
/// </summary>
/// <remarks>
///     <para>
///         <b>Camera Ownership Pattern:</b>
///         Scenes own and manage cameras. They create a RenderContext with the scene's
///         camera and pass it to render systems. Render systems DO NOT query for cameras.
///     </para>
///     <para>
///         This ensures:
///         - Scenes control what camera is used
///         - Render systems are stateless (testable)
///         - Multi-scene support (each scene has its own camera)
///         - Proper separation of concerns
///     </para>
/// </remarks>
public interface IRenderSystem : ISystem
{
    /// <summary>
    ///     Gets the order for render execution.
    ///     Lower values render first (background). Higher values render last (foreground).
    ///     Typical values: 0 (background), 1 (world), 2 (UI), 3 (debug).
    /// </summary>
    int RenderOrder { get; }

    /// <summary>
    ///     Renders the system's visual representation using the provided render context.
    ///     This method is called during the Draw phase of the game loop.
    /// </summary>
    /// <param name="world">The ECS world containing all entities to render.</param>
    /// <param name="context">The render context containing camera and rendering parameters.</param>
    void Render(World world, RenderContext context);
}
