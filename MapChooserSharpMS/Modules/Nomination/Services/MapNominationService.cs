using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using McsCancellableEvent = MapChooserSharpMS.Shared.Events.McsCancellableEvent;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class MapNominationService(
    IServiceProvider provider,
    TnmsPlugin plugin,
    IMcsPluginConfigProvider configProvider,
    IInternalEventManager eventManager,
    IMcsInternalNominationManager nominationManager,
    IMcsInternalNominationController nominationController,
    INominationValidateService nominationValidator,
    NominationConVars conVars,
    PlayerNominationCooldownService playerCooldownService
) : IMcsInternalMapNominationService
{

    public int NominationCountLimit => configProvider.PluginConfig.VoteConfig.MaxMenuElements;

    public IReadOnlyList<NominationCheckResult> TryNominateMap(IGameClient nominator, IMapConfig mapConfig)
    {
        var result = nominationValidator.PlayerCanNominateMap(nominator, mapConfig);

        if (result.Count != 0)
            return result;

        if (!nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nomination))
        {
            var newNom = new McsNominationData(mapConfig);
            nomination = newNom;

            if (!nominationManager.AddNomination(newNom))
                throw new InvalidOperationException("Failed to add nomination");
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

        if (previousNomination is McsNominationData prevData)
        {
            prevData.Participants.Remove(nominator.Slot);

            if (prevData.Participants.Count <= 0)
                TryRemoveNomination(previousNomination.MapConfig, nominator);
        }

        ((McsNominationData)nomination).Participants.Add(nominator.Slot);

        var nominationParam = ActivatorUtilities.CreateInstance<NominationParams>(provider, nominationController, nomination, nominator);
        eventManager.Fire<INominationEventListener>(evt => evt.OnNomination(nominationParam));

        if (previousNomination != null)
        {
            var changedParams = ActivatorUtilities.CreateInstance<NominationChangedParams>(provider, nominationController, nomination, nominator);
            eventManager.Fire<INominationEventListener>(evt => evt.OnNominationChanged(changedParams));
        }

        nominationController.BroadcastNomination(nominator, mapConfig, isNominationChanged: previousNomination != null);

        ApplyPlayerNominationCooldown(nominator.SteamId);

        nominationController.NotifyVoteSlotOverflow();

        return [];
    }

    public IReadOnlyList<NominationCheckResult> TryAdminNominateMap(IGameClient? nominator, IMapConfig mapConfig)
    {
        var checkResult = nominationValidator.CanAdminNominateMap(mapConfig, nominator);
        if (checkResult.Count != 0)
            return checkResult;

        // Post-validation: console path is guaranteed `existing == null`.
        // Player-admin path may have a non-admin existing nomination to upgrade.
        nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var existing);

        IMcsNominationData nomination;
        if (existing == null)
        {
            var newNom = new McsNominationData(mapConfig)
            {
                IsForceNominated = true
            };

            var adminNominationParam = new AdminNominationEventParams(
                plugin, (PluginModuleBase)nominationController, newNom, nominator);

            if (eventManager.FireCancellable<INominationEventListener>(evt => evt.OnAdminNomination(adminNominationParam)) != McsCancellableEvent.Continue)
                return [NominationCheckResult.CancelledByExternalPlugin];

            if (!nominationManager.AddNomination(newNom))
                throw new InvalidOperationException("Failed to add admin nomination");

            nomination = newNom;
        }
        else
        {
            var adminNominationParam = new AdminNominationEventParams(
                plugin, (PluginModuleBase)nominationController, existing, nominator);

            if (eventManager.FireCancellable<INominationEventListener>(evt => evt.OnAdminNomination(adminNominationParam)) != McsCancellableEvent.Continue)
                return [NominationCheckResult.CancelledByExternalPlugin];

            ((McsNominationData)existing).IsForceNominated = true;
            nomination = existing;
        }

        if (nominator != null)
            ((McsNominationData)nomination).Participants.Add(nominator.Slot);

        nominationController.BroadcastAdminNomination(nominator, mapConfig, changedExistingToAdmin: existing != null);

        nominationController.NotifyVoteSlotOverflow();

        return [];
    }

    public bool TryRemoveNomination(IMapConfig mapConfig, IGameClient? executor = null, bool forceRemoval = false)
    {
        if (!nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nomination))
            return false;

        if (nomination.IsForceNominated && !forceRemoval)
            return false;

        if (!nominationManager.RemoveNomination(nomination))
            return false;

        var nominationRemovedParam = new NominationRemovedEventParams(
            plugin,
            (PluginModuleBase)nominationController,
            nomination,
            forceRemoval,
            executor,
            executor);

        eventManager.Fire<INominationEventListener>(evt => evt.OnNominationRemoved(nominationRemovedParam));

        // forceRemoval only comes from the admin command / remove menu —
        // organic pruning (last participant left) stays silent.
        if (forceRemoval)
            nominationController.BroadcastNominationRemoved(executor, mapConfig);

        return true;
    }

    public bool TryUnNominate(IGameClient client, UnNominateReason reason = UnNominateReason.Normally)
        => TryUnNominateInternal(client.Slot, client, reason);

    public bool TryUnNominate(int slot, UnNominateReason reason = UnNominateReason.Normally)
        => TryUnNominateInternal(slot, client: null, reason);

    private bool TryUnNominateInternal(int slot, IGameClient? client, UnNominateReason reason)
    {
        McsNominationData? participating = null;
        foreach (var nomination in nominationManager.NominatedMaps.Values)
        {
            if (nomination.NominationParticipants.Contains(slot))
            {
                participating = nomination as McsNominationData;
                break;
            }
        }

        if (participating == null)
            return false;

        participating.Participants.Remove(slot);

        var unNominateParam = new UnNominateEventParams(
            plugin, (PluginModuleBase)nominationController, participating, slot, reason, client);

        eventManager.Fire<INominationEventListener>(evt => evt.OnUnNominate(unNominateParam));

        if (participating.Participants.Count == 0 && !participating.IsForceNominated)
            TryRemoveNomination(participating.MapConfig);

        return true;
    }

    public bool ClearNominations()
    {
        if (!nominationManager.NominatedMaps.Any())
            return false;

        nominationManager.ClearNominations();
        return true;
    }

    private void ApplyPlayerNominationCooldown(ulong steamId)
    {
        int count = conVars.PlayerCooldown.GetInt32();
        float timed = conVars.PlayerTimedCooldown.GetFloat();

        if (count <= 0 && timed <= 0f) return;

        playerCooldownService.SetCooldown(steamId, count, timed);
    }
}
