using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.MapCycle.Services;

public interface IMapCooldownCommandService
{
    /// <summary>
    /// Applies cooldown to specified map.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <param name="cooldown"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> SetCooldown(IMapConfig mapConfig, int cooldown);
    
    /// <summary>
    /// Applies timed cooldown to specified map.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <param name="cooldown"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> SetTimedCooldown(IMapConfig mapConfig, TimeSpan cooldown);
    
    /// <summary>
    /// This will apply the maximum int32 number to nomination cooldown.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> ExcludeFromNomination(IMapConfig mapConfig);
    
    /// <summary>
    /// This will try to clear cooldown
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> ClearCooldown(IMapConfig mapConfig);
    
    /// <summary>
    /// This will try to clear timed cooldown
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> ClearTimedCooldown(IMapConfig mapConfig);
}