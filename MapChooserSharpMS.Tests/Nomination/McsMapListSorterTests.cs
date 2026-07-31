using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination;
using Xunit;

namespace MapChooserSharpMS.Tests.Nomination;

public class McsMapListSorterTests
{
    #region Fakes

    private sealed class FakeMapConfig : IMapConfig
    {
        public required string MapName { get; init; }
        public string MapNameAlias => MapName;
        public string MapDescription => string.Empty;
        public long WorkshopId => 0;
        public IReadOnlyList<string> SearchTags { get; init; } = [];
        public List<IMapGroupConfig> GroupSettings { get; } = [];
        public bool IsDisabled => false;
        public int MaxExtends => 0;
        public int MaxExtCommandUses => 0;
        public int MapTime => 0;
        public int ExtendTimePerExtends => 0;
        public int MapRounds => 0;
        public int ExtendRoundsPerExtends => 0;
        public IRandomPickConfig RandomPickConfig => null!;
        public INominationConfig NominationConfig => null!;
        public IMcsCooldownSettings CooldownSettings => null!;
        public IExtraConfigAccessor ExtraConfiguration => null!;
    }

    private sealed class FakeCooldownResult : IDetailedCooldownResult
    {
        public required IMapConfig MapConfig { get; init; }
        public int HighestCooldownCount { get; init; }
        public DateTime LongestTimedCooldown { get; init; }
        public bool HasCooldown => HighestCooldownCount > 0 || LongestTimedCooldown > DateTime.MinValue;
        public int CooldownCount => HighestCooldownCount;
        public DateTime TimedCooldown => LongestTimedCooldown;
        public IReadOnlyDictionary<string, int> GroupCooldowns { get; } = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, DateTime> GroupTimedCooldowns { get; } = new Dictionary<string, DateTime>();
    }

    private sealed class FakeCooldownQueryService : IMapCooldownQueryService
    {
        public Dictionary<string, int> Cooldowns { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> TimedCooldowns { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IDetailedCooldownResult?> QueryCurrentCooldowns(IMapConfig mapConfig)
            => Task.FromResult<IDetailedCooldownResult?>(GetCurrentCooldowns(mapConfig));

        public IDetailedCooldownResult GetCurrentCooldowns(IMapConfig mapConfig)
        {
            return new FakeCooldownResult
            {
                MapConfig = mapConfig,
                HighestCooldownCount = Cooldowns.GetValueOrDefault(mapConfig.MapName),
                LongestTimedCooldown = TimedCooldowns.GetValueOrDefault(mapConfig.MapName, DateTime.MinValue),
            };
        }
    }

    #endregion

    private static FakeMapConfig Map(string name, params string[] tags)
        => new() { MapName = name, SearchTags = tags };

    private static List<string> SortNames(
        IEnumerable<IMapConfig> maps,
        NominationSortOrder order,
        IMapCooldownQueryService? cooldownService = null)
    {
        return McsMapListSorter
            .Sort(maps, c => c, order, cooldownService ?? new FakeCooldownQueryService())
            .Select(c => c.MapName)
            .ToList();
    }

    [Fact]
    public void AlphabeticalAscending_SortsByMapName_CaseInsensitive()
    {
        var maps = new IMapConfig[] { Map("ze_C"), Map("ZE_a"), Map("ze_b") };

        var result = SortNames(maps, NominationSortOrder.AlphabeticalAscending);

        Assert.Equal(["ZE_a", "ze_b", "ze_C"], result);
    }

    [Fact]
    public void AlphabeticalDescending_SortsByMapNameReversed()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_c"), Map("ze_b") };

        var result = SortNames(maps, NominationSortOrder.AlphabeticalDescending);

        Assert.Equal(["ze_c", "ze_b", "ze_a"], result);
    }

