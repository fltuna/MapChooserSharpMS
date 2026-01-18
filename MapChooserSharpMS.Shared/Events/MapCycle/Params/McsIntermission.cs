using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when going to intermission state
/// </summary>
public interface IMcsIntermissionParams: IEventBaseParams
{
    /// <summary>
    /// Next map to transition
    /// </summary>
    IMapConfig NextMap { get; }
}