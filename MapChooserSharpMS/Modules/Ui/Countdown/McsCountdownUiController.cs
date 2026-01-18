using System;
using System.Collections.Generic;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

internal sealed class McsCountdownUiController(IServiceProvider serviceProvider, bool hotReload) : PluginModuleBase(serviceProvider, hotReload), IClientListener
{
    public int ListenerVersion => 1;
    public int ListenerPriority => 500;
    
    public override string PluginModuleName => "McsCountdownUiController";
    public override string ModuleChatPrefix => "unused";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    
    // player slot - Countdown type
    private readonly Dictionary<int, McsCountdownUiType> _mcsCountdownTypes = new();
    
    private readonly Dictionary<McsCountdownUiType, IMcsCountdownUi> _countdownUis = new();
    
    private IPluginConfigProvider _mcsPluginConfigProvider = null!;
    
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    protected override void OnInitialize()
    {
        SharedSystem.GetClientManager().InstallClientListener(this);
        
        _countdownUis[McsCountdownUiType.CenterHud] = new CenterHudCountdownUi(ServiceProvider);
        _countdownUis[McsCountdownUiType.CenterAlert] = new CenterAlertCountdownUi(ServiceProvider);
        _countdownUis[McsCountdownUiType.CenterHtml] = new CenterHtmlCountdownUi(ServiceProvider);
        _countdownUis[McsCountdownUiType.Chat] = new ChatCountdownUi(ServiceProvider);

        _mcsPluginConfigProvider = ServiceProvider.GetRequiredService<IPluginConfigProvider>();
        
        if (HotReload)
        {
            foreach (IGameClient client in SharedSystem.GetModSharp().GetIServer().GetGameClients())
            {
                if (client.IsFakeClient || client.IsHltv)
                    continue;

                PlayerConnectFull(client);
            }
        }
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetClientManager().RemoveClientListener(this);
    }

    public void OnClientPutInServer(IGameClient client)
    {
        PlayerConnectFull(client);
    }

    private void PlayerConnectFull(IGameClient client)
    {
        McsCountdownUiType uiType = _mcsPluginConfigProvider.PluginConfig.VoteConfig.CurrentCountdownUiType;
            
        // TODO() Player preference from DB
        
        UpdateCountdownType(client, uiType);
    }
    
    

    private void UpdateCountdownType(IGameClient client, McsCountdownUiType uiType)
    {
        _mcsCountdownTypes[client.Slot] = uiType;
    }

    internal void CloseCountdownUiAll()
    {
        foreach (IGameClient client in SharedSystem.GetModSharp().GetIServer().GetGameClients())
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            CloseCountdownUi(client);
        }
    }

    internal void ShowCountdownToAll(int secondsLeft, McsCountdownType countdownType)
    {
        foreach (IGameClient client in SharedSystem.GetModSharp().GetIServer().GetGameClients())
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            ShowCountdown(client, secondsLeft, countdownType);
        }
    }

    private void ShowCountdown(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        var type = GetPlayerCountdownUiType(client);
        ShowCountdown(client, secondsLeft, type, countdownType);
    }

    private void ShowCountdown(IGameClient client, int secondsLeft ,McsCountdownUiType uiType, McsCountdownType countdownType)
    {
        switch (countdownType)
        {
            case McsCountdownType.VoteStart:
                if (uiType.HasFlag(McsCountdownUiType.CenterHud))
                {
                    _countdownUis[McsCountdownUiType.CenterHud].ShowCountdownToPlayer(client, secondsLeft, countdownType);
                }
        
                if (uiType.HasFlag(McsCountdownUiType.CenterAlert))
                {
                    _countdownUis[McsCountdownUiType.CenterAlert].ShowCountdownToPlayer(client, secondsLeft, countdownType);
                }
        
                if (uiType.HasFlag(McsCountdownUiType.CenterHtml))
                {
                    _countdownUis[McsCountdownUiType.CenterHtml].ShowCountdownToPlayer(client, secondsLeft, countdownType);
                }
        
                if (uiType.HasFlag(McsCountdownUiType.Chat))
                {
                    _countdownUis[McsCountdownUiType.Chat].ShowCountdownToPlayer(client, secondsLeft, countdownType);
                }
                break;
            
            case McsCountdownType.Voting:
                _countdownUis[McsCountdownUiType.CenterAlert].ShowCountdownToPlayer(client, secondsLeft, countdownType);
                break;
        }
    }

    private void CloseCountdownUi(IGameClient client)
    {
        var type = GetPlayerCountdownUiType(client);
        CloseCountdownUi(client, type);
    }

    private void CloseCountdownUi(IGameClient client, McsCountdownUiType uiType)
    {
        if (uiType.HasFlag(McsCountdownUiType.CenterHud))
        {
            _countdownUis[McsCountdownUiType.CenterHud].Close(client);
        }
        
        if (uiType.HasFlag(McsCountdownUiType.CenterAlert))
        {
            _countdownUis[McsCountdownUiType.CenterAlert].Close(client);
        }
        
        if (uiType.HasFlag(McsCountdownUiType.CenterHtml))
        {
            _countdownUis[McsCountdownUiType.CenterHtml].Close(client);
        }
        
        if (uiType.HasFlag(McsCountdownUiType.Chat))
        {
            _countdownUis[McsCountdownUiType.Chat].Close(client);
        }
    }


    private McsCountdownUiType GetPlayerCountdownUiType(IGameClient client)
    {
        var type = _mcsCountdownTypes.GetValueOrDefault(client.Slot, McsCountdownUiType.CenterHtml);

        return type;
    }
}