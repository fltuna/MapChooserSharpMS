using MapChooserSharp.Modules.MapVote.Countdown;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;

public interface IMcsCountdownUi
{
    public void ShowCountdownToPlayer(IGameClient client, int secondsLeft, McsCountdownType countdownType);

    public void Close(IGameClient client);
}