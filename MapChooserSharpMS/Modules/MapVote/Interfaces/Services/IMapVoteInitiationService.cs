using MapChooserSharpMS.Shared.MapVote;

namespace MapChooserSharpMS.Modules.MapVote.Interfaces.Services;

internal interface IMapVoteInitiationService
{
    IInternalMapVoteInformation CreateNewVote();
    
    IInternalMapVoteInformation PickRandomMap(IInternalMapVoteInformation currentMapVote);
}