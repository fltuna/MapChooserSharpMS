using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
///  This event will be called when player executes a !mapinfo command. <br/>
/// You can extend !mapinfo command result by listening this event and printing additional information.
/// </summary>
public interface IMapInfoCommandExecutedParams: ICommandEventBaseParams
{
    /// <summary>
    /// Map config that executor searched for.
    /// </summary>
    IMapConfig MapConfig { get; }
}