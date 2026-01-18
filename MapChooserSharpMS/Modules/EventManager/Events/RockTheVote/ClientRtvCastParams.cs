using System.Globalization;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;

internal sealed class ClientRtvCastParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IGameClient client,
    bool isRtvTrigger
) : IClientRtvCastParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];
    }

    public bool IsRtvTrigger { get; } = isRtvTrigger;
    public IGameClient Client { get; } = client;
}
