namespace MonoBallFramework.Game.Engine.Scenes.Services;

/// <summary>
///     Interface for orchestrating map popup display during map transitions.
///     Subscribes to map events and manages the popup scene lifecycle.
/// </summary>
public interface IMapPopupOrchestrator : IDisposable
{
    // This interface is primarily a marker for DI registration.
    // The implementation subscribes to events internally.
}
