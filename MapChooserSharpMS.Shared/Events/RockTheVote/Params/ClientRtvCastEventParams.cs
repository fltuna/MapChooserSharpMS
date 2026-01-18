using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.RockTheVote.Params;

/// <summary>
/// Fired when client cast their RTV
/// </summary>
public interface IClientRtvCastParams: IEventBaseParams
{
    /// <summary>
    /// If true, Rock The Vote will triggerers after this event
    /// </summary>
    bool IsRtvTrigger { get; }
    
    /// <summary>
    /// Client who cast rtv.
    /// </summary>
    IGameClient Client { get; }
}