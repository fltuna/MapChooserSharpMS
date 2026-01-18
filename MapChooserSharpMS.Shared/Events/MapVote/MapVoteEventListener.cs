using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapVote;

public interface IMapVoteEventListener: IEventListenerBase
{
    /// <summary>
    /// You can cancel map vote when return true
    /// </summary>
    bool OnMapVoteStart(IMapVoteStartParams @params) 
        => false;
    
    /// <summary>
    /// When you return non-empty list, then map vote will use listed maps to vote.
    /// </summary>
    List<IMapConfig> OnRandomMapPick(IMapVoteRandomMapPickParams @params)
        => [];
    
    void OnMapVoteCancelled(IMapVoteCancelledParams @params) {}
    
    void OnMapExtended(IMapVoteExtendParams @params) {}
    
    void OnMapNotChanged(IMapVoteNotChangedParams @params) {}
    
    void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params) {}
}