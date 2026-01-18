using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.RockTheVote.Params;

/// <summary>
/// Fired when client cancelled their RTV
/// </summary>
public interface IClientRtvUnCastParams: IEventBaseParams, IEnforceableEvent
{
    /// <summary>
    /// Client who uncast rtv.
    /// </summary>
    IGameClient Client { get; }
}