using System.Collections.Generic;
using MapChooserSharpMS.Modules.MapConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class MapConfigParsingService: IMapConfigParsingService
{
    public IMapConfigParsingResult? ParseConfigs(string configPath)
    {
        return null;
    }
    
    internal record MapConfigParsingResult(
        Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> MapGroupSettings,
        Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsNameMapping,
        Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsWorkshopIdMapping): IMapConfigParsingResult
    {
        public  Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> MapGroupSettings { get; } =
            MapGroupSettings;

        public  Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsNameMapping { get; } =
            MapConfigsNameMapping;

        public Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsWorkshopIdMapping { get; } =
            MapConfigsWorkshopIdMapping;
    }
}