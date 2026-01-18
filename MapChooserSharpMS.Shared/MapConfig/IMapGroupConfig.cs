namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Map group's name and cooldown
/// </summary>
public interface IMapGroupConfig: IBaseMapConfig
{
    /// <summary>
    /// Group name
    /// </summary>
    public string GroupName { get; }
    
    /// <summary>
    /// Map cooldown will override when set to positive integer
    /// </summary>
    public int MapCooldownOverride { get; }
}