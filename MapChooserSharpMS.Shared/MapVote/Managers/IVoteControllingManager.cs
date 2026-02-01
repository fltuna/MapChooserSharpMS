using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapVote.Managers;

public interface IVoteControllingManager
{
    IMapVoteInformation? CurrentVote { get; }
}