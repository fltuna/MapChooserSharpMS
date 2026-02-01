using System;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Map's cooldown
/// </summary>
public interface ICooldownConfig
{
    /// <summary>
    /// Cooldown value specified in map config
    /// </summary>
    public int ConfigCooldown { get; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan TimedCooldown { get; }
    
    /// <summary>
    /// Current cooldown in memory.
    /// </summary>
    public int CurrentCooldown { get; }
    
    /// <summary>
    /// 
    /// </summary>
    public DateTime LastPlayedAt { get; }
}