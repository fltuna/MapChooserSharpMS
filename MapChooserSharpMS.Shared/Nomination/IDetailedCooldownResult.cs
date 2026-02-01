using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Nomination;

public interface IDetailedCooldownResult
{
    /// <summary>
    /// True when any cooldown has applied
    /// </summary>
    bool HasCooldown { get; }
    
    /// <summary>
    /// Highest cooldown that applied to this map currently
    /// </summary>
    int HighestCooldownCount { get; }
    
    /// <summary>
    /// Highest timed cooldown that applied to this map currently
    /// </summary>
    DateTime LongestTimedCooldown { get; }
    
    /// <summary>
    /// Map config data for checking the default cooldown of the map
    /// </summary>
    IMapConfig MapConfig { get; }
    
    /// <summary>
    /// Map cooldown that applied to this map currently
    /// </summary>
    int CooldownCount { get; }
    
    /// <summary>
    /// Timed map cooldown that applied to this map currently
    /// </summary>
    DateTime TimedCooldown { get; }
    
    /// <summary>
    /// Group cooldowns that applied to this map currently
    /// </summary>
    IReadOnlyDictionary<string, int> GroupCooldowns { get; }
    
    /// <summary>
    /// Group timed cooldowns that applied to this map currently
    /// </summary>
    IReadOnlyDictionary<string, DateTime> GroupTimedCooldowns { get; }
}