using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

public interface IMapConfigOverrides: IBaseOverrideConfig
{
    IMapConfig MapConfig { get; }
}