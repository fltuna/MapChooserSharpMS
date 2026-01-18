using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when vote is start
/// </summary>
public interface IMapVoteStartParams: IEventBaseParams
{
    /// <summary>
    /// Maps will be displayed in vote
    /// </summary>
    IReadOnlyList<IMapConfig> MapsToVote { get; }

    /// <summary>
    /// Participants for this vote
    /// </summary>
    IReadOnlyList<PlayerSlot> VoteParticipants { get; }
}