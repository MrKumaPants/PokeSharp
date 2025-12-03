using PokeSharp.Engine.UI.Debug.Components.Base;
using PokeSharp.Engine.UI.Debug.Components.Controls;
using PokeSharp.Engine.UI.Debug.Core;
using PokeSharp.Engine.UI.Debug.Layout;
using PokeSharp.Engine.UI.Debug.Models;

namespace PokeSharp.Engine.UI.Debug.Components.Debug;

/// <summary>
///     Debug panel for inspecting event bus activity.
///     Shows registered events, subscriptions, and performance metrics in real-time.
/// </summary>
public class EventInspectorPanel : DebugPanelBase
{
    private readonly EventInspectorContent _content;

    /// <summary>
    ///     Creates an EventInspectorPanel with the specified components.
    /// </summary>
    public EventInspectorPanel(EventInspectorContent content, StatusBar statusBar)
        : base(statusBar)
    {
        _content = content;
        Id = "event_inspector_panel";

        // Content fills space above StatusBar
        _content.Constraint.Anchor = Anchor.StretchTop;
        AddChild(_content);
    }

    public bool HasProvider => _content.HasProvider;

    /// <summary>
    ///     Sets the data provider function for event inspector data.
    /// </summary>
    public void SetDataProvider(Func<EventInspectorData>? provider)
    {
        _content.SetDataProvider(provider);
    }

    /// <summary>
    ///     Refreshes the event inspector display immediately.
    /// </summary>
    public void Refresh()
    {
        _content.Refresh();
    }

    /// <summary>
    ///     Sets the refresh interval in frames.
    /// </summary>
    public void SetRefreshInterval(int frameInterval)
    {
        _content.SetRefreshInterval(frameInterval);
    }

    /// <summary>
    ///     Gets the current refresh interval.
    /// </summary>
    public int GetRefreshInterval()
    {
        return _content.GetRefreshInterval();
    }

    /// <summary>
    ///     Toggles subscription details visibility.
    /// </summary>
    public void ToggleSubscriptions()
    {
        _content.ToggleSubscriptions();
    }

    /// <summary>
    ///     Selects the next event in the list.
    /// </summary>
    public void SelectNextEvent()
    {
        _content.SelectNextEvent();
    }

    /// <summary>
    ///     Selects the previous event in the list.
    /// </summary>
    public void SelectPreviousEvent()
    {
        _content.SelectPreviousEvent();
    }

    /// <summary>
    ///     Scrolls the content up.
    /// </summary>
    public void ScrollUp(int lines = 1)
    {
        _content.ScrollUp(lines);
    }

    /// <summary>
    ///     Scrolls the content down.
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        _content.ScrollDown(lines);
    }

    protected override UIComponent GetContentComponent()
    {
        return _content;
    }

    protected override void UpdateStatusBar()
    {
        // Update content first
        _content.Update();

        if (!_content.HasProvider)
        {
            SetStatusBar("No event data provider configured", "");
            return;
        }

        int refreshRate = 60 / _content.GetRefreshInterval();
        string hints = $"↑↓: Select Event | Tab: Toggle Details | R: Refresh | Refresh: ~{refreshRate}fps";
        SetStatusBar("Event Inspector Active", hints);
    }
}
