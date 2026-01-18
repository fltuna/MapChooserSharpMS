using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when next map is confirmed or changed
/// </summary>
public interface INextMapConfirmedEventParams: IEventBaseParams
{
    /// <summary>
    /// Next map config data
    /// </summary>
    IMapConfig NextMap { get; }
    
    /// <summary>
    /// Old next map config data before changed
    /// </summary>
    IMapConfig? OldNextMap { get; }
}