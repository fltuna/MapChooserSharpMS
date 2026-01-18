using System;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

[Flags]
internal enum TimeLimitStatusFlag
{
    LimitReached = 1 << 0,
    VoteStartThresholdElapsed = 1 << 1,
}