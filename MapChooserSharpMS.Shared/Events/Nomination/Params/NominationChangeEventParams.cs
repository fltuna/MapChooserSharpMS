namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Fired when nomination is changing
/// </summary>
public interface INominationChangeParams : IEventBaseParams, IMcsNominationEventBaseParams, IEnforceableEvent;