using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Nomination;


/// <summary>
/// Nomination data of MapChooserSharp
/// </summary>
public interface IMcsNominationData
{
    /// <summary>
    /// Nominated map config
    /// </summary>
    public IMapConfig MapConfig { get; }
    
    /// <summary>
    /// UserID of nomination participants
    /// </summary>
    public HashSet<int> NominationParticipants { get; }
    
    /// <summary>
    /// Is force nominated by admin
    /// </summary>
    public bool IsForceNominated { get; set; }
}