using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Modules.MapVote.Services;

internal class RandomMapPickingService(IServiceProvider provider, INominationValidateService nominationValidateService, IPluginConfigProvider pluginConfigProvider, IMcsMapConfigProvider mapConfigProvider)
{
    public List<IMapConfig> PickRandomMaps(int amount = -1)
    {
        if (amount == -1)
        {
            amount = pluginConfigProvider.PluginConfig.VoteConfig.MaxMenuElements;
        }

        var sorted = mapConfigProvider.GetMapConfigs().Values.OrderBy(_ => Random.Shared.Next()).ToList();

        var picked = new List<IMapConfig>();
        
        foreach (var mapConfig in sorted.AsEnumerable())
        {
            if (nominationValidateService.PlayerCanNominateMap())
        }
        
        throw new NotImplementedException("This feature is not fully implemented yet");
    }
}