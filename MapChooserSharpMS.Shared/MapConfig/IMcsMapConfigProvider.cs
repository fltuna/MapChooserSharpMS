using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// 
/// </summary>
public interface IMcsMapConfigProvider
{
    /// <summary>
    /// Reloads the configs from disk.
    /// </summary>
    void ReloadConfigs();
    
    /// <summary>
    /// Returns all group config data that loaded in this server <br/>
    /// Key = group name (e.g. Group1) | Value = group settings
    /// </summary>
    /// <returns>Group config data</returns>
    IReadOnlyDictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> GetGroupSettings();
    
    /// <summary>
    /// Returns all map configs that loaded in this server. <br/>
    /// Key = map name (e.g. ze_example) | Value = map config
    /// </summary>
    /// <returns>Map configs that loaded in this server</returns>
    IReadOnlyDictionary<string, IReadOnlyCollection<IMapConfigOverrides>> GetMapConfigs();

    /// <summary>
    /// Search map config by map name <br/>
    /// This method will returns correspond map configs if override config exists
    /// </summary>
    /// <param name="mapName">Map name defined in config (e.g. ze_example_v1)</param>
    /// <param name="found">MapConfig if found, otherwise null</param>
    /// <returns>true if found, otherwise false</returns>
    bool TryGetMapConfig(string mapName, out IMapConfig found);
    
    
    /// <summary>
    /// Search map config by workshop ID <br/>
    /// This method will returns correspond map configs if override config exists
    /// </summary>
    /// <param name="workshopId">Map workshop ID defined in config</param>
    /// <param name="found">MapConfig if found, otherwise null</param>
    /// <returns>true if found, otherwise false</returns>
    bool TryGetMapConfig(long workshopId, out IMapConfig found);
}