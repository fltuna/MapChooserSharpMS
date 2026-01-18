namespace MapChooserSharpMS.Shared.RtvController;

/// <summary>
/// Result of player rtv execution
/// </summary>
public enum RtvExecutionResult
{
    /// <summary>
    /// RTV is successfully executed
    /// </summary>
    Success = 0,
    
    /// <summary>
    /// Player is already voted to RTV
    /// </summary>
    AlreadyVoted,
    
    /// <summary>
    /// If RTV command is in cooldown
    /// </summary>
    CommandInCooldown,
    
    /// <summary>
    /// If RTV command is disabled by admin command
    /// </summary>
    CommandDisabled,
    
    /// <summary>
    /// If another vote is ongoing. like map vote
    /// </summary>
    AnotherVoteOngoing,
    
    /// <summary>
    /// If player is disallowed by some reasons
    /// </summary>
    NotAllowed,
    
    /// <summary>
    /// If player RTV is disallowed by external API consumer
    /// </summary>
    DisallowedByExternalConsumer,
    
    /// <summary>
    /// If RTV is already triggered, we don't need to do anything
    /// </summary>
    RtvTriggeredAlready,
}