using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapVote;
using MapChooserSharpMS.Shared.Events;
using McsCancellableEvent = MapChooserSharpMS.Shared.Events.McsCancellableEvent;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Handlers;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Managers;
using MapChooserSharpMS.Modules.MapVote.Models;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Services;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapVote.Services;

internal sealed class MapVoteControllingService : IMapVoteControllingService
{
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly ILogger _logger;
    private readonly VoteControllingManager _voteManager;
    private readonly IMcsInternalMainVoteState _voteState;
    private readonly IInternalEventManager _eventManager;
    private readonly INativeVoteManager _nativeVoteManager;
    private readonly MapVoteConVars _conVars;
    private readonly IMcsPluginConfigProvider _configProvider;
    private readonly RandomMapPickingService _randomMapPicker;
    private readonly INominationManager _nominationManager;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly IMcsInternalMapExtendService _mapExtendService;
    private readonly McsMapCooldownLifecycleService _cooldownLifecycleService;
    private readonly McsMapVoteSoundPlayer _soundPlayer;
    private readonly Ui.Countdown.McsCountdownUiController _countdownUi;
    private readonly Wuling.Abstract.Tianshi.Menu.IMenu _wulingMenu;
    private readonly Wuling.Abstract.Tianshi.Registry.IRegistry _wulingRegistry;

    private Guid _countdownTimerId = Guid.Empty;
    private Guid _voteEndTimerId = Guid.Empty;
    private float _voteDuration;
    private DateTime _voteStartTime;

    internal Func<float>? CustomWinnerThresholdProvider { get; set; }

    internal MapVoteControllingService(
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        ILogger logger,
        VoteControllingManager voteManager,
        IMcsInternalMainVoteState voteState,
        IInternalEventManager eventManager,
        INativeVoteManager nativeVoteManager,
        MapVoteConVars conVars,
        IMcsPluginConfigProvider configProvider,
        RandomMapPickingService randomMapPicker,
        INominationManager nominationManager,
        IMcsMapConfigProvider mapConfigProvider,
        IMcsInternalMapExtendService mapExtendService,
        McsMapCooldownLifecycleService cooldownLifecycleService,
        McsMapVoteSoundPlayer soundPlayer,
        Ui.Countdown.McsCountdownUiController countdownUi,
        Wuling.Abstract.Tianshi.Menu.IMenu wulingMenu,
        Wuling.Abstract.Tianshi.Registry.IRegistry wulingRegistry)
    {
        _plugin = plugin;
        _moduleBase = moduleBase;
        _logger = logger;
        _voteManager = voteManager;
        _voteState = voteState;
        _eventManager = eventManager;
        _nativeVoteManager = nativeVoteManager;
        _conVars = conVars;
        _configProvider = configProvider;
        _randomMapPicker = randomMapPicker;
        _nominationManager = nominationManager;
        _mapConfigProvider = mapConfigProvider;
        _mapExtendService = mapExtendService;
        _cooldownLifecycleService = cooldownLifecycleService;
        _soundPlayer = soundPlayer;
        _countdownUi = countdownUi;
        _wulingMenu = wulingMenu;
        _wulingRegistry = wulingRegistry;
    }

