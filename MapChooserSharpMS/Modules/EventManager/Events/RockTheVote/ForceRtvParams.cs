using System.Globalization;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;

internal sealed class ForceRtvParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IGameClient? client,
    bool isSilent = false
) : IForceRtvParam
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];
    }

    public IGameClient? Client { get; } = client;
    public bool IsSilent { get; } = isSilent;
    public bool EnforcedByAdmin => Client != null;
    public IGameClient? Enforcer => Client;
}
