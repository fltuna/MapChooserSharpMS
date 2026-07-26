using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Audit.Collectors;
using MapChooserSharpMS.Modules.Audit.Services;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.Services;
using McsCancellableEvent = MapChooserSharpMS.Shared.Events.McsCancellableEvent;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Models;
using MapChooserSharpMS.Modules.Statistics;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;
using Wuling.Abstract;

namespace MapChooserSharpMS.Modules.Audit;

internal sealed class McsAuditController
    : PluginModuleBase,
      IGameListener,
      IMapVoteEventListener,
      IRockTheVoteEventListener,
      IMapCycleEventListener,
      INominationEventListener
{
    public override string PluginModuleName => "McsAuditController";
    public override string ModuleChatPrefix => "unused";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    public int ListenerVersion => 1;
    public int ListenerPriority => -100;

    private IInternalEventManager _eventManager = null!;
    private McsStatisticsTracker _tracker = null!;
    private IAuditPersistence _persistence = NullAuditPersistence.Instance;
    private string _serverId = "";

    private MapPlayAuditCollector? _mapPlayCollector;
    private NominationAuditCollector? _nominationCollector;
    private VoteAuditCollector? _voteCollector;
    private ExtendVoteAuditCollector? _extendVoteCollector;
    private RtvAuditCollector? _rtvCollector;
    private ExtAuditCollector? _extCollector;

    private bool _voteActive;

    public McsAuditController(IServiceProvider serviceProvider, bool hotReload)
        : base(serviceProvider, hotReload)
    {
    }

    protected override void OnInitialize()
    {
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        _tracker = ServiceProvider.GetRequiredService<McsStatisticsTracker>();

        SharedSystem.GetModSharp().InstallGameListener(this);
    }

    protected override void OnAllModulesLoaded()
    {
        InitializeAuditPersistence();

        _eventManager.RegisterListener<IMapVoteEventListener>(this);
        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
        _eventManager.RegisterListener<IMapCycleEventListener>(this);
        _eventManager.RegisterListener<INominationEventListener>(this);
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
        _eventManager.RemoveListener<IMapVoteEventListener>(this);
        _eventManager.RemoveListener<IRockTheVoteEventListener>(this);
        _eventManager.RemoveListener<IMapCycleEventListener>(this);
        _eventManager.RemoveListener<INominationEventListener>(this);
    }

    private void InitializeAuditPersistence()
    {
        var wuling = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IWuling>(IWuling.Identity)
            .Instance!;

        var surreal = wuling.Surreal;
        _serverId = surreal.ServerId;
        var persistence = new SurrealAuditRepository(surreal, Logger, Plugin.ModuleDirectory);

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await persistence.EnsureSchemasAsync();
                _persistence = persistence;

                var cooldownLifecycle = ServiceProvider.GetService<McsMapCooldownLifecycleService>();
                cooldownLifecycle?.SetAuditPersistence(persistence, _serverId);

                Logger.LogInformation("[Audit] Audit persistence initialized via Wuling SurrealDB (server_id={ServerId})", _serverId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[Audit] Failed to initialize audit persistence — audit data will not be saved");
            }
        });
    }

    private void InitializeCollectors()
    {
        _mapPlayCollector = new MapPlayAuditCollector(_tracker, _serverId);
        _nominationCollector = new NominationAuditCollector(_serverId);
        _voteCollector = new VoteAuditCollector(_serverId);
        _extendVoteCollector = new ExtendVoteAuditCollector(
            _serverId,
            () => SharedSystem.GetConVarManager()
                .FindConVar("mcs_vote_extend_success_threshold")?.GetFloat() ?? 0.5f);
        _rtvCollector = new RtvAuditCollector(_serverId);
        _extCollector = new ExtAuditCollector(_serverId);
    }

    #region IGameListener

    public void OnGameActivate()
    {
        if (ServiceProvider.GetRequiredService<IMcsBootPhaseTracker>().IsBootPhase)
            return;

        InitializeCollectors();

        var transitionManager = ServiceProvider.GetRequiredService<IMcsInternalMapTransitionManager>();
        var currentMap = transitionManager.CurrentMap?.MapConfig;
        string mapName = SharedSystem.GetModSharp().GetMapName() ?? "";
        long? workshopId = currentMap?.WorkshopId is > 0 ? currentMap.WorkshopId : null;
        var groupNames = currentMap?.GroupSettings.Select(g => g.GroupName).ToList()
            ?? new List<string>();

        string timelimitType = "none";
        float configuredTimelimit = 0f;
        int maxExtends = 0;
        int maxExtCommandUses = 0;
        try
        {
            var timeLimitManager = ServiceProvider.GetRequiredService<IMapCycleController>().CurrentMapTimeLimitManager;
            timelimitType = timeLimitManager.TimeLimitType switch
            {
                TimeLimitType.Time => "time",
                TimeLimitType.Round => "round",
                _ => "none"
            };

            // MapCycle sets mp_timelimit/mp_maxrounds to sentinel values during the same
            // activate phase, so reading the ConVars here is listener-order-dependent.
            // Derive the configured limit from the map config instead.
            configuredTimelimit = timelimitType switch
            {
                "time" => currentMap is null ? 0f : currentMap.MapTime,
                "round" => currentMap is null ? 0f : currentMap.MapRounds,
                _ => 0f
            };
        }
        catch
        {
        }

        try
        {
            var extendService = ServiceProvider.GetRequiredService<IMcsInternalMapExtendService>();
            maxExtends = extendService.ExtendsLeft;
            maxExtCommandUses = extendService.ExtCommandUsesLeft;
        }
        catch
        {
        }

        _mapPlayCollector?.OnMapStart(mapName, workshopId, groupNames, timelimitType, configuredTimelimit,
            maxExtends, maxExtCommandUses);
        _voteActive = false;
    }

    public void OnGameDeactivate()
    {
        if (_mapPlayCollector is null)
            return;

        if (_tracker.MapEndReason == "unknown")
            _tracker.SetMapEndReason("admin");

        var record = _mapPlayCollector.BuildRecord();
        if (record is not null)
            _persistence.InsertMapPlayFireAndForget(record);
    }

    #endregion

    #region IMapVoteEventListener

    public McsCancellableEvent OnMapVoteStart(IMapVoteStartParams @params)
    {
        _voteActive = true;

        var voteController = ServiceProvider.GetService<MapVote.Interfaces.IMcsInternalVoteController>();
        var currentVote = voteController?.MapVoteManager.CurrentVote;

        bool isRtv = currentVote is MapVoteInformation info && info.IsRtvVote;

        float voteDuration = SharedSystem.GetConVarManager()
            .FindConVar("mcs_vote_end_time")?.GetFloat() ?? 30f;

        _voteCollector?.OnVoteInitiated(isRtv, voteDuration, @params.VoteParticipants.Count);

        if (currentVote is not null)
            _voteCollector?.CaptureInitialCandidates(currentVote.VoteOptions);

        var candidateOptions = @params.MapsToVote
            .Select(m => (Shared.MapVote.IMapVoteOption)new SimpleVoteOption(m.MapName, m))
            .ToList();
        var notPickedRecords = _nominationCollector?.OnVoteStarted(candidateOptions);
        if (notPickedRecords is { Count: > 0 })
            _persistence.InsertNominationsFireAndForget(notPickedRecords);

        return McsCancellableEvent.Continue;
    }

    public void OnMapVoteFinished(IMapVoteFinishedEventParams @params)
    {
        _voteActive = false;

        if (_voteCollector is not null)
        {
            var (vote, candidates) = _voteCollector.BuildFinishedRecord(
                @params.VoteInformation, @params.NominatedMaps);
            _persistence.InsertVoteFireAndForget(vote, candidates);
        }

        if (_nominationCollector is not null)
        {
            var nominationRecords = _nominationCollector.OnVoteFinished(
                @params.VoteInformation.Winner, @params.NominatedMaps);
            if (nominationRecords.Count > 0)
                _persistence.InsertNominationsFireAndForget(nominationRecords);
        }
    }

    public void OnMapVoteCancelled(IMapVoteCancelledParams @params)
    {
        if (!_voteActive) return;
        _voteActive = false;

        if (_voteCollector is not null)
        {
            var vote = _voteCollector.BuildCancelledRecord();
            _persistence.InsertVoteFireAndForget(vote, Array.Empty<AuditVoteCandidate>());
        }
    }

    public void OnMapExtended(IMapVoteExtendParams @params)
    {
        _mapPlayCollector?.OnExtend();
    }

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params) { }

    #endregion

    #region IMapCycleEventListener

    public void OnExtendVoteStarted(IExtendVoteStartedEventParams @params)
    {
        int totalPlayers = SharedSystem.GetModSharp().GetIServer()
            .GetGameClients(false, false)
            .Count(c => !c.IsFakeClient && !c.IsHltv);

        _extendVoteCollector?.OnVoteStarted(totalPlayers, @params.Initiator?.SteamId);
    }

    public void OnExtendVoteFinished(IExtendVoteFinishedEventParams @params)
    {
        if (_extendVoteCollector is null) return;

        if (@params.Passed)
            _mapPlayCollector?.OnAdminExtend();

        var record = _extendVoteCollector.BuildFinishedRecord(@params.Passed, @params.YesCount, @params.NoCount);
        _persistence.InsertExtendVoteFireAndForget(record);
    }

    public void OnExtendVoteCancelled(IExtendVoteCancelledEventParams @params)
    {
        if (_extendVoteCollector is null) return;

        var record = _extendVoteCollector.BuildCancelledRecord();
        _persistence.InsertExtendVoteFireAndForget(record);
    }

    public McsCancellableEvent OnExtCommandExecute(IExtCommandExecuteEventParams @params)
    {
        if (@params.Client is { } client)
        {
            _extCollector?.OnExtCommandExecute(client.SteamId);

            if (@params.CurrentExtVotes + 1 >= @params.CurrentRequiredVotes && _extCollector is not null)
            {
                var transitionManager = ServiceProvider.GetRequiredService<IMcsInternalMapTransitionManager>();
                var (ext, votes) = _extCollector.BuildTriggerRecord(
                    @params.CurrentRequiredVotes, transitionManager.IsNextMapConfirmed);
                _persistence.InsertExtFireAndForget(ext, votes);
            }
        }

        return McsCancellableEvent.Continue;
    }

    public void OnVoteStartThresholdReached(IVoteStartThresholdReachedEventParams @params)
    {
        _tracker.SetMapEndReason("timelimit");
    }

    #endregion

    #region IRockTheVoteEventListener

    public McsCancellableEvent OnClientRtvCast(IClientRtvCastParams @params)
    {
        if (@params.Client is { } client)
            _rtvCollector?.OnClientRtvCast(client.SteamId);

        return McsCancellableEvent.Continue;
    }

    public McsCancellableEvent OnClientRtvUnCast(IClientRtvUnCastParams @params)
    {
        if (@params.Client is { } client)
            _rtvCollector?.OnClientRtvUnCast(client.SteamId);

        return McsCancellableEvent.Continue;
    }

    public void OnRtvConfirmed(IRtvConfirmedParams @params)
    {
        _tracker.SetMapEndReason("rtv");

        if (_rtvCollector is null) return;

        var transitionManager = ServiceProvider.GetRequiredService<IMcsInternalMapTransitionManager>();
        int threshold = 0;
        int? immediateThreshold = null;

        var rtvController = ServiceProvider.GetService<Modules.RockTheVote.Interfaces.IMcsInternalRtvController>();
        if (rtvController?.RtvManager is { } mgr)
        {
            threshold = mgr.RequiredCounts;
            if (mgr is Modules.RockTheVote.Managers.InternalRtvManager concrete)
            {
                int imm = concrete.ImmediateRequiredCounts;
                if (imm < int.MaxValue)
                    immediateThreshold = imm;
            }
        }

        bool isNextMapConfirmed = transitionManager.IsNextMapConfirmed;

        var (rtv, votes) = _rtvCollector.BuildTriggerRecord(threshold, immediateThreshold, @params.IsForced, isNextMapConfirmed);
        _persistence.InsertRtvFireAndForget(rtv, votes);
    }

    #endregion

    #region INominationEventListener

    public McsCancellableEvent OnNomination(INominationParams @params)
    {
        var data = @params.NominationData;
        ulong? steamId = @params.Client?.SteamId;
        _nominationCollector?.OnNomination(data.MapConfig, steamId, "user", DateTime.UtcNow);
        return McsCancellableEvent.Continue;
    }

    public McsCancellableEvent OnAdminNomination(IAdminNominationParams @params)
    {
        var data = @params.NominationData;
        ulong? steamId = @params.Client?.SteamId;
        string type = @params.Client is null ? "console" : "admin";
        _nominationCollector?.OnNomination(data.MapConfig, steamId, type, DateTime.UtcNow);
        return McsCancellableEvent.Continue;
    }

    public void OnNominationRemoved(INominationRemovedParams @params)
    {
        var records = _nominationCollector?.OnNominationCancelled(
            @params.NominationData.MapConfig.MapName, "cancelled_by_admin", null);
        if (records is { Count: > 0 })
            _persistence.InsertNominationsFireAndForget(records);
    }

    public void OnUnNominate(IUnNominateParams @params)
    {
        ulong? steamId = @params.Client?.SteamId;
        var records = _nominationCollector?.OnNominationCancelled(
            @params.NominationData.MapConfig.MapName, "cancelled_by_self", steamId);
        if (records is { Count: > 0 })
            _persistence.InsertNominationsFireAndForget(records);
    }

    #endregion

    private sealed class SimpleVoteOption : Shared.MapVote.IMapVoteOption
    {
        public string MapName { get; }
        public Shared.MapConfig.IMapConfig? MapConfig { get; }
        public System.Collections.Generic.IReadOnlyCollection<Sharp.Shared.Units.PlayerSlot> VoteParticipants { get; }
            = Array.Empty<Sharp.Shared.Units.PlayerSlot>();

        internal SimpleVoteOption(string mapName, Shared.MapConfig.IMapConfig? mapConfig)
        {
            MapName = mapName;
            MapConfig = mapConfig;
        }
    }
}
