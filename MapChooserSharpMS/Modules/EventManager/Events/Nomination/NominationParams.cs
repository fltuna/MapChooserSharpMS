using System;
using System.Globalization;
using MapChooserSharpMS.Modules.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Nomination;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.Nomination;

internal sealed class NominationParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMcsNominationData nominationData,
    IGameClient? client = null
    ): INominationParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];
    }

    public IGameClient? Client { get; } = client;
    public IMcsNominationData NominationData { get; } = nominationData;
}