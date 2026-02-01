using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.MapVote.Managers;
using MapChooserSharpMS.Shared.MapVote.Services;
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
    McsMapVoteState? CurrentVoteState { get; }
    
    /// <summary>
    /// MapVoteManager
    /// </summary>
    IVoteControllingManager MapVoteManager { get; }

    /// <summary>
    /// Service class for starting/cancelling map vote
    /// </summary>
    IMapVoteControllingService MapVoteControllingService { get; }
    
    /// <summary>
    /// Handles player votes
    /// </summary>
    IClientVoteHandlingService  ClientVoteHandlingService { get; }
    
    /// <summary>
    /// Installs event listener
    /// </summary>
    void InstallEventListener(IMapVoteEventListener listener);

    /// <summary>
    /// Remove event listener
    /// </summary>
    void RemoveEventListener(IMapVoteEventListener listener);
}