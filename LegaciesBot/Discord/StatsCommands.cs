using LegaciesBot.Core;
using NetCord.Services.Commands;
using LegaciesBot.Services;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private readonly PlayerStatsService _playerStats;
        private readonly PlayerRegistryService _playerRegistry;
        private readonly NicknameService _nicknames;

        public StatsCommands(
            PlayerStatsService playerStats, 
            PlayerRegistryService playerRegistry,
            NicknameService nicknames)
        {
            _playerStats = playerStats;
            _playerRegistry = playerRegistry;
            _nicknames = nicknames;
        }
        
        [Command("stats")]
        public async Task Stats(string? input = null, ulong testUserId = 0)
        {
            var ctx = this.Context;
            ulong targetId;

            if (testUserId != 0) 
                targetId = testUserId;
            else if (string.IsNullOrWhiteSpace(input))
                targetId = ctx.Message.Author.Id;
            else if (ctx.Message.MentionedUsers.Count > 0)
                targetId = ctx.Message.MentionedUsers.First().Id;
            else
                targetId = _nicknames.ResolvePlayerId(input) ?? 0;

            if (targetId == 0)
            {
                await ctx.Message.ReplyAsync($"`{input}` is not a valid player.");
                return;
            }

            var reg = _playerRegistry.GetPlayer(targetId);
            string displayName = reg?.DisplayName() ?? "Unknown Player";

            var stats = _playerStats.GetOrCreate(targetId);

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

            await ctx.Message.ReplyAsync("Leaderboard:\n" + string.Join("\n", lines));
        }

        [Command("compare")]
        public async Task Compare(string name1, string name2)
        {
            var ctx = this.Context;
            
            ulong id1 = ctx.Message.MentionedUsers.Count > 0 
                ? ctx.Message.MentionedUsers.First().Id 
                : _nicknames.ResolvePlayerId(name1) ?? 0;
            
            ulong id2 = ctx.Message.MentionedUsers.Count > 1 
                ? ctx.Message.MentionedUsers.Skip(1).First().Id 
                : _nicknames.ResolvePlayerId(name2) ?? 0;

            if (id1 == 0 || id2 == 0)
            {
                await ctx.Message.ReplyAsync("Could not resolve one or both players. Use nicknames or @mentions.");
                return;
            }

            var p1 = _playerStats.GetOrCreate(id1);
            var p2 = _playerStats.GetOrCreate(id2);
            var r1 = _playerRegistry.GetPlayer(id1);
            var r2 = _playerRegistry.GetPlayer(id2);

            string displayName1 = r1?.DisplayName() ?? name1;
            string displayName2 = r2?.DisplayName() ?? name2;

            var lines = new List<string>
            {
                $"Comparison: **{displayName1}** vs **{displayName2}**",
                "",
                "Overall:",
                $"- Elo: {p1.Elo} vs {p2.Elo}",
                $"- Winrate: {p1.WinRate:F1}% vs {p2.WinRate:F1}%",
                $"- Games: {p1.GamesPlayed} vs {p2.GamesPlayed}",
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
