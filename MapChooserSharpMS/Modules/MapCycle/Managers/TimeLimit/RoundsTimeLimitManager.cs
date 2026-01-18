using System.Globalization;
using System.Threading;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

internal sealed class RoundsTimeLimitManager(int initialRoundLimit): IInternalRoundBaseTimeLimitManager
{
    private readonly Lock _lock = new();
    
    public TimeLimitType TimeLimitType => TimeLimitType.Round;
    public bool IsLimitReached => RoundsLeft <= 0;

    public int RoundsLeft { get; private set; } = initialRoundLimit;
    
    public bool Extend(int rounds)
    {
        lock (_lock)
        {
            RoundsLeft += rounds;
            return true;
        }
    }

    public bool Set(int rounds)
    {
        lock (_lock)
        {
            RoundsLeft = rounds;
            return true;
        }
    }

    // TODO() We will implement it later
    public string GetFormattedRoundsLeft(CultureInfo? info = null)
    {
        throw new System.NotImplementedException();
    }

    public TimeLimitStatusFlag Tick()
    {
        lock (_lock)
        {
            TimeLimitStatusFlag statusFlag = 0;

            if (IsLimitReached)
            {
                statusFlag = TimeLimitStatusFlag.LimitReached;
            }

            RoundsLeft--;
            
            // TODO Get TimeLimit from ConVar
            const bool todoBool = false;
            if (todoBool)
            {
                statusFlag |= TimeLimitStatusFlag.VoteStartThresholdElapsed;
            }
                
            
            if (RoundsLeft < 0)
                RoundsLeft = 0;
            
            return statusFlag;
        }
    }
}