# Command List

All commands are registered with the `ms_` prefix (e.g. `ms_nominate`).
In chat, `!nominate` / `css_nominate` can also be used.

## Map Name Search

Every command that takes a `<map>` argument (`!nominate`, `!nominate_addmap`, `!nominate_removemap`, `!map`, `!setnextmap`, `!mapinfo`) shares the same search behavior:

1. Case-insensitive **substring match** on the map name
2. When several maps match, an **exact map name** match takes priority
3. When nothing matched, the query is looked up as a **SearchTag**

Exactly one match executes the command immediately. Multiple matches open a selection menu (for players) or list the candidates (for the server console). `!nominate_removemap` searches the current nominations only, so the SearchTag fallback does not apply there.

## Player Commands

| Command | Alias | Description |
|---|---|---|
| !nominate \<map\> | !nom | Nominate a map. Without arguments, shows the full map list. Partial match with multiple hits shows a menu |
| !nomlist | - | Show the current nomination list in vote pick order (admin nominations first, then by nomination count). Entries that would not fit into the vote appear below a separator line. `!nomlist full` shows nominator names for admins |
| !rtv | chat "rtv" | Vote for Rock The Vote |
| !unnominate | !unnom | Remove your nomination |
| !unrtv | - | Cancel your RTV vote |
| !ext | - | Vote to extend the current map (RTV-style counting) |
| !timeleft | chat "timeleft" | Show remaining time/rounds |
| !nextmap | chat "nextmap" | Show the next map |
| !currentmap | chat "currentmap" | Show the current map |
| !mapinfo \<map\> | - | Show map information. Without arguments, shows the current map. Supports partial-match search |
| !extends | - | Show remaining extend count |
| !thetime | chat "thetime" | Show the current server time |
| !mcs_settings \<subcommand\> [value] | !mcss | Change personal settings. `volume` (`vol`) \<0-100\> for vote sound volume, `countdown` (`cd`) \<type\> for countdown UI. No arguments shows current settings |

## Admin Commands

| Command | Alias | Permission Node | Description |
|---|---|---|---|
| !nominate_addmap \<map\> | - | mcs.admin.command.nomination.addmap | Add a map as an admin nomination. Supports partial-match / SearchTag search (disabled maps included) |
| !nominate_addwsmap \<workshopId\> | - | mcs.admin.command.nomination.addwsmap | Add a map as an admin nomination by Workshop ID (fetches from API even if not in config) |
| !nominate_removemap \<map\> | - | mcs.admin.command.nomination.removemap | Remove a map from nominations |
| !map \<map\> | - | mcs.admin.command.mapcycle.map | Immediately change to the specified map. Supports partial-match search; multiple matches open a selection menu |
| !wsmap \<workshopId\> | - | mcs.admin.command.mapcycle.wsmap | Immediately change to a map by Workshop ID (fetches from API even if not in config) |
| !setnextmap \<map\> | - | mcs.admin.command.mapcycle.setnextmap | Set the next map. Supports partial-match search; multiple matches open a selection menu |
| !setnextwsmap \<workshopId\> | - | mcs.admin.command.mapcycle.setnextwsmap | Set the next map by Workshop ID (fetches from API even if not in config) |
| !removenextmap | - | mcs.admin.command.mapcycle.removenextmap | Remove the next map setting |
| !extend \<amount\> | - | mcs.admin.command.mapcycle.extend | Extend the map's time/rounds (negative values to shorten) |
| !voteextend \<minutes\> | !ve | mcs.admin.command.mapcycle.voteextend | Start an extend vote (NVM YesNo vote) |
| !setmapcooldown \<map\> \<cd\> | !setmapcd | mcs.admin.command.mapcycle.setmapcooldown | Set a map's cooldown (count) |
| !setgroupcooldown \<group\> \<cd\> | !setgroupcd | mcs.admin.command.mapcycle.setgroupcooldown | Set a group's cooldown (count) |
| !setmaptcd \<map\> \<duration\> | - | mcs.admin.command.mapcycle.setmaptcd | Set a map's timed cooldown (e.g. 2h, 3d, 1w) |
| !setgrouptcd \<group\> \<duration\> | - | mcs.admin.command.mapcycle.setgrouptcd | Set a group's timed cooldown (e.g. 2h, 3d, 1w) |
| !forceresetmcs | - | mcs.admin.command.mapvote.forceresetmcs | Force reset all MCS state (vote/RTV/nominations) |
| !reloadmapcfgs | - | mcs.admin.command.mapconfig.reloadmapcfgs | Reload map configs |
| !forcertv | - | mcs.admin.command.rtv.forcertv | Force trigger RTV |
| !enablertv | - | mcs.admin.command.rtv.enablertv | Enable RTV |
| !disablertv | - | mcs.admin.command.rtv.disablertv | Disable RTV |
| !enableext | - | mcs.admin.command.mapcycle.enableext | Enable !ext |
| !disableext | - | mcs.admin.command.mapcycle.disableext | Disable !ext |
| !setext \<count\> | - | mcs.admin.command.mapcycle.setext | Set the remaining !ext count |
| !mcsdebug \<subcommand\> | - | mcs.admin.command.mapcycle.mcsdebug | Debug command. `config <map>` shows map config details, `state` shows internal MCS state |

## Permission Nodes (Nomination)

| Node | Description |
|---|---|
| mcs.nominate.map.bypass.\<map\> | Bypass all checks and allow nomination (exact match) |
| mcs.nominate.group.bypass.\<group\> | Same as above (group-level, exact match) |
| mcs.nominate.map.allow.\<map\> | Allow nomination on maps with `RestrictToAllowedUsersOnly = true` (wildcard-capable) |
| mcs.nominate.group.allow.\<group\> | Same as above (group-level) |
| mcs.nominate.map.deny.\<map\> | Deny nomination of a specific map (exact match) |
| mcs.nominate.group.deny.\<group\> | Deny nomination of a specific group (exact match) |
| mcs.admin.command.nomination.nomlist.verbose | Show nominator names with !nomlist full |

Check order: **Bypass (immediate allow)** → Disabled → Cooldown → Deny → **Allow (only when config restricts)** → Normal checks (Day/Time/Players etc.)
