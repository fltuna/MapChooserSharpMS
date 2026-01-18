using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when random map picking <br/>
/// Returning XXXXXX
/// </summary>
public interface IMapVoteRandomMapPickParams: IEventBaseParams
{
    /// <summary>
    /// Minimum map counts that required for current map vote
    /// </summary>
    /// <returns></returns>
    int MinimumMapCounts { get; }
    
    /// <summary>
    /// Full map configs list for manipulating vote list.
    /// </summary>
    /// <returns></returns>
    IReadOnlyDictionary<string, IMapConfig> MapConfigs { get; }
}