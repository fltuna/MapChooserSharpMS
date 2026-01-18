using System;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

public class CenterHtmlCountdownUi(IServiceProvider provider): IMcsCountdownUi
{
    private readonly TnmsPlugin _plugin = provider.GetRequiredService<TnmsPlugin>();
    
    public void ShowCountdownToPlayer(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        client.GetPlayerController()?.PrintToCenterHtml(_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.CenterHtml", secondsLeft));
    }

    public void Close(IGameClient client)
    {
        client.GetPlayerController()?.PrintToCenterHtml(" ");
    }
}