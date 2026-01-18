using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination.Services;

public interface IMapNominationService
{
    /// <summary>
    /// Add map to nomination
    /// </summary>
    /// <param name="nominator"></param>
    /// <param name="mapConfig"></param>
    /// <returns>True if successfully nominated, otherwise false</returns>
    NominationCheckResult TryNominateMap(IGameClient nominator, IMapConfig mapConfig);
    
    /// <summary>
    /// Add map to nomination as an Admin
    /// </summary>
    /// <param name="nominator"></param>
    /// <param name="mapConfig"></param>
    /// <returns>True if successfully nominated, otherwise false</returns>
    NominationCheckResult TryAdminNominateMap(IGameClient? nominator, IMapConfig mapConfig);

    /// <summary>
    /// Removes map from nomination
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <param name="executor"></param>
    /// <param name="forceRemoval"></param>
    /// <returns>True if successfully removed, otherwise false</returns>
    bool TryRemoveNomination(IMapConfig mapConfig, IGameClient? executor = null, bool forceRemoval = false);
}