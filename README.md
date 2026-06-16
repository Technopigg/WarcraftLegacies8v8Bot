# LegaciesBot — Commands

Discord bot for 8v8 Warcraft III: Legacies matches.
Cycle: `!join` × 16 → team draft → faction assignment → `!score` / `!forcescore` → Elo.

**Env vars:** `WL8v8_BOT_TOKEN` (required), `WL_DISCORD_GUILD_ID`, `WL_DISCORD_DRAFT_CHANNEL_ID`,
`WL_DISCORD_TEAM1/2_ROLE_ID`, `WL_DISCORD_COMMAND_CHANNEL_ID`, `WL_DISCORD_REPLAY_CHANNEL_ID`.
Defaults in `Config/DiscordConfig.cs`. Test/prod values in `DEPLOY.md` (agent-docs).

**Roles:** Player → Captain (2 slots, `!captain`) → Moderator (`Mods[]`) → Admin (`Admins[]`).
Bootstrap: if `Admins[]` is empty — add the first admin manually to `permissions.json`.

---

### Registration and profile

`!register` / `!reg` — register. Required before `!join`.
`!nickname <nick>` — set display name (2–20 chars, letters and digits only).
`!nickname <@user> <nick>` — set another player's nickname. Permission: mod/admin.
`!recent` — last 5 matches with rosters and Elo changes.

### Lobby

`!join` / `!j` — join the lobby. At 16/16, the draft starts automatically.
`!leave` / `!l` — leave the lobby. ⚠️ Has no effect after `IsLocked`, but the command still replies "has left".
`!lobby` — show the player list and their Elo at join time.

### Faction preferences

`!prefs` / `!p` — show own preferences.
`!prefs @user` — another player's preferences.
`!prefs clear` — clear own list.
`!prefs add <Faction>` — append a faction. Duplicates are silently ignored.
`!prefs remove <Faction>` — remove a faction.
`!prefs <F1> <F2> ...` — replace the list entirely. ⚠️ Invalid factions are silently dropped; if all are invalid — no reply, no change.

### Captain draft

`!captain` — claim Captain A or B slot (first caller gets A). Creates the Captain role if missing.
`!uncaptain` / `!drop` — give up captaincy. ⚠️ The Discord Captain role is not removed.
`!captains` — show current captains.
`!pass` — Captain A passes the first pick to Captain B. Only before the first pick. Reply goes to `[draft]`.
`!d <player>` / `!draft <player>` — pick a player (your turn in snake order). Reply goes to `[draft]`.

### Manual faction assignment

`!assignf <player> <faction>` — assign one faction to a player on your team. Short codes work: `lord`, `sc`, `fel`, `sw`, `ws`, etc. Any error → single `"Invalid faction assignment."`.
`!assignfactions <...>` — bulk assignment, one `<player> <faction>` per line. Partial success is not rolled back.
`!lockfactions` — lock your team's factions. Requires 8 players and 8 assigned factions. ⚠️ Summary shows Discord IDs, not names. When both teams lock — the game becomes active.

### Draft mode

`!mode` / `!mode show` — current mode.
`!mode auto` — AutoDraft + AutoFaction. Permission: captain / mod / admin.
`!mode auto-manual` — AutoDraft + manual factions.
`!mode captain` / `!mode captain-manual` — Captain Draft + manual factions.
`!mode captain-auto` — Captain Draft + AutoFaction.

### Game and result

`!games` / `!g` — list active and unfinished games.
`!score <0|1>` — vote for the winner: `1` = Team A, `0` = Team B. Threshold — 6 votes.
`!scores` — voting summary: who voted for whom, who hasn't voted yet.
`!forcescore [gameId] <scoreA> <scoreB>` — force a result (`1 0` / `0 1` / `0 0`). Permission: mod/admin or own game captain. Recalculates Elo and writes to history.
`!kill [gameId]` — emergency termination, no Elo change, no history record. Permission: mod/admin.

### Stats

`!stats [user] [lifetime|season]` — player stats. Default: current season.
`!leaderboard [count|lifetime] [count]` — top players by Elo. Default: season, 10 entries. `count` up to 50.
`!compare <user1> <user2> [lifetime]` — side-by-side comparison of two players.

### Seasons

`!season` — current season: number, start date, player count with matches.
`!season history` — all seasons.
`!season show <n>` — details for a specific season.
`!season summary` — hall of fame: top Elo, Best Winrate (≥5 games only), Most Games/Wins/Losses, Most Improved.
`!season showleaderboard <n>` — top 20 players for a specific season.
`!season start` — **irreversible**: end the current season and start a new one. Permission: mod/admin.

### Moderation

`!warn <user> <reason>` — issue a warning. At ≥3 active warnings — auto-ban. Permission: mod/admin.
`!removewarn <user> <index>` — remove a warning (0-based). Drops below 3 warnings → auto-unban. Permission: mod/admin.
`!ban <user> <reason>` — direct ban. Permission: mod/admin.
`!unban <user>` — lift a ban. ⚠️ Clears the entire `Bans[]` list, not one entry. Permission: mod/admin.
`!warns <user>` — ban status and active warnings. Permission: none (anyone can read).

### Permission management

`!addadmin <@/id>` — add an admin. Current admins only.
`!removeadmin <@/id>` — remove an admin. Self-removal is possible.
`!addmod <@/id>` — add a moderator. Admins only.
`!removemod <@/id>` — remove a moderator. Admins only.
`!admins` — list admins.
`!mods` — list moderators. ⚠️ Does not include admins.

### Debug (mod/admin)

`!debugfill` — fill the lobby with 16 fake players (clears current players).
`!debugcaptains [gameId]` — assign Captain A/B to the first two players.
`!debugdraft [gameId]` — run the draft engine with seed 12345.
`!debugfactions [gameId]` — auto-assign factions.
`!debugstart [gameId]` — set the game to `IsActive = true`.
`!debugstate [gameId]` — dump lobby state. ⚠️ No permission check.
`!debugclear [gameId]` — reset lobby. ⚠️ Does not reset `IsLocked`, roles, or `ManualFactionAssignments`.

---

**Not implemented:** `!replay` (Phase 1.5) · `!sub` (Phase 3) · `!recalc` (Phase 4)
