using MapChooserSharpMS.Shared.Nomination;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Base parameter class for McsNomination events
/// </summary>
public interface IMcsNominationEventBaseParams
{
    /// <summary>
    /// Client who activated this event. if it is console, then param is null
    /// </summary>
    IGameClient? Client { get; }

    /// <summary>
    /// Nomination Data
    /// </summary>
    IMcsNominationData NominationData { get; }
}