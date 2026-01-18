using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when next map is removed
/// </summary>
public interface INextMapRemovedEventParams: IEventBaseParams
{
    /// <summary>
    /// The previous map that was set.
    /// </summary>
    IMapConfig PreviousNextMap { get; }
}