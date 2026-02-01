using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapVote;

public interface IMapVoteInformation
{
    McsMapVoteState CurrentState { get; }
    
    IReadOnlyCollection<IMapVoteOption> VoteOptions { get; }
}