namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IPluginConfig
{
    internal IVoteConfig VoteConfig { get; }
    
    internal INominationConfig NominationConfig { get; }
    
    internal IMapCycleConfig MapCycleConfig { get; }
    
    internal IGeneralConfig GeneralConfig { get; }
}