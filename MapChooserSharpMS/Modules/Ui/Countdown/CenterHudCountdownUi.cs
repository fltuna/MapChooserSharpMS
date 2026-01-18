using System;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

public class CenterHudCountdownUi(IServiceProvider provider): IMcsCountdownUi
{
    private readonly TnmsPlugin _plugin = provider.GetRequiredService<TnmsPlugin>();
    
    public void ShowCountdownToPlayer(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        client.GetPlayerController()?.PrintToCenter(_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.Countdown", secondsLeft));
    }

    public void Close(IGameClient client)
    {
        client.GetPlayerController()?.PrintToCenter(" ");
    }
}