using System.Globalization;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;

internal sealed class ClientRtvUnCastParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IGameClient client,
    bool enforcedByAdmin = false,
    IGameClient? enforcer = null
) : IClientRtvUnCastParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];
    }

    public IGameClient Client { get; } = client;
    public bool EnforcedByAdmin { get; } = enforcedByAdmin;
    public IGameClient? Enforcer { get; } = enforcer;
}
