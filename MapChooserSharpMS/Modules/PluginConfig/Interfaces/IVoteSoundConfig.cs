namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IVoteSoundConfig
{
    /// <summary>
    /// Vsndevts file path
    /// </summary>
    internal string VSndEvtsSoundFilePath { get; }
    
    /// <summary>
    /// Initial vote sounds
    /// </summary>
    internal IVoteSound InitialVoteSounds { get; }
    
    
    /// <summary>
    /// Runoff vote sounds
    /// </summary>
    internal IVoteSound RunoffVoteSounds { get; }
}