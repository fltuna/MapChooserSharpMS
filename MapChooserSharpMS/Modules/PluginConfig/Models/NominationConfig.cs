using System.Collections.Generic;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

public class NominationConfig(
    List<McsSupportedMenuType> availableMenuTypes,
    McsSupportedMenuType currentMenuType)
    : INominationConfig
{
    public List<McsSupportedMenuType> AvailableMenuTypes { get; } = availableMenuTypes;
    public McsSupportedMenuType CurrentMenuType { get; } = currentMenuType;
}