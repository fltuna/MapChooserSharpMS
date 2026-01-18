using System.Threading.Tasks;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.WorkshopManagement;

namespace MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;

public interface IMapTransitionManager
{
    /// <summary>
    /// Next map of IMapConfig.
    /// null until MapChange -> Next map confirmed.
    /// </summary>
    IMapConfig? NextMap { get; }
    
    /// <summary>
    /// Current map of IMapConfig.
    /// null if MCS couldn't find map config for current map.
    /// </summary>
    IMapConfig? CurrentMap { get; }
    
    /// <summary>
    /// True if next map confirmed
    /// </summary>
    bool IsNextMapConfirmed { get; }

    /// <summary>
    /// If true, map will be transit to next map when round end.
    /// </summary>
    bool ChangeMapOnNextRoundEnd { get; set; }
    
    /// <summary>
    /// Set next map to specified map config
    /// </summary>
    /// <param name="mapConfig">Map Config</param>
    /// <returns>True if map is valid and next map successfully changed, otherwise false</returns>
    bool TrySetNextMap(IMapConfig mapConfig);

    /// <summary>
    /// Set next map to specified map name.
    /// </summary>
    /// <param name="mapName">Map Name</param>
    /// <returns>True if map is found by name and next map successfully changed, otherwise false</returns>
    bool TrySetNextMap(string mapName);

    /// <summary>
    /// Set next map to specified workshop ID. <br/>
    /// First, will find from existing map config by workshop ID <br/>
    /// Second, will find from Steam Workshop by using HTTP client and fetch existence.
    /// </summary>
    /// <param name="workshopId">Workshop ID</param>
    /// <param name="fetchResult">Result of fetch</param>
    /// <returns>True if map is found and successfully set, otherwise false</returns>
    Task<bool> TrySetNextMap(long workshopId, out IWorkshopFetchResult fetchResult);
    
    /// <summary>
    /// Removes next map
    /// </summary>
    /// <returns>True if next map is successfully removed, otherwise false</returns>
    bool TryRemoveNextMap();

    /// <summary>
    /// Change to next map <br/>
    /// If next map is null, this method will silently fail and do nothing.
    /// </summary>
    /// <param name="seconds">Seconds to change map</param>
    void TransitionToNextMap(float seconds);
}