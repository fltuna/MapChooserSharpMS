using System;
using System.Globalization;

namespace MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

public interface ITimeBasedTimeLimitManager: ITimeLimitManager
{
    TimeSpan TimeLeft { get; }

    bool Extend(TimeSpan time);
    
    bool Set(TimeSpan time);
    
    string GetFormattedTimeLeft(CultureInfo? info = null);
}