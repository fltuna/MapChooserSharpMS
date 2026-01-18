using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.Nomination.Models;

public class DetailedCooldownResult
    : IDetailedCooldownResult
{
    public DetailedCooldownResult(IMapConfig mapConfig, int mapCooldown, Dictionary<string, int> groupCooldown)
    {
        MapConfig = mapConfig;
        MapCooldown = mapCooldown;
        GroupCooldowns = groupCooldown;
        HighestCooldown = Math.Max(mapCooldown, groupCooldown.Values.MaxBy(t => t));
    }
    
    public int HighestCooldown { get; }
    public IMapConfig MapConfig { get; }
    public int MapCooldown { get; }
    public Dictionary<string, int> GroupCooldowns { get; }
}