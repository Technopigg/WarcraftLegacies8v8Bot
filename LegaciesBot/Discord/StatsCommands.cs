using LegaciesBot.Core;
using NetCord;
using NetCord.Services.Commands;
using LegaciesBot.Services;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private readonly PlayerStatsService _playerStats;
        private readonly PlayerRegistryService _playerRegistry;

        public StatsCommands(PlayerStatsService playerStats, PlayerRegistryService playerRegistry)
        {
            _playerStats = playerStats;
            _playerRegistry = playerRegistry;
        }
        
        [Command("stats")]
        public async Task Stats()
        {
            var ctx = this.Context;
            var playerId = ctx.Message.Author.Id;

            var reg = _playerRegistry.GetPlayer(playerId);
            string displayName = reg?.DisplayName() ?? ctx.Message.Author.Username;

            var stats = _playerStats.GetOrCreate(playerId);

            var lines = new List<string>
            {
                $"Stats for **{displayName}**:",
                $"- Elo: {stats.Elo}",
                $"- Games: {stats.GamesPlayed}",
                $"- Wins: {stats.Wins}",
                $"- Losses: {stats.Losses}",
                $"- Win rate: {stats.WinRate:F1}%"
            };

            if (stats.FactionHistory.Any())
            {
                lines.Add("");
                lines.Add("Faction performance:");

                foreach (var kvp in stats.FactionHistory
                             .OrderByDescending(k => k.Value.Wins + k.Value.Losses)
                             .ThenBy(k => k.Key))
                {
                    var name = kvp.Key;
                    var rec = kvp.Value;
                    lines.Add($"- {name}: {rec.Wins}W / {rec.Losses}L ({rec.WinRate:F1}%)");
                }
            }

            await ctx.Message.ReplyAsync(string.Join("\n", lines));
        }
        
        [Command("leaderboard")]
        public async Task Leaderboard(int count = 10)
        {
            var ctx = this.Context;

            if (count <= 0) count = 10;
            if (count > 50) count = 50;

            var all = _playerStats.GetAll()
                .OrderByDescending(s => s.Elo)
                .Take(count)
                .ToList();

            if (!all.Any())
            {
                await ctx.Message.ReplyAsync("No stats available yet.");
                return;
            }

            var lines = all.Select((s, i) =>
            {
                var reg = _playerRegistry.GetPlayer(s.DiscordId);
                string name = reg?.DisplayName() ?? $"<@{s.DiscordId}>";

                return $"{i + 1}. {name} — Elo: {s.Elo} (W:{s.Wins}/L:{s.Losses})";
            });

            var msg = "Leaderboard:\n" + string.Join("\n", lines);

            await ctx.Message.ReplyAsync(msg);
        }

        [Command("compare")]
        public async Task Compare(User user1, User user2)
        {
            var ctx = this.Context;

            var p1 = _playerStats.GetOrCreate(user1.Id);
            var p2 = _playerStats.GetOrCreate(user2.Id);

            var r1 = _playerRegistry.GetPlayer(user1.Id);
            var r2 = _playerRegistry.GetPlayer(user2.Id);

            string name1 = r1?.DisplayName() ?? user1.Username;
            string name2 = r2?.DisplayName() ?? user2.Username;

            var lines = new List<string>
            {
                $"Comparison:",
                $"{name1} vs {name2}",
                "",
                "Overall:",
                $"- {p1.Elo} Elo vs {p2.Elo} Elo",
                $"- {p1.WinRate:F1}% winrate vs {p2.WinRate:F1}%",
                $"- {p1.GamesPlayed} games vs {p2.GamesPlayed} games",
                ""
            };

            var shared = p1.FactionHistory.Keys.Intersect(p2.FactionHistory.Keys).ToList();

            if (shared.Any())
            {
                lines.Add("Shared faction performance:");

                foreach (var faction in shared.OrderBy(f => f))
                {
                    var f1 = p1.FactionHistory[faction];
                    var f2 = p2.FactionHistory[faction];

                    lines.Add($"- {faction}: {f1.WinRate:F1}% vs {f2.WinRate:F1}%");
                }
            }
            else
            {
                lines.Add("No shared faction data.");
            }

            await ctx.Message.ReplyAsync(string.Join("\n", lines));
        }
    }
}