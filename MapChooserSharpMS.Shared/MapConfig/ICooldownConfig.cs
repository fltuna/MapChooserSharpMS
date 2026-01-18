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
    /// Current cooldown in memory.
    /// </summary>
    public int CurrentCooldown { get; set; }
}