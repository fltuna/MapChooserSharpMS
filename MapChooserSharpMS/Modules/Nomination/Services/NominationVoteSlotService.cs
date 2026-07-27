using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Nomination;
using Microsoft.Extensions.DependencyInjection;

namespace MapChooserSharpMS.Modules.Nomination.Services;

/// <summary>
/// Simulates the vote candidate pick performed by
/// MapVoteControllingService.BuildNominatedCandidates, so that nomination-side
/// features (!nomlist ordering / vote slot overflow notification) stay 1:1
/// with the order maps actually enter the vote.
/// Keep this in sync when changing the pick logic on the MapVote side.
/// </summary>
internal sealed class NominationVoteSlotService(IServiceProvider provider)
{
    private IMcsPluginConfigProvider? _configProvider;
    private IMcsInternalMapExtendService? _extendService;
    private IMcsInternalNominationManager? _nominationManager;

    private IMcsPluginConfigProvider ConfigProvider =>
        _configProvider ??= provider.GetRequiredService<IMcsPluginConfigProvider>();

    private IMcsInternalMapExtendService ExtendService =>
        _extendService ??= provider.GetRequiredService<IMcsInternalMapExtendService>();

    private IMcsInternalNominationManager NominationManager =>
        _nominationManager ??= provider.GetRequiredService<IMcsInternalNominationManager>();

    /// <summary>
    /// Map slots available in the next vote. The Extend placeholder occupies
    /// one slot while the extend budget remains; the Don't Change placeholder
    /// of an RTV vote cannot be known ahead of time and is not accounted for.
    /// </summary>
    public int GetAvailableMapSlots()
    {
        int maxElements = ConfigProvider.PluginConfig.VoteConfig.MaxMenuElements;
        return ExtendService.ExtendsLeft > 0 ? maxElements - 1 : maxElements;
    }

    /// <summary>
    /// Whether the current nomination count no longer fits into the vote,
    /// i.e. at least one nomination would be dropped at vote start.
    /// </summary>
    public bool IsOverflowing()
        => NominationManager.NominatedMaps.Count > GetAvailableMapSlots();

    /// <summary>
    /// Splits the current nominations, in pick order (admin nominations
    /// first, then community nominations by participant count), into the
    /// entries that would enter the vote right now and the ones that would
    /// not (over capacity or below MinNominationCountForVote).
    /// </summary>
    public (List<IMcsNominationData> Included, List<IMcsNominationData> Excluded) SplitByPickOrder()
    {
        var nominations = NominationManager.NominatedMaps.Values.ToList();

        var ordered = nominations
            .Where(n => n.IsForceNominated)
            .Concat(nominations
                .Where(n => !n.IsForceNominated)
                .OrderByDescending(n => n.NominationParticipants.Count))
            .ToList();

        int slots = GetAvailableMapSlots();
        var included = new List<IMcsNominationData>();
        var excluded = new List<IMcsNominationData>();

        foreach (var nomination in ordered)
        {
            bool qualifies = nomination.IsForceNominated
                             || nomination.NominationParticipants.Count >=
                             nomination.MapConfig.NominationConfig.MinNominationCountForVote;

            if (qualifies && included.Count < slots)
                included.Add(nomination);
            else
                excluded.Add(nomination);
        }

        return (included, excluded);
    }
}
