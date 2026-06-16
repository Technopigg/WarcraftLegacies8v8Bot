# LegaciesBot — Commands

Discord bot for 8v8 Warcraft III: Legacies matches.
Cycle: `!join` × 16 → team draft → faction assignment → upload replay to site → Elo on site.

**Env vars:** `WL8v8_BOT_TOKEN` (required), `WL_DISCORD_GUILD_ID`, `WL_DISCORD_DRAFT_CHANNEL_ID`,
`WL_DISCORD_TEAM1/2_ROLE_ID`, `WL_DISCORD_COMMAND_CHANNEL_ID`, `WL_DISCORD_REPLAY_CHANNEL_ID`.
Defaults in `Config/DiscordConfig.cs`.

**Roles:** Player → Captain (2 slots, `!captain`) → Moderator (`Mods[]`) → Admin (`Admins[]`).
Bootstrap: if `Admins[]` is empty — add the first admin manually to `permissions.json` (in `/app/data` volume).

**Season 5 architecture:** match results and ratings (Elo) are stored exclusively on **warcraftlegacies.com**.
The bot manages lobby, draft, factions, and Discord roles. The winner is determined on the site via replay.
`!score`, `!scores`, `!forcescore` are disabled — they contradict this architecture.
`!stats`, `!leaderboard`, `!compare`, `!recent` redirect to the site (Phase 5 — full API integration).

---

### 1. Registration and profile

1. `!register` / `!reg` — register. Required before `!join`.
2. `!nickname <nick>` — set display name (2–20 chars, letters and digits only).
3. `!nickname <@user> <nick>` — set another player's nickname. Permission: mod/admin.

### 2. Lobby

4. `!join` / `!j` — join the lobby. At 16/16, the draft starts automatically.
5. `!leave` / `!l` — leave the lobby. ⚠️ Has no effect after `IsLocked`, but the command still replies "has left".
6. `!lobby` — show the player list and their Elo at join time.

### 3. Faction preferences

7. `!prefs` / `!p` — show own preferences.
8. `!prefs @user` — another player's preferences.
9. `!prefs clear` — clear own list.
10. `!prefs add <Faction>` — append a faction. Duplicates are silently ignored.
11. `!prefs remove <Faction>` — remove a faction.
12. `!prefs <F1> <F2> ...` — replace the list entirely. ⚠️ Invalid factions are silently dropped.

### 4. Captain draft

13. `!captain` — claim Captain A or B slot (first caller gets A). Creates the Captain role if missing.
14. `!uncaptain` / `!drop` — give up captaincy. ⚠️ The Discord Captain role is not removed.
15. `!captains` — show current captains.
16. `!pass` — Captain A passes the first pick to Captain B. Only before the first pick. Reply goes to `[draft]`.
17. `!d <player>` / `!draft <player>` — pick a player (your turn in snake order). Reply goes to `[draft]`.

### 5. Manual faction assignment

18. `!assignf <player> <faction>` — assign one faction to a player on your team. Short codes: `lord`, `sc`, `fel`, `sw`, `ws`, etc.
19. `!assignfactions <...>` — bulk assignment, one `<player> <faction>` per line. Partial success is not rolled back.
20. `!lockfactions` — lock your team's factions. Requires 8 players and 8 assigned factions. When both teams lock — the game becomes active.

### 6. Draft mode

21. `!mode` / `!mode show` — current mode.
22. `!mode auto` — AutoDraft + AutoFaction. Permission: captain / mod / admin.
23. `!mode auto-manual` — AutoDraft + manual factions.
24. `!mode captain` / `!mode captain-manual` — Captain Draft + manual factions.
25. `!mode captain-auto` — Captain Draft + AutoFaction.

### 7. Game

26. `!games` / `!g` — list active and unfinished games.
27. `!kill [gameId]` — emergency termination: removes Discord roles, resets lobby. No result recorded. Permission: mod/admin.
28. `!sub <out> <in>` — substitute a player in an active game. `out` = leaving player (@mention or nickname), `in` = replacement. Faction is transferred; Discord team roles are reassigned. Permission: mod/admin.

### 8. Stats and history

> Season 5: ratings and match history are on the site. These commands redirect to warcraftlegacies.com.

29. `!stats` — player stats → **https://warcraftlegacies.com/players**
30. `!leaderboard` — leaderboard → **https://warcraftlegacies.com/leaderboard**
31. `!compare` — player comparison → **https://warcraftlegacies.com/players**
32. `!recent` — match history → **https://warcraftlegacies.com/replays**

### 9. Seasons

33. `!season` — current season: number, start date, player count with matches.
34. `!season history` — all seasons.
35. `!season show <n>` — details for a specific season.
36. `!season summary` — hall of fame: top Elo, Best Winrate (≥5 games only), Most Games/Wins/Losses, Most Improved.
37. `!season showleaderboard <n>` — top 20 players for a specific season.
38. `!season start` — **irreversible**: end the current season and start a new one. Permission: mod/admin.

### 10. Moderation

39. `!warn <user> <reason>` — issue a warning. At ≥3 active warnings — auto-ban. Permission: mod/admin.
40. `!removewarn <user> <index>` — remove a warning (0-based). Drops below 3 → auto-unban. Permission: mod/admin.
41. `!ban <user> <reason>` — direct ban. Permission: mod/admin.
42. `!unban <user>` — lift a ban. ⚠️ Clears the entire `Bans[]` list. Permission: mod/admin.
43. `!warns <user>` — ban status and active warnings. Permission: none (anyone can read).

### 11. Permission management

44. `!addadmin <@/id>` — add an admin. Current admins only.
45. `!removeadmin <@/id>` — remove an admin. Self-removal is possible.
46. `!addmod <@/id>` — add a moderator. Admins only.
47. `!removemod <@/id>` — remove a moderator. Admins only.
48. `!admins` — list admins.
49. `!mods` — list moderators. ⚠️ Does not include admins.

### 12. Debug (mod/admin)

50. `!debugfill` — fill the lobby with 16 fake players (clears current players).
51. `!debugcaptains [gameId]` — assign Captain A/B to the first two players.
52. `!debugdraft [gameId]` — run the draft engine with seed 12345.
53. `!debugfactions [gameId]` — auto-assign factions.
54. `!debugstart [gameId]` — set the game to `IsActive = true`.
55. `!debugstate [gameId]` — dump lobby state. ⚠️ No permission check.
56. `!debugclear [gameId]` — reset lobby.

### 13. Help

57. `!help` — command list.

---

