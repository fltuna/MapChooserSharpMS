using MapChooserSharpMS.Shared.Events.MapCycle.Params;

namespace MapChooserSharpMS.Shared.Events.MapCycle;

public interface IMapCycleEventListener : IEventListenerBase
{
    /// <summary>
    /// When ext command executed, 
    /// </summary>
    /// <param name="params"></param>
    /// <returns></returns>
    bool OnExtCommandExecute(IExtCommandExecuteEventParams @params)
        => false;
    
    /// <summary>
    /// Fired when map info command is fully executed (you can add more information for !mapinfo command by this)
    /// </summary>
    /// <param name="params"></param>
    void OnMapInfoCommandExecuted(IMapInfoCommandExecutedParams @params) {}

    /// <summary>
    /// Fired when extend vote start
    /// </summary>
    /// <param name="params"></param>
    void OnExtendVoteStarted(IExtendVoteStartedEventParams @params) {}
    
    /// <summary>
    /// Fired when extend vote cancelled
    /// </summary>
    /// <param name="params"></param>
    void OnExtendVoteCancelled(IExtendVoteCancelledEventParams @params) {}
    
    
    /// <summary>
    /// FIred when next map is confirmed
    /// </summary>
    /// <param name="params"></param>
    void OnNextMapConfirmed(INextMapConfirmedEventParams @params) {}
    
    /// <summary>
    /// Fired when next map is removed
    /// </summary>
    /// <param name="params"></param>
    void OnNextMapRemoved(INextMapRemovedEventParams @params) {}
    
    
    /// <summary>
    /// Fired when going to intermission state
    /// </summary>
    void OnMcsIntermission(IMcsIntermissionParams @params) {}
}