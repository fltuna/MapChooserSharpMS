# Nomination API

Provides functionality for players and server administrators to recommend (nominate) maps as vote candidates.
Nominated maps are given priority when building the map vote candidate list.

Access via `IMapChooserSharpShared.McsNominationController`.

---

## IMcsNominationController

Top-level facade for the nomination module. Provides access to services and managers, and event listener registration.

| Member | Type | Description |
|---|---|---|
| `NominationService` | `IMapNominationService` | Service for adding and removing nominations |
| `NominationValidateService` | `INominationValidateService` | Validation service for checking nomination eligibility |
| `NominationMenuManagementService` | `INominationMenuManagementService` | Service for displaying nomination menus |
| `NominationManager` | `INominationManager` | Manager for reading current nomination state |

| Method | Return Type | Description |
|---|---|---|
| `InstallEventListener(INominationEventListener)` | `void` | Register a nomination event listener |
| `RemoveEventListener(INominationEventListener)` | `void` | Unregister a listener |

---

## IMapNominationService

Core service for adding and removing nominations. Handles validation, event firing, and state updates in one operation.

| Method | Return Type | Description |
|---|---|---|
| `TryNominateMap(IGameClient, IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | Attempt a player nomination. Empty list = success |
| `TryAdminNominateMap(IGameClient?, IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | Attempt an admin nomination. `null` nominator is treated as console execution. Empty list = success |
| `TryRemoveNomination(IMapConfig, IGameClient?, bool)` | `bool` | Remove a nomination for the specified map. When `forceRemoval` is `true`, admin nominations can also be forcibly removed |
| `TryUnNominate(IGameClient, UnNominateReason)` | `bool` | Remove a player's participation from their current nomination. If they were the only participant and it is not admin-forced, the entire nomination entry is also removed |
| `TryUnNominate(int, UnNominateReason)` | `bool` | Slot-based variant of `TryUnNominate`. For use in disconnect hooks when `IGameClient` is unavailable |
| `ClearNominations()` | `bool` | Clear all nominations |

### Nomination Flow

1. Player calls `TryNominateMap`
2. Internal validation via `INominationValidateService.PlayerCanNominateMap` (permissions, player count, day/time, cooldown, etc.)
3. After validation passes, `INominationEventListener.OnNominationCheckPassed` fires (external plugin additional validation)
4. If no external plugin cancels, `INominationEventListener.OnNomination` fires (final cancellation point)
5. All passed -> nomination succeeds, `OnNominationChanged` event fires
6. If rejected at any stage -> a `NominationCheckResult` list is returned

For admin nominations, step 2 uses `CanAdminNominateMap` instead, which bypasses player count, day/time, and permission checks.

---

## INominationValidateService

Service exposing individual validation conditions. Provides both composite check methods and individual check methods.

### Composite Check Methods

| Method | Return Type | Description |
|---|---|---|
| `PlayerCanNominateMap(IGameClient, IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | Full validation for player nomination. Empty list = allowed |
| `CanPickupMap(IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | Validation for random map selection. Player-dependent checks (e.g. permissions) are skipped. Empty list = eligible |
| `CanAdminNominateMap(IMapConfig, IGameClient?)` | `IReadOnlyList<NominationCheckResult>` | Admin nomination validation. `null` nominator = console execution. Empty list = allowed |
| `GetNominationState(IMapConfig, IGameClient?)` | `IReadOnlyList<NominationCheckResult>` | Returns the nomination state for the given map: `AlreadyNominated`, `NominatedByAdmin`, or empty list (not nominated) |

### Individual Check Methods

| Method | Return Type | Description |
|---|---|---|
| `IsDuringVotingPeriod()` | `bool` | Whether a vote is currently in progress |
| `IsMapDisabled(IMapConfig)` | `bool` | Whether the map is disabled |
| `IsCurrentMap(IMapConfig)` | `bool` | Whether this is the currently playing map |
| `IsWithinTimeRange(IMapConfig)` | `bool` | Whether the current time is within the map's allowed time ranges |
| `IsWithinAllowedDays(IMapConfig)` | `bool` | Whether today is one of the map's allowed days |
| `IsGreaterThanMinPlayers(IMapConfig)` | `bool` | Whether the current player count meets the minimum |
| `IsLowerThanMaxPlayers(IMapConfig)` | `bool` | Whether the current player count is below the maximum |
| `IsMapInCooldown(IMapConfig)` | `bool` | Whether the map is on cooldown |
| `SharesCooldownGroupWithCurrentMap(IMapConfig)` | `bool` | Whether the map shares a cooldown-enabled group (config `Cooldown` > 0 or `CooldownDateTime` set) with the currently playing map. Returns `false` for the current map itself (covered by `IsCurrentMap`) |
| `IsMapInNominationCooldown(IMapConfig)` | `bool` | Whether the map is on nomination-specific cooldown |
| `IsPlayerInNominationCooldown(ulong)` | `bool` | Whether the player (by SteamID) is on per-player nomination cooldown |
| `GetPlayerCooldownState(ulong)` | `IPlayerNominationCooldownState?` | Returns the player's nomination cooldown state, or `null` if not on cooldown |
| `HasReachedGroupNominationLimit(IMapConfig)` | `bool` | Whether the map's group has reached its nomination limit |
| `HasBypassPermission(IMapConfig, IGameClient)` | `bool` | Whether the player has a bypass permission that skips all nomination checks (exact match) |
| `IsPlayerAllowedByPermission(IMapConfig, IGameClient)` | `bool` | Whether the player has an allow permission for restricted maps (wildcard-capable) |
| `IsPlayerDeniedByPermission(IMapConfig, IGameClient)` | `bool` | Whether the player is denied by permission nodes (exact match). Resolution order: Any Deny > Any Allow > Default (allowed) |
| `GetCooldownInformations(IMapConfig)` | `IDetailedCooldownResult` | Detailed cooldown breakdown for the map |

---

## INominationMenuManagementService

Service for managing in-game nomination menu display.

| Method | Return Type | Description |
|---|---|---|
| `ShowNominationMenu(IGameClient, List<IMapConfig>)` | `void` | Show a nomination menu with the specified map list |
| `ShowNominationMenu(IGameClient)` | `void` | Show a nomination menu with all maps |
| `ShowAdminNominationMenu(IGameClient, List<IMapConfig>)` | `void` | Show an admin nomination menu with the specified map list |
| `ShowAdminNominationMenu(IGameClient)` | `void` | Show an admin nomination menu with all maps |
| `ShowRemoveNominationMenu(IGameClient, List<IMcsNominationData>)` | `void` | Show a nomination removal menu with the specified nomination data |
| `ShowRemoveNominationMenu(IGameClient)` | `void` | Show a nomination removal menu with all current nominations |
| `NominateOrConfirm(IGameClient, IMapConfig, bool)` | `void` | Nominate a map. Non-admin nominations always show a confirmation menu first. When `isAdmin` is `true`, the nomination is processed immediately as an admin nomination |

---

## INominationManager

Read-only manager for current nomination state.

| Property | Type | Description |
|---|---|---|
| `NominatedMaps` | `IReadOnlyDictionary<string, IMcsNominationData>` | Dictionary of nominated maps. Key is the map name |

---

## IMcsNominationData

Data for an individual nomination entry.

| Property | Type | Description |
|---|---|---|
| `MapConfig` | `IMapConfig` | Configuration of the nominated map |
| `NominationParticipants` | `IReadOnlySet<int>` | Set of UserIDs for players participating in this nomination |
| `IsForceNominated` | `bool` | Whether this was force-nominated by an admin |

When multiple players nominate the same map, all of them are added to `NominationParticipants`. For admin nominations, `IsForceNominated` is `true` and the nomination persists even if all participants leave.

---

## IDetailedCooldownResult

Provides a detailed breakdown of cooldowns applied to a map. Includes both the map's own cooldown and cooldowns applied through its groups.

MCS cooldowns have two axes:

- **Count-based**: A counter set to the configured value when the map is played, decremented by 1 each time another map is played
- **Timed**: A cooldown that remains active until the configured duration elapses from when the map was last played

When a map belongs to multiple groups, each group applies its cooldown independently. `HighestCooldownCount` / `LongestTimedCooldown` return the most restrictive values across the map and all its groups.

| Property | Type | Description |
|---|---|---|
| `HasCooldown` | `bool` | `true` when any cooldown is currently active |
| `HighestCooldownCount` | `int` | Highest count-based cooldown across the map and all groups |
| `LongestTimedCooldown` | `DateTime` | Latest timed cooldown expiration (UTC) across the map and all groups |
| `MapConfig` | `IMapConfig` | The target map's configuration. Can be used to reference default cooldown values |
| `CooldownCount` | `int` | The map's own current count-based cooldown |
| `TimedCooldown` | `DateTime` | The map's own timed cooldown expiration (UTC) |
| `GroupCooldowns` | `IReadOnlyDictionary<string, int>` | Count-based cooldowns keyed by group name |
| `GroupTimedCooldowns` | `IReadOnlyDictionary<string, DateTime>` | Timed cooldown expirations (UTC) keyed by group name |

---

## NominationCheckResult

Enum representing reasons why a nomination was rejected. Methods like `TryNominateMap` return an empty list on success, and a list of all applicable values on failure.

| Value | Description |
|---|---|
| `Disabled` | The map is disabled (`IsDisabled = true`) |
| `NotEnoughPermissions` | The player is denied by permission nodes |
| `TooMuchPlayers` | The server player count exceeds `MaxPlayers` |
| `NotEnoughPlayers` | The server player count is below `MinPlayers` |
| `VotingPeriod` | A vote is in progress; nominations are blocked |
| `OnlySpecificDay` | Today is not an allowed nomination day for this map |
| `OnlySpecificTime` | The current time is outside the allowed nomination time range |
| `MapIsInCooldown` | The map or one of its groups is on cooldown |
| `NominationCooldownActive` | Nomination-specific cooldown is active (applied after a map is consumed as a vote candidate) |
| `AlreadyNominated` | The map is already nominated |
| `NominatedByAdmin` | The map has been force-nominated by an admin (cannot be overwritten by normal players) |
| `SameMap` | The map is the same as the currently playing map |
| `GroupNominationLimitReached` | The map's group has reached its nomination limit |
| `CancelledByExternalPlugin` | Cancelled by an external plugin's event listener |
| `ProhibitAdminNomination` | Admin nomination is prohibited for this map in its configuration |
| `PlayerCooldownActive` | The player is on per-player nomination cooldown |
| `SameMapGroup` | The map shares a cooldown-enabled group with the currently playing map |

---

## IPlayerNominationCooldownState

Per-player nomination cooldown state.

| Property | Type | Description |
|---|---|---|
| `RemainingCount` | `int` | Remaining cooldown count |
| `CooldownUntil` | `DateTime` | Cooldown expiration time |

---

## NominationSortOrder

Enum specifying the sort order for map lists in nomination menus and similar contexts.

| Value | Description |
|---|---|
| `AlphabeticalAscending` | Map name ascending (A to Z) |
| `AlphabeticalDescending` | Map name descending (Z to A) |
| `CooldownAscending` | Count-based cooldown ascending (fewest remaining first) |
| `CooldownDescending` | Count-based cooldown descending (most remaining first) |
| `TimedCooldownAscending` | Timed cooldown ascending (soonest expiration first) |
| `TimedCooldownDescending` | Timed cooldown descending (latest expiration first) |
| `TagAscending` | Smallest search tag ascending (A to Z). Maps without tags are placed last |
| `TagDescending` | Smallest search tag descending (Z to A). Maps without tags are placed last |

---

## McsMapListSorter

Static helper that sorts map lists by a `NominationSortOrder`. Used by MCS core to apply the
default order (map name ascending) when building nomination menus, and available to menu compat
plugins for player-selected sort orders.

**Namespace**: `MapChooserSharpMS.Shared.Nomination`

```csharp
public static List<T> Sort<T>(
    IEnumerable<T> source,
    Func<T, IMapConfig> configSelector,
    NominationSortOrder order,
    IMapCooldownQueryService cooldownQueryService)
```

- Returns a new sorted list; the source is never mutated.
- Non-alphabetical orders tie-break by map name ascending.
- Cooldown-based orders use `IMapCooldownQueryService.GetCurrentCooldowns()` (in-memory only).
- Tag orders use the alphabetically smallest tag of each map as the key; maps without tags always sort last.
- String comparison is ordinal, case-insensitive.

---

## UnNominateReason

Enum representing why a player's nomination participation was removed. Passed as an argument to `TryUnNominate` and included in the `OnUnNominate` event parameters.

| Value | Description |
|---|---|
| `Normally` | Voluntary un-nomination. The player nominated a different map or explicitly cancelled their nomination |
| `PlayerDisconnect` | The player disconnected from the server; their participation was automatically removed by the disconnect hook |

---

## Event Listener (INominationEventListener)

Listener interface registered via `IMcsNominationController.InstallEventListener`. All methods have default implementations, so you only need to override the events you are interested in.

| Method | Return Type | Description |
|---|---|---|
| `OnNominationCheckPassed(INominationCheckPassedEventParams)` | `McsCancellableEvent` | Fires after internal validation passes. Return `Stop` to reject the nomination (for external plugin additional validation) |
| `OnNomination(INominationParams)` | `McsCancellableEvent` | Fires just before a normal nomination is committed. Return `Stop` to cancel |
| `OnAdminNomination(IAdminNominationParams)` | `McsCancellableEvent` | Fires just before an admin nomination is committed. Return `Stop` to cancel |
| `OnNominationChanged(INominationChangeParams)` | `void` | Fires when nomination state changes (addition or participant change) |
| `OnNominationRemoved(INominationRemovedParams)` | `void` | Fires when a nomination entry is removed |
| `OnUnNominate(IUnNominateParams)` | `void` | Fires when a player's nomination participation is removed. Called per client -- if the last participant leaves a non-admin nomination, an additional `OnNominationRemoved` fires for the whole entry |
| `OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams)` | `void` | Fires when a nomination detail menu is about to open. Add extra `McsMenuItem` via `ExtraItems` |
