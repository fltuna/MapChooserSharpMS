using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when going to intermission state
/// </summary>
public interface IMapCooldownApplyEventParams: IEnforceableEvent, IEventBaseParams
{
    /// <summary>
    /// Map cooldown application target
    /// </summary>
    IMapConfig AppliesTo { get; }
}