using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Shared.MapCycle.Services;

public interface IMapCooldownQueryService
{
    /// <summary>
    /// Obtain cooldown information from database
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns></returns>
    Task<IDetailedCooldownResult?> QueryCurrentCooldowns(IMapConfig mapConfig);
    
    /// <summary>
    /// Obtain cooldown information from in-memory.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns></returns>
    IDetailedCooldownResult GetCurrentCooldowns(IMapConfig mapConfig);
}