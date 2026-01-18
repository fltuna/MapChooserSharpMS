using System;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.RockTheVote.Interfaces;

internal interface IInternalRtvManager: IRtvManager
{
    bool AddParticipants(IGameClient client);
    
    bool AddParticipants(int slot);
    
    bool RemoveParticipants(IGameClient client);
    
    bool RemoveParticipants(int slot);
    
    bool TrySetRtvStatus(RtvStatus status);
    
    void ForceSetRtvStatus(RtvStatus status);
    
    void SetNextRtvCommandUnlockTime(TimeSpan nextRtvCommandUnlockTime);

    void ForceReset();
}