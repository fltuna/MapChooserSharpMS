using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when map is extending
/// </summary>
public interface IMapVoteExtendParams: IEventBaseParams
{
    /// <summary>
    /// How long the map will be extended in minutes or rounds, depends on TimeLimitType
    /// </summary>
    int ExtendTime { get; }
    
    /// <summary>
    /// What time type will be extended
    /// </summary>
    TimeLimitType TimeLimitType { get; }
}