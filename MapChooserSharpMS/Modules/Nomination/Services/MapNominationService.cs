using System;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class MapNominationService(
    IServiceProvider provider,
    IPluginConfigProvider configProvider, 
    IInternalEventManager eventManager,
    IMcsInternalNominationManager nominationManager,
    IMcsNominationController nominationController,
    INominationValidateService nominationValidator
) : IMcsInternalMapNominationService
{
    
    public int NominationCountLimit => configProvider.PluginConfig.VoteConfig.MaxMenuElements;
    
    public NominationCheckResult TryNominateMap(IGameClient nominator, IMapConfig mapConfig)
    {
        var result = nominationValidator.PlayerCanNominateMap(nominator, mapConfig);
        
        if (result != NominationCheckResult.Success)
            return result;
        
        if (!nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nomination))
        {
            var newNom = new McsNominationData(mapConfig);
            
            nomination = newNom;
            var nominationParam = ActivatorUtilities.CreateInstance<NominationParams>(provider, nominationController, nomination, nominator);
            if (eventManager.FireCancellable<INominationEventListener>(evt => evt.OnNomination(nominationParam)))
                return NominationCheckResult.CancelledByExternalPlugin;
            
            if (!nominationManager.AddNomination(newNom))
                throw new InvalidOperationException("Failed to add nomination");
        }
        else
        {
            var nominationParam = ActivatorUtilities.CreateInstance<NominationParams>(provider, nominationController, nomination, nominator);
            if (eventManager.FireCancellable<INominationEventListener>(evt => evt.OnNomination(nominationParam)))
                return NominationCheckResult.CancelledByExternalPlugin;
        }

        

        IMcsNominationData? previousNomination = null;
        foreach (var nominatedMapsValue in nominationManager.NominatedMaps.Values)
        {
            if (nominatedMapsValue.NominationParticipants.Contains(nominator.Slot))
            {
                previousNomination = nominatedMapsValue;
                break;
            }
        }

        if (previousNomination != null)
        {
            previousNomination.NominationParticipants.Remove(nominator.Slot);

            if (previousNomination.NominationParticipants.Count <= 0)
                TryRemoveNomination(mapConfig, nominator);
        }
        
        nomination.NominationParticipants.Add(nominator.Slot);
        
        return NominationCheckResult.Success;
    }    
    
    public NominationCheckResult TryAdminNominateMap(IGameClient? nominator, IMapConfig mapConfig)
    {
        if (!nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nomination))
        {
            var newNom = new McsNominationData(mapConfig);

            if (!nominationManager.AddNomination(newNom))
                throw new InvalidOperationException("Failed to add nomination as a admin");
            
            nomination = newNom;
        }
        
        if (nomination.IsForceNominated)
            return NominationCheckResult.NominatedByAdmin;
        
        nomination.IsForceNominated = true;


        var nominationParam = ActivatorUtilities.CreateInstance<AdminNominationEventParams>(provider, nominationController, nomination);

        if (eventManager.FireCancellable<INominationEventListener>(evt => evt.OnAdminNomination(nominationParam)))
            return NominationCheckResult.CancelledByExternalPlugin;
        
        return NominationCheckResult.Success;
    }

    public bool TryRemoveNomination(IMapConfig mapConfig, IGameClient? executor = null, bool forceRemoval = false)
    {
        if (!nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nomination))
            return false;
        
        if (nomination.IsForceNominated && !forceRemoval)
            return false;

        if (!nominationManager.RemoveNomination(nomination))
            return false;
        
        // TODO()
        var nominationRemovedParam = ActivatorUtilities.CreateInstance<NominationRemovedEventParams>(provider, nominationController, nomination);
        eventManager.Fire<INominationEventListener>(evt => evt.OnNominationRemoved(nominationRemovedParam));

        return true;
    }

    public bool ClearNominations()
    {
        if (!nominationManager.NominatedMaps.Any())
            return false;

        nominationManager.ClearNominations();
        return true;
    }
}