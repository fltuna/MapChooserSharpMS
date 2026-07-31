using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;

namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// Sorts map lists for nomination menus. Shared between MCS core (default order)
/// and menu compat plugins (player-selected order).
/// </summary>
public static class McsMapListSorter
{
    /// <summary>
    /// Returns a new list sorted by <paramref name="order"/>.
    /// Non-alphabetical orders tie-break by map name ascending.
    /// Maps without search tags are always placed last for tag orders.
    /// </summary>
    /// <param name="source">Elements to sort.</param>
    /// <param name="configSelector">Extracts the <see cref="IMapConfig"/> from an element.</param>
    /// <param name="order">Sort order to apply.</param>
    /// <param name="cooldownQueryService">Used for cooldown-based orders; queries in-memory state only.</param>
    public static List<T> Sort<T>(
        IEnumerable<T> source,
        Func<T, IMapConfig> configSelector,
        NominationSortOrder order,
        IMapCooldownQueryService cooldownQueryService)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        string NameOf(T element) => configSelector(element).MapName;

        switch (order)
        {
            case NominationSortOrder.AlphabeticalAscending:
                return source.OrderBy(NameOf, comparer).ToList();

            case NominationSortOrder.AlphabeticalDescending:
                return source.OrderByDescending(NameOf, comparer).ToList();

            case NominationSortOrder.CooldownAscending:
            case NominationSortOrder.CooldownDescending:
            {
                int CooldownOf(T element) =>
                    cooldownQueryService.GetCurrentCooldowns(configSelector(element)).HighestCooldownCount;

                var ordered = order == NominationSortOrder.CooldownAscending
                    ? source.OrderBy(CooldownOf)
                    : source.OrderByDescending(CooldownOf);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            case NominationSortOrder.TimedCooldownAscending:
            case NominationSortOrder.TimedCooldownDescending:
            {
                DateTime TimedCooldownOf(T element) =>
                    cooldownQueryService.GetCurrentCooldowns(configSelector(element)).LongestTimedCooldown;

                var ordered = order == NominationSortOrder.TimedCooldownAscending
                    ? source.OrderBy(TimedCooldownOf)
                    : source.OrderByDescending(TimedCooldownOf);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            case NominationSortOrder.TagAscending:
            case NominationSortOrder.TagDescending:
            {
                string? TagKeyOf(T element)
                {
                    var tags = configSelector(element).SearchTags;
                    return tags.Count > 0 ? tags.Min(comparer) : null;
                }

                var withTagFlag = source.OrderBy(e => TagKeyOf(e) is null);
                var ordered = order == NominationSortOrder.TagAscending
                    ? withTagFlag.ThenBy(TagKeyOf, comparer)
                    : withTagFlag.ThenByDescending(TagKeyOf, comparer);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown sort order");
        }
    }
}
