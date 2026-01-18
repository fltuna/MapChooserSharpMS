using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.RockTheVote.Interfaces;
using MapChooserSharpMS.Shared.RockTheVote;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.RockTheVote.Managers;

internal sealed class InternalRtvManager : IInternalRtvManager
{
    private readonly HashSet<int> _participants = new();
    private RtvStatus _status = RtvStatus.Enabled;
    private TimeSpan _rtvCommandUnlockTime =  TimeSpan.Zero;
    
    public RtvStatus RtvStatus => _status;
    public TimeSpan RtvCommandUnlockTime => _rtvCommandUnlockTime;
    public int RtvCounts => RtvParticipants.Count;

    public int RequiredCounts
    {
        get
        {
            // TODO()
            return 0;
        }
    }

    public float RtvCompletionRatio => RtvCounts / (float)RequiredCounts;
    public IReadOnlySet<int> RtvParticipants => _participants;
    
    
    
    public bool AddParticipants(IGameClient client)
        => AddParticipants(client.Slot);

    public bool AddParticipants(int slot)
        => _participants.Add(slot);

    public bool RemoveParticipants(IGameClient client)
        => RemoveParticipants(client.Slot);

    public bool RemoveParticipants(int slot)
        => _participants.Remove(slot);

    public bool TrySetRtvStatus(RtvStatus status)
    {
        if (status == RtvStatus.TriggeredWaitingForMapTransition || status == RtvStatus.TriggeredWaitingForVote)
            return false;

        if (status == RtvStatus.AnotherVoteOngoing)
            return false;
        
        _status = status;
        return true;
    }

    public void ForceSetRtvStatus(RtvStatus status)
    {
        _status = status;
    }

    public void SetNextRtvCommandUnlockTime(TimeSpan nextRtvCommandUnlockTime)
    {
        _rtvCommandUnlockTime = nextRtvCommandUnlockTime;
    }

    public void ForceReset()
    {
        _rtvCommandUnlockTime = TimeSpan.Zero;
        _participants.Clear();
        _status = RtvStatus.Enabled;
    }
}
