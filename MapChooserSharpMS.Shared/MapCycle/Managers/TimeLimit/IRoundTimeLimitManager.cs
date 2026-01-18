using System.Globalization;

namespace MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

public interface IRoundTimeLimitManager: ITimeLimitManager
{
    int RoundsLeft { get; }

    bool Extend(int rounds);
    
    bool Set(int rounds);
    
    string GetFormattedRoundsLeft(CultureInfo? info = null);
}