    public McsMapVoteState InitiateVote(bool isActivatedByRtv = false)
    {
        if (_voteManager.CurrentSession is not null)
            return _voteManager.CurrentSession.CurrentState;

        var currentState = _voteState as IMcsReadOnlyVoteState;
        if (currentState?.IsVotingPeriod() == true)
            return currentState.CurrentVoteState ?? McsMapVoteState.NoActiveVote;

        // The next map is already decided — never start another vote on top of it.
        // (RTV success in this state transitions to the next map instead; see RtvService.)
        if (currentState?.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
            return McsMapVoteState.NextMapConfirmed;

        var session = _voteManager.CreateSession(isActivatedByRtv);
        session.CurrentState = McsMapVoteState.Initializing;
        _voteState.SetState(McsMapVoteState.Initializing);

        session.CurrentState = McsMapVoteState.InitializeAccepted;
        _voteState.SetState(McsMapVoteState.InitializeAccepted);

        int countdownSeconds = _conVars.VoteCountdownTime.GetInt32();
        if (countdownSeconds > 0)
        {
            _soundPlayer.SetRunoff(false);
            _soundPlayer.PlayVoteCountdownStartSoundToAll();
            StartPreVoteCountdown(session, countdownSeconds);
        }
        else
        {
            OnCountdownFinished(session);
        }

        return session.CurrentState;
    }

    public McsMapVoteState CancelVote(IGameClient? client)
    {
        var session = _voteManager.CurrentSession;
        if (session is null)
            return McsMapVoteState.NoActiveVote;

        StopCountdownTimer();

        session.CurrentState = McsMapVoteState.Cancelling;
        _voteState.SetState(McsMapVoteState.Cancelling);

        // Clear session BEFORE calling NVM — NVM's CancelVote synchronously invokes
        // OnVoteCancelled on the handler, and the stale guard needs to see the session
        // as already cleared to prevent HandleExternalCancel from double-firing.
        _voteState.Reset();
        _voteManager.ClearSession();

        _nativeVoteManager.CancelVote();

        var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, client, SnapshotNominations());
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

        return McsMapVoteState.Cancelling;
    }

    public bool ForceResetVote()
    {
        var session = _voteManager.CurrentSession;
        if (session is null)
            return false;

        StopCountdownTimer();
        _voteState.Reset();
        _voteManager.ClearSession();

        _nativeVoteManager.CancelVote();

        var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null, SnapshotNominations());
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

