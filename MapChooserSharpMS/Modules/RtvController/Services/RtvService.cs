using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.RtvController.Interfaces;
using MapChooserSharpMS.Modules.RtvController.Managers;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using MapChooserSharpMS.Shared.RtvController;
using MapChooserSharpMS.Shared.RtvController.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.RtvController.Services;

internal sealed class RtvService(
    TnmsPlugin plugin,
    IMcsInternalRtvController controller,
    InternalRtvManager rtvManager,
    IInternalEventManager eventManager,
    IServiceProvider serviceProvider
    ) : IRtvService
{
    public RtvExecutionResult AddClientToRtv(IGameClient client)
    {
        if (rtvManager.RtvStatus == RtvStatus.AnotherVoteOngoing)
            return RtvExecutionResult.AnotherVoteOngoing;

        if (rtvManager.RtvStatus == RtvStatus.Disabled)
            return RtvExecutionResult.CommandDisabled;

        if (rtvManager.RtvStatus == RtvStatus.TriggeredWaitingForMapTransition)
            return RtvExecutionResult.TriggeredWaitingForMapTransition;
        
        if (rtvManager.RtvStatus == RtvStatus.TriggeredWaitingForVote)
            return RtvExecutionResult.TriggeredWaitingForVote;
        
        if (rtvManager.RtvStatus == RtvStatus.InCooldown)
            return RtvExecutionResult.CommandInCooldown;
        
        
        IClientRtvCastParams @params = ActivatorUtilities.CreateInstance<ClientRtvCastParams>(serviceProvider, plugin, controller, client, false);
        bool cancelled = eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvCast(@params));
        if (cancelled)
            return RtvExecutionResult.DisallowedByExternalConsumer;
        
        
        int slot = client.Slot;

        if (!rtvManager.AddParticipants(client))
            return RtvExecutionResult.AlreadyVoted;

        return RtvExecutionResult.Success;
    }

    public RtvExecutionResult AddClientToRtv(int slot)
    {
        var client = plugin.SharedSystem.GetClientManager().GetGameClient(new PlayerSlot(slot));

        if (client == null)
            return RtvExecutionResult.NotAllowed;
        
        return AddClientToRtv(client);
    }

    public bool RemoveClientFromRtv(IGameClient client, IGameClient? enforcer = null)
    {
        IClientRtvUnCastParams @params;
        
        if (enforcer != null)
        {
            @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(serviceProvider, plugin, controller, client, true, enforcer);
        }
        else
        {
            @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(serviceProvider, plugin, controller, client, false);
        }
        
        bool cancelled = eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvUnCast(@params));

        if (cancelled)
            return false;
        
        return rtvManager.RemoveParticipants(client);
    }

    public bool RemoveClientFromRtv(int slot)
        => rtvManager.RemoveParticipants(slot);

    public void InitiateRtvVote()
    {
    }

    public void EnableRtvCommand(IGameClient? client = null, bool silently = false)
    {
        bool success = rtvManager.TrySetRtvStatus(RtvStatus.Enabled);

        if (silently)
            return;
        
        if (success)
        {
            // TODO() Set notification
        }
        else
        {
            // TODO() Failure notification
        }
    }

    public void DisableRtvCommand(IGameClient? client = null, bool silently = false)
    {
        bool success = rtvManager.TrySetRtvStatus(RtvStatus.Disabled);

        if (silently)
            return;
        
        if (success)
        {
            // TODO() Set notification
        }
        else
        {
            // TODO() Failure notification
        }
    }

    public void InitiateForceRtvVote(IGameClient? client)
    {
    }
}
