using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Modules.Nomination.Interfaces;

public interface IMcsInternalMapNominationService: IMapNominationService
{
    int NominationCountLimit { get; }
    
    bool ClearNominations();
}