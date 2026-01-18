namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when !ext command is executed
/// </summary>
public interface IExtCommandExecuteEventParams: ICommandEventBaseParams
{
    /// <summary>
    /// Requirements of current ext vote
    /// </summary>
    int CurrentRequiredVotes { get; }
    
    /// <summary>
    /// Current ext votes (incremented after this event passed)
    /// </summary>
    int CurrentExtVotes { get; }
}