using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.RtvController.Interfaces;
using MapChooserSharpMS.Modules.RtvController.Managers;
using MapChooserSharpMS.Modules.RtvController.Services;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.RtvController;
using MapChooserSharpMS.Shared.RtvController.Managers;
using MapChooserSharpMS.Shared.RtvController.Services;
using Microsoft.Extensions.DependencyInjection;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.RtvController;

internal sealed class McsRtvController(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMcsInternalRtvController, IMapVoteEventListener, IMapCycleEventListener
{
    public override string PluginModuleName => "McsRtvController";
    public override string ModuleChatPrefix => "Prefix.Rtv";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private InternalRtvManager _rtvManager = null!;
    private RtvService _rtvService = null!;
    private IInternalEventManager _eventManager = null!;
    private IPluginConfigProvider _configProvider = null!;

    public IRtvManager RtvManager => _rtvManager;
    public IRtvService RtvService => _rtvService;

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsInternalRtvController>(this);
    }

    protected override void OnInitialize()
    {
        _rtvManager = new InternalRtvManager();
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        _configProvider = ServiceProvider.GetRequiredService<IPluginConfigProvider>();
        _rtvService = new RtvService(Plugin, this, _rtvManager, _eventManager, ServiceProvider);
    }

    protected override void OnAllModulesLoaded()
    {

    }

    protected override void OnUnloadModule()
    {
    }

    public void InstallEventListener(IRockTheVoteEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IRockTheVoteEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    internal void ResetRtvState()
    {
        _rtvManager.ForceReset();
    }

    public void OnClientDisconnect(int slot)
    {
        _rtvService.RemoveClientFromRtv(slot);
    }

    public int ListenerPriority => 9999999;

    public void OnMapVoteFinished(IMapVoteFinishedEventParams @params)
    {
        ResetRtvState();
    }
}
