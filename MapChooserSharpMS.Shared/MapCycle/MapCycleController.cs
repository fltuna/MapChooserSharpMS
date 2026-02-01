using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.MapCycle.Services;

namespace MapChooserSharpMS.Shared.MapCycle;

public interface IMapCycleController
{
    ITimeLimitManager CurrentMapTimeLimitManager { get; }
    
    IMapCooldownQueryService  MapCooldownQueryService { get; }
    
    IMapCooldownCommandService  MapCooldownCommandService { get; }
}

