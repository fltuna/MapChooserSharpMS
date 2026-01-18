namespace MapChooserSharpMS.Shared.Nomination;

public enum NominationCheckResult
{
    Success,
    Failed,
    Disabled,
    NotEnoughPermissions,
    TooMuchPlayers,
    NotEnoughPlayers,
    NotAllowed,
    RestrictedToCertainUser,
    BlockedBySteamId,
    DisabledAtThisTime,
    VotingPeriod,
    OnlySpecificDay,
    OnlySpecificTime,
    MapIsInCooldown,
    AlreadyNominated,
    NominatedByAdmin,
    SameMap,
    GroupNominationLimitReached,
    CancelledByExternalPlugin
}