using NetCord;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using System.Linq;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private readonly PlayerStatsService _playerStats;

        public StatsCommands(PlayerStatsService playerStats)
        {
            _playerStats = playerStats;
        }

        [Command("stats")]
        public async Task Stats()
        {
            var ctx = this.Context;
            var stats = _playerStats.GetOrCreate(ctx.Message.Author.Id);

            var lines = new List<string>
            {
                $"Stats for {ctx.Message.Author.Username}:",
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

            var lines = all
                .Select((s, i) =>
                    $"{i + 1}. <@{s.DiscordId}> — Elo: {s.Elo} (W:{s.Wins}/L:{s.Losses})");

            var msg = "Leaderboard:\n" + string.Join("\n", lines);

            await ctx.Message.ReplyAsync(msg);
        }

        [Command("compare")]
        public async Task Compare(params string[] args)
        {
            var ctx = this.Context;

            if (args.Length != 2)
            {
                await ctx.Message.ReplyAsync(
                    "Usage: `!compare <user1> <user2>`\n" +
                    "Examples:\n" +
                    "`!compare @Alice @Bob`\n" +
                    "`!compare Alice Bob`\n" +
                    "`!compare 123456789012345678 987654321098765432`"
                );
                return;
            }

            var user1 = await ResolveUser(args[0], ctx);
            var user2 = await ResolveUser(args[1], ctx);

            if (user1 == null)
            {
                await ctx.Message.ReplyAsync($"Could not find a user matching `{args[0]}`.");
                return;
            }

            if (user2 == null)
            {
                await ctx.Message.ReplyAsync($"Could not find a user matching `{args[1]}`.");
                return;
            }

            var p1 = _playerStats.GetOrCreate(user1.Id);
            var p2 = _playerStats.GetOrCreate(user2.Id);

            var lines = new List<string>
            {
                $"Comparison:",
                $"{user1.Username} vs {user2.Username}",
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
                    var r1 = p1.FactionHistory[faction];
                    var r2 = p2.FactionHistory[faction];

                    lines.Add($"- {faction}: {r1.WinRate:F1}% vs {r2.WinRate:F1}%");
                }
            }
            else
            {
                lines.Add("No shared faction data.");
            }

            await ctx.Message.ReplyAsync(string.Join("\n", lines));
        }

        private async Task<User?> ResolveUser(string input, CommandContext ctx)
        {
            if (ctx.Message.MentionedUsers.Count > 0)
            {
                var mentioned = ctx.Message.MentionedUsers.FirstOrDefault();
                if (mentioned != null)
                    return mentioned;
            }

            if (ulong.TryParse(input, out ulong id))
            {
                try
                {
                    return await ctx.Client.Rest.GetUserAsync(id);
                }
                catch { }
            }

            if (ctx.Message.GuildId.HasValue)
            {
                var guild = ctx.Client.Guilds.GetValueOrDefault(ctx.Message.GuildId.Value);

                if (guild != null)
                {
                    var members = guild.Members.Values;

                    var exact = members.FirstOrDefault(m =>
                        string.Equals(m.User.Username, input, StringComparison.OrdinalIgnoreCase) ||
                        (m.Nick != null && string.Equals(m.Nick, input, StringComparison.OrdinalIgnoreCase)));

                    if (exact != null)
                        return exact.User;

                    var partial = members.FirstOrDefault(m =>
                        m.User.Username.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                        (m.Nick != null && m.Nick.Contains(input, StringComparison.OrdinalIgnoreCase)));

                    if (partial != null)
                        return partial.User;
                }
            }

            return null;
        }
    }
}