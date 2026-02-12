using System.Linq;
using System.Threading.Tasks;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Core;

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

            await ctx.Message.ReplyAsync(
                $"Stats for {ctx.Message.Author.Username}:\n" +
                $"- Elo: {stats.Elo}\n" +
                $"- Games: {stats.GamesPlayed}\n" +
                $"- Wins: {stats.Wins}\n" +
                $"- Losses: {stats.Losses}\n" +
                $"- Win rate: {stats.WinRate:F1}%"
            );
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
    }
}