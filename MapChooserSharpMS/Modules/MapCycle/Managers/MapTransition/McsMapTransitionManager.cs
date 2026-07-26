using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.WorkshopSync;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.WorkshopManagement;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Hooks;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;
using TnmsPluginFoundation.Utils.Other;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;

internal sealed class McsMapTransitionManager : IMcsInternalMapTransitionManager
{
    private readonly ISharedSystem _sharedSystem;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly ILogger _logger;
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly IInternalEventManager _eventManager;
    private readonly MapCycleConVars _conVars;
    private readonly Func<bool> _shouldStopSourceTv;
    private readonly WorkshopProvisioningService? _workshopProvisioning;
    private Action? _suppressConVarGuard;
    private Action? _resumeConVarGuard;
    private readonly IDetourHook? _goToIntermissionHook;

    private static McsMapTransitionManager? _instance;
    private static unsafe delegate* unmanaged<nint, byte, void> _goToIntermissionTrampoline;

    private IMapInformation? _currentMap;
    private IMapInformation? _nextMap;
    private bool _isNextMapConfirmed;
    private bool _intermissionFired;
    private Guid _retryTimerId;
    private Guid _transitionTimerId;
    private int _changeAttemptsUsed;
    private bool _matchLimitsForced;

    public McsMapTransitionManager(
        ISharedSystem sharedSystem,
        IMcsMapConfigProvider mapConfigProvider,
        ILogger logger,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IInternalEventManager eventManager,
        MapCycleConVars conVars,
        Func<bool> shouldStopSourceTv,
        WorkshopProvisioningService? workshopProvisioning)
    {
        _sharedSystem = sharedSystem;
        _mapConfigProvider = mapConfigProvider;
        _logger = logger;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _eventManager = eventManager;
        _conVars = conVars;
        _shouldStopSourceTv = shouldStopSourceTv;
        _workshopProvisioning = workshopProvisioning;
        _instance = this;

        unsafe
        {
            var server = sharedSystem.GetLibraryModuleManager().Server;
            nint goToIntermissionAddr = server.FindFunction("Going to intermission...\n");
            if (goToIntermissionAddr != nint.Zero)
            {
                logger.LogInformation("[MapTransition] Resolved GoToIntermission at 0x{Addr:X}", goToIntermissionAddr);
                var hookManager = sharedSystem.GetHookManager();
                _goToIntermissionHook = hookManager.CreateDetourHook();
                _goToIntermissionHook.Prepare(goToIntermissionAddr,
                    (nint)(delegate* unmanaged[Cdecl]<nint, byte, void>)&OnGoToIntermissionDetour);
                _goToIntermissionHook.Install();
                _goToIntermissionTrampoline = (delegate* unmanaged<nint, byte, void>)_goToIntermissionHook.Trampoline;
                logger.LogInformation("[MapTransition] GoToIntermission detour installed");
            }
            else
            {
                logger.LogError("[MapTransition] Failed to resolve GoToIntermission — detour not installed");
            }
        }
    }

    public IMapInformation? NextMap => _nextMap;
    public IMapInformation? CurrentMap => _currentMap;
    public bool IsNextMapConfirmed => _isNextMapConfirmed;
    public bool ChangeMapOnNextRoundEnd { get; set; }
    public bool IsIntermissionFired => _intermissionFired;

    #region BeginMapTransition — single entry point

    public void BeginMapTransition(MapTransitionTrigger trigger, float? delayOverride = null)
    {
        if (_intermissionFired || _nextMap is null)
        {
            _logger.LogWarning(
                "[MapTransition] BeginMapTransition({Trigger}) skipped: _intermissionFired={Fired}, _nextMap={HasNext}",
                trigger, _intermissionFired, _nextMap is not null);
            return;
        }

        _logger.LogInformation("[MapTransition] BeginMapTransition triggered by {Trigger}", trigger);

        switch (trigger)
        {
            case MapTransitionTrigger.TimeLimitReached:
            case MapTransitionTrigger.AdminForceEnd:
                HandleEndMatch(delayOverride);
                break;

            case MapTransitionTrigger.RtvImmediate:
                HandleRtvImmediate(delayOverride ?? _conVars.TransitionDelay.GetFloat());
                break;

            case MapTransitionTrigger.RtvRoundEnd:
                HandleDeferred();
                break;

            case MapTransitionTrigger.GameIntermission:
                HandleGameIntermission(delayOverride);
                break;
        }
    }

