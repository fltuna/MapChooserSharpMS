using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Nomination.Managers;

internal sealed class InternalNominationManager: IMcsInternalNominationManager
{
    private readonly Dictionary<string, IMcsNominationData> _internalNominations = new();
    
    public IReadOnlyDictionary<string, IMcsNominationData> NominatedMaps => _internalNominations;

    public bool ClearNominations()
    {
        if (!_internalNominations.Any())
            return false;
        
        _internalNominations.Clear();
        return true;
    }

    public bool AddNomination(IMcsNominationData nominationData)
    {
        return _internalNominations.TryAdd(nominationData.MapConfig.MapName, nominationData);
    }

    public bool RemoveNomination(IMcsNominationData nominationData)
    {
        return _internalNominations.Remove(nominationData.MapConfig.MapName);
    }
}