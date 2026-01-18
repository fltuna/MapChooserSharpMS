using System.Collections.Generic;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class VoteSound(
    string voteCountdownStartSound,
    string voteStartSound,
    string voteFinishSound,
    List<string> voteCountdownSounds)
    : IVoteSound
{
    public string VoteCountdownStartSound { get; } = voteCountdownStartSound;
    public string VoteStartSound { get; } = voteStartSound;
    public string VoteFinishSound { get; } = voteFinishSound;
    public List<string> VoteCountdownSounds { get; } = voteCountdownSounds;
}