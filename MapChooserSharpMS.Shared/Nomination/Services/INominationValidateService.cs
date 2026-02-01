using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Shared.Nomination.Services;

public interface INominationValidateService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="mapConfig"></param>
    /// <returns></returns>
    NominationCheckResult PlayerCanNominateMap(IGameClient client, IMapConfig mapConfig);
    
    /// <summary>
    /// Usually used for random map picking <br/>
    /// This method will ignore the player based check such as permission.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns></returns>
    NominationCheckResult CanPickupMap(IMapConfig mapConfig);

    bool IsDuringVotingPeriod();
    
    bool IsMapDisabled(IMapConfig mapConfig);

    bool IsCurrentMap(IMapConfig mapConfig);
    
    bool IsWithinTimeRange(IMapConfig mapConfig);
    
    bool IsWithinAllowedDays(IMapConfig mapConfig);
    
    bool IsGreaterThanMinPlayers(IMapConfig mapConfig, bool includeBots = false);
    
    bool IsLowerThanMaxPlayers(IMapConfig mapConfig, bool includeBots = false);
    
    bool IsPlayerHasRequiredPermission(IMapConfig mapConfig, IGameClient? client);
    
    bool IsPlayerHasRequiredPermission(IMapConfig mapConfig, SteamID steamId);
    
    bool IsRequiresAnyPermission(IMapConfig mapConfig);
    
    bool IsDisallowedBySteamId(IMapConfig mapConfig, SteamID steamId);
    
    bool IsAllowedBySteamId(IMapConfig mapConfig, SteamID steamId);
    
    bool IsRestrictedToCertainUser(IMapConfig mapConfig);
    
    bool IsMapInCooldown(IMapConfig mapConfig);
    
    bool HasReachedGroupNominationLimit(IMapConfig mapConfig);

    /// <summary>
    /// This method returns 3 different result <br/>
    /// NominationCheckResult.AlreadyNominated: If map is already nominated <br/>
    /// NominationCheckResult.NominatedByAdmin: If map is nominated and nominated by admin <br/>
    /// NominationCheckResult.Failed: If map is not nominated <br/>
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    NominationCheckResult GetNominationState(IMapConfig mapConfig, IGameClient? client = null);
    
    IDetailedCooldownResult GetCooldownInformations(IMapConfig mapConfig);
}