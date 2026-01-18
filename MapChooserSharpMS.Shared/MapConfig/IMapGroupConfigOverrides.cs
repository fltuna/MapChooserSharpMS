using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

public interface IMapGroupConfigOverrides: IBaseOverrideConfig
{
    IMapGroupConfig GroupConfig { get; }
}