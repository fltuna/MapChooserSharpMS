using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

public interface IBaseOverrideConfig
{
    string OverrideConfigName { get; }
 
    bool Enabled { get; }
    
    bool ForceOverride { get; }
    
    int OverridePriority { get; }
    
    IReadOnlyCollection<DayOfWeek> TargetDays { get; }
    
    IReadOnlyCollection<ITimeRange> TargetTimeRanges { get; }
}