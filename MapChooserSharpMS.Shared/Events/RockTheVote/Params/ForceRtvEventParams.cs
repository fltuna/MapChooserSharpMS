using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.RockTheVote.Params;

/// <summary>
/// Fired when admin is enforced this event
/// </summary>
public interface IForceRtvParam: IEventBaseParams, IEnforceableEvent
{
    /// <summary>
    /// Client who initiated force RTV. if executor is console, then param is null
    /// </summary>
    IGameClient? Client { get; }
    
    /// <summary>
    /// Is silent trigger?
    /// </summary>
    bool IsSilent { get; }
}