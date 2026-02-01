using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Shared.MapVote;

public interface IMapVoteOption
{
    string MapName { get; }
    
    IMapConfig MapConfig { get; }
    
    IReadOnlyCollection<PlayerSlot> VoteParticipants { get; }
}