using System;
using System.Linq;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.WorkshopSync;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.Services;
using Wuling.Abstract;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Modules.MapConfig;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Modules.MapCycle.Cooldown;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle;

internal sealed class McsMapCycleController
    : PluginModuleBase,
      IMapCycleController,
      IMapCycleExtendController,
      IGameListener,
      IClientListener,
      IEventListener,
      IMapVoteEventListener
{
    public override string PluginModuleName => "McsMapCycleController";
    public override string ModuleChatPrefix => "Prefix.MapCycle";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private IInternalEventManager _eventManager = null!;
    private MapCycleConVars _conVars = null!;

    // Writer for the extend-vote slot of the plugin-owned state manager.
    // Received via ctor already narrowed to <see cref="IMcsInternalExtendVoteState"/>
    // — this controller cannot touch the main-vote slot even by accident.

    private IInternalTimeLimitManager? _internalTimeLimitManager;
    private TimeLimitTransitionTracker? _transitionTracker;
    private IMcsInternalMapTransitionManager _mapTransitionManager = null!;
    private McsMapExtendService _extendService = null!;
    private McsExtCommandService _extCommandService = null!;
    private McsExtendVoteService _extendVoteService = null!;
    private McsMapCooldownQueryService _cooldownQueryService = null!;
    private McsMapCooldownCommandService _cooldownCommandService = null!;
    private McsMapCooldownLifecycleService _cooldownLifecycleService = null!;
    private MapConfigExecutionService _mapConfigExecutionService = null!;
    private McsMapSelectMenuService _mapSelectMenuService = null!;
    private IMcsPluginConfigProvider _pluginConfigProvider = null!;
    private IMcsBootPhaseTracker _bootPhaseTracker = null!;
    private WorkshopProvisioningService? _workshopProvisioningService;
    private SurrealCooldownRepository? _cooldownRepository;
    private readonly McsCooldownStore _cooldownStore = new();
    private string _serverKey = "";
    private readonly TaskCompletionSource _persistenceReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private MapCycleMode _mode = MapCycleMode.None;
    private Guid _tickTimerId = Guid.Empty;

    private IConVar? _mpTimelimit;
    private IConVar? _mpMaxrounds;
    private bool _conVarGuardActive;
    private bool _conVarGuardSuppressed;

    public ITimeLimitManager CurrentMapTimeLimitManager =>
        _internalTimeLimitManager
        ?? throw new InvalidOperationException("TimeLimit manager is not initialized for the current map");

    public IMapTransitionManager MapTransitionManager => _mapTransitionManager;

    public IMapCooldownQueryService MapCooldownQueryService => _cooldownQueryService;
    public IMapCooldownCommandService MapCooldownCommandService => _cooldownCommandService;
    public IMcsCooldownStore CooldownStore => _cooldownStore;

    #region IMapCycleExtendController

    public int ExtendsLeft => _extendService.ExtendsLeft;

    public int ExtCommandUsesLeft => _extendService.ExtCommandUsesLeft;

    public bool IsExtendVoteInProgress => _extendVoteService.IsExtendVoteInProgress;

    public McsMapExtendResult TryExtendCurrentMap(int? overrideAmount = null)
        => _extendService.TryExtend(McsExtendTrigger.AdminOrApi, overrideAmount);

    public void SetExtCommandUsesLeft(int count)
        => _extendService.SetExtCommandUsesLeft(count);

    public bool IsExtEnabled => _extCommandService.IsEnabled;

    public void EnableExt() => _extCommandService.IsEnabled = true;

    public void DisableExt() => _extCommandService.IsEnabled = false;

    public McsExtendVoteStartResult StartExtendVote(IGameClient? initiator = null, int? overrideAmount = null)
        => _extendVoteService.StartExtendVote(initiator, overrideAmount);

    public bool CancelExtendVote(IGameClient? canceller = null)
        => _extendVoteService.CancelExtendVote(canceller);

    #endregion

    internal McsExtCommandService ExtCommandService => _extCommandService;

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;

    public McsMapCycleController(
        IServiceProvider serviceProvider,
        bool hotReload) : base(serviceProvider, hotReload)
    {
        _conVars = new MapCycleConVars(Plugin.SharedSystem.GetConVarManager());
        foreach (var cv in _conVars.All()) TrackConVar(cv);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMapCycleController>(this);
        services.AddSingleton<IMapCycleExtendController>(this);
        // Created during OnInitialize; expose via factory so DI consumers
        // resolve the same instance once it's constructed.
        services.AddSingleton<IMapTransitionManager>(_ => _mapTransitionManager);
        services.AddSingleton<IMcsInternalMapTransitionManager>(_ => _mapTransitionManager);
        services.AddSingleton<IMcsInternalMapExtendService>(_ => _extendService);
        services.AddSingleton<McsExtCommandService>(_ => _extCommandService);
        services.AddSingleton<McsMapCooldownLifecycleService>(_ => _cooldownLifecycleService);
        services.AddSingleton<McsMapCooldownCommandService>(_ => _cooldownCommandService);
        services.AddSingleton<IMapCooldownQueryService>(_ => _cooldownQueryService);
        services.AddSingleton<IMcsCooldownStore>(_cooldownStore);
        services.AddSingleton<IMcsInternalCooldownStore>(_cooldownStore);
        services.AddSingleton<McsMapSelectMenuService>(_ => _mapSelectMenuService);
    }

    protected override void OnInitialize()
    {
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        _workshopProvisioningService = CreateWorkshopProvisioningService();
        var workshopProvisioning = _workshopProvisioningService;
        _pluginConfigProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        _bootPhaseTracker = ServiceProvider.GetRequiredService<IMcsBootPhaseTracker>();
        var configProvider = _pluginConfigProvider;

        var transitionManager = new McsMapTransitionManager(
            SharedSystem,
            ServiceProvider.GetRequiredService<IMcsMapConfigProvider>(),
            Logger,
            Plugin,
            this,
            _eventManager,
            _conVars,
            () => configProvider.PluginConfig.MapCycleConfig.ShouldStopSourceTvRecording,
            workshopProvisioning);
        transitionManager.SetConVarGuardCallbacks(SuppressConVarGuard, ResumeConVarGuard);
        _mapTransitionManager = transitionManager;
        var readOnlyVoteState = ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();

        _extendService = new McsMapExtendService(
            Plugin, this, Logger, _eventManager, configProvider,
            () => _internalTimeLimitManager,
            OnTimeLimitChanged);

        _extCommandService = new McsExtCommandService(
            Plugin, this, Logger, _eventManager, _extendService, readOnlyVoteState, _conVars);

        var extendVoteState = ServiceProvider.GetRequiredService<IMcsInternalExtendVoteState>();
        _extendVoteService = new McsExtendVoteService(
            Plugin, this, Logger, _eventManager, _extendService,
            extendVoteState, _conVars,
            () => _mapTransitionManager.CurrentMap?.MapConfig);

        _mapConfigExecutionService = new MapConfigExecutionService(
            SharedSystem, Logger,
            () => configProvider.PluginConfig.MapCycleConfig.MapConfigExecutionType);

        var mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _cooldownQueryService = new McsMapCooldownQueryService(_cooldownStore);
        _cooldownCommandService = new McsMapCooldownCommandService(Logger, mapConfigProvider, _cooldownStore);
        _cooldownLifecycleService = new McsMapCooldownLifecycleService(
            Logger, Plugin, this, mapConfigProvider, _eventManager, _cooldownStore);

        SharedSystem.GetModSharp().InstallGameListener(this);
        SharedSystem.GetClientManager().InstallClientListener(this);

        var em = SharedSystem.GetEventManager();
        em.InstallEventListener(this);
        em.HookEvent("round_start");
        em.HookEvent("round_end");
        em.HookEvent("cs_intermission");

        if (HotReload)
        {
            InitializeForCurrentMap();
        }
    }

    protected override void OnAllModulesLoaded()
    {
        _extendVoteService.NativeVoteManager = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<INativeVoteManager>(INativeVoteManager.ModSharpModuleIdentity)
            .Instance;

        var wuling = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IWuling>(IWuling.Identity)
            .Instance!;
        _mapSelectMenuService = new McsMapSelectMenuService(
            wuling.Menu, wuling.Registry,
            ServiceProvider.GetRequiredService<IMapConfigToolingService>());

        InitializeCooldownPersistence(wuling);

        _eventManager.RegisterListener<IMapVoteEventListener>(this);

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.MapCycle.Commands");
    }

    private void InitializeCooldownPersistence(IWuling wuling)
    {
        var surreal = wuling.Surreal;
        _serverKey = surreal.ServerId;
        _cooldownRepository = new SurrealCooldownRepository(surreal, Logger, Plugin.ModuleDirectory, _serverKey);
        var persistence = _cooldownRepository;

        _ = Task.Run(async () =>
        {
            try
            {
                await persistence.EnsureSchemasAsync();
                _cooldownLifecycleService.SetPersistence(persistence);
                _cooldownCommandService.SetPersistence(persistence);
                Logger.LogInformation("[MapCycle] Cooldown persistence initialized via Wuling SurrealDB (server_key={ServerKey})", _serverKey);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[MapCycle] Failed to initialize cooldown persistence — falling back to in-memory only");
            }
            finally
            {
                _persistenceReady.TrySetResult();
            }
        });
    }

    /// <summary>
    /// Resolves the configured cooldown scope. An empty pattern means
    /// "this server only" and is replaced with the Wuling server key.
    /// </summary>
    private CooldownScopeQuery ResolveCooldownScope()
    {
        var config = _pluginConfigProvider.PluginConfig.CooldownScopeConfig;
        string pattern = string.IsNullOrEmpty(config.ScopePattern) ? _serverKey : config.ScopePattern;
        return new CooldownScopeQuery(config.ScopeMatchMode, pattern);
    }

    protected override void OnUnloadModule()
    {
        TearDownCurrentMap();

        if (_mapTransitionManager is McsMapTransitionManager concreteManager)
            concreteManager.UninstallHook();
        _workshopProvisioningService?.Dispose();
        _cooldownRepository?.Dispose();
        _cooldownRepository = null;
        _eventManager.RemoveListener<IMapVoteEventListener>(this);
        SharedSystem.GetModSharp().RemoveGameListener(this);
        SharedSystem.GetClientManager().RemoveClientListener(this);
        SharedSystem.GetEventManager().RemoveEventListener(this);
    }

    #region IMapVoteEventListener

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params)
    {
        _mapTransitionManager.TrySetNextMap(@params.MapInformation);

        if (_transitionTracker is { IsLimitReachedFired: true })
        {
            _mapTransitionManager.BeginMapTransition(
                Managers.MapTransition.MapTransitionTrigger.TimeLimitReached);
        }
    }

    public void OnMapNotChanged(IMapVoteNotChangedParams @params)
    {
        if (_transitionTracker is null)
            return;

        _transitionTracker.ResetVoteThresholdFlag();
        SharedSystem.GetModSharp().InvokeFrameAction(() => FireTransitions());
    }

    #endregion

    #region IClientListener

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        _extCommandService.RemoveParticipant(client.Slot);
    }

    #endregion

    public void InstallEventListener(IMapCycleEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IMapCycleEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    #region IGameListener

    public void OnGameActivate()
    {
        InitializeForCurrentMap();
    }

    public void OnGameDeactivate()
    {
        TearDownCurrentMap();
    }

    #endregion

    #region IEventListener

    public void FireGameEvent(IGameEvent @event)
    {
        switch (@event.Name)
        {
            case "round_start":
                if (_mode == MapCycleMode.Round)
                    OnRoundStart();
                break;
            case "round_end":
                _mapTransitionManager.OnRoundEnd();
                break;
            case "cs_intermission":
                OnGameIntermission();
                break;
        }
    }

    /// <summary>
    /// The game entered its native end-match flow (timelimit/maxrounds ran
    /// out naturally, or <see cref="IMcsInternalMapTransitionManager.ForceEndMatch"/>
    /// forced it). When a next map is confirmed, take over the transition so
    /// the game's own mapcycle never picks a different map.
    /// </summary>
    private void OnGameIntermission()
    {
        if (_mapTransitionManager.NextMap is null)
            return;

        if (_mapTransitionManager.ChangeMapOnNextRoundEnd || _mapTransitionManager.IsIntermissionFired)
            return;

        _mapTransitionManager.BeginMapTransition(
            Managers.MapTransition.MapTransitionTrigger.GameIntermission);
    }

    #endregion

    #region Lifecycle

    private void InitializeForCurrentMap()
    {
        TearDownCurrentMap();

        _ = Task.Run(async () =>
        {
            await _persistenceReady.Task;
            var scope = ResolveCooldownScope();
            var loaded = await _cooldownLifecycleService.FetchCooldownsFromDatabaseAsync(scope);
            SharedSystem.GetModSharp().InvokeFrameAction(() =>
            {
                if (loaded is var (maps, groups))
                    _cooldownStore.ApplyLoadedRecords(maps, groups, _serverKey);
                _cooldownLifecycleService.DecrementAllCooldowns();
            });
        });

        var currentMapName = SharedSystem.GetModSharp().GetMapName() ?? string.Empty;
        _mapTransitionManager.SetCurrentMap(currentMapName);
        _extendService.InitializeForCurrentMap(_mapTransitionManager.CurrentMap?.MapConfig);
        _extCommandService.ClearParticipants();

        var mapConfig = _mapTransitionManager.CurrentMap?.MapConfig;
        if (mapConfig is not null)
            _mapConfigExecutionService.ExecuteConfigsForMap(mapConfig);

        var mode = ParseMode(_conVars.Mode.GetString());
        var cvm = SharedSystem.GetConVarManager();
        if (mapConfig is not null)
        {
            switch (mode)
            {
                case MapCycleMode.Time:
                    cvm.FindConVar("mp_timelimit")?.Set((float)mapConfig.MapTime);
                    Logger.LogInformation(
                        "[MapCycle] Applied MapTime={MapTime} from map config to mp_timelimit",
                        mapConfig.MapTime);
                    break;
                case MapCycleMode.Round:
                    cvm.FindConVar("mp_maxrounds")?.Set(mapConfig.MapRounds);
                    Logger.LogInformation(
                        "[MapCycle] Applied MapRounds={MapRounds} from map config to mp_maxrounds",
                        mapConfig.MapRounds);
                    break;
            }
        }

        switch (mode)
        {
            case MapCycleMode.Round:
            {
                int maxRounds = cvm.FindConVar("mp_maxrounds")?.GetInt32() ?? 0;
                int roundThreshold = _conVars.VoteStartRoundThreshold.GetInt32();

                // mp_maxrounds=0 would initialize an already-reached limit:
                // instant vote at map start and a permanent "last round"
                // state. Give it headroom past the vote threshold instead.
                if (maxRounds <= 0)
                {
                    maxRounds = roundThreshold + 1;
                    Logger.LogWarning(
                        "[MapCycle] Round mode but mp_maxrounds is 0 — forcing internal round limit to {Rounds}",
                        maxRounds);
                }

                InitializeRoundBasedLimit(maxRounds, roundThreshold);
                _mode = MapCycleMode.Round;
                Logger.LogInformation(
                    "[MapCycle] Round-based mode: maxRounds={Max}, voteStartThreshold={Threshold}",
                    maxRounds, roundThreshold);
                break;
            }
            case MapCycleMode.Time:
            {
                float timeLimitMinutes = cvm.FindConVar("mp_timelimit")?.GetFloat() ?? 0f;
                var timeLimit = TimeSpan.FromMinutes(timeLimitMinutes);
                var voteThreshold = TimeSpan.FromSeconds(_conVars.VoteStartTimeThresholdSeconds.GetInt32());

                // Same guard for time mode: mp_timelimit=0 means "already
                // reached". Leave enough room for the vote to start normally.
                if (timeLimit <= TimeSpan.Zero)
                {
                    timeLimit = voteThreshold + TimeSpan.FromSeconds(60);
                    Logger.LogWarning(
                        "[MapCycle] Time mode but mp_timelimit is 0 — forcing internal time limit to {Limit}",
                        timeLimit);
                }

                InitializeTimeBasedLimit(timeLimit, voteThreshold);
                _mode = MapCycleMode.Time;
                Logger.LogInformation(
                    "[MapCycle] Time-based mode: timeLimit={Limit}, voteStartThreshold={Threshold}",
                    timeLimit, voteThreshold);
                break;
            }
            default:
                _mode = MapCycleMode.None;
                Logger.LogInformation("[MapCycle] Mode=None; skipping TimeLimit init");
                return;
        }

        // 99999f: large enough sentinel that still fits float precision and
        // formats in fixed notation ("99999", not "1e+08") on every platform.
        cvm.FindConVar("mp_timelimit")?.Set(99999f);
        cvm.FindConVar("mp_maxrounds")?.Set(99999999);
        cvm.FindConVar("mp_match_end_changelevel")?.Set(0);

        InstallConVarGuard();

        _tickTimerId = SharedSystem.GetModSharp().PushTimer(
            OnTimerTick,
            1.0,
            GameTimerFlags.Repeatable | GameTimerFlags.StopOnMapEnd);
    }

    private MapCycleMode ParseMode(string raw)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "time":
                return MapCycleMode.Time;
            case "round":
                return MapCycleMode.Round;
            case "none":
            case "":
                return MapCycleMode.None;
            default:
                Logger.LogWarning(
                    "[MapCycle] Unknown mcs_mapcycle_mode value '{Raw}'; falling back to None",
                    raw);
                return MapCycleMode.None;
        }
    }

    private void TearDownCurrentMap()
    {
        if (_tickTimerId != Guid.Empty)
        {
            SharedSystem.GetModSharp().StopTimer(_tickTimerId);
            _tickTimerId = Guid.Empty;
        }

        if (_cooldownLifecycleService is not null && _mapTransitionManager?.CurrentMap?.MapConfig is { } playedMap
            && !_bootPhaseTracker.IsBootPhase)
            _cooldownLifecycleService.ApplyPlayedMapCooldown(playedMap);

        _internalTimeLimitManager = null;
        _transitionTracker = null;
        _mode = MapCycleMode.None;
        _mapTransitionManager?.ClearState();
        _extendService?.ClearState();
        _extCommandService?.ClearParticipants();
        _extendVoteService?.ResetOnMapEnd();
        UninstallConVarGuard();
    }

    #region ConVar Guard

    internal void SuppressConVarGuard() => _conVarGuardSuppressed = true;
    internal void ResumeConVarGuard() => _conVarGuardSuppressed = false;

    private void InstallConVarGuard()
    {
        var cvm = SharedSystem.GetConVarManager();
        _mpTimelimit = cvm.FindConVar("mp_timelimit");
        _mpMaxrounds = cvm.FindConVar("mp_maxrounds");

        if (_mpTimelimit is not null)
            cvm.InstallChangeHook(_mpTimelimit, OnMatchConVarChanged);
        if (_mpMaxrounds is not null)
            cvm.InstallChangeHook(_mpMaxrounds, OnMatchConVarChanged);

        _conVarGuardActive = true;
    }

    private void UninstallConVarGuard()
    {
        if (!_conVarGuardActive)
            return;

        var cvm = SharedSystem.GetConVarManager();
        if (_mpTimelimit is not null)
            cvm.RemoveChangeHook(_mpTimelimit, OnMatchConVarChanged);
        if (_mpMaxrounds is not null)
            cvm.RemoveChangeHook(_mpMaxrounds, OnMatchConVarChanged);

        _conVarGuardActive = false;
    }

    private void OnMatchConVarChanged(IConVar conVar)
    {
        if (_conVarGuardSuppressed)
            return;

        Logger.LogWarning(
            "[MapCycle] External change detected on {Name} — will restore sentinel next frame",
            conVar.Name);

        SharedSystem.GetModSharp().InvokeFrameAction(() =>
        {
            if (!_conVarGuardActive || _conVarGuardSuppressed)
                return;

            _conVarGuardSuppressed = true;
            try
            {
                // ConVarVariantValue is a union: Set(int) on a Float32 ConVar
                // reinterprets the int bits as float (99999999 -> 2.3e-35),
                // so the overload must match the ConVar's type.
                if (conVar.Type is ConVarType.Float32 or ConVarType.Float64)
                    conVar.Set(99999f);
                else
                    conVar.Set(99999999);
                Logger.LogInformation("[MapCycle] Restored {Name} to sentinel value", conVar.Name);
            }
            finally
            {
                _conVarGuardSuppressed = false;
            }
        });
    }

    #endregion

    #endregion

    /// <summary>
    /// Initializes a time-based time limit for the current map.
    /// </summary>
    private void InitializeTimeBasedLimit(TimeSpan timeLimit, TimeSpan voteStartThreshold)
    {
        var clock = new SystemTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(timeLimit, clock);
        _internalTimeLimitManager = manager;

        _transitionTracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= voteStartThreshold,
            isLimitReached: () => manager.IsLimitReached);
    }

    /// <summary>
    /// Initializes a round-based time limit for the current map.
    /// </summary>
    private void InitializeRoundBasedLimit(int roundLimit, int voteStartThreshold)
    {
        var manager = new RoundsTimeLimitManager(roundLimit);
        _internalTimeLimitManager = manager;

        _transitionTracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.RoundsLeft <= voteStartThreshold,
            isLimitReached: () => manager.IsLimitReached);
    }

    /// <summary>
    /// Called every second by the map-scoped timer. Drives time-based limits.
    /// </summary>
    private void OnTimerTick()
    {
        if (_mode != MapCycleMode.Time)
            return;

        _internalTimeLimitManager?.OnTick();
        FireTransitions();
    }

    /// <summary>
    /// Called on round_start. Advances the round-based counter.
    /// </summary>
    private void OnRoundStart()
    {
        if (_mode != MapCycleMode.Round)
            return;
        
        _internalTimeLimitManager?.OnTick();
        FireTransitions();
    }

    /// <summary>
    /// Called after Extend/Set to re-evaluate transition flags.
    /// </summary>
    public void OnTimeLimitChanged()
    {
        _transitionTracker?.ResetFlags();

        // An extend past an already-reached limit means "keep playing" —
        // otherwise the pending round-end transition would fire anyway and
        // the extend would be silently ineffective.
        if (_internalTimeLimitManager is { IsLimitReached: false })
        {
            _mapTransitionManager.ChangeMapOnNextRoundEnd = false;
            _mapTransitionManager.RestoreMatchLimits();
        }
    }

    private WorkshopProvisioningService? CreateWorkshopProvisioningService()
    {
        try
        {
            var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
            string apiKey = configProvider.PluginConfig.GeneralConfig.SteamWebApiKey;
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var apiService = new SteamWorkshopApiService();
            apiService.SetApiKey(apiKey);
            return new WorkshopProvisioningService(apiService);
        }
        catch
        {
            return null;
        }
    }

    private void FireTransitions()
    {
        if (_transitionTracker is null || _internalTimeLimitManager is null)
            return;

        if (IsServerEmptyAndPaused() || _bootPhaseTracker.IsBootPhase)
            return;

        var transitions = _transitionTracker.CheckTransitions();

        foreach (var transition in transitions)
        {
            switch (transition)
            {
                case TimeLimitTransitionState.VoteStartThresholdReached:
                    _eventManager.Fire<IMapCycleEventListener>(
                        l => l.OnVoteStartThresholdReached(
                            new EventManager.Events.MapCycle.VoteStartThresholdReachedParams(
                                Plugin, this, _internalTimeLimitManager.TimeLimitType)));
                    break;

                case TimeLimitTransitionState.LimitReached:
                    _eventManager.Fire<IMapCycleEventListener>(
                        l => l.OnTimeLimitReached(
                            new EventManager.Events.MapCycle.TimeLimitReachedParams(
                                Plugin, this, _internalTimeLimitManager.TimeLimitType)));

                    if (_mapTransitionManager.IsNextMapConfirmed)
                        _mapTransitionManager.BeginMapTransition(
                            Managers.MapTransition.MapTransitionTrigger.TimeLimitReached);
                    break;
            }
        }
    }

    private bool IsServerEmptyAndPaused()
    {
        if (!_pluginConfigProvider.PluginConfig.MapCycleConfig.PauseMapCycleWhenServerEmpty)
            return false;

        return !SharedSystem.GetModSharp().GetIServer().GetGameClients(true)
            .Any(c => !c.IsFakeClient && !c.IsHltv);
    }
}