    [Fact]
    public void CooldownAscending_SortsByCooldownCount_TieBrokenByName()
    {
        var maps = new IMapConfig[] { Map("ze_b"), Map("ze_a"), Map("ze_c") };
        var service = new FakeCooldownQueryService();
        service.Cooldowns["ze_a"] = 5;
        service.Cooldowns["ze_b"] = 0;
        service.Cooldowns["ze_c"] = 0;

        var result = SortNames(maps, NominationSortOrder.CooldownAscending, service);

        Assert.Equal(["ze_b", "ze_c", "ze_a"], result);
    }

    [Fact]
    public void CooldownDescending_SortsByCooldownCountReversed()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_b"), Map("ze_c") };
        var service = new FakeCooldownQueryService();
        service.Cooldowns["ze_a"] = 1;
        service.Cooldowns["ze_b"] = 3;
        service.Cooldowns["ze_c"] = 2;

        var result = SortNames(maps, NominationSortOrder.CooldownDescending, service);

        Assert.Equal(["ze_b", "ze_c", "ze_a"], result);
    }

    [Fact]
    public void TimedCooldownAscending_SortsByTimedCooldown_TieBrokenByName()
    {
        var maps = new IMapConfig[] { Map("ze_c"), Map("ze_b"), Map("ze_a") };
        var service = new FakeCooldownQueryService();
        service.TimedCooldowns["ze_b"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        service.TimedCooldowns["ze_c"] = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = SortNames(maps, NominationSortOrder.TimedCooldownAscending, service);

        Assert.Equal(["ze_a", "ze_b", "ze_c"], result);
    }

    [Fact]
    public void TimedCooldownDescending_SortsByTimedCooldownReversed()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_b") };
        var service = new FakeCooldownQueryService();
        service.TimedCooldowns["ze_a"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        service.TimedCooldowns["ze_b"] = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = SortNames(maps, NominationSortOrder.TimedCooldownDescending, service);

        Assert.Equal(["ze_b", "ze_a"], result);
    }

    [Fact]
    public void TagAscending_SortsBySmallestTag_UntaggedLast()
    {
        var maps = new IMapConfig[]
        {
            Map("ze_untagged"),
            Map("ze_hard", "hard", "long"),
            Map("ze_easy", "short", "easy"),
        };

        var result = SortNames(maps, NominationSortOrder.TagAscending);

        Assert.Equal(["ze_easy", "ze_hard", "ze_untagged"], result);
    }

    [Fact]
    public void TagDescending_UntaggedStillLast()
    {
        var maps = new IMapConfig[]
        {
            Map("ze_untagged"),
            Map("ze_easy", "easy"),
            Map("ze_hard", "hard"),
        };

        var result = SortNames(maps, NominationSortOrder.TagDescending);

        Assert.Equal(["ze_hard", "ze_easy", "ze_untagged"], result);
    }

    [Fact]
    public void TagAscending_SameTag_TieBrokenByName()
    {
        var maps = new IMapConfig[]
        {
            Map("ze_b", "easy"),
            Map("ze_a", "easy"),
        };

        var result = SortNames(maps, NominationSortOrder.TagAscending);

        Assert.Equal(["ze_a", "ze_b"], result);
    }

    [Fact]
    public void Sort_WithSelector_SortsWrappedElements()
    {
        var items = new List<(string Label, IMapConfig Config)>
        {
            ("second", Map("ze_b")),
            ("first", Map("ze_a")),
        };

        var result = McsMapListSorter.Sort(
            items, i => i.Config, NominationSortOrder.AlphabeticalAscending, new FakeCooldownQueryService());

        Assert.Equal(["first", "second"], result.Select(i => i.Label).ToList());
    }

    [Fact]
    public void Sort_DoesNotMutateSource()
    {
        var maps = new List<IMapConfig> { Map("ze_b"), Map("ze_a") };

        McsMapListSorter.Sort(maps, c => c, NominationSortOrder.AlphabeticalAscending, new FakeCooldownQueryService());

        Assert.Equal("ze_b", maps[0].MapName);
    }
}
