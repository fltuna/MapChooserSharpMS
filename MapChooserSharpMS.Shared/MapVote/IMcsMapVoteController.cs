using MapChooserSharpMS.Shared.Events.MapVote;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.MapVote;

/// <summary>
/// MapVoteController API
/// </summary>
public interface IMcsMapVoteController
{
    /// <summary>
    /// Current state of vote <see cref="McsMapVoteState"/>
    /// </summary>
    McsMapVoteState CurrentVoteState { get; }

    /// <summary>
    /// Installs event listener
    /// </summary>
    void InstallEventListener(IMapVoteEventListener listener);

    /// <summary>
    /// Remove event listener
    /// </summary>
    void RemoveEventListener(IMapVoteEventListener listener);

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
    /// Remove client's vote from the current vote.
    /// </summary>
    /// <param name="client">Client Controller</param>
    void RemoveClientVote(IGameClient client);

    /// <summary>
    /// Remove client's vote from the current vote.
    /// </summary>
    /// <param name="userId">Client userId</param>
    void RemoveClientVote(int userId);


    /// <summary>
    /// Removes client's vote and show vote menu to client
    /// </summary>
    /// <param name="client">Client Controller</param>
    void ClientReVote(IGameClient client);
}