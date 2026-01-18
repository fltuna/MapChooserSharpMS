using MapChooserSharpMS.Shared.Events.Nomination.Params;

namespace MapChooserSharpMS.Shared.Events.Nomination;

public interface INominationEventListener: IEventListenerBase
{
    /// <summary>
    /// You can add additional nomination validation in here <br/>
    /// This event will fire before OnNomination and OnAdminNomination <br/>
    /// If return true, nomination check will fail <br/>
    /// </summary>
    /// <param name="params"></param>
    /// <returns></returns>
    bool OnNominationCheckPassed(INominationCheckPassedEventParams @params)
        => false;
    
    /// <summary>
    /// If return true, nomination will be cancelled
    /// </summary>
    bool OnNomination(INominationParams @params)
        => false;
    
    /// <summary>
    /// If return true, nomination will be cancelled
    /// </summary>
    bool OnAdminNomination(IAdminNominationParams @params)
        => false;
    
    void OnNominationChanged(INominationChangeParams @params) {}

    void OnNominationRemoved(INominationRemovedParams @params) {}
}