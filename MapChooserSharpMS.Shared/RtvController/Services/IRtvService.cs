using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.RtvController.Services;

public interface IRtvService
{
    /// <summary>
    /// Add player to RTV participant
    /// </summary>
    /// <param name="client"></param>
    /// <returns>Client's RTV result</returns>
    RtvExecutionResult AddClientToRtv(IGameClient client);
    
    /// <summary>
    /// Add player to RTV participant
    /// </summary>
    /// <param name="slot"></param>
    /// <returns>Client's RTV result</returns>
    RtvExecutionResult AddClientToRtv(int slot);

    /// <summary>
    /// Remove player from RTV participant
    /// </summary>
    /// <param name="client"></param>
    /// <param name="enforcer"></param>
    /// <returns></returns>
    bool RemoveClientFromRtv(IGameClient client, IGameClient? enforcer = null);

    /// <summary>
    /// Remove player from RTV participant
    /// </summary>
    /// <param name="slot"></param>
    /// <returns></returns>
    bool RemoveClientFromRtv(int slot);
    
    /// <summary>
    /// Initiate a rtv vote
    /// </summary>
    void InitiateRtvVote();

    /// <summary>
    /// Enables RTV
    /// </summary>
    /// <param name="client">Client who enabled, if null then treated as Console</param>
    /// <param name="silently">If true, method will not print the broadcast message</param>
    void EnableRtvCommand(IGameClient? client = null, bool silently = false);


    /// <summary>
    /// Disables RTV
    /// </summary>
    /// <param name="client">Client who disabled, if null then treated as Console</param>
    /// <param name="silently">If true, method will not print the broadcast message</param>
    void DisableRtvCommand(IGameClient? client = null, bool silently = false);


    /// <summary>
    /// Initiate a force RTV
    /// </summary>
    /// <param name="client">Client who triggered, if null then treated as Console</param>
    void InitiateForceRtvVote(IGameClient? client);
}