using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.RockTheVote.Managers;

public interface IRtvManager
{
    /// <summary>
    /// Describes current status of RTV
    /// </summary>
    RtvStatus RtvStatus { get; }
    
    /// <summary>
    /// Time to be RTV command unlocked to players <br/>
    /// You can get remaining time in seconds by `RtvCommandUnlockTime - ISharedSystem.GetModSharp().EngineTime()`
    /// </summary>
    TimeSpan RtvCommandUnlockTime { get; }
    
    int RtvCounts { get; }
    
    int RequiredCounts { get; }
    
    float RtvCompletionRatio { get; }
    
    /// <summary>
    /// User slot of RTV participants
    /// </summary>
    IReadOnlySet<int> RtvParticipants { get; }
}