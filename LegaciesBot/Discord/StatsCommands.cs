using LegaciesBot.Core;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Seasons;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private readonly PlayerStatsService _playerStats;
        private readonly PlayerRegistryService _playerRegistry;
        private readonly NicknameService _nicknames;
        private readonly SeasonService _seasons;

        public StatsCommands()
        {
            _playerStats = GlobalServices.PlayerStatsService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
            _nicknames = GlobalServices.NicknameService;
            _seasons = GlobalServices.SeasonService;
        }

        [Command("stats")]
        public async Task Stats(string? input = null, ulong testUserId = 0)
        {
            var ctx = this.Context;

            bool lifetime = string.Equals(input, "lifetime", StringComparison.OrdinalIgnoreCase);
            bool seasonal = string.Equals(input, "season", StringComparison.OrdinalIgnoreCase);

            ulong targetId =
                testUserId != 0 ? testUserId :
                ctx.Message.MentionedUsers.Count > 0 ? ctx.Message.MentionedUsers.First().Id :
                (!string.IsNullOrWhiteSpace(input) && !lifetime && !seasonal)
                    ? _nicknames.ResolvePlayerId(input) ?? 0 :
                ctx.Message.Author.Id;

            if (targetId == 0)
            {
                await ctx.Message.ReplyAsync($"`{input}` is not a valid player.");
                return;
            }

            var reg = _playerRegistry.GetPlayer(targetId);
            if (reg == null)
            {
                await ctx.Message.ReplyAsync("That player is not registered.");
                return;
            }

            string displayName = reg.DisplayName() ?? "Unknown Player";

            if (!lifetime)
            {
                var s = _seasons.GetOrCreateSeasonStats(targetId);

                var lines = new List<string>
                {
                    "Season stats for " + displayName + ":",
                    "Elo: " + s.Elo,
                    "Games: " + s.GamesPlayed,
                    "Wins: " + s.Wins,
                    "Losses: " + s.Losses,
                    "Win rate: " + s.WinRate.ToString("F1") + "%"
                };

                if (s.FactionHistory.Any())
                {
                    lines.Add("");
                    lines.Add("Faction performance:");
                    foreach (var kvp in s.FactionHistory
                                 .OrderByDescending(k => k.Value.Wins + k.Value.Losses)
                                 .ThenBy(k => k.Key))
                    {
                        var name = kvp.Key;
                        var rec = kvp.Value;
                        lines.Add(name + ": " + rec.Wins + "W / " + rec.Losses + "L (" + rec.WinRate.ToString("F1") + "%)");
                    }
                }

                await ctx.Message.ReplyAsync(string.Join("\n", lines));
                return;
            }

            var stats = _playerStats.GetOrCreate(targetId);

            var lines2 = new List<string>
            {
                "Lifetime stats for " + displayName + ":",
                "Elo: " + stats.Elo,
                "Games: " + stats.GamesPlayed,
                "Wins: " + stats.Wins,
                "Losses: " + stats.Losses,
                "Win rate: " + stats.WinRate.ToString("F1") + "%"
            };

            if (stats.FactionHistory.Any())
            {
                lines2.Add("");
                lines2.Add("Faction performance:");
                foreach (var kvp in stats.FactionHistory
                             .OrderByDescending(k => k.Value.Wins + k.Value.Losses)
                             .ThenBy(k => k.Key))
                {
                    var name = kvp.Key;
                    var rec = kvp.Value;
                    lines2.Add(name + ": " + rec.Wins + "W / " + rec.Losses + "L (" + rec.WinRate.ToString("F1") + "%)");
                }
            }

            await ctx.Message.ReplyAsync(string.Join("\n", lines2));
        }

        [Command("leaderboard")]
        public async Task Leaderboard(string? mode = null, int count = 10)
        {
            var ctx = this.Context;

            bool lifetime = string.Equals(mode, "lifetime", StringComparison.OrdinalIgnoreCase);

            if (count <= 0) count = 10;
            if (count > 50) count = 50;

            if (!lifetime)
            {
                var all = _seasons.CurrentSeason.PlayerStats.Values
                    .OrderByDescending(s => s.Elo)
                    .ToList();

                all = all
                    .Where(s => _playerRegistry.GetPlayer(s.DiscordId) != null)
                    .Take(count)
                    .ToList();

                if (!all.Any())
                {
                    await ctx.Message.ReplyAsync("No seasonal stats available yet.");
                    return;
                }

                var lines = all.Select((s, i) =>
                {
                    var reg = _playerRegistry.GetPlayer(s.DiscordId);
                    string name = reg?.DisplayName() ?? "<@" + s.DiscordId + ">";
                    return (i + 1) + ". " + name + " — Elo: " + s.Elo + " (W:" + s.Wins + "/L:" + s.Losses + ")";
                });

                await ctx.Message.ReplyAsync("Season Leaderboard:\n" + string.Join("\n", lines));
                return;
            }

            var all2 = _playerStats.GetAll()
                .OrderByDescending(s => s.Elo)
                .ToList();

            all2 = all2
                .Where(s => _playerRegistry.GetPlayer(s.DiscordId) != null)
                .Take(count)
                .ToList();

            if (!all2.Any())
            {
                await ctx.Message.ReplyAsync("No stats available yet.");
                return;
            }

            var lines2 = all2.Select((s, i) =>
            {
                var reg = _playerRegistry.GetPlayer(s.DiscordId);
                string name = reg?.DisplayName() ?? "<@" + s.DiscordId + ">";
                return (i + 1) + ". " + name + " — Elo: " + s.Elo + " (W:" + s.Wins + "/L:" + s.Losses + ")";
            });

            await ctx.Message.ReplyAsync("Lifetime Leaderboard:\n" + string.Join("\n", lines2));
        }

        [Command("compare")]
        public async Task Compare(string name1, string name2, string? mode = null)
        {
            var ctx = this.Context;

            bool lifetime = string.Equals(mode, "lifetime", StringComparison.OrdinalIgnoreCase);

            ulong id1 = ctx.Message.MentionedUsers.Count > 0
                ? ctx.Message.MentionedUsers.First().Id
                : _nicknames.ResolvePlayerId(name1) ?? 0;

            ulong id2 = ctx.Message.MentionedUsers.Count > 1
                ? ctx.Message.MentionedUsers.Skip(1).First().Id
                : _nicknames.ResolvePlayerId(name2) ?? 0;

            if (id1 == 0 || id2 == 0)
            {
                await ctx.Message.ReplyAsync("Could not resolve one or both players.");
                return;
            }

            var r1 = _playerRegistry.GetPlayer(id1);
            var r2 = _playerRegistry.GetPlayer(id2);

            if (r1 == null || r2 == null)
            {
                await ctx.Message.ReplyAsync("One or both players are not registered.");
                return;
            }

            string displayName1 = r1.DisplayName() ?? name1;
            string displayName2 = r2.DisplayName() ?? name2;

            if (!lifetime)
            {
                var p1 = _seasons.GetOrCreateSeasonStats(id1);
                var p2 = _seasons.GetOrCreateSeasonStats(id2);

                var lines = new List<string>
                {
                    "Season comparison: " + displayName1 + " vs " + displayName2,
                    "",
                    "Season:",
                    "Elo: " + p1.Elo + " vs " + p2.Elo,
                    "Winrate: " + p1.WinRate.ToString("F1") + "% vs " + p2.WinRate.ToString("F1") + "%", 
                    "Games: " + p1.GamesPlayed + " vs " + p2.GamesPlayed
                };

                await ctx.Message.ReplyAsync(string.Join("\n", lines));
                return;
            }

            var lp1 = _playerStats.GetOrCreate(id1);
            var lp2 = _playerStats.GetOrCreate(id2);

            var lines2 = new List<string>
            {
                "Lifetime comparison: " + displayName1 + " vs " + displayName2,
                "",
                "Overall:",
                "Elo: " + lp1.Elo + " vs " + lp2.Elo,
                "Winrate: " + lp1.WinRate.ToString("F1") + "% vs " + lp2.WinRate.ToString("F1") + "%", 
                "Games: " + lp1.GamesPlayed + " vs " + lp2.GamesPlayed
            };

            await ctx.Message.ReplyAsync(string.Join("\n", lines2));
        }
    }
}
