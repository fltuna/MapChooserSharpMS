using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Shared.MapCycle;

public interface IMapCycleController
{
    ITimeLimitManager CurrentMapTimeLimitManager { get; }
    
    
}

