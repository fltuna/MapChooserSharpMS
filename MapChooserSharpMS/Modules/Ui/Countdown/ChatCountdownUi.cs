using System;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

public class ChatCountdownUi(IServiceProvider provider): IMcsCountdownUi
{
    private readonly TnmsPlugin _plugin = provider.GetRequiredService<TnmsPlugin>();

    private bool _isFirstNotificationNotified = false;
    
    public void ShowCountdownToPlayer(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        if (!_isFirstNotificationNotified || secondsLeft <= 10)
        {
            client.GetPlayerController()?.PrintToChat(_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.Countdown", secondsLeft));
            _isFirstNotificationNotified = true;
        }
    }

    public void Close(IGameClient client){}
}