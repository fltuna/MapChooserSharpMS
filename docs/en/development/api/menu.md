# Nomination Menu Compat API

MCS separates menu rendering from the plugin core through an abstraction layer.
This allows server operators to choose their preferred menu plugin (FPM MenuManager, Wuling IMenu, etc.).

Access via `IMapChooserSharpShared.SetNominationMenuCompat()`.

---

## Overview

1. A companion module (e.g. `McsFPMCompat`) creates an `IMcsNominationMenuCompat` implementation in `OnAllModulesLoaded`
2. It registers it via `IMapChooserSharpShared.SetNominationMenuCompat()`
3. When MCS needs to display a nomination menu, it calls `ShowNominationMenu()` on the registered implementation

If MCS attempts to display a menu before any implementation is registered, an `InvalidOperationException` is thrown.

---

## IMcsNominationMenuCompat

Menu compat adapter for nomination-related menus.

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Member | Type | Description |
|---|---|---|
| `NominationMenuService` | `INominationMenuManagementService` | Set by MCS during registration. Initialize with `null!` — MCS sets this before any menu is shown |

| Method | Return Type | Description |
|---|---|---|
| `ShowNominationMenu(IGameClient, McsNominationMenuContext)` | `void` | Display the nomination menu to the target client |
| `CloseMenu(IGameClient)` | `void` | Close the currently open MCS menu for the client. No-op when no menu is open |
| `Cleanup()` | `void` | Discard all cached menu state. Called during plugin unload or map changes |

---

## McsNominationMenuContext

Context passed to `ShowNominationMenu`. Contains all data and services the compat needs to build a rich nomination menu.

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Property | Type | Description |
|---|---|---|
| `Title` | `string` | Menu title string |
| `Items` | `IReadOnlyList<McsNominationMenuItem>` | List of nomination menu items, pre-sorted by map name ascending |
| `AllowSortSelection` | `bool` | True when the compat may offer a sort-order selection (e.g. via `McsMapListSorter`) before showing the list. True for full map list menus; false for search results, confirmation and removal menus |
| `ToolingService` | `IMapConfigToolingService` | Utilities for resolving display names, checking Workshop IDs, etc. |
| `CooldownQueryService` | `IMapCooldownQueryService` | Query cooldown state for maps |
| `NominationMenuService` | `INominationMenuManagementService` | Same service as the property on the compat interface |

All properties except `AllowSortSelection` (optional, defaults to `false`) are `required init`.
Honoring `AllowSortSelection` is optional — a compat may simply display `Items` in the given order.

---

## McsNominationMenuItem

A single row in a nomination menu.

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Property | Type | Description |
|---|---|---|
| `DisplayText` | `string` | Display text (already translated) |
| `MapConfig` | `IMapConfig` | The map configuration associated with this item |
| `OnNominate` | `Action<IGameClient>?` | Callback invoked when the client selects this item. `null` means no-op |

---

## McsMenuItem

A generic menu item used by nomination detail menus and events (e.g. `OnNominationMenuDetailsOpening`).

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Property | Type | Description |
|---|---|---|
| `DisplayText` | `string` | Display text (already translated) |
| `OnSelect` | `Action<IGameClient>?` | Callback invoked when the client selects this item. `null` means no-op |

---

## INominationMenuManagementService

Service for showing and managing nomination menus. Available via `IMcsNominationMenuCompat.NominationMenuService` (set by MCS during registration) and `IMcsNominationController.NominationMenuManagementService`.

| Method | Return Type | Description |
|---|---|---|
| `ShowNominationMenu(IGameClient, List<IMapConfig>)` | `void` | Show nomination menu with the specified map list |
| `ShowNominationMenu(IGameClient)` | `void` | Show nomination menu with all maps |
| `ShowAdminNominationMenu(IGameClient, List<IMapConfig>)` | `void` | Show admin nomination menu with the specified map list |
| `ShowAdminNominationMenu(IGameClient)` | `void` | Show admin nomination menu with all maps |
| `ShowRemoveNominationMenu(IGameClient, List<IMcsNominationData>)` | `void` | Show nomination removal menu with the specified nominations |
| `ShowRemoveNominationMenu(IGameClient)` | `void` | Show nomination removal menu with all nominations |
| `NominateOrConfirm(IGameClient, IMapConfig, bool)` | `void` | Nominate a map. Non-admin nominations always show a confirm menu first; admin nominations execute immediately |
| `CollectExtraMenuItems(IMapConfig, IGameClient)` | `List<McsMenuItem>` | Fire `OnNominationMenuDetailsOpening` and return the collected extra items |

---

## Registration

Register the nomination menu compat via `IMapChooserSharpShared`:

```csharp
public void OnAllModulesLoaded()
{
    var mcs = _sharedSystem.GetSharpModuleManager()
        .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
            IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

    var menuManager = GetMenuManager(); // your menu plugin's API

    mcs.SetNominationMenuCompat(new MyNominationMenuCompat(menuManager));
}
```

---

## OnNominationMenuDetailsOpening Event

Fired when a nomination detail/confirm menu is about to be shown. External plugins can append extra `McsMenuItem` items.

Implement `INominationEventListener.OnNominationMenuDetailsOpening`:

```csharp
public void OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams @params)
{
    @params.ExtraItems.Add(new McsMenuItem
    {
        DisplayText = $"Cooldown: {@params.MapConfig.CooldownConfig.CurrentCooldown}",
        OnSelect = _ => { },
    });
}
```

| Parameter | Type | Description |
|---|---|---|
| `MapConfig` | `IMapConfig` | The map whose detail menu is being shown |
| `Client` | `IGameClient` | The client who is opening the menu |
| `ExtraItems` | `List<McsMenuItem>` | Mutable list — append your extra items here |

---

## Implementation Example

```csharp
public sealed class MyNominationMenuCompat : IMcsNominationMenuCompat
{
    private readonly Dictionary<IGameClient, object> _activeMenus = new();

    public INominationMenuManagementService NominationMenuService { get; set; } = null!;

    public void ShowNominationMenu(IGameClient target, McsNominationMenuContext context)
    {
        CloseMenu(target);

        var builder = new SomeMenuBuilder();
        builder.SetTitle(context.Title);

        foreach (var item in context.Items)
        {
            var onNominate = item.OnNominate;
            builder.AddItem(item.DisplayText, () =>
            {
                CloseMenu(target);
                onNominate?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;
        SomeMenuPlugin.Display(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;
        SomeMenuPlugin.Close(target, menu);
        _activeMenus.Remove(target);
    }

    public void Cleanup() => _activeMenus.Clear();
}
```