    private void HandleEndMatch(float? delayOverride)
    {
        if (_conVars.EndMatchImmediately.GetInt32() != 0)
        {
            float delay = delayOverride ?? ResolveIntermissionDelay();
            ForceMatchEnd(delay);
            ShowMapTransitionPanel(delay);
            TransitionToNextMap(delay);
        }
        else
        {
            ChangeMapOnNextRoundEnd = true;
            ForceMatchLimitsForRoundEnd();
            _logger.LogInformation("[MapTransition] Deferred to round end");
        }
    }

    private void HandleRtvImmediate(float delay)
    {
        string mapDisplay = ResolveDisplayName(_nextMap!.MapConfig);
        for (int i = 0; i < 3; i++)
            BroadcastToAll("MapCycle.Broadcast.MapChanging", mapDisplay);

        if (TnmsPluginFoundation.Utils.Entity.GameRulesUtil.IsWarmup())
            _sharedSystem.GetModSharp().ServerCommand("mp_warmup_end");

        ForceMatchEnd(delay);
        ShowMapTransitionPanel(delay);
        TransitionToNextMap(delay);
    }

    private void HandleDeferred()
    {
        float delay = _conVars.TransitionDelay.GetFloat();
        ForceMatchLimitsForRoundEnd();
        ShowMapTransitionPanel(delay);
        TransitionToNextMap(delay);
    }

    private void HandleGameIntermission(float? delayOverride)
    {

        float delay = delayOverride ?? ResolveIntermissionDelay();
        ShowMapTransitionPanel(delay);
        TransitionToNextMap(delay);
    }

    private void ForceMatchEnd(float terminateDelay)
    {
        _suppressConVarGuard?.Invoke();
        var cvm = _sharedSystem.GetConVarManager();
        cvm.FindConVar("mp_timelimit")?.Set(0.01f);
        cvm.FindConVar("mp_maxrounds")?.Set(1);
        _matchLimitsForced = true;
        _resumeConVarGuard?.Invoke();
        _logger.LogInformation("[MapTransition] Set mp_timelimit=0.01, mp_maxrounds=1");

        _logger.LogInformation("[MapTransition] TerminateRound in {Delay}s", terminateDelay);
        TnmsPluginFoundation.Utils.Entity.GameRulesUtil.TerminateRound(terminateDelay, RoundEndReason.RoundDraw);
    }

    private void ForceMatchLimitsForRoundEnd()
    {
        _suppressConVarGuard?.Invoke();
        var cvm = _sharedSystem.GetConVarManager();
        cvm.FindConVar("mp_timelimit")?.Set(0.01f);
        cvm.FindConVar("mp_maxrounds")?.Set(1);
        _matchLimitsForced = true;
        _resumeConVarGuard?.Invoke();
        _logger.LogInformation("[MapTransition] Set mp_timelimit=0.01, mp_maxrounds=1 (deferred)");
    }

    public void RestoreMatchLimits()
    {
        if (!_matchLimitsForced)
            return;

        _suppressConVarGuard?.Invoke();
        var cvm = _sharedSystem.GetConVarManager();
        cvm.FindConVar("mp_timelimit")?.Set(99999f);
        cvm.FindConVar("mp_maxrounds")?.Set(99999999);
        _matchLimitsForced = false;
        _resumeConVarGuard?.Invoke();
        _logger.LogInformation("[MapTransition] Restored mp_timelimit=99999, mp_maxrounds=99999999");
    }

