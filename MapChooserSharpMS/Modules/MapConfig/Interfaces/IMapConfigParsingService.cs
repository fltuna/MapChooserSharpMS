namespace MapChooserSharpMS.Modules.MapConfig.Interfaces;

internal interface IMapConfigParsingService
{
    IMapConfigParsingResult? ParseConfigs(string configPath);
}