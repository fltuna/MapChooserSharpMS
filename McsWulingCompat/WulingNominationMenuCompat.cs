using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;
using Wuling.Abstract.Tianshi.Menu;
using Wuling.Abstract.Tianshi.Registry;

namespace McsWulingCompat;

public sealed class WulingNominationMenuCompat(IMenu menu, IRegistry registry) : IMcsNominationMenuCompat
{
    private static readonly (string Label, NominationSortOrder Order)[] SortOptions =
    [
        ("Name (A → Z)", NominationSortOrder.AlphabeticalAscending),
        ("Name (Z → A)", NominationSortOrder.AlphabeticalDescending),
        ("Cooldown (Low → High)", NominationSortOrder.CooldownAscending),
        ("Cooldown (High → Low)", NominationSortOrder.CooldownDescending),
        ("Tag (A → Z)", NominationSortOrder.TagAscending),
        ("Tag (Z → A)", NominationSortOrder.TagDescending),
    ];

    private readonly Dictionary<int, IMenuInstance> _activeMenus = new();

    public INominationMenuManagementService NominationMenuService { get; set; } = null!;

    public void ShowNominationMenu(IGameClient target, McsNominationMenuContext context)
    {
        var player = registry.GetPlayer(target);
        if (player is null)
            return;

        if (context.AllowSortSelection && context.Items.Count > 1)
        {
            ShowSortSelectionMenu(target, player, context);
            return;
        }

        ShowMapListMenu(target, player, context, context.Items);
    }

    private void ShowSortSelectionMenu(IGameClient target, IPlayerEntry player, McsNominationMenuContext context)
    {
        var instance = menu.CreateMenu();
        instance.Title = $"{context.Title} - Sort";

        foreach (var (label, order) in SortOptions)
        {
            var capturedOrder = order;
            instance.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, label,
                (_, p, _, _) =>
                {
                    var sorted = McsMapListSorter.Sort(
                        context.Items, i => i.MapConfig, capturedOrder, context.CooldownQueryService);
                    ShowMapListMenu(target, p, context, sorted);
                });
        }

        _activeMenus[target.Slot] = instance;
        instance.DisplayToPlayer(player);
    }

    private void ShowMapListMenu(IGameClient target, IPlayerEntry player, McsNominationMenuContext context, IReadOnlyList<McsNominationMenuItem> items)
    {
        var instance = menu.CreateMenu();
        instance.Title = context.Title;

        foreach (var item in items)
        {
            var capturedItem = item;
            instance.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, item.DisplayText,
                (menuInstance, p, _, _) =>
                {
                    ShowDetailMenu(target, p, capturedItem, context);
                });
        }

        _activeMenus[target.Slot] = instance;
        instance.DisplayToPlayer(player);
    }

    private void ShowDetailMenu(IGameClient target, IPlayerEntry player, McsNominationMenuItem item, McsNominationMenuContext context)
    {
        var instance = menu.CreateMenu();
        instance.Title = item.DisplayText;

        instance.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "» Nominate",
            (menuInstance, _, _, _) =>
            {
                menuInstance.Close();
                item.OnNominate?.Invoke(target);
            });

        var cooldownResult = context.CooldownQueryService.GetCurrentCooldowns(item.MapConfig);
        if (cooldownResult.HighestCooldownCount > 0)
            instance.AddItem(MenuItemStyleFlags.Disabled, $"Cooldown: {cooldownResult.HighestCooldownCount} maps");

        if (cooldownResult.LongestTimedCooldown > DateTime.UtcNow)
            instance.AddItem(MenuItemStyleFlags.Disabled, $"Restricted until: {cooldownResult.LongestTimedCooldown.ToLocalTime():yyyy/MM/dd HH:mm}");

        var tags = item.MapConfig.SearchTags;
        if (tags.Count > 0)
            instance.AddItem(MenuItemStyleFlags.Disabled, $"Tags: {string.Join(", ", tags)}");

        var groups = item.MapConfig.GroupSettings;
        if (groups.Count > 0)
            instance.AddItem(MenuItemStyleFlags.Disabled, $"Groups: {string.Join(", ", groups.Select(g => g.GroupName))}");

        var extras = NominationMenuService.CollectExtraMenuItems(item.MapConfig, target);
        foreach (var extra in extras)
        {
            var capturedExtra = extra;
            instance.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, extra.DisplayText,
                (menuInstance, _, _, _) =>
                {
                    menuInstance.Close();
                    capturedExtra.OnSelect?.Invoke(target);
                });
        }

        instance.DisplayToPlayer(player);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target.Slot, out var instance))
            return;

        if (!instance.IsClosed)
            instance.Close();

        _activeMenus.Remove(target.Slot);
    }

    public void Cleanup()
    {
        foreach (var instance in _activeMenus.Values)
        {
            if (!instance.IsClosed)
                instance.Close();
        }

        _activeMenus.Clear();
    }
}
