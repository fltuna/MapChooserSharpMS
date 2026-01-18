namespace MapChooserSharpMS.Shared.MapConfig;

public interface IRandomPickConfig
{
    /// <summary>
    /// Weight for map selection, higher is more priority
    /// </summary>
    uint MapSelectionWeight { get; }
    
    /// <summary>
    /// This map is pickable in random map pick?
    /// </summary>
    bool IsPickable { get; }
    
    /// <summary>
    /// If pickable, can we bypass nomination restrictions?
    /// </summary>
    bool BypassNominationRestriction { get; }
}