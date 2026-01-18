namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class VoteConfig(List<McsSupportedMenuType> availableVoteMenuTypes, McsSupportedMenuType currentMcsVoteMenuType, int maxMenuElements, bool shouldPrintVoteToChat, bool shouldPrintVoteRemainingTime, IMcsVoteSoundConfig voteSoundConfig, McsCountdownUiType currentCountdownUiType)
    : IMcsVoteConfig
{
    public List<McsSupportedMenuType> AvailableMenuTypes { get; } = availableVoteMenuTypes;
    public McsSupportedMenuType CurrentMenuType { get; } = currentMcsVoteMenuType;
    public McsCountdownUiType CurrentCountdownUiType { get; } = currentCountdownUiType;
    public int MaxMenuElements { get; } = maxMenuElements;
    public bool ShouldPrintVoteToChat { get; } = shouldPrintVoteToChat;
    public bool ShouldPrintVoteRemainingTime { get; } = shouldPrintVoteRemainingTime;
    public IMcsVoteSoundConfig VoteSoundConfig { get; } = voteSoundConfig;
}