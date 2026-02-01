using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.Nomination.Models;

public class DetailedCooldownResult
    : IDetailedCooldownResult
{
    public DetailedCooldownResult(IMapConfig mapConfig, int mapCooldown, Dictionary<string, int> groupCooldown, DateTime timedCooldown, IReadOnlyDictionary<string, DateTime> groupTimedCooldowns)
    {
        MapConfig = mapConfig;
        CooldownCount = mapCooldown;
        GroupCooldowns = groupCooldown;
        HighestCooldownCount = groupCooldown.Values.Append(mapCooldown).Max();
        LongestTimedCooldown = groupTimedCooldowns.Values.Append(timedCooldown).Max();
        TimedCooldown = timedCooldown;
        GroupTimedCooldowns = groupTimedCooldowns;
        HasCooldown = HighestCooldownCount > 0 || LongestTimedCooldown.Second > 0;
    }

    public bool HasCooldown { get; }
    public int HighestCooldownCount { get; }
    public DateTime LongestTimedCooldown { get; }
    public IMapConfig MapConfig { get; }
    public int CooldownCount { get; }
    public DateTime TimedCooldown { get; }
    public IReadOnlyDictionary<string, int> GroupCooldowns { get; }
    public IReadOnlyDictionary<string, DateTime> GroupTimedCooldowns { get; }
}