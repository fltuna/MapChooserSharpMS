namespace MapChooserSharpMS.Shared.Events;

public interface IEventListenerBase
{
    /// <summary>
    /// Higher is more priority <br/>
    /// if same priority provided, no execution order guarantee. 
    /// </summary>
    int ListenerPriority { get; }
}