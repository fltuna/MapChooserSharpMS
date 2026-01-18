using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Fired when normal nomination is occured
/// </summary>
public interface INominationCheckPassedEventParams : IEventBaseParams
{
    /// <summary>
    /// Client who activated this event. if it is console, then param is null
    /// </summary>
    IGameClient? Client { get; }
}