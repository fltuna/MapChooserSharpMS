# Internal Behaviour

Documents implicit behaviours that are not directly configurable but affect how MCS operates.

## Boot Phase (`+host_workshop_map`)

When the server is launched with `+host_workshop_map <workshopId>`, MCS enters a **boot phase**. During this phase:

- Cooldown consumption is suppressed (map cooldowns are not decremented)
- Audit records are not created
- Map cycle transitions are not triggered

This prevents the intermediate maps loaded during workshop map download from consuming cooldowns or generating audit entries.

The boot phase ends when the map matching the specified workshop ID is loaded. From that point on (including that map), all systems operate normally.

If `+host_workshop_map` is not present in the launch arguments, the boot phase is skipped entirely.

## Empty Server Pause (`PauseMapCycleWhenServerEmpty`)

When enabled via `[MapCycle] PauseMapCycleWhenServerEmpty = true`:

- Map cycle transitions are paused while no real players (non-bot, non-HLTV) are on the server
- Cooldown consumption is skipped for maps that end with no players
- The internal time/round limit tracker is also paused, so transitions fire correctly when a player joins

This prevents unattended servers from cycling through maps and wasting cooldowns. When a player connects, normal operation resumes within 1 second.

## Time Limit Management

MCS takes full control of the map's time/round limit:

1. **Config application**: On map start, `MapTime` (minutes) or `MapRounds` from the map config is applied to `mp_timelimit` / `mp_maxrounds`
2. **Internal manager**: MCS reads the ConVar value and initializes an internal TimeLimitManager
3. **ConVar override**: Both `mp_timelimit` and `mp_maxrounds` are set to `99999999` to prevent the game from ending the match on its own
4. **MCS-driven lifecycle**: Vote thresholds and limit-reached events are driven by the internal manager, not the game's native checks

Map config execution (cfg files) runs **before** the MapTime/MapRounds application, so game-related ConVars like `sv_airaccelerate` are set first.

## End Match Flow

MCS installs a **`GoToIntermission` detour** (hook on `server.dll`'s `GoToIntermission` function, resolved at startup). When the time/round limit is reached and the next map is confirmed:

- If `mcs_end_match_immediately = 1` (default): `ForceMatchEnd()` sets `mp_timelimit=0.01` and `mp_maxrounds=1`, then calls `TerminateRound` for the end-of-round visual (Round Won/Lost). The game engine naturally invokes `GoToIntermission` after the round ends, and the MCS detour intercepts it to fire the intermission event.
- If `mcs_end_match_immediately = 0`: the deferred transition flag is set and `mp_timelimit=0.01, mp_maxrounds=1` are applied. On the next natural round end, the deferred handler fires and triggers the transition.

An idempotency guard ensures that intermission is only fired once per map, even if multiple code paths attempt to trigger it.

## Map Vote Candidate Selection (Pick Order)

When a map vote starts, the candidate list is filled in the following order, up to `[Vote] MaxMenuElements` total entries. Duplicates are skipped by map name — the first occurrence wins its slot.

1. **Special option (slot 0)**
   - RTV vote: the *Don't Change* option
   - Scheduled (time/round limit) vote: the *Extend* option — only shown while the map's vote-extend budget remains (`MaxExtends` not exhausted; `MaxExtends = 0` hides it entirely)
2. **Admin nominations** (force-nominated via `!nominate_addmap` / `!nominate_addwsmap`)
   - The `OnAdminNominatedMapPick` event fires first; a listener may replace this list entirely
3. **Community nominations**, sorted by participant count (descending)
   - The `OnNominatedMapPick` event fires first; a listener may replace this list entirely
   - Without an override, nominations whose participant count is below the map's `MinNominationCountForVote` are excluded (and recorded in the audit as `not_picked`)
4. **Random picks** to fill the remaining slots
   - The `OnRandomMapPick` event fires first; a listener may replace this list entirely
   - Without an override, the built-in random pick applies:
     1. **Eligibility filter** — a map is excluded if any of the following holds: disabled, `OnlyNomination = true`, currently being played, already nominated, its group's `NominationLimit` is reached, map or group cooldown is active, player count is outside `MinPlayers`/`MaxPlayers`, or the current day/time is outside `DaysAllowed`/`AllowedTimeRanges`
     2. **`OnNominationCheckPassed` event** — fired once per eligible map; an external plugin may veto individual maps
     3. **Weighted shuffle** — maps are drawn by `MapSelectionWeight` (higher = more likely; `0` = never picked)

Notes:

- Override events always receive the **unfiltered** list (e.g. `OnNominatedMapPick` sees all community nominations before the `MinNominationCountForVote` filter). An override replaces the default filtering entirely — the returned list is used as-is with no further checks.
- All pick events fire synchronously on the game thread. Only the eligibility filter of the built-in random pick runs on a worker thread.
- Nominations that do not make it into the final candidate list are recorded in the audit as `not_picked` (see [Audit System](AUDIT.md)).
- `!nomlist` mirrors this pick order and shows a separator line above the entries that would not make it into the vote (over capacity or below `MinNominationCountForVote`). Pick-override events cannot be predicted and are not reflected.
- Whenever a nomination succeeds while the nomination count exceeds the available map slots (`MaxMenuElements`, minus one for the *Extend* option while the extend budget remains), every current nominator is notified in chat that maps are picked by nomination count and that `!unnom` cancels their nomination.

## Vote Menu Layout

The vote menu (rendered via the Wuling world-HUD menu) is split into two pages:

- **Page 1** — the vote description and a **No Vote** option. Choosing *No Vote* abstains: it never counts toward any candidate's tally, but it does count toward "all participants have voted", so an abstaining player does not delay the early vote finish.
- **Page 2** — the *Extend* (scheduled vote) / *Don't Change* (RTV vote) option **pinned at the top**, followed by the map candidates. When menu shuffle is enabled, only the map candidates are shuffled (per player) — the pinned options always stay on top.

Players flip pages with the menu's Next/Back keys (9/8 by default). `!revote` reopens the menu.

## Map Config Execution (cfg files)

On every map start, MCS executes `.cfg` files from `csgo/cfg/mcsms/maps/` and `csgo/cfg/mcsms/groups/` via the `exec` command. See [Plugin Config](../configuration/PLUGIN_CONFIG.md#map-config-execution-cfg-files) for details on matching modes and directory layout.

## Sound Precache

The `.vsndevts` file specified in `[MapVote.Sound] SoundFile` is automatically precached on every map start during the `OnResourcePrecache` callback. No additional setup is needed beyond setting the path.

## SourceTV Recording

When `[MapCycle] ShouldStopSourceTvRecording = true`, MCS executes `tv_stoprecord` before each map change to prevent SourceTV crashes.

## Map Config Resolution

When resolving the current map's config, MCS checks in this order:

1. **Addon ID** (workshop ID from `GetAddonName()`) — checked first for workshop maps where the BSP name may differ from the config name
2. **Map name** (`GetMapName()`) — standard name-based lookup

This ensures workshop maps are correctly matched even when the server's internal map name doesn't match the config file name.
