using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events;

/// <summary>
/// If event is enforceable by admin, it should implement this interface.
/// </summary>
public interface IEnforceableEvent
{
    /// <summary>
    /// This change event is occured by admin or not
    /// </summary>
    bool EnforcedByAdmin { get; }
    
    /// <summary>
    /// Who enforced this event <br/>
    /// When null, if EnforcedByAdmin is false this event is not enforced. otherwise means console
    /// </summary>
    IGameClient? Enforcer { get; }
}