using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination.Managers;

public interface INominationManager
{
    IReadOnlyDictionary<string, IMcsNominationData> NominatedMaps { get; }
}