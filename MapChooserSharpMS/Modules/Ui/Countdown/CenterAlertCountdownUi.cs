using System;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

public class CenterAlertCountdownUi(IServiceProvider provider): IMcsCountdownUi
{
    private readonly TnmsPlugin _plugin = provider.GetRequiredService<TnmsPlugin>();
    
    public void ShowCountdownToPlayer(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        switch (countdownType)
        {
            case McsCountdownType.VoteStart:
                client.GetPlayerController()?.PrintToSayText2(_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.Countdown", secondsLeft));
                break;
                
            case McsCountdownType.Voting:
                client.GetPlayerController()?.PrintToSayText2(_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.Voting.VoteEndCountdown", secondsLeft));
                break;
        }
        
    }

    public void Close(IGameClient client)
    {
        client.GetPlayerController()?.PrintToSayText2(" ");
    }
}