using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.MapVote.Services;

public interface IMapVoteControllingService
{
    /// <summary>
    /// Initiate a map vote.
    /// </summary>
    /// <param name="isActivatedByRtv">If true, first option is "don't change", if false then "Extend Current Map"</param>
    /// <returns>McsMapVoteState.InitializeAccepted if successfully initiated, otherwise it's state of current vote</returns>
    McsMapVoteState InitiateVote(bool isActivatedByRtv = false);
    
    /// <summary>
    /// Cancel the current vote.
    /// </summary>
    /// <returns>McsMapVoteState.Cancelling if successfully canceled, otherwise it's state of current vote</returns>
    McsMapVoteState CancelVote(IGameClient? client);

    /// <summary>
    /// Force reset the current vote.
    /// </summary>
    /// <returns></returns>
    bool ForceResetVote();
}