using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharp.Modules.PluginConfig.Models;

internal class VoteSoundConfig: IVoteSoundConfig
{
    public VoteSoundConfig(string vSndEvtsSoundFilePath, IVoteSound voteCountdownSounds, IVoteSound runoffVoteCountdownSounds)
    {
        if (!vSndEvtsSoundFilePath.EndsWith(".vsndevts"))
        {
            VSndEvtsSoundFilePath = string.Empty;
        }
        else
        {
            VSndEvtsSoundFilePath = vSndEvtsSoundFilePath;
        }
        
        InitialVoteSounds = voteCountdownSounds;
        RunoffVoteSounds = runoffVoteCountdownSounds;
    }

    public string VSndEvtsSoundFilePath { get; }
    public IVoteSound InitialVoteSounds { get; }
    public IVoteSound RunoffVoteSounds { get; }
}