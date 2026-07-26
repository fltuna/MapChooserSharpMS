namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// Represents a single reason why a nomination check failed.
/// An empty list indicates success (no issues found).
/// </summary>
public enum NominationCheckResult
{
    Disabled,
    NotEnoughPermissions,
    TooMuchPlayers,
    NotEnoughPlayers,
    VotingPeriod,
    OnlySpecificDay,
    OnlySpecificTime,
    MapIsInCooldown,
    NominationCooldownActive,
    AlreadyNominated,
    NominatedByAdmin,
    SameMap,
    GroupNominationLimitReached,
    CancelledByExternalPlugin,
    ProhibitAdminNomination,
    PlayerCooldownActive,
    SameMapGroup,
}
