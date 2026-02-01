using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Managers;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Modules.Nomination.Services;
using MapChooserSharpMS.Modules.Ui.Menu;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation.Models.Plugin;
using TnmsPluginFoundation.Utils.Entity;

namespace MapChooserSharpMS.Modules.Nomination;

internal sealed class McsNominationController(IServiceProvider serviceProvider, bool hotReload) : PluginModuleBase(serviceProvider, hotReload), IMcsInternalNominationController, IRockTheVoteEventListener
{
    public override string PluginModuleName => "McsMapNominationController";
    public override string ModuleChatPrefix => "Prefix.Nomination";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public INominationValidateService NominationValidateService { get; private set; } = null!;
    
    private IMcsInternalNominationManager _internalNominationManager = null!;
    
    private readonly Dictionary<int, IMcsNominationUserInterface> _mcsActiveUserNominationMenu = new();

    public IReadOnlyDictionary<string, IMcsNominationData> GetNominatedMaps() => NominatedMaps;
    
    private IInternalEventManager _eventManager = null!;

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsInternalNominationController>(this);
        services.AddSingleton<IMcsInternalNominationManager>(_internalNominationManager);
    }

    protected override void OnInitialize()
    {
        _internalNominationManager = ActivatorUtilities.CreateInstance<InternalNominationManager>(ServiceProvider);
        
    }

    protected override void OnAllModulesLoaded()
    {
        NominationValidateService = ActivatorUtilities.CreateInstance<NominationValidateService>(ServiceProvider, this);
        
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        NominationValidateService = new NominationValidateService(ServiceProvider, _internalNominationManager, _eventManager, this);
        
        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
    }

    protected override void OnUnloadModule()
    {
    }

    private void OnMapStart(string mapName)
    {
        ResetNominations();
    }


    private void OnClientDisconnect(int slot)
    {
        foreach (IMcsNominationData data in NominatedMaps.Values)
        {
            data.NominationParticipants.Remove(slot);
        }
    }
    

    public bool TryNominateMap(IGameClient client, IMapConfig mapConfig)
    {
        CCSPlayerController player = client.GetPlayerController();
        NominationCheck check = PlayerCanNominateMap(client, mapConfig);

        bool processed = ProcessNominationCheckResult(player, mapConfig, check);

        if (!processed)
            return false;

        if (!NominatedMaps.TryGetValue(mapConfig.MapName, out var nominated))
        {
            nominated = new McsNominationData(mapConfig);
        }

        INominationParams nominationParam = new NominationParams(ServiceProvider, client, nominated);
        
        var shouldCancel = _eventManager.FireCancellable<INominationEventListener>(l => l.OnNomination(nominationParam));

        if (shouldCancel)
        {
            DebugLogger.LogInformation("Nomination begin event cancelled by a another plugin.");
            return false;
        }

        // When this nomination is first nomination of the map
        // It will require to store, and takes no effect if already existed
        NominatedMaps[mapConfig.MapName] = nominated;

        bool isFirstNomination = true;
        foreach (var (key, value) in NominatedMaps)
        {
            if (value.NominationParticipants.Contains(player.Slot))
            {
                // TODO() This can be improved, because Remove method is returns boolean that indicates player is removed or not from list.
                value.NominationParticipants.Remove(player.Slot);

                // If there is no nomination participants left, remove the nomination
                if (value.NominationParticipants.Count == 0)
                {
                    NominatedMaps.Remove(key);
                }

                isFirstNomination = false;
                // break because player should be in 1 nomination, not multiple nomination.
                break;
            }
        }

        nominated.NominationParticipants.Add(player.Slot);

        PrintNominationResult(player, mapConfig, isFirstNomination);

        if (isFirstNomination)
        {
            var eventNominated = new McsMapNominatedEvent(player, nominated, GetTextWithModulePrefix(null, ""));
            _mcsEventManager.FireEventNoResult(eventNominated);
        }
        else
        {
            var eventNominationChanged = new McsMapNominationChangedEvent(player, nominated, GetTextWithModulePrefix(null, ""));
            _mcsEventManager.FireEventNoResult(eventNominationChanged);
        }

        return true;
    }

    public bool TryAdminNominateMap(IGameClient? client, IMapConfig mapConfig)
    {
        CCSPlayerController? player = client?.GetPlayerController();
        if (mapConfig.NominationConfig.ProhibitAdminNomination && player != null)
        {
            player.PrintToChat(LocalizeWithModulePrefix(player, "NominationAddMap.Command.Notification.AdminNominationProhibited"));
            return false;
        }

        bool isFirstNomination = NominatedMaps.TryGetValue(mapConfig.MapName, out var nominated);

        if (!isFirstNomination || nominated == null)
        {
            nominated = new McsNominationData(mapConfig);
        }

        // When this nomination is first nomination of the map
        // It will require to store, and takes no effect if already existed
        NominatedMaps[mapConfig.MapName] = nominated;

        nominated.IsForceNominated = true;

        var adminNominateEvent = new McsMapAdminNominatedEvent(player, nominated, GetTextWithModulePrefix(null, ""));
        _mcsEventManager.FireEventNoResult(adminNominateEvent);


        string executorName = PlayerUtil.GetPlayerName(player);

        if (isFirstNomination)
        {
            PrintLocalizedChatToAllWithModulePrefix("Nomination.Broadcast.Admin.ChangedToAdminNomination", executorName, _mcsInternalMapConfigProviderApi.GetMapName(mapConfig));
        }
        else
        {
            PrintLocalizedChatToAllWithModulePrefix("Nomination.Broadcast.Admin.Nominated", executorName, _mcsInternalMapConfigProviderApi.GetMapName(mapConfig));
        }
        Logger.LogInformation($"Admin {executorName} is inserted {mapConfig.MapName} to nomination.");

        return true;
    }

    /// <summary>
    /// Show nomination menu to player, this method is show maps in given config list
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="configs">Map configs to show</param>
    public void ShowNominationMenu(IGameClient client, List<IMapConfig> configs)
    {
        if(configs.Count == 0)
            return;

        CCSPlayerController player = client.GetPlayerController();
        var ui = _mcsNominationMenuProvider.CreateNewNominationUi(player);

        List<IMcsNominationMenuOption> menuOptions = new();

        foreach (IMapConfig config in configs)
        {
            // TODO() More menu disablation check
            bool isMenuDisabled = config.IsDisabled;

            menuOptions.Add(new McsNominationMenuOption(new McsNominationOption(config, false), OnPlayerCastNominationMenu, isMenuDisabled));
        }

        ui.SetNominationOption(menuOptions);
        ui.SetMenuOption(new McsGeneralMenuOption("Nomination.Menu.MenuTitle", true));
        ui.OpenMenu();
        _mcsActiveUserNominationMenu[player.Slot] = ui;
    }

    /// <summary>
    /// Show nomination menu to player, this method is shows all maps in map config provider
    /// </summary>
    /// <param name="client">Client</param>
    public void ShowNominationMenu(IGameClient client)
    {
        ShowNominationMenu(client, _mcsInternalMapConfigProviderApi.GetMapConfigs().Select(kv => kv.Value).ToList());
    }

    /// <summary>
    /// Show admin nomination menu to player, this method is show maps in given config list
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="configs">Map configs to show</param>
    public void ShowAdminNominationMenu(IGameClient client, List<IMapConfig> configs)
    {
        if(configs.Count == 0)
            return;

        CCSPlayerController player = client.GetPlayerController();
        var ui = _mcsNominationMenuProvider.CreateNewNominationUi(player);

        List<IMcsNominationMenuOption> menuOptions = new();

        foreach (IMapConfig config in configs)
        {
            // TODO() More menu disablation check
            bool isMenuDisabled = config.IsDisabled;

            menuOptions.Add(new McsNominationMenuOption(new McsNominationOption(config, true), OnPlayerCastNominationMenu, isMenuDisabled));
        }

        ui.SetNominationOption(menuOptions);
        ui.SetMenuOption(new McsGeneralMenuOption("Nomination.Menu.MenuTitle", true));
        ui.OpenMenu();
        _mcsActiveUserNominationMenu[player.Slot] = ui;
    }

    /// <summary>
    /// Show admin nomination menu to player, this method is shows all maps in map config provider
    /// </summary>
    /// <param name="client">Client</param>
    public void ShowAdminNominationMenu(IGameClient client)
    {
        ShowAdminNominationMenu(client, _mcsInternalMapConfigProviderApi.GetMapConfigs().Select(kv => kv.Value).ToList());
    }
    
    private void OnPlayerCastNominationMenu(CCSPlayerController client, IMcsNominationOption option)
    {
        if (option.IsAdminNomination)
        {
            AdminNominateMapInternal(client, option.MapConfig);
        }
        else
        {
            NominateMapInternal(client, option.MapConfig);
        }

        if (_mcsActiveUserNominationMenu.TryGetValue(client.Slot, out var ui))
        {
            ui.CloseMenu();
            _mcsActiveUserNominationMenu.Remove(client.Slot);
        }
    }
    
    
    
    public void ShowRemoveNominationMenu(IGameClient client, List<IMcsNominationData> nominationData)
    {
        if (nominationData.Count == 0)
            return;
        
        var ui = _mcsNominationMenuProvider.CreateNewNominationUi(client);

        List<IMcsNominationMenuOption> menuOptions = new();
        
        foreach (IMcsNominationData data in nominationData)
        {
            menuOptions.Add(new McsNominationMenuOption(new McsNominationOption(data.MapConfig), OnPlayerCastRemoveNominationMenu, false));
        }
        
        ui.SetNominationOption(menuOptions);
        ui.SetMenuOption(new McsGeneralMenuOption("NominationRemoveMap.Menu.MenuTitle", true));
        ui.OpenMenu();
        _mcsActiveUserNominationMenu[client.Slot] = ui;
    }
    
    public void ShowRemoveNominationMenu(IGameClient player)
    {
        ShowRemoveNominationMenu(player, NominatedMaps.Select(kv => kv.Value).ToList());
    }
    
    private void OnPlayerCastRemoveNominationMenu(IGameClient client, IMcsNominationOption option)
    {
        client.ExecuteClientCommandFromServer($"css_nominate_removemap {option.MapConfig.MapName}");

        if (_mcsActiveUserNominationMenu.TryGetValue(client.Slot, out var ui))
        {
            ui.CloseMenu();
            _mcsActiveUserNominationMenu.Remove(client.Slot);
        }
    }


    public void RemoveNomination(IGameClient? player, IMapConfig mapConfig)
    {
        if (!NominatedMaps.Remove(mapConfig.MapName))
            return;
        
        string executorName = PlayerUtil.GetPlayerName(player);
        PrintLocalizedChatToAllWithModulePrefix("Nomination.Broadcast.Admin.RemovedNomiantion", executorName, _mcsInternalMapConfigProviderApi.GetMapName(mapConfig));
        Logger.LogInformation($"Admin {executorName} is removed {mapConfig.MapName} from nomination.");
    }

    private void PrintNominationResult(IGameClient player, IMapConfig mapConfig, bool isFirstNomination)
    {
        string executorName = player.PlayerName;

        if (isFirstNomination)
        {
            PrintLocalizedChatToAllWithModulePrefix("Nomination.Broadcast.Nominated", executorName, _mcsInternalMapConfigProviderApi.GetMapName(mapConfig));
        }
        else
        {
            PrintLocalizedChatToAllWithModulePrefix("Nomination.Broadcast.NominationChanged", executorName, _mcsInternalMapConfigProviderApi.GetMapName(mapConfig));
        }
    }

    private bool ProcessNominationCheckResult(IGameClient player, IMapConfig mapConfig, NominationCheckResult check)
    {
        // Success - no flags set
        if (check == NominationCheckResult.None)
            return true;

        // Process each flag and notify player
        int playerCountCurrently = Utilities.GetPlayers().Select(p => p is { IsBot: false, IsHLTV: false }).Count();

        if (check.HasFlag(NominationCheckResult.Disabled))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapDisabled", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.HasFlag(NominationCheckResult.NotEnoughPermissions))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPermission", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.HasFlag(NominationCheckResult.TooMuchPlayers))
        {
            int maxPlayers = mapConfig.NominationConfig.MaxPlayers;
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.TooMuchPlayers", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig), playerCountCurrently, maxPlayers));
        }

        if (check.HasFlag(NominationCheckResult.NotEnoughPlayers))
        {
            int minPlayers = mapConfig.NominationConfig.MinPlayers;
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPlayers", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig), playerCountCurrently, minPlayers));
        }

        if (check.HasFlag(NominationCheckResult.RestrictedToCertainUser))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotAllowed", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.HasFlag(NominationCheckResult.BlockedBySteamId))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotAllowed", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.HasFlag(NominationCheckResult.VotingPeriod))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.DisableAtThisTime"));

        if (check.HasFlag(NominationCheckResult.OnlySpecificDay))
        {
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificDay", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));
            player.PrintToChat(GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificDay.Days", string.Join(", ", mapConfig.NominationConfig.DaysAllowed))));
        }

        if (check.HasFlag(NominationCheckResult.OnlySpecificTime))
        {
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificTime", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));
            player.PrintToChat(GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificTime.Times", string.Join(", ", mapConfig.NominationConfig.AllowedTimeRanges))));
        }

        if (check.HasFlag(NominationCheckResult.MapIsInCooldown))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapIsInCooldown", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig), GetHighestCooldown(mapConfig)));

        if (check.HasFlag(NominationCheckResult.AlreadyNominated))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedSameMap", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.HasFlag(NominationCheckResult.NominatedByAdmin))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedByAdmin", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.HasFlag(NominationCheckResult.SameMap))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.SameMap"));

        if (check.HasFlag(NominationCheckResult.GroupNominationLimitReached))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.GroupLimitReached", PerGroupNominationLimit.Value));

        if (check.HasFlag(NominationCheckResult.CancelledByExternalPlugin))
            player.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.Generic.WithMapName", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        return false;
    }
    
    private void ResetNominations()
    {
        NominatedMaps.Clear();
    }
}