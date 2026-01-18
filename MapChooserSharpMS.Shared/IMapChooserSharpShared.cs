using MapChooserSharpMS.Shared.Events;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycleController;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.RtvController;

namespace MapChooserSharpMS.Shared;

/// <summary>
/// API for MapChooserSharp
/// </summary>
public interface IMapChooserSharpShared
{
    /// <summary>
    /// ModSharp module identity
    /// </summary>
    static readonly string ModSharpModuleIdentity = typeof(IMapChooserSharpShared).Assembly.GetName().FullName;
    
    /// <summary>
    /// MapCycleController API, You can manipulate map cycle system
    /// </summary>
    IMapCycleControllerApi MapCycleController { get; }
    
    /// <summary>
    /// MapCycleExtendController API, You can manipulate map extend system
    /// </summary>
    IMapCycleExtendControllerApi MapCycleExtendController { get; }
    
    /// <summary>
    /// McsMapCycleExtendVoteController API, You can manipulate vote map extend system
    /// </summary>
    IMapCycleExtendVoteControllerApi MapCycleExtendVoteController { get; }
    
    /// <summary>
    /// Nomination API, You can manipulate nomination system
    /// </summary>
    IMcsNominationController McsNominationController { get; }
    
    /// <summary>
    /// VoteController API, You can manipulate vote system
    /// </summary>
    IMcsMapVoteController McsMapVoteController { get; }
    
    /// <summary>
    /// RTVController API, You can manipulate RTV system
    /// </summary>
    IMcsRtvController McsRtvController { get; }
    
    /// <summary>
    /// MapConfigProvider API, You can manipulate map config
    /// </summary>
    IMcsMapConfigProvider McsMapConfigProvider { get; }
}