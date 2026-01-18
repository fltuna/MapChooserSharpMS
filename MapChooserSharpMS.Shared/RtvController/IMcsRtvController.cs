using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.RtvController.Managers;
using MapChooserSharpMS.Shared.RtvController.Services;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.RtvController;

/// <summary>
/// RTV Controller API
/// </summary>
public interface IMcsRtvController
{
    
    /// <summary>
    /// Manager class
    /// </summary>
    IRtvManager RtvManager { get; }
    
    /// <summary>
    /// Service for RTV
    /// </summary>
    IRtvService RtvService { get; }
    
    /// <summary>
    /// Installs event listener
    /// </summary>
    void InstallEventListener(IRockTheVoteEventListener listener);

    /// <summary>
    /// Remove event listener
    /// </summary>
    void RemoveEventListener(IRockTheVoteEventListener listener);
}