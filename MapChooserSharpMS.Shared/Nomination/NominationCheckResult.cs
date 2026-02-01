using System;

namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// Flags representing the reasons why a nomination check failed.
/// None (0) indicates success.
/// </summary>
[Flags]
public enum NominationCheckResult
{
    /// <summary>
    /// No issues found, nomination is allowed.
    /// </summary>
    None = 0,

    Disabled = 1 << 0,
    NotEnoughPermissions = 1 << 1,
    TooMuchPlayers = 1 << 2,
    NotEnoughPlayers = 1 << 3,
    RestrictedToCertainUser = 1 << 4,
    BlockedBySteamId = 1 << 5,
    VotingPeriod = 1 << 6,
    OnlySpecificDay = 1 << 7,
    OnlySpecificTime = 1 << 8,
    MapIsInCooldown = 1 << 9,
    AlreadyNominated = 1 << 10,
    NominatedByAdmin = 1 << 11,
    SameMap = 1 << 12,
    GroupNominationLimitReached = 1 << 13,
    CancelledByExternalPlugin = 1 << 14,
}