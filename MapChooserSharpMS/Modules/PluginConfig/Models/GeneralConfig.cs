namespace MapChooserSharpMS.Modules.PluginConfig.Models;

public class GeneralConfig(bool shouldUseAliasMapNameIfAvailable, bool verboseCooldownPrint, string[] workshopCollectionIds, bool shouldAutoFixMapName, IMcsSqlConfig sqlConfig, RtvMapChangeBehaviourType rtvMapChangeBehaviour) : IMcsGeneralConfig
{
    public bool ShouldUseAliasMapNameIfAvailable { get; } = shouldUseAliasMapNameIfAvailable;
    public bool VerboseCooldownPrint { get; } = verboseCooldownPrint;
    public string[] WorkshopCollectionIds { get; } = workshopCollectionIds;
    public bool ShouldAutoFixMapName { get; } = shouldAutoFixMapName;
    public IMcsSqlConfig SqlConfig { get; } = sqlConfig;
    public RtvMapChangeBehaviourType RtvMapChangeBehaviour { get; } = rtvMapChangeBehaviour;
}