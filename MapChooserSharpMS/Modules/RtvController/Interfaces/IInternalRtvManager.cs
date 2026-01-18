using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.RtvController;
using MapChooserSharpMS.Shared.RtvController.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.RtvController.Interfaces;

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