        return true;
    }

    internal void HandleExternalCancel(MapVoteInformation session)
    {
        StopCountdownTimer();
        var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null, SnapshotNominations());
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

        session.CurrentState = McsMapVoteState.NoActiveVote;
        _voteState.Reset();
        _voteManager.ClearSession();
    }

    internal void StartRunoffVote(MapVoteInformation originalSession, List<MapVoteOption> runoffCandidates)
    {
        if (runoffCandidates.Count < 2)
        {
            HandleWinner(originalSession, runoffCandidates.FirstOrDefault());
            return;
        }

        originalSession.CurrentState = McsMapVoteState.RunoffVoting;
        _voteState.SetState(McsMapVoteState.RunoffVoting);

        // Defer to next frame — NVM still holds the active handler during the
        // OnVotePassed/OnVoteFailed callback, so a synchronous InitiateMultiChoiceVote
        // would return VoteAlreadyInProgress.
        _plugin.SharedSystem.GetModSharp().PushTimer(() =>
        {
            if (!ReferenceEquals(_voteManager.CurrentSession, originalSession))
                return;

            var freshOptions = runoffCandidates
                .Select(c => (IMapVoteOption)new MapVoteOption(c.MapName, c.MapConfig))
                .ToList();

            originalSession.SetVoteOptions(freshOptions);

            var participants = GetVoteParticipants();
            if (!StartNativeVote(originalSession, freshOptions, participants, true))
            {
                HandleWinner(originalSession, originalSession.GetTopVotedOption());
            }
        }, 0f, Sharp.Shared.Enums.GameTimerFlags.None);
    }

    internal void HandleVoteResult(MapVoteInformation session, bool isRunoff)
    {
        session.CurrentState = McsMapVoteState.Finalizing;
        _voteState.SetState(McsMapVoteState.Finalizing);

        float winnerThreshold = _conVars.WinnerPickupThreshold.GetFloat();
        float runoffThreshold = _conVars.RunoffMapPickupThreshold.GetFloat();

        var winners = session.GetOptionsAboveThreshold(winnerThreshold);

        if (winners.Count == 0 && !isRunoff)
        {
            var runoffCandidates = session.GetOptionsAboveThreshold(runoffThreshold);

            if (runoffCandidates.Count < 2)
            {
                var top = session.GetTopVotedOption();
                if (top is not null && !runoffCandidates.Contains(top))
                    runoffCandidates.Insert(0, top);
            }

            if (runoffCandidates.Count >= 2)
            {
                _logger.LogInformation("No decisive winner, starting runoff with {Count} candidates", runoffCandidates.Count);
                BroadcastToAll("MapVote.Broadcast.StartingRunoffVote", (int)(winnerThreshold * 100));
                StartRunoffVote(session, runoffCandidates);
                return;
            }
        }

        MapVoteOption? winner;
        if (winners.Count > 0)
        {
            winner = winners[0];
        }
        else
        {
            winner = session.GetTopVotedOption();
            if (winner is null && session.TotalVoteCount == 0)
            {
                var nonPlaceholders = session.VoteOptions
                    .OfType<MapVoteOption>()
                    .Where(o => o.MapConfig is not null)
                    .ToList();

                if (nonPlaceholders.Count > 0)
                    winner = nonPlaceholders[Random.Shared.Next(nonPlaceholders.Count)];
            }
        }

        HandleWinner(session, winner);
    }

    private void HandleWinner(MapVoteInformation session, MapVoteOption? winner)
    {
        StopCountdownTimer();
        _soundPlayer.PlayVoteFinishedSoundToAll();

        session.SetWinner(winner);

        int totalVotes = session.TotalVoteCount;
        int participants = session.ParticipantCount;

        // Nobody voted: the winner (if any) was picked at random — announce
        // that instead of the regular vote stats + confirmed-by-vote lines.
        bool isRandomPick = totalVotes == 0 && winner is not null;

        if (!isRandomPick)
        {
            int pct = participants > 0 ? (int)(totalVotes * 100.0 / participants) : 0;
            BroadcastToAll("MapVote.Broadcast.VoteFinished", participants, totalVotes, pct);
        }

        if (winner is null)
        {
            // 0 votes AND no pickable candidates — nothing to randomly pick from.
            BroadcastToAll("MapVote.Broadcast.VoteResult.NotChanging", 0, 0);
        }
        else if (winner.MapName == MapVoteConstants.ExtendMapInternalName)
        {
            int winnerPct = totalVotes > 0 ? (int)(winner.VoteCount * 100.0 / totalVotes) : 0;
            BroadcastToAll("MapVote.Broadcast.VoteResult.Extend", winnerPct, totalVotes);
        }
        else if (winner.MapName == MapVoteConstants.DontChangeMapInternalName)
        {
            int winnerPct = totalVotes > 0 ? (int)(winner.VoteCount * 100.0 / totalVotes) : 0;
            BroadcastToAll("MapVote.Broadcast.VoteResult.NotChanging", winnerPct, totalVotes);
        }
        else if (winner.MapConfig is not null)
        {
            if (isRandomPick)
            {
                BroadcastToAll("MapVote.Broadcast.VoteResult.NoVotes", winner.MapName);
            }
            else
            {
                int winnerPct = totalVotes > 0 ? (int)(winner.VoteCount * 100.0 / totalVotes) : 0;
                BroadcastToAll("MapVote.Broadcast.VoteResult.NextMapConfirmed", winner.MapName, winnerPct, totalVotes);
            }
        }

        ApplyNominationCooldownToNominatedMaps();

        var finishedParams = new MapVoteFinishedParams(_plugin, _moduleBase, session, session.IsRtvVote, SnapshotNominations());
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteFinished(finishedParams));

        if (winner is null)
        {
            _logger.LogWarning("Vote concluded with no winner");
            var notChangedParams = new MapVoteNotChangedParams(_plugin, _moduleBase);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapNotChanged(notChangedParams));
        }
        else if (winner.MapName == MapVoteConstants.ExtendMapInternalName)
        {
            _logger.LogInformation("Vote result: Extend current map");
            var extendResult = _mapExtendService.TryExtend(McsExtendTrigger.MapVote);
            if (extendResult != Shared.MapCycle.McsMapExtendResult.Extended)
            {
                _logger.LogWarning("Extend option won but extend failed: {Result}", extendResult);
                var notChangedParams = new MapVoteNotChangedParams(_plugin, _moduleBase);
                _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapNotChanged(notChangedParams));
            }
        }
        else if (winner.MapName == MapVoteConstants.DontChangeMapInternalName)
        {
            _logger.LogInformation("Vote result: Don't change map");
            var notChangedParams = new MapVoteNotChangedParams(_plugin, _moduleBase);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapNotChanged(notChangedParams));
        }
        else if (winner.MapConfig is not null)
        {
            _logger.LogInformation("Vote result: Next map confirmed as {MapName}", winner.MapName);
            var mapInfo = BuildMapInformation(winner.MapConfig);
            var confirmedParams = new MapVoteMapConfirmedParams(_plugin, _moduleBase, mapInfo, session.IsRtvVote);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapConfirmed(confirmedParams));

            session.CurrentState = McsMapVoteState.NextMapConfirmed;
            _voteState.SetState(McsMapVoteState.NextMapConfirmed);
            _voteManager.ClearSession();
            return;
        }

        session.CurrentState = McsMapVoteState.NoActiveVote;
        _voteState.Reset();
        _voteManager.ClearSession();
    }

    /// <summary>
    /// Phase 1 (game thread): placeholder option + admin/community nomination
    /// candidates. Synchronous, unchanged from the pre-deferred implementation.
    /// NominationVoteSlotService mirrors this pick order/capacity for !nomlist
    /// and the overflow notification — keep both in sync.
    /// </summary>
    private (List<IMapVoteOption> Candidates, HashSet<string> UsedMapNames, int SlotsForMaps) BuildNominatedCandidates(
        bool isRtvVote, int maxElements)
    {
        var candidates = new List<IMapVoteOption>();
        var usedMapNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Placeholder option (slot 0).
        // The Extend option only appears while the vote-extend budget remains.
        if (isRtvVote)
            candidates.Add(new MapVoteOption(MapVoteConstants.DontChangeMapInternalName, null));
        else if (_mapExtendService.ExtendsLeft > 0)
            candidates.Add(new MapVoteOption(MapVoteConstants.ExtendMapInternalName, null));

        int slotsForMaps = maxElements - 1;

        // Admin-nominated maps first
        var nominations = _nominationManager.NominatedMaps;
        var forceNominated = nominations.Values
            .Where(n => n.IsForceNominated)
            .Select(n => n.MapConfig)
            .ToList();

        {
            var adminPickParams = new AdminNominatedMapPickParams(
                _plugin, _moduleBase, forceNominated.AsReadOnly());

            var adminOverride = _eventManager.FireWithResult<IMapVoteEventListener, McsValueOverrideEvent<List<IMapConfig>>>(
                e => e.OnAdminNominatedMapPick(adminPickParams),
                r => r.HasValue,
                McsValueOverrideEvent<List<IMapConfig>>.NoOverride);

            AddCandidates(candidates, usedMapNames, slotsForMaps,
                adminOverride.HasValue ? adminOverride.Value : forceNominated);
        }

        // Community nominations sorted by participant count
        var communityNominated = nominations.Values
            .Where(n => !n.IsForceNominated)
            .OrderByDescending(n => n.NominationParticipants.Count)
            .Select(n => n.MapConfig)
            .ToList();

        {
            var nomPickParams = new NominatedMapPickParams(
                _plugin, _moduleBase, communityNominated.AsReadOnly());

            var nomOverride = _eventManager.FireWithResult<IMapVoteEventListener, McsValueOverrideEvent<List<IMapConfig>>>(
                e => e.OnNominatedMapPick(nomPickParams),
                r => r.HasValue,
                McsValueOverrideEvent<List<IMapConfig>>.NoOverride);

            IEnumerable<IMapConfig> nomMapsToAdd;
            if (nomOverride.HasValue)
            {
                nomMapsToAdd = nomOverride.Value;
            }
            else
            {
                nomMapsToAdd = communityNominated.Where(m =>
                    nominations.TryGetValue(m.MapName, out var nd)
                    && nd.NominationParticipants.Count >= m.NominationConfig.MinNominationCountForVote);
            }

            AddCandidates(candidates, usedMapNames, slotsForMaps, nomMapsToAdd);
        }

        return (candidates, usedMapNames, slotsForMaps);
    }

    private static void AddCandidates(
        List<IMapVoteOption> candidates,
        HashSet<string> usedMapNames,
        int slotsForMaps,
        IEnumerable<IMapConfig> maps)
    {
        foreach (var map in maps)
        {
            if (candidates.Count - 1 >= slotsForMaps)
                break;

            if (usedMapNames.Add(map.MapName))
                candidates.Add(new MapVoteOption(map.MapName, map));
        }
    }

    private List<IGameClient> GetVoteParticipants()
    {
        bool excludeSpectators = _conVars.ExcludeSpectators.GetInt32() != 0;
        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);

        return clients
            .Where(c => !c.IsFakeClient && !c.IsHltv)
            .Where(c => !excludeSpectators
                        || c.GetPlayerController()?.Team != CStrikeTeam.Spectator)
            .ToList();
    }

    private bool StartNativeVote(
        MapVoteInformation session,
        IReadOnlyList<IMapVoteOption> candidates,
        List<IGameClient> participants,
        bool isRunoff)
    {
        float voteDuration = _conVars.VoteEndTime.GetFloat();
        float winnerThreshold = _conVars.WinnerPickupThreshold.GetFloat();

        var voteContents = new List<VoteContent>();
        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];

            LocalizedString visibleName;
            if (candidate.MapName == MapVoteConstants.ExtendMapInternalName)
                visibleName = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Option.ExtendMap", c ?? System.Globalization.CultureInfo.CurrentCulture));
            else if (candidate.MapName == MapVoteConstants.DontChangeMapInternalName)
                visibleName = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Option.DontChange", c ?? System.Globalization.CultureInfo.CurrentCulture));
            else
            {
                string displayName = candidate.MapConfig is not null
                    ? _mapConfigProvider.ToolingService.ResolveMapDisplayName(candidate.MapConfig)
                    : candidate.MapName;
                bool isNominated = _nominationManager.NominatedMaps.ContainsKey(candidate.MapName);
                visibleName = LocalizedString.From(_ => isNominated ? $"(N) {displayName}" : displayName);
            }

            voteContents.Add(new VoteContent
            {
                Index = i,
                InternalName = candidate.MapName,
                VisibleName = visibleName,
            });
        }

        // Abstain option (page 1 of the menu). Deliberately has no matching
        // session vote option — see MapVoteConstants.NoVoteInternalName.
        voteContents.Add(new VoteContent
        {
            Index = voteContents.Count,
            InternalName = MapVoteConstants.NoVoteInternalName,
            VisibleName = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Option.NoVote", c ?? System.Globalization.CultureInfo.CurrentCulture)),
        });

        IMultiChoiceVoteHandler handler = isRunoff
            ? new RunoffVoteNativeHandler(this, _voteManager, session, _logger)
            : new InitialVoteNativeHandler(this, _voteManager, session, _logger);

        VotePassCondition passCondition;
        if (isRunoff)
        {
            passCondition = _ => true;
        }
        else
        {
            float threshold = winnerThreshold;
            if (CustomWinnerThresholdProvider is { } provider)
            {
                try { threshold = provider(); }
                catch { /* fall back to ConVar threshold */ }
            }
            passCondition = VotePassConditions.Default(threshold);
        }

        var voteOptions = new MultiChoiceVoteOptions
        {
            Title = LocalizedString.From(c => _plugin.Localizer.ForCulture(isRunoff ? "Vote.Title.Runoff" : "Vote.Title.Initial", c ?? System.Globalization.CultureInfo.CurrentCulture)),
            Description = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Description", c ?? System.Globalization.CultureInfo.CurrentCulture)),
            VoteDuration = voteDuration,
            VoteHandler = handler,
            VoteContents = voteContents,
            PassCondition = passCondition,
            RandomShuffle = _conVars.ShuffleMenu.GetInt32() != 0,
            Participants = participants,
        };

        var result = _nativeVoteManager.InitiateMultiChoiceVote(
            voteOptions, new McsVoteMenuCompat(_wulingMenu, _wulingRegistry, _plugin.LocalizationPlatform));

        if (result != VoteInitiateResult.Success)
        {
            _logger.LogWarning("Failed to initiate native vote: {Result}", result);
            var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null, SnapshotNominations());
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));
            _voteState.Reset();
            _voteManager.ClearSession();
            return false;
        }

        session.CurrentState = isRunoff ? McsMapVoteState.RunoffVoting : McsMapVoteState.Voting;
        _voteState.SetState(session.CurrentState);

        _soundPlayer.SetRunoff(isRunoff);
        _soundPlayer.PlayVoteStartSoundToAll();

        if (_configProvider.PluginConfig.VoteConfig.ShouldPrintVoteRemainingTime)
            StartVoteEndCountdown(session, voteDuration);

        NotifyExcludedSpectators();

        return true;
    }

    private void ApplyNominationCooldownToNominatedMaps()
    {
        foreach (var entry in _nominationManager.NominatedMaps)
        {
            _cooldownLifecycleService.ApplyNominationCooldown(entry.Value.MapConfig);
        }
    }

    private IReadOnlyDictionary<string, IMcsNominationData> SnapshotNominations()
        => new Dictionary<string, IMcsNominationData>(_nominationManager.NominatedMaps, StringComparer.OrdinalIgnoreCase);

    private IMapInformation BuildMapInformation(IMapConfig mapConfig)
    {
        var builder = MapInformation.For(mapConfig);

        if (_nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nomination))
        {
            var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
            var steamIds = new List<ulong>();
            foreach (int slot in nomination.NominationParticipants)
            {
                foreach (var client in clients)
                {
                    if (client.Slot == slot)
                    {
                        steamIds.Add(client.SteamId);
                        break;
                    }
                }
            }

            if (steamIds.Count > 0)
                builder.WithNominators(steamIds);
        }

        return builder.Build();
    }

    private void StartPreVoteCountdown(
        MapVoteInformation session,
        int totalSeconds)
    {
        _voteDuration = totalSeconds;
        _voteStartTime = DateTime.UtcNow;

        _countdownTimerId = _plugin.SharedSystem.GetModSharp().PushTimer(() =>
        {
            if (!ReferenceEquals(_voteManager.CurrentSession, session))
            {
                StopCountdownTimer();
                return;
            }

            var readOnlyState = _voteState as IMcsReadOnlyVoteState;
            if (readOnlyState?.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
            {
                StopCountdownTimer();
                _countdownUi.CloseCountdownUiAll();

                var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null, SnapshotNominations());
                _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

                session.CurrentState = McsMapVoteState.NoActiveVote;
                _voteState.Reset();
                _voteManager.ClearSession();
                return;
            }

            int elapsed = (int)(DateTime.UtcNow - _voteStartTime).TotalSeconds;
            int remaining = totalSeconds - elapsed;

            if (remaining <= 0)
            {
                StopCountdownTimer();
                _countdownUi.CloseCountdownUiAll();
                OnCountdownFinished(session);
                return;
            }

            _countdownUi.ShowCountdownToAll(remaining, Ui.Countdown.McsCountdownType.VoteStart);
            _soundPlayer.PlayVoteCountdownSoundToAll(remaining);

        }, 1.0, Sharp.Shared.Enums.GameTimerFlags.Repeatable | Sharp.Shared.Enums.GameTimerFlags.StopOnMapEnd);
    }

    private void OnCountdownFinished(MapVoteInformation session)
    {
        int maxElements = _configProvider.PluginConfig.VoteConfig.MaxMenuElements;

        List<IMapVoteOption> candidates;
        HashSet<string> usedMapNames;
        int slotsForMaps;
        try
        {
            (candidates, usedMapNames, slotsForMaps) = BuildNominatedCandidates(session.IsRtvVote, maxElements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build candidate list after countdown");
            ResetToNoActiveVote(session);
            return;
        }

        // Phase 2 (game thread): OnRandomMapPick may supply a full override —
        // in that case finish synchronously, no thread hop needed.
        int remaining = slotsForMaps - (candidates.Count - 1);
        if (remaining <= 0)
        {
            StartVoteWithCandidates(session, candidates);
            return;
        }

        var allConfigs = _mapConfigProvider.GetMapConfigs()
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.First().MapConfig,
                StringComparer.OrdinalIgnoreCase);

        var pickParams = new MapVoteRandomMapPickParams(_plugin, _moduleBase, remaining, allConfigs);

        var overrideResult = _eventManager.FireWithResult<IMapVoteEventListener, McsValueOverrideEvent<List<IMapConfig>>>(
            e => e.OnRandomMapPick(pickParams),
            r => r.HasValue,
            McsValueOverrideEvent<List<IMapConfig>>.NoOverride);

        if (overrideResult.HasValue)
        {
            AddCandidates(candidates, usedMapNames, slotsForMaps, overrideResult.Value);
            StartVoteWithCandidates(session, candidates);
            return;
        }

        // No override — snapshot on the game thread, then hop to the thread
        // pool for the pure filter ONLY. Event firing and the shuffle resume
        // on the game thread via InvokeFrameAction (events must stay synchronous
        // and fire on the game thread).
        var plan = _randomMapPicker.PreparePick(remaining, usedMapNames);

        _ = Task.Run(() =>
        {
            List<IMapConfig> filtered = [];
            Exception? failure = null;
            try
            {
                filtered = _randomMapPicker.FilterPickable(plan);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            _plugin.SharedSystem.GetModSharp().InvokeFrameAction(() =>
            {
                if (!ReferenceEquals(_voteManager.CurrentSession, session))
                {
                    _logger.LogDebug("Vote session changed while picking random maps; discarding stale continuation");
                    return;
                }

                var readOnlyState = _voteState as IMcsReadOnlyVoteState;
                if (readOnlyState?.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
                {
                    var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null, SnapshotNominations());
                    _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

                    session.CurrentState = McsMapVoteState.NoActiveVote;
                    _voteState.Reset();
                    _voteManager.ClearSession();
                    return;
                }

                if (failure is not null)
                {
                    _logger.LogError(failure, "Failed to build candidate list after countdown");
                    ResetToNoActiveVote(session);
                    return;
                }

                var picked = _randomMapPicker.FinishPick(filtered, plan.Amount);
                AddCandidates(candidates, usedMapNames, slotsForMaps, picked);

                StartVoteWithCandidates(session, candidates);
            });
        });
    }

    private void StartVoteWithCandidates(MapVoteInformation session, List<IMapVoteOption> candidates)
    {
        if (candidates.Count < 2)
        {
            _logger.LogWarning("Not enough maps to start vote after countdown ({Count} candidates)", candidates.Count);
            BroadcastToAll("MapVote.Broadcast.NotEnoughMapsToStartVote");
            ResetToNoActiveVote(session);
            return;
        }

        session.SetVoteOptions(candidates);

        var realMapConfigs = candidates
            .Where(c => c.MapConfig is not null)
            .Select(c => c.MapConfig!)
            .ToList();

        var participants = GetVoteParticipants();
        session.ParticipantCount = participants.Count;

        var participantSlots = participants.Select(c => c.Slot).ToList();
        var startParams = new MapVoteStartParams(_plugin, _moduleBase, realMapConfigs, participantSlots);
        if (_eventManager.FireCancellable<IMapVoteEventListener>(e => e.OnMapVoteStart(startParams)) != McsCancellableEvent.Continue)
        {
            _logger.LogInformation("Vote start cancelled by event listener");
            var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null, SnapshotNominations());
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));
            ResetToNoActiveVote(session);
            return;
        }

        if (!StartNativeVote(session, candidates, participants, false))
        {
            ResetToNoActiveVote(session);
        }
    }

    private void ResetToNoActiveVote(MapVoteInformation session)
    {
        session.CurrentState = McsMapVoteState.NoActiveVote;
        _voteState.Reset();
        _voteManager.ClearSession();
    }

    private void StopCountdownTimer()
    {
        if (_countdownTimerId != Guid.Empty)
        {
            _plugin.SharedSystem.GetModSharp().StopTimer(_countdownTimerId);
            _countdownTimerId = Guid.Empty;
        }
        _countdownUi.CloseCountdownUiAll();
        StopVoteEndTimer();
    }

    private void StopVoteEndTimer()
    {
        if (_voteEndTimerId != Guid.Empty)
        {
            _plugin.SharedSystem.GetModSharp().StopTimer(_voteEndTimerId);
            _voteEndTimerId = Guid.Empty;
        }
    }

    private void StartVoteEndCountdown(MapVoteInformation session, float durationSeconds)
    {
        var startTime = DateTime.UtcNow;
        int totalSeconds = (int)durationSeconds;

        _voteEndTimerId = _plugin.SharedSystem.GetModSharp().PushTimer(() =>
        {
            if (!ReferenceEquals(_voteManager.CurrentSession, session))
            {
                StopVoteEndTimer();
                return;
            }

            int elapsed = (int)(DateTime.UtcNow - startTime).TotalSeconds;
            int remaining = totalSeconds - elapsed;

            if (remaining <= 0)
            {
                StopVoteEndTimer();
                return;
            }

            // Countdown sound is reserved for the pre-vote countdown; during
            // the vote itself only the visual countdown is shown.
            _countdownUi.ShowCountdownToAll(remaining, Ui.Countdown.McsCountdownType.Voting);
        }, 1.0, Sharp.Shared.Enums.GameTimerFlags.Repeatable | Sharp.Shared.Enums.GameTimerFlags.StopOnMapEnd);
    }

    internal void NotifyVoteCast(Sharp.Shared.Objects.IGameClient voter, IMapVoteOption option)
    {
        if (!_configProvider.PluginConfig.VoteConfig.ShouldPrintVoteToChat)
            return;

        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            string mapDisplay = ResolveVoteOptionDisplayName(option, client);
            client.GetPlayerController()?.PrintToChat(
                $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.VoteCast", voter.Name, mapDisplay)}");
        }
    }

    private string ResolveVoteOptionDisplayName(IMapVoteOption option, Sharp.Shared.Objects.IGameClient client)
    {
        if (option.MapName == MapVoteConstants.ExtendMapInternalName)
            return _plugin.LocalizeStringForPlayer(client, "Vote.Option.ExtendMap");
        if (option.MapName == MapVoteConstants.DontChangeMapInternalName)
            return _plugin.LocalizeStringForPlayer(client, "Vote.Option.DontChange");
        if (option.MapConfig is not null)
            return _mapConfigProvider.ToolingService.ResolveMapDisplayName(option.MapConfig);
        return option.MapName;
    }

    private void NotifyExcludedSpectators()
    {
        if (_conVars.ExcludeSpectators.GetInt32() == 0)
            return;

        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            if (client.GetPlayerController()?.Team == CStrikeTeam.Spectator)
            {
                client.GetPlayerController()?.PrintToChat(
                    $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, "MapVote.Notification.SpectatorIsExcluded")}");
            }
        }
    }

    private void BroadcastToAll(string key, params object[] args)
    {
        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            client.GetPlayerController()?.PrintToChat(
                $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, key, args)}");
        }
    }
}