    private void FireIntermissionEvent()
    {
        var intermissionParams = new McsIntermissionParams(_plugin, _moduleBase, _nextMap!.MapConfig);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));
    }

    private float ResolveIntermissionDelay()
    {
        var cvm = _sharedSystem.GetConVarManager();
        float voteTime = cvm.FindConVar("mp_endmatch_votenextleveltime")?.GetFloat() ?? 10.0f;
        float restartDelay = cvm.FindConVar("mp_match_restart_delay")?.GetFloat() ?? 5.0f;
        return Math.Max(Math.Max(voteTime, restartDelay) - 3.0f, 0f);
    }

    #endregion

    #region OnRoundEnd — handles deferred transitions

    public void OnRoundEnd()
    {
        if (!ChangeMapOnNextRoundEnd || _nextMap is null || _intermissionFired)
            return;

        BeginMapTransition(MapTransitionTrigger.RtvRoundEnd);
    }

    #endregion

    #region TrySetNextMap

    public bool TrySetNextMap(IMapInformation mapInformation)
    {
        var oldNextMap = _nextMap;
        _nextMap = mapInformation;
        _isNextMapConfirmed = true;

        if (oldNextMap?.MapConfig is not { } oldConfig
            || !ReferenceEquals(oldConfig, mapInformation.MapConfig))
        {
            var confirmedParams = new NextMapConfirmedParams(
                _plugin, _moduleBase, mapInformation.MapConfig, oldNextMap?.MapConfig);
            _eventManager.Fire<IMapCycleEventListener>(e => e.OnNextMapConfirmed(confirmedParams));
        }

        return true;
    }

    public bool TrySetNextMap(IMapConfig mapConfig)
        => TrySetNextMap(MapInformation.For(mapConfig).Build());

    public bool TrySetNextMap(string mapName)
    {
        if (!_mapConfigProvider.TryGetMapConfig(mapName, out var found))
            return false;
        return TrySetNextMap(found);
    }

    public async Task<(bool Success, IWorkshopFetchResult FetchResult)> TrySetNextMap(long workshopId)
    {
        if (_mapConfigProvider.TryGetMapConfig(workshopId, out var found))
        {
            var cachedConfig = found;
            var tcs = new TaskCompletionSource<(bool, IWorkshopFetchResult)>();
            _sharedSystem.GetModSharp().InvokeFrameAction(() =>
            {
                try
                {
                    TrySetNextMap(cachedConfig);
                    IWorkshopFetchResult hit = new WorkshopFetchResult
                    {
                        ExistenceStatus = ExistenceStatus.FoundInMemoryConfig,
                        MapName = cachedConfig.MapName,
                        WorkshopId = workshopId,
                    };
                    tcs.SetResult((true, hit));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return await tcs.Task;
        }

        if (_workshopProvisioning is null || !_workshopProvisioning.IsAvailable)
        {
            IWorkshopFetchResult noApi = new WorkshopFetchResult
            {
                ExistenceStatus = ExistenceStatus.FailedToFetchUnknown,
                MapName = null,
                WorkshopId = workshopId,
            };
            return (false, noApi);
        }

        try
        {
            var provision = await _workshopProvisioning.TryProvisionAsync(workshopId);

            if (provision.MapConfig is null)
            {
                _logger.LogWarning("Workshop item {Id} is not available (status: {Status})", workshopId, provision.Status);
                IWorkshopFetchResult unavailable = new WorkshopFetchResult
                {
                    ExistenceStatus = provision.Status,
                    MapName = provision.Title,
                    WorkshopId = workshopId,
                };
                return (false, unavailable);
            }

            var tcs = new TaskCompletionSource<(bool, IWorkshopFetchResult)>();
            _sharedSystem.GetModSharp().InvokeFrameAction(() =>
            {
                try
                {
                    TrySetNextMap(provision.MapConfig);
                    _logger.LogInformation("Set next map from workshop: {Title} (ID: {Id})", provision.Title, workshopId);

                    IWorkshopFetchResult workshopHit = new WorkshopFetchResult
                    {
                        ExistenceStatus = ExistenceStatus.FoundInWorkshop,
                        MapName = provision.MapConfig.MapName,
                        WorkshopId = workshopId,
                    };
                    tcs.SetResult((true, workshopHit));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch workshop item {Id}", workshopId);
            IWorkshopFetchResult error = new WorkshopFetchResult
            {
                ExistenceStatus = ExistenceStatus.FailedToFetchHttpError,
                MapName = null,
                WorkshopId = workshopId,
            };
            return (false, error);
        }
    }

    public bool TryRemoveNextMap()
    {
        if (_nextMap is null)
            return false;

        var previousNextMap = _nextMap;
        _nextMap = null;
        _isNextMapConfirmed = false;

        var removedParams = new NextMapRemovedParams(_plugin, _moduleBase, previousNextMap.MapConfig);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnNextMapRemoved(removedParams));

        return true;
    }

    #endregion

    #region Map transition execution

    public void TransitionToNextMap(float seconds)
    {
        if (_nextMap is null)
            return;

        var targetConfig = _nextMap.MapConfig;

        void ChangeNow()
        {
            _changeAttemptsUsed = 1;
            IssueMapChange(targetConfig);
            ArmEarlyTransitionNotice(targetConfig);
            ArmTransitionRetryWatchdog(targetConfig);
        }

        if (seconds <= 0f)
        {
            ChangeNow();
            return;
        }

        _transitionTimerId = _sharedSystem.GetModSharp().PushTimer(
            ChangeNow, seconds, GameTimerFlags.StopOnMapEnd);
    }

    private void IssueMapChange(IMapConfig target)
    {
        if (_shouldStopSourceTv())
            _sharedSystem.GetModSharp().ServerCommand("tv_stoprecord");

        if (target.WorkshopId > 0)
        {
            MapUtil.ChangeToWorkshopMap(target.WorkshopId);
            return;
        }

        MapUtil.ChangeMap(target.MapName);
    }

    #endregion

    #region Lifecycle

    public void SetCurrentMap(string mapName)
    {
        string? addonIds = _sharedSystem.GetModSharp().GetAddonName();
        if (addonIds is not null)
        {
            foreach (string segment in addonIds.Split(','))
            {
                if (long.TryParse(segment.Trim(), out long addonId) && addonId > 0
                    && _mapConfigProvider.TryGetMapConfig(addonId, out var byWorkshop))
                {
                    _logger.LogInformation(
                        "[MapTransition] Matched current map by addon ID {AddonId} -> '{ConfigMapName}'",
                        addonId, byWorkshop.MapName);
                    _currentMap = MapInformation.For(byWorkshop).Build();
                    return;
                }
            }
        }

        if (_mapConfigProvider.TryGetMapConfig(mapName, out var found))
        {
            _currentMap = MapInformation.For(found).Build();
            return;
        }

        _logger.LogInformation(
            "[MapTransition] No map config found for current map '{MapName}'; CurrentMap is null",
            mapName);
        _currentMap = null;
    }

    public void SetConVarGuardCallbacks(Action suppress, Action resume)
    {
        _suppressConVarGuard = suppress;
        _resumeConVarGuard = resume;
    }

    public void ClearState()
    {
        _logger.LogInformation(
            "[MapTransition] ClearState: _intermissionFired was {Fired}, ChangeMapOnNextRoundEnd was {RoundEnd}",
            _intermissionFired, ChangeMapOnNextRoundEnd);
        _currentMap = null;
        _nextMap = null;
        _isNextMapConfirmed = false;
        _intermissionFired = false;
        ChangeMapOnNextRoundEnd = false;
        _matchLimitsForced = false;
        StopTransitionTimer();
        StopRetryWatchdog(resetAttempts: true);
    }

    #endregion

    #region GoToIntermission detour

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe void OnGoToIntermissionDetour(nint gameRules, byte bAbortedMatch)
    {
        var self = _instance;
        if (self is null)
        {
            _goToIntermissionTrampoline(gameRules, bAbortedMatch);
            return;
        }

        if (self._intermissionFired)
        {
            self._logger.LogInformation("[MapTransition] GoToIntermission blocked (already fired)");
            return;
        }

        self._intermissionFired = true;
        self._logger.LogInformation("[MapTransition] GoToIntermission allowed (first call)");

        if (self._nextMap is not null)
            self.FireIntermissionEvent();

        _goToIntermissionTrampoline(gameRules, bAbortedMatch);
    }

    public void UninstallHook()
    {
        _goToIntermissionHook?.Uninstall();
        _goToIntermissionHook?.Dispose();
        if (_instance == this)
            _instance = null;
    }

    #endregion

    #region Internal helpers

    private void ArmEarlyTransitionNotice(IMapConfig target)
    {
        _sharedSystem.GetModSharp().PushTimer(() =>
        {
            BroadcastToAll("MapCycle.Broadcast.MapTransitionPending", ResolveDisplayName(target));
        }, 5.0, GameTimerFlags.StopOnMapEnd);
    }

    private void ArmTransitionRetryWatchdog(IMapConfig target)
    {
        StopRetryWatchdog(resetAttempts: false);

        float interval = _conVars.TransitionRetryInterval.GetFloat();
        _retryTimerId = _sharedSystem.GetModSharp().PushTimer(() =>
        {
            int maxAttempts = _conVars.TransitionRetryAttempts.GetInt32();

            if (_changeAttemptsUsed < maxAttempts)
            {
                _changeAttemptsUsed++;
                _logger.LogWarning(
                    "[MapTransition] Map change to {Map} did not happen — retrying ({Attempt}/{Max})",
                    target.MapName, _changeAttemptsUsed, maxAttempts);
                BroadcastToAll("MapCycle.Broadcast.MapTransitionRetry",
                    ResolveDisplayName(target), _changeAttemptsUsed, maxAttempts);
                IssueMapChange(target);
                return;
            }

            StopRetryWatchdog(resetAttempts: true);

            string fallbackMap = _conVars.TransitionFallbackMap.GetString();
            _logger.LogError(
                "[MapTransition] Map change to {Map} failed after {Max} attempts — falling back to {Fallback}",
                target.MapName, maxAttempts, fallbackMap);
            BroadcastToAll("MapCycle.Broadcast.MapTransitionFallbackMap",
                ResolveDisplayName(target), fallbackMap);
            MapUtil.ChangeMap(fallbackMap);
        }, interval, GameTimerFlags.Repeatable | GameTimerFlags.StopOnMapEnd);
    }

    private void StopTransitionTimer()
    {
        if (_transitionTimerId != Guid.Empty)
        {
            _sharedSystem.GetModSharp().StopTimer(_transitionTimerId);
            _transitionTimerId = Guid.Empty;
        }
    }

    private void StopRetryWatchdog(bool resetAttempts)
    {
        if (_retryTimerId != Guid.Empty)
        {
            _sharedSystem.GetModSharp().StopTimer(_retryTimerId);
            _retryTimerId = Guid.Empty;
        }

        if (resetAttempts)
            _changeAttemptsUsed = 0;
    }

    private void ShowMapTransitionPanel(float duration)
    {
        if (_nextMap is null)
            return;

        string mapDisplay = ResolveDisplayName(_nextMap.MapConfig);
        var em = _sharedSystem.GetEventManager();
        var clients = _sharedSystem.GetModSharp().GetIServer().GetGameClients(true);

        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            var controller = client.GetPlayerController();
            if (controller is null)
                continue;

            string html = _plugin.LocalizeStringForPlayer(
                client, "MapCycle.Panel.MapTransition", mapDisplay);

            var gameEvent = em.CreateEvent("cs_win_panel_round", true);
            if (gameEvent is null)
                continue;

            gameEvent.SetBool("show_timer_defend", false);
            gameEvent.SetBool("show_timer_attack", false);
            gameEvent.SetInt("timer_time", -1);
            gameEvent.SetInt("final_event", -1);
            gameEvent.SetPlayer("funfact_player", controller);
            gameEvent.SetString("funfact_token", html);
            gameEvent.FireToClient(client);
            gameEvent.Dispose();
        }

        if (duration > 0f)
        {
            _sharedSystem.GetModSharp().PushTimer(
                CleanupTransitionPanel, duration, GameTimerFlags.StopOnMapEnd);
        }
    }

    private void CleanupTransitionPanel()
    {
        var em = _sharedSystem.GetEventManager();
        var clients = _sharedSystem.GetModSharp().GetIServer().GetGameClients(true);

        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            var gameEvent = em.CreateEvent("round_freeze_end", true);
            if (gameEvent is null)
                continue;

            gameEvent.FireToClient(client);
            gameEvent.Dispose();
        }
    }

    private string ResolveDisplayName(IMapConfig map)
        => _mapConfigProvider.ToolingService.ResolveMapDisplayName(map);

    private void BroadcastToAll(string key, params object[] args)
    {
        var clients = _sharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            client.GetPlayerController()?.PrintToChat(
                $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, key, args)}");
        }
    }

    #endregion
}
