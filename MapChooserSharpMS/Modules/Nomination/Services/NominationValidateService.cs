using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using McsCancellableEvent = MapChooserSharpMS.Shared.Events.McsCancellableEvent;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class NominationValidateService
    : PluginBasicFeatureBase, INominationValidateService
{
    private readonly IMcsInternalNominationManager _nominationManager;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsInternalNominationController _nominationController;
    private readonly IMapTransitionManager _mapTransitionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly PlayerNominationCooldownService _playerCooldownService;
    private readonly IMcsCooldownStore _cooldownStore;
    private readonly NominationConVars _conVars;

    public NominationValidateService(IServiceProvider serviceProvider, IMcsInternalNominationManager nominationManager, IInternalEventManager internalEventManager, IMcsInternalNominationController nominationController, IMapTransitionManager mapTransitionManager, PlayerNominationCooldownService playerCooldownService, IMcsCooldownStore cooldownStore, NominationConVars conVars):base(serviceProvider)
    {
        _nominationManager  = nominationManager;
        _eventManager = internalEventManager;
        _nominationController = nominationController;
        _mapTransitionManager = mapTransitionManager;
        _serviceProvider = serviceProvider;
        _playerCooldownService = playerCooldownService;
        _cooldownStore = cooldownStore;
        _conVars = conVars;
    }

    public IReadOnlyList<NominationCheckResult> PlayerCanNominateMap(IGameClient client, IMapConfig mapConfig)
    {
        var result = new List<NominationCheckResult>();

        // Phase 1: Bypass — allow permission skips everything.
        if (HasBypassPermission(mapConfig, client))
            return result;

        // Phase 2: Always checked — cannot be bypassed.
        if (IsPlayerInNominationCooldown(client.SteamId))
            result.Add(NominationCheckResult.PlayerCooldownActive);

        if (IsMapDisabled(mapConfig))
            result.Add(NominationCheckResult.Disabled);

        if (IsMapInCooldown(mapConfig))
            result.Add(NominationCheckResult.MapIsInCooldown);

        if (IsMapInNominationCooldown(mapConfig))
            result.Add(NominationCheckResult.NominationCooldownActive);

        if (SharesCooldownGroupWithCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMapGroup);

        if (result.Count > 0)
            return result;

        // Phase 3: Deny permission check.
        if (IsPlayerDeniedByPermission(mapConfig, client))
        {
            result.Add(NominationCheckResult.NotEnoughPermissions);
            return result;
        }

        // Phase 4: Allow restriction — if config requires allow permission, check it.
        if (mapConfig.NominationConfig.RestrictToAllowedUsersOnly
            && !IsPlayerAllowedByPermission(mapConfig, client))
        {
            result.Add(NominationCheckResult.NotEnoughPermissions);
            return result;
        }

        // Phase 5: Normal checks.
        if (IsCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMap);

        result.AddRange(GetNominationState(mapConfig, client));

        if (HasReachedGroupNominationLimit(mapConfig))
            result.Add(NominationCheckResult.GroupNominationLimitReached);

        if (IsDuringVotingPeriod())
            result.Add(NominationCheckResult.VotingPeriod);

        if (!IsLowerThanMaxPlayers(mapConfig))
            result.Add(NominationCheckResult.TooMuchPlayers);

        if (!IsGreaterThanMinPlayers(mapConfig))
            result.Add(NominationCheckResult.NotEnoughPlayers);

        if (!IsWithinAllowedDays(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificDay);

        if (!IsWithinTimeRange(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificTime);

        if (result.Count == 0)
        {
            var nominationEvent = CreateCheckPassedEvent(mapConfig, client);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)) != McsCancellableEvent.Continue)
            {
                result.Add(NominationCheckResult.CancelledByExternalPlugin);
            }
        }

        return result;
    }

    public IReadOnlyList<NominationCheckResult> CanAdminNominateMap(IMapConfig mapConfig, IGameClient? nominator)
    {
        var result = new List<NominationCheckResult>();
        bool isConsole = nominator == null;

        if (IsCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMap);

        if (_nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var existing))
        {
            if (isConsole)
                result.Add(NominationCheckResult.AlreadyNominated);
            else if (existing.IsForceNominated)
                result.Add(NominationCheckResult.NominatedByAdmin);
        }

        if (!isConsole)
        {
            if (mapConfig.NominationConfig.ProhibitAdminNomination)
                result.Add(NominationCheckResult.ProhibitAdminNomination);
        }

        if (result.Count == 0)
        {
            var nominationEvent = CreateCheckPassedEvent(mapConfig, nominator, enforcedByAdmin: true);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)) != McsCancellableEvent.Continue)
            {
                result.Add(NominationCheckResult.CancelledByExternalPlugin);
            }
        }

        return result;
    }

    public IReadOnlyList<NominationCheckResult> CanPickupMap(IMapConfig mapConfig)
    {
        var result = new List<NominationCheckResult>();

        if (IsMapDisabled(mapConfig))
            result.Add(NominationCheckResult.Disabled);

        if (IsCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMap);

        result.AddRange(GetNominationState(mapConfig));

        if (HasReachedGroupNominationLimit(mapConfig))
            result.Add(NominationCheckResult.GroupNominationLimitReached);

        if (IsMapInCooldown(mapConfig))
            result.Add(NominationCheckResult.MapIsInCooldown);

        if (SharesCooldownGroupWithCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMapGroup);

        if (!IsLowerThanMaxPlayers(mapConfig))
            result.Add(NominationCheckResult.TooMuchPlayers);

        if (!IsGreaterThanMinPlayers(mapConfig))
            result.Add(NominationCheckResult.NotEnoughPlayers);

        if (!IsWithinAllowedDays(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificDay);

        if (!IsWithinTimeRange(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificTime);

        // Only fire event if all other checks passed
        if (result.Count == 0)
        {
            var nominationEvent = CreateCheckPassedEvent(mapConfig, client: null);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)) != McsCancellableEvent.Continue)
            {
                result.Add(NominationCheckResult.CancelledByExternalPlugin);
            }
        }

        return result;
    }

    /// <summary>
    /// Phase 2/game-thread step of the deferred random-pick pipeline: captures
    /// everything <see cref="CanPickupPure"/> needs from live, non-thread-safe
    /// state (current map, player count, nomination manager) into an immutable
    /// snapshot so the pure filter can safely run on the thread pool afterwards.
    /// </summary>
    public PickupSnapshot CreatePickupSnapshot()
    {
        var nominatedMapNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groupNominatedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var data in _nominationManager.NominatedMaps)
        {
            nominatedMapNames.Add(data.Key);

            foreach (IMapGroupConfig groupSetting in data.Value.MapConfig.GroupSettings)
            {
                groupNominatedCounts.TryGetValue(groupSetting.GroupName, out int count);
                groupNominatedCounts[groupSetting.GroupName] = count + 1;
            }
        }

        var mapsInCooldown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mapConfigProvider = _serviceProvider.GetRequiredService<IMcsMapConfigProvider>();
        foreach (var mapName in mapConfigProvider.GetMapConfigs().Keys)
        {
            if (mapConfigProvider.TryGetMapConfig(mapName, out var resolved) && IsMapInCooldown(resolved))
                mapsInCooldown.Add(resolved.MapName);
        }

        var currentMapCooldownGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ResolveCurrentMapConfig() is { } currentConfig)
        {
            foreach (IMapGroupConfig group in currentConfig.GroupSettings)
            {
                if (HasCooldownConfigured(group))
                    currentMapCooldownGroups.Add(group.GroupName);
            }
        }

        return new PickupSnapshot(
            CurrentMapName: _mapTransitionManager.CurrentMap?.MapConfig.MapName ?? SharedSystem.GetModSharp().GetMapName(),
            RealPlayerCount: SharedSystem.GetModSharp().GetIServer().GetGameClients(true).Count(u => !u.IsFakeClient && !u.IsHltv),
            NominatedMapNames: nominatedMapNames,
            GroupNominatedCounts: groupNominatedCounts,
            MapsInCooldown: mapsInCooldown,
            CurrentMapCooldownGroups: currentMapCooldownGroups
        );
    }

    /// <summary>
    /// Phase 3/thread-pool step: pure filter over <paramref name="maps"/> using
    /// only <paramref name="snapshot"/> data — no event firing, no game API access,
    /// safe to call off the game thread.
    /// </summary>
    public List<IMapConfig> FilterPickableMapsPure(List<IMapConfig> maps, PickupSnapshot snapshot)
        => maps.Where(m => CanPickupPure(m, snapshot)).ToList();

    /// <summary>
    /// Phase 4/game-thread step: fires <see cref="INominationEventListener.OnNominationCheckPassed"/>
    /// per map. Must be called on the game thread.
    /// </summary>
    public List<IMapConfig> FilterByNominationCheckEvent(List<IMapConfig> maps)
    {
        return maps.Where(m =>
        {
            var nominationEvent = CreateCheckPassedEvent(m, client: null);
            return _eventManager.FireCancellable<INominationEventListener>(evt => evt.OnNominationCheckPassed(nominationEvent)) == McsCancellableEvent.Continue;
        }).ToList();
    }

    private NominationCheckPassedEventParams CreateCheckPassedEvent(IMapConfig mapConfig, IGameClient? client, bool enforcedByAdmin = false)
    {
        return new NominationCheckPassedEventParams(Plugin, (PluginModuleBase)_nominationController, mapConfig, client, enforcedByAdmin);
    }

    private bool CanPickupPure(IMapConfig mapConfig, PickupSnapshot snapshot)
    {
        if (mapConfig.IsDisabled)
            return false;

        if (!mapConfig.RandomPickConfig.IsPickable)
            return false;

        if (snapshot.CurrentMapName is not null
            && mapConfig.MapName.Equals(snapshot.CurrentMapName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (snapshot.NominatedMapNames.Contains(mapConfig.MapName))
            return false;

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (groupSetting.NominationLimit > 0
                && snapshot.GroupNominatedCounts.TryGetValue(groupSetting.GroupName, out int nominatedCount)
                && nominatedCount >= groupSetting.NominationLimit)
                return false;
        }

        if (snapshot.MapsInCooldown.Contains(mapConfig.MapName))
            return false;

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (snapshot.CurrentMapCooldownGroups.Contains(groupSetting.GroupName))
                return false;
        }

        if (mapConfig.NominationConfig.MaxPlayers > 0
            && mapConfig.NominationConfig.MaxPlayers < snapshot.RealPlayerCount)
            return false;

        if (mapConfig.NominationConfig.MinPlayers > 0
            && mapConfig.NominationConfig.MinPlayers > snapshot.RealPlayerCount)
            return false;

        if (!IsWithinAllowedDays(mapConfig))
            return false;

        if (!IsWithinTimeRange(mapConfig))
            return false;

        return true;
    }

    /// <summary>
    /// Point-in-time copy of the live state <see cref="CanPickupPure"/> needs,
    /// taken on the game thread so the pure filter is safe to run off it.
    /// </summary>
    internal sealed record PickupSnapshot(
        string? CurrentMapName,
        int RealPlayerCount,
        IReadOnlySet<string> NominatedMapNames,
        IReadOnlyDictionary<string, int> GroupNominatedCounts,
        IReadOnlySet<string> MapsInCooldown,
        IReadOnlySet<string> CurrentMapCooldownGroups);

    public bool IsDuringVotingPeriod()
    {
        // IMcsReadOnlyVoteState is the combined view exposed by MapVote — it
        // reports true when *any* vote (main or extend) is in progress, so
        // nomination is blocked in either case. No code change needed here
        // when MapCycle later wires its extend-vote state.
        var voteState = _serviceProvider.GetRequiredService<IMcsReadOnlyVoteState>();
        if (voteState.IsVotingPeriod())
            return true;

        // Pre-vote countdown (candidates are built when it finishes, so late
        // nominations still make the ballot). Open by default; servers can
        // close nominations at countdown start via the ConVar.
        return _conVars.AllowDuringVoteCountdown.GetInt32() == 0
               && voteState.CurrentVoteState
                   is McsMapVoteState.Initializing
                   or McsMapVoteState.InitializeAccepted;
    }

    public bool IsMapDisabled(IMapConfig mapConfig)
    {
        return mapConfig.IsDisabled;
    }

    public bool IsCurrentMap(IMapConfig mapConfig)
    {
        if (_mapTransitionManager.CurrentMap is not null)
            return _mapTransitionManager.CurrentMap.MapConfig.MapName.Equals(mapConfig.MapName, StringComparison.OrdinalIgnoreCase);

        var liveMapName = SharedSystem.GetModSharp().GetMapName();
        if (liveMapName is null)
            return false;

        return mapConfig.MapName.Equals(liveMapName, StringComparison.OrdinalIgnoreCase);
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

    public bool IsGreaterThanMinPlayers(IMapConfig mapConfig)
    {
        return mapConfig.NominationConfig.MinPlayers <= 0 ||
               mapConfig.NominationConfig.MinPlayers <= SharedSystem.GetModSharp().GetIServer().GetGameClients(true)
                   .Count(u => !u.IsFakeClient && !u.IsHltv);
    }

    public bool IsLowerThanMaxPlayers(IMapConfig mapConfig)
    {
        return mapConfig.NominationConfig.MaxPlayers <= 0 ||
               mapConfig.NominationConfig.MaxPlayers >= SharedSystem.GetModSharp().GetIServer().GetGameClients(true)
                   .Count(u => !u.IsFakeClient && !u.IsHltv);
    }

    public bool IsMapInCooldown(IMapConfig mapConfig)
    {
        return GetCooldownInformations(mapConfig).HasCooldown;
    }

    public bool SharesCooldownGroupWithCurrentMap(IMapConfig mapConfig)
    {
        var currentConfig = ResolveCurrentMapConfig();
        if (currentConfig is null)
            return false;

        // The current map itself is IsCurrentMap's responsibility — reporting
        // it as a group conflict would mask the more precise SameMap result.
        if (currentConfig.MapName.Equals(mapConfig.MapName, StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (IMapGroupConfig group in mapConfig.GroupSettings)
        {
            if (!HasCooldownConfigured(group))
                continue;

            foreach (IMapGroupConfig currentGroup in currentConfig.GroupSettings)
            {
                if (group.GroupName.Equals(currentGroup.GroupName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    // Group cooldowns are written to the store only at map end, so while a map
    // is being played its groups look cooldown-free. Conflict detection must
    // therefore look at the configured values, not the runtime store.
    private static bool HasCooldownConfigured(IMapGroupConfig group)
    {
        return group.CooldownSettings.ConfigCooldown > 0
               || group.CooldownSettings.TimedCooldown > TimeSpan.Zero;
    }

    private IMapConfig? ResolveCurrentMapConfig()
    {
        if (_mapTransitionManager.CurrentMap is not null)
            return _mapTransitionManager.CurrentMap.MapConfig;

        var liveMapName = SharedSystem.GetModSharp().GetMapName();
        if (liveMapName is null)
            return null;

        var mapConfigProvider = _serviceProvider.GetRequiredService<IMcsMapConfigProvider>();
        return mapConfigProvider.TryGetMapConfig(liveMapName, out var found) ? found : null;
    }

    public bool IsPlayerInNominationCooldown(ulong steamId)
    {
        return _playerCooldownService.IsInCooldown(steamId);
    }

    public Shared.Nomination.IPlayerNominationCooldownState? GetPlayerCooldownState(ulong steamId)
    {
        return _playerCooldownService.GetState(steamId);
    }

    public bool IsMapInNominationCooldown(IMapConfig mapConfig)
    {
        return _cooldownStore.GetEffectiveMapState(mapConfig.MapName).IsNominationCooldownActive;
    }

    public IReadOnlyList<NominationCheckResult> GetNominationState(IMapConfig mapConfig, IGameClient? client = null)
    {
        if (!_nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nominated))
            return [];

        if (nominated.IsForceNominated)
            return [NominationCheckResult.NominatedByAdmin];

        if (client != null)
        {
            if (nominated.NominationParticipants.Contains(client.Slot))
                return [NominationCheckResult.AlreadyNominated];

            return [];
        }

        // For CanPickupMap (client is null), map is already nominated
        return [NominationCheckResult.AlreadyNominated];
    }

    public IDetailedCooldownResult GetCooldownInformations(IMapConfig mapConfig)
    {
        var mapState = _cooldownStore.GetEffectiveMapState(mapConfig.MapName);
        Dictionary<string, int> groupCooldown = new Dictionary<string, int>();
        Dictionary<string, DateTime> groupTimedCooldown = new ();

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            var groupState = _cooldownStore.GetEffectiveGroupState(groupSetting.GroupName);
            groupCooldown[groupSetting.GroupName] = groupState.CurrentCooldown;
            groupTimedCooldown[groupSetting.GroupName] = groupState.TimedCooldownEndUtc;
        }

        return new DetailedCooldownResult(mapConfig, mapState.CurrentCooldown, groupCooldown, mapState.TimedCooldownEndUtc, groupTimedCooldown);
    }

    public bool HasBypassPermission(IMapConfig mapConfig, IGameClient client)
    {
        if (TnmsPlugin.AdminManager.PlayerHasPermissionExact(client.SteamId, $"mcs.nominate.map.bypass.{mapConfig.MapName}"))
            return true;

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (TnmsPlugin.AdminManager.PlayerHasPermissionExact(client.SteamId, $"mcs.nominate.group.bypass.{groupSetting.GroupName}"))
                return true;
        }

        return false;
    }

    public bool IsPlayerAllowedByPermission(IMapConfig mapConfig, IGameClient client)
    {
        if (TnmsPlugin.AdminManager.PlayerHasPermission(client.SteamId, $"mcs.nominate.map.allow.{mapConfig.MapName}"))
            return true;

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (TnmsPlugin.AdminManager.PlayerHasPermission(client.SteamId, $"mcs.nominate.group.allow.{groupSetting.GroupName}"))
                return true;
        }

        return false;
    }

    public bool IsPlayerDeniedByPermission(IMapConfig mapConfig, IGameClient client)
    {
        if (TnmsPlugin.AdminManager.PlayerHasPermissionExact(client.SteamId, $"mcs.nominate.map.deny.{mapConfig.MapName}"))
            return true;

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (TnmsPlugin.AdminManager.PlayerHasPermissionExact(client.SteamId, $"mcs.nominate.group.deny.{groupSetting.GroupName}"))
                return true;
        }

        return false;
    }

    public bool HasReachedGroupNominationLimit(IMapConfig mapConfig)
    {
        bool anyGroupHasLimit = false;
        foreach (IMapGroupConfig g in mapConfig.GroupSettings)
        {
            if (g.NominationLimit > 0)
            {
                anyGroupHasLimit = true;
                break;
            }
        }

        if (!anyGroupHasLimit)
            return false;

        Dictionary<string, int> groupsNominatedCount = new(StringComparer.OrdinalIgnoreCase);
        foreach (var data in _nominationManager.NominatedMaps)
        {
            foreach (IMapGroupConfig groupSetting in data.Value.MapConfig.GroupSettings)
            {
                if (!groupsNominatedCount.TryGetValue(groupSetting.GroupName, out _))
                    groupsNominatedCount[groupSetting.GroupName] = 0;

                groupsNominatedCount[groupSetting.GroupName]++;
            }
        }

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (groupSetting.NominationLimit > 0
                && groupsNominatedCount.TryGetValue(groupSetting.GroupName, out int count)
                && count >= groupSetting.NominationLimit)
                return true;
        }

        return false;
    }
}
