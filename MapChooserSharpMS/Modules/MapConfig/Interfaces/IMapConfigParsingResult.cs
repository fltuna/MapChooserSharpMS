using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Interfaces;

public interface IMapConfigParsingResult
{
    Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> MapGroupSettings { get; }
    Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsNameMapping { get; }
    Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsWorkshopIdMapping { get; }
}