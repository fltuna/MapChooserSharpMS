using System;
using System.Collections.Generic;
using System.IO;
using MapChooserSharpMS.Modules.MapConfig.Interfaces;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapConfig;

internal sealed class MapConfigProvider(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMcsMapConfigProvider
{
    public override string PluginModuleName => "MapChooserSharpMS - MapConfigProvider";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;
    
    
    private Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>  _mapGroupSettings = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>>  _mapConfigsNameMapping = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>>  _mapConfigsWorkshopIdMapping = new();

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IMapConfigGenerationService, MapConfigGenerationService>();
        services.AddTransient<IMapConfigParsingService, MapConfigParsingService>();
    }


    protected override void OnAllModulesLoaded()
    {
        ReloadConfigs();
    }


    public void ReloadConfigs()
    {
        var mapConfigParseService = ServiceProvider.GetRequiredService<IMapConfigParsingService>();

        IMapConfigParsingResult? parseResult;
        
        try
        {
            parseResult = mapConfigParseService.ParseConfigs(Plugin.BaseCfgDirectoryPath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse configs");
            return;
        }

        if (parseResult == null)
        {
            Logger.LogError("Failed to parse configs");
            return;
        }
        
        _mapGroupSettings = parseResult.MapGroupSettings ;
        _mapConfigsNameMapping = parseResult.MapConfigsNameMapping;
        _mapConfigsWorkshopIdMapping =  parseResult.MapConfigsWorkshopIdMapping;
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> GetGroupSettings()
    {
        return _mapGroupSettings;
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<IMapConfigOverrides>> GetMapConfigs()
    {
        return _mapConfigsNameMapping;
    }

    public bool TryGetMapConfigs(string mapName, out IReadOnlyCollection<IMapConfigOverrides>? found)
    {
        return _mapConfigsNameMapping.TryGetValue(mapName, out found);
    }

    public bool TryGetMapConfigs(long workshopId, out IReadOnlyCollection<IMapConfigOverrides>? found)
    {
        return _mapConfigsWorkshopIdMapping.TryGetValue(workshopId, out found);
    }

    public bool TryGetMapConfig(string mapName, out IMapConfig found)
    {
        throw new NotImplementedException();
    }

    public bool TryGetMapConfig(long workshopId, out IMapConfig found)
    {
        throw new NotImplementedException();
    }

    public IMapGroupConfig? TryGetGroupConfig(IMapGroupConfigOverrides overrides)
    {
        throw new NotImplementedException();
    }
}