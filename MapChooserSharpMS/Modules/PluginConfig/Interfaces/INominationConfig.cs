using System.Collections.Generic;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface INominationConfig
{
    internal List<McsSupportedMenuType> AvailableMenuTypes { get; }
    
    internal McsSupportedMenuType CurrentMenuType { get; }
}