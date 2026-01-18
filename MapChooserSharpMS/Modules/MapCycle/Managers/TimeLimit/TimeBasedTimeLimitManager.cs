using System;
using System.Globalization;
using System.Threading;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

internal sealed class TimeBasedTimeLimitManager(TimeSpan initialTimeLimit): IInternalTimeBasedTimeLimitManager
{
    private readonly Lock _lock = new();
    
    public TimeLimitType TimeLimitType => TimeLimitType.Time;

    public bool IsLimitReached => TimeLeft <= TimeSpan.Zero;

    public TimeSpan TimeLeft { get; private set; } = initialTimeLimit;
    
    public bool Extend(TimeSpan time)
    {
        if (IsLimitReached)
            TimeLeft = TimeSpan.Zero;
        
        TimeLeft += time;
        return true;
    }

    public bool Set(TimeSpan time)
    {
        TimeLeft = time;
        return true;
    }

    // TODO() We will implement it later
    public string GetFormattedTimeLeft(CultureInfo? info = null)
    {
        throw new NotImplementedException();
    }

    public TimeLimitStatusFlag Tick()
    {
        lock (_lock)
        {
            if (IsLimitReached)
                return false;
            
            TimeLeft -= TimeSpan.FromSeconds(1);
            
            if (TimeLeft <= TimeSpan.Zero)
                TimeLeft = TimeSpan.Zero;
            
            return TimeLeft > TimeSpan.Zero;
        }
    }
}