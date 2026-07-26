using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Managers;
using MapChooserSharpMS.Modules.Nomination.Services;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;
using Wuling.Abstract;

namespace MapChooserSharpMS.Modules.Nomination;

internal sealed class McsNominationController(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload),
      IMcsInternalNominationController,
      IRockTheVoteEventListener,
      IMapVoteEventListener,
      IClientListener,
      Sharp.Shared.Listeners.IGameListener
{
    public override string PluginModuleName => "McsMapNominationController";
    public override string ModuleChatPrefix => "Prefix.Nomination";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public int ListenerVersion => 1;
    public int ListenerPriority => 999999;

    public IMapNominationService NominationService { get; private set; } = null!;
    public INominationValidateService NominationValidateService { get; private set; } = null!;
    public INominationMenuManagementService NominationMenuManagementService { get; private set; } = null!;
    public INominationManager NominationManager => _internalNominationManager;

    public void InstallEventListener(INominationEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(INominationEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    private IMcsInternalNominationManager _internalNominationManager = null!;
    private IInternalEventManager _eventManager = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMapConfigToolingService _mapConfigToolingService = null!;
    private NominationConVars _conVars = null!;
    private PlayerNominationCooldownService _playerCooldownService = null!;

    public IReadOnlyDictionary<string, IMcsNominationData> GetNominatedMaps() => _internalNominationManager.NominatedMaps;

    public override void RegisterServices(IServiceCollection services)
    {
        // IMcsInternalNominationController : IMcsNominationController — the
        // public API is wired by the plugin entrypoint (MapChooserSharpMs.cs),
        // which resolves this instance and casts to IMcsNominationController
        // when building the shared API object.
        services.AddSingleton<IMcsInternalNominationController>(this);
        // NominationManager is created during OnInitialize; register a factory
        // so other modules resolving IMcsInternalNominationManager via DI
        // still get the same instance.
        services.AddSingleton<IMcsInternalNominationManager>(_ => _internalNominationManager);
        services.AddSingleton<INominationManager>(_ => _internalNominationManager);
        services.AddSingleton<INominationValidateService>(_ => NominationValidateService);
        services.AddSingleton<IMapNominationService>(_ => NominationService);
    }

    protected override void OnInitialize()
    {
        _internalNominationManager = ActivatorUtilities.CreateInstance<InternalNominationManager>(ServiceProvider);
        _conVars = new NominationConVars(SharedSystem.GetConVarManager());
        // Must be ready before OnAllModulesLoaded: the shared API is registered
        // at PostInit, so external modules may call InstallEventListener before
        // our own OnAllModulesLoaded has run.
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
    }

    protected override void OnAllModulesLoaded()
    {
        _mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _mapConfigToolingService = ServiceProvider.GetRequiredService<IMapConfigToolingService>();

        InitializePlayerCooldownService();

        NominationValidateService = ActivatorUtilities.CreateInstance<NominationValidateService>(ServiceProvider, this, _playerCooldownService, _conVars);
        NominationService = ActivatorUtilities.CreateInstance<MapNominationService>(ServiceProvider, this, NominationValidateService, _conVars, _playerCooldownService);

        var cooldownQueryService = ServiceProvider.GetRequiredService<IMapCooldownQueryService>();
        NominationMenuManagementService = new NominationMenuManagementService(
            () => ((MapChooserSharpMs)Plugin).NominationMenuCompat,
            _mapConfigProvider,
            _internalNominationManager,
            NominationService,
            _mapConfigToolingService,
            cooldownQueryService,
            NotifyNominationFailure,
            Plugin,
            this,
            _eventManager);

        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
        _eventManager.RegisterListener<IMapVoteEventListener>(this);
        SharedSystem.GetClientManager().InstallClientListener(this);
        SharedSystem.GetModSharp().InstallGameListener(this);

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.Nomination.Commands");
    }

    protected override void OnUnloadModule()
    {
        _eventManager.RemoveListener<IRockTheVoteEventListener>(this);
        _eventManager.RemoveListener<IMapVoteEventListener>(this);
        SharedSystem.GetClientManager().RemoveClientListener(this);
        SharedSystem.GetModSharp().RemoveGameListener(this);
    }

    public void OnGameActivate()
    {
        NominationService.ClearNominations();
        _playerCooldownService?.DecrementAll();
    }

    private void InitializePlayerCooldownService()
    {
        var surreal = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IWuling>(IWuling.Identity)
            .Instance!.Surreal;

        _playerCooldownService = new PlayerNominationCooldownService(Logger, surreal.ServerId, surreal);

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try { await _playerCooldownService.LoadFromDbAsync(); }
            catch (Exception ex) { Logger.LogWarning(ex, "[PlayerNomCD] Failed to load from DB"); }
        });
    }

    public void OnMapVoteFinished(IMapVoteFinishedEventParams @params)
    {
        NominationService.ClearNominations();
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        NominationService.TryUnNominate(client, UnNominateReason.PlayerDisconnect);
    }

    public void NotifyNominationFailure(IGameClient? player, IMapConfig mapConfig, IReadOnlyList<NominationCheckResult> check)
    {
        // Success - empty list
        if (check.Count == 0)
            return;

        string mapDisplay = _mapConfigToolingService.ResolveMapDisplayName(mapConfig);

        // Only the first (highest-priority) failure is reported — printing
        // every reason floods the chat when several checks fail at once.
        switch (check[0])
        {
            case NominationCheckResult.Disabled:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapDisabled", mapDisplay));
                break;

            case NominationCheckResult.NotEnoughPermissions:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPermission", mapDisplay));
                break;

            case NominationCheckResult.TooMuchPlayers:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.TooMuchPlayers", mapDisplay,
                    SharedSystem.GetClientManager().GetClientCount(), mapConfig.NominationConfig.MaxPlayers));
                break;

            case NominationCheckResult.NotEnoughPlayers:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPlayers", mapDisplay,
                    SharedSystem.GetClientManager().GetClientCount(), mapConfig.NominationConfig.MinPlayers));
                break;

            case NominationCheckResult.VotingPeriod:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.DisableAtThisTime"));
                break;

            case NominationCheckResult.OnlySpecificDay:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificDay", mapDisplay));
                PrintMessageToServerOrPlayerChat(player, GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificDay.Days", string.Join(", ", mapConfig.NominationConfig.DaysAllowed))));
                break;

            case NominationCheckResult.OnlySpecificTime:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificTime", mapDisplay));
                PrintMessageToServerOrPlayerChat(player, GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificTime.Times", string.Join(", ", mapConfig.NominationConfig.AllowedTimeRanges))));
                break;

            case NominationCheckResult.MapIsInCooldown:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapIsInCooldown", mapDisplay, _mapConfigToolingService.GetHighestCooldown(mapConfig)));
                break;

            case NominationCheckResult.NominationCooldownActive:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NominationCooldownActive", mapDisplay));
                break;

            case NominationCheckResult.AlreadyNominated:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedSameMap", mapDisplay));
                break;

            case NominationCheckResult.NominatedByAdmin:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedByAdmin", mapDisplay));
                break;

            case NominationCheckResult.SameMap:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.SameMap"));
                break;

            case NominationCheckResult.GroupNominationLimitReached:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.GroupLimitReached"));
                break;

            case NominationCheckResult.CancelledByExternalPlugin:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.Generic.WithMapName", mapDisplay));
                break;

            case NominationCheckResult.ProhibitAdminNomination:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.ProhibitAdminNomination", mapDisplay));
                break;

            case NominationCheckResult.PlayerCooldownActive:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.PlayerCooldownActive"));
                break;

            case NominationCheckResult.SameMapGroup:
                PrintMessageToServerOrPlayerChat(player, LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.SameMapGroup", mapDisplay));
                break;
        }
    }

    public void BroadcastNomination(IGameClient nominator, IMapConfig mapConfig, bool isNominationChanged)
    {
        if (_conVars.BroadcastEnabled.GetInt32() == 0)
            return;

        string mapDisplay = _mapConfigToolingService.ResolveMapDisplayName(mapConfig);

        PrintLocalizedChatToAllWithModulePrefix(
            isNominationChanged
                ? "Nomination.Broadcast.NominationChanged"
                : "Nomination.Broadcast.Nominated",
            nominator.Name, mapDisplay);
    }

    public void BroadcastAdminNomination(IGameClient? executor, IMapConfig mapConfig, bool changedExistingToAdmin)
    {
        string mapDisplay = _mapConfigToolingService.ResolveMapDisplayName(mapConfig);

        PrintLocalizedChatToAllWithModulePrefix(
            changedExistingToAdmin
                ? "Nomination.Broadcast.Admin.ChangedToAdminNomination"
                : "Nomination.Broadcast.Admin.Nominated",
            executor?.Name ?? "Console", mapDisplay);
    }

    public void BroadcastNominationRemoved(IGameClient? executor, IMapConfig mapConfig)
    {
        string mapDisplay = _mapConfigToolingService.ResolveMapDisplayName(mapConfig);

        PrintLocalizedChatToAllWithModulePrefix(
            "Nomination.Broadcast.Admin.RemovedNomination",
            executor?.Name ?? "Console", mapDisplay);
    }

}
