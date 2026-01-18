using System.Globalization;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Nomination;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.Nomination;

public class NominationCheckPassedEventParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IGameClient? nominator = null
    ): INominationCheckPassedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return plugin.Localizer[moduleBase.ModuleChatPrefix, culture];
    }

    public IGameClient? Client { get; } = nominator;
}