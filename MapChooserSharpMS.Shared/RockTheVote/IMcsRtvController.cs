using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote.Managers;
using MapChooserSharpMS.Shared.RockTheVote.Services;

namespace MapChooserSharpMS.Shared.RockTheVote;

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