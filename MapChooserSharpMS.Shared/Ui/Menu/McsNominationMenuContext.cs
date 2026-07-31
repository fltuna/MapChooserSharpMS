using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Context passed to <see cref="IMcsNominationMenuCompat.ShowNominationMenu"/>.
/// Contains all data and services the compat needs to build a rich nomination menu.
/// </summary>
public sealed class McsNominationMenuContext
{
    public required string Title { get; init; }

    public required IReadOnlyList<McsNominationMenuItem> Items { get; init; }

    /// <summary>
    /// True when the compat may offer a sort-order selection before showing the list
    /// (full map list menus). False for search results, confirmation and removal menus —
    /// those are always pre-sorted by map name ascending.
    /// </summary>
    public bool AllowSortSelection { get; init; }

    public required IMapConfigToolingService ToolingService { get; init; }

    public required IMapCooldownQueryService CooldownQueryService { get; init; }

    public required INominationMenuManagementService NominationMenuService { get; init; }
}
