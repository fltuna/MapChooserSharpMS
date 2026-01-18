using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Map Configuration
/// </summary>
public interface IMapConfig: IBaseMapConfig
{
    /// <summary>
    /// Map name
    /// </summary>
    public string MapName { get; }
    
    /// <summary>
    /// Alias name for display
    /// </summary>
    public string MapNameAlias { get; }
    
    /// <summary>
    /// Map description
    /// </summary>
    public string MapDescription { get; }
    
    /// <summary>
    /// The value should be 0, when workshop ID is not specified in map config.
    /// </summary>
    public long WorkshopId { get; }
    
    /// <summary>
    /// Group settings 
    /// </summary>
    public List<IMapGroupConfig> GroupSettings { get; }
}