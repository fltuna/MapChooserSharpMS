using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;

namespace MapChooserSharpMS.Shared.Events.RockTheVote;

public interface IRockTheVoteEventListener: IEventListenerBase
{
    /// <summary>
    /// If true, client's RTV will be cancelled
    /// </summary>
    bool OnClientRtvCast(IClientRtvCastParams @params)
        => false;
    
    /// <summary>
    /// If true, client's RTV will be cancelled
    /// </summary>
    bool OnClientRtvUnCast(IClientRtvUnCastParams @params)
        => false;

    /// <summary>
    /// If true, force RTV will be cancelled
    /// </summary>
    bool OnForceRtv(IForceRtvParam @params)
        => false;
}