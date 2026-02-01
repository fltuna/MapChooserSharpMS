using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class NominationValidateService
    : PluginBasicFeatureBase, INominationValidateService
{
    public readonly IConVar PerGroupNominationLimit;
    
    private readonly IMcsInternalNominationManager _nominationManager;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsInternalNominationController _nominationController;
    private readonly IMapTransitionManager _mapTransitionManager;

    public NominationValidateService(IServiceProvider serviceProvider, IMcsInternalNominationManager nominationManager, IInternalEventManager internalEventManager, IMcsInternalNominationController nominationController, IMapTransitionManager mapTransitionManager):base(serviceProvider)
    {
        var conv = SharedSystem.GetConVarManager().CreateConVar("", 0, 0, 999, "Help", ConVarFlags.None);
        
        if (conv == null)
            throw new InvalidOperationException($"Failed to initialize ConVar in {GetType().Name}");

        PerGroupNominationLimit = conv;
        _nominationManager  = nominationManager;
        _eventManager = internalEventManager;
        _nominationController = nominationController;
        _mapTransitionManager = mapTransitionManager;
        
    }
    
    public NominationCheckResult PlayerCanNominateMap(IGameClient client, IMapConfig mapConfig)
    {
        var result = NominationCheckResult.None;

        if (IsMapDisabled(mapConfig))
            result |= NominationCheckResult.Disabled;

        if (IsCurrentMap(mapConfig))
            result |= NominationCheckResult.SameMap;

        result |= GetNominationState(mapConfig, client);

        if (HasReachedGroupNominationLimit(mapConfig))
            result |= NominationCheckResult.GroupNominationLimitReached;

        if (IsDuringVotingPeriod())
            result |= NominationCheckResult.VotingPeriod;

        if (IsMapInCooldown(mapConfig))
            result |= NominationCheckResult.MapIsInCooldown;

        SteamID steamId = client.SteamId;

        // Bypasses admin check - if allowed by SteamId, skip permission/restriction checks
        bool bypassedByAllowList = IsAllowedBySteamId(mapConfig, steamId);

        if (!bypassedByAllowList)
        {
            if (IsRestrictedToCertainUser(mapConfig))
                result |= NominationCheckResult.RestrictedToCertainUser;

            // Bypasses admin check too
            if (IsDisallowedBySteamId(mapConfig, steamId))
                result |= NominationCheckResult.BlockedBySteamId;

            if (!IsPlayerHasRequiredPermission(mapConfig, client))
                result |= NominationCheckResult.NotEnoughPermissions;
        }

        if (!IsLowerThanMaxPlayers(mapConfig))
            result |= NominationCheckResult.TooMuchPlayers;

        if (!IsGreaterThanMinPlayers(mapConfig))
            result |= NominationCheckResult.NotEnoughPlayers;

        if (!IsWithinAllowedDays(mapConfig))
            result |= NominationCheckResult.OnlySpecificDay;

        if (!IsWithinTimeRange(mapConfig))
            result |= NominationCheckResult.OnlySpecificTime;

        // Only fire event if all other checks passed
        if (result == NominationCheckResult.None)
        {
            var nominationEvent = ActivatorUtilities.CreateInstance<NominationCheckPassedEventParams>(ServiceProvider, _nominationController);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)))
            {
                result |= NominationCheckResult.CancelledByExternalPlugin;
            }
        }

        return result;
    }

    public NominationCheckResult CanPickupMap(IMapConfig mapConfig)
    {
        var result = NominationCheckResult.None;

        if (IsMapDisabled(mapConfig))
            result |= NominationCheckResult.Disabled;

        if (IsCurrentMap(mapConfig))
            result |= NominationCheckResult.SameMap;

        result |= GetNominationState(mapConfig);

        if (HasReachedGroupNominationLimit(mapConfig))
            result |= NominationCheckResult.GroupNominationLimitReached;

        if (IsMapInCooldown(mapConfig))
            result |= NominationCheckResult.MapIsInCooldown;

        if (IsRestrictedToCertainUser(mapConfig))
            result |= NominationCheckResult.RestrictedToCertainUser;

        if (IsRequiresAnyPermission(mapConfig))
            result |= NominationCheckResult.NotEnoughPermissions;

        if (!IsLowerThanMaxPlayers(mapConfig))
            result |= NominationCheckResult.TooMuchPlayers;

        if (!IsGreaterThanMinPlayers(mapConfig))
            result |= NominationCheckResult.NotEnoughPlayers;

        if (!IsWithinAllowedDays(mapConfig))
            result |= NominationCheckResult.OnlySpecificDay;

        if (!IsWithinTimeRange(mapConfig))
            result |= NominationCheckResult.OnlySpecificTime;

        // Only fire event if all other checks passed
        if (result == NominationCheckResult.None)
        {
            var nominationEvent = ActivatorUtilities.CreateInstance<NominationCheckPassedEventParams>(ServiceProvider, _nominationController);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)))
            {
                result |= NominationCheckResult.CancelledByExternalPlugin;
            }
        }

        return result;
    }

    public bool IsDuringVotingPeriod()
    {
        // TODO() Will implement once after Vote controller implemented.
        throw new NotImplementedException();
    }

    public bool IsMapDisabled(IMapConfig mapConfig)
    {
        return mapConfig.IsDisabled;
    }

    public bool IsCurrentMap(IMapConfig mapConfig)
    {
        if (_mapTransitionManager.CurrentMap is null)
            return false;

        return _mapTransitionManager.CurrentMap.MapName.Equals(mapConfig.MapName);
    }

    public bool IsWithinTimeRange(IMapConfig mapConfig)
    {
        return !mapConfig.NominationConfig.AllowedTimeRanges.Any() ||
               mapConfig.NominationConfig.AllowedTimeRanges.Count(range => range.IsInRange(TimeOnly.FromDateTime(DateTime.Now))) >
               0;
    }

    public bool IsWithinAllowedDays(IMapConfig mapConfig)
    {
        return !mapConfig.NominationConfig.DaysAllowed.Any() ||
               mapConfig.NominationConfig.DaysAllowed.Contains(DateTime.Today.DayOfWeek);
    }

    public bool IsGreaterThanMinPlayers(IMapConfig mapConfig, bool includeBots = false)
    {
        return mapConfig.NominationConfig.MinPlayers <= 0 ||
               mapConfig.NominationConfig.MinPlayers < SharedSystem.GetModSharp().GetIServer().GetGameClients()
                   .Count(u => u.IsFakeClient == includeBots && !u.IsHltv);
    }

    public bool IsLowerThanMaxPlayers(IMapConfig mapConfig, bool includeBots = false)
    {
        return mapConfig.NominationConfig.MaxPlayers <= 0 ||
               mapConfig.NominationConfig.MaxPlayers > SharedSystem.GetModSharp().GetIServer().GetGameClients()
                   .Count(u => u.IsFakeClient == includeBots && !u.IsHltv);
    }

    public bool IsPlayerHasRequiredPermission(IMapConfig mapConfig, IGameClient? client)
    {
        if (!mapConfig.NominationConfig.RequiredPermissions.Any())
            return true;
        
        foreach (var perm in mapConfig.NominationConfig.RequiredPermissions.ToArray())
        {
            if (TnmsPlugin.AdminManager.ClientHasPermission(client, perm))
                return true;
        }
        return false;
    }

    public bool IsRequiresAnyPermission(IMapConfig mapConfig)
    {
        return mapConfig.NominationConfig.RequiredPermissions.Any();
    }

    public bool IsPlayerHasRequiredPermission(IMapConfig mapConfig, SteamID steamId)
    {
        throw new NotImplementedException();
    }

    public bool IsDisallowedBySteamId(IMapConfig mapConfig, SteamID steamId)
    {
        return mapConfig.NominationConfig.DisallowedSteamIds.Contains(steamId.AccountId);
    }

    public bool IsAllowedBySteamId(IMapConfig mapConfig, SteamID steamId)
    {
        return mapConfig.NominationConfig.AllowedSteamIds.Contains(steamId.AccountId);
    }

    public bool IsRestrictedToCertainUser(IMapConfig mapConfig)
    {
        return mapConfig.NominationConfig.RestrictToAllowedUsersOnly;
    }

    public bool IsMapInCooldown(IMapConfig mapConfig)
    {
        return GetCooldownInformations(mapConfig).HasCooldown;
    }


    public NominationCheckResult GetNominationState(IMapConfig mapConfig, IGameClient? client = null)
    {
        if (!_nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nominated))
            return NominationCheckResult.None;

        if (nominated.IsForceNominated)
            return NominationCheckResult.NominatedByAdmin;

        if (client != null)
        {
            if (nominated.NominationParticipants.Contains(client.Slot))
                return NominationCheckResult.AlreadyNominated;

            return NominationCheckResult.None;
        }

        // For CanPickupMap (client is null), map is already nominated
        return NominationCheckResult.AlreadyNominated;
    }

    public IDetailedCooldownResult GetCooldownInformations(IMapConfig mapConfig)
    {
        int curMapCooldown = mapConfig.CooldownConfig.CurrentCooldown;
        var curMapTimedCooldown = mapConfig.CooldownConfig.LastPlayedAt + mapConfig.CooldownConfig.TimedCooldown;
        Dictionary<string, int> groupCooldown = new Dictionary<string, int>();
        Dictionary<string, DateTime> groupTimedCooldown = new ();
        
        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            groupCooldown[groupSetting.GroupName] = groupSetting.CooldownConfig.CurrentCooldown;
            groupTimedCooldown[groupSetting.GroupName] = groupSetting.CooldownConfig.LastPlayedAt + groupSetting.CooldownConfig.TimedCooldown;
        }
        
        return new DetailedCooldownResult(mapConfig, curMapCooldown, groupCooldown, curMapTimedCooldown, groupTimedCooldown);
    }

    public bool HasReachedGroupNominationLimit(IMapConfig mapConfig)
    {
        if (PerGroupNominationLimit.GetInt16() == 0)
            return false;

        Dictionary<string, int> groupsNominatedCount = new(StringComparer.OrdinalIgnoreCase);
        foreach (var data in _nominationManager.NominatedMaps)
        {
            foreach (IMapGroupConfig groupSetting in data.Value.MapConfig.GroupSettings)
            {
                if (!groupsNominatedCount.TryGetValue(groupSetting.GroupName, out int count))
                {
                    groupsNominatedCount.Add(groupSetting.GroupName, 0);
                }
                
                groupsNominatedCount[groupSetting.GroupName]++;
            }
        }
        
        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (groupsNominatedCount[groupSetting.GroupName] >= PerGroupNominationLimit.GetInt16())
                return true;
        }
        
        return false;
    }
}