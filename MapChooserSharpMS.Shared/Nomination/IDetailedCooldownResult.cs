using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Nomination;

public interface IDetailedCooldownResult
{
    /// <summary>
    /// Highest cooldown that applied to this map currently
    /// </summary>
    public int HighestCooldown { get; }
    
    /// <summary>
    /// Map config data for checking the default cooldown of the map
    /// </summary>
    public IMapConfig MapConfig { get; }
    
    /// <summary>
    /// Map cooldown that applied to this map currently
    /// </summary>
    public int MapCooldown { get; }
    
    /// <summary>
    /// Group cooldowns that applied to this map currently
    /// </summary>
    public Dictionary<string, int> GroupCooldowns { get; }
}