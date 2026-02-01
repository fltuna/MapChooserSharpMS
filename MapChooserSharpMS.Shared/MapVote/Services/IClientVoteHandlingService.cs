using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.MapVote.Services;

public interface IClientVoteHandlingService
{
    /// <summary>
    /// Tries to add vote.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    bool TryAddClientVote(IGameClient? client, IMapVoteOption option);
    
    /// <summary>
    /// Remove client's vote from the current vote.
    /// </summary>
    /// <param name="client">Client Controller</param>
    void RemoveClientVote(IGameClient client);

    /// <summary>
    /// Remove client's vote from the current vote.
    /// </summary>
    /// <param name="userId">Client userId</param>
    void RemoveClientVote(int userId);
    
    /// <summary>
    /// Removes client's vote and show vote menu to client <br/>
    /// This will not available for Native vote UI and silently ignored.
    /// </summary>
    /// <param name="client">Client Controller</param>
    void ClientReVote(IGameClient client);
}