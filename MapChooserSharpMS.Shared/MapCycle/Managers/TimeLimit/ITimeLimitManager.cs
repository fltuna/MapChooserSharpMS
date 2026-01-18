namespace MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

public interface ITimeLimitManager
{
    TimeLimitType TimeLimitType { get; }

    bool IsLimitReached { get; }
}