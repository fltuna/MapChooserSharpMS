namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IGeneralConfig
{
    internal bool ShouldUseAliasMapNameIfAvailable { get; }
    
    internal bool VerboseCooldownPrint { get; }
    
    internal string[] WorkshopCollectionIds { get; }
    
    internal bool ShouldAutoFixMapName { get; }
    
    internal ISqlConfig SqlConfig { get; }
    
    internal RtvMapChangeBehaviourType RtvMapChangeBehaviour { get; }
}