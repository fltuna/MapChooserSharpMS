using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace MapChooserSharpMS.Shared.Events;

public interface ICommandEventBaseParams: IEventBaseParams
{
    /// <summary>
    /// Client who executed command. if it is console, then param is null
    /// </summary>
    IGameClient? Client { get; }
    
    /// <summary>
    /// Current command information
    /// </summary>
    ref StringCommand Command { get; }
}