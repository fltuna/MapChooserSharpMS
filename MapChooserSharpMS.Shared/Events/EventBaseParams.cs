using System.Globalization;

namespace MapChooserSharpMS.Shared.Events;

/// <summary>
/// Base parameter interface for McsEvent
/// </summary>
public interface IEventBaseParams
{
    /// <summary>
    /// Module prefix
    /// </summary>
    public string ModulePrefix(CultureInfo? culture = null);
}