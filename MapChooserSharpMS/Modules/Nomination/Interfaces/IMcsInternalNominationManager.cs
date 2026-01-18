using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;

namespace MapChooserSharpMS.Modules.Nomination.Interfaces;

internal interface IMcsInternalNominationManager: INominationManager
{
    bool ClearNominations();
    
    bool AddNomination(IMcsNominationData nominationData);
    
    bool RemoveNomination(IMcsNominationData nominationData);
}