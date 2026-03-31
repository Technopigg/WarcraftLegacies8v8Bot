using LegaciesBot.Core;
using LegaciesBot.Seasons;
using LegaciesBot.Services;
using NetCord.Services.Commands;

namespace LegaciesBot.Discord
{
    public class SeasonCommands : CommandModule<CommandContext>
    {
        private readonly SeasonService _seasons;
        private readonly PlayerRegistryService _registry;

        public SeasonCommands(SeasonService seasons, PlayerRegistryService registry)
        {
            _seasons = seasons;
            _registry = registry;
        }

        [Command("season")]
        public async Task Season(string? sub = null, string? arg = null)
        {
            var ctx = this.Context;

            if (string.Equals(sub, "start", StringComparison.OrdinalIgnoreCase))
            {
                _seasons.StartNewSeason();
                await ctx.Message.ReplyAsync("A new season has begun!");
                return;
            }

            if (string.Equals(sub, "history", StringComparison.OrdinalIgnoreCase))
            {
                var all = _seasons.GetAllSeasons()
                    .OrderByDescending(s => s.SeasonNumber)
                    .ToList();

                if (!all.Any())
                {
                    await ctx.Message.ReplyAsync("No seasons recorded yet.");
                    return;
                }

                var lines = all.Select(s =>
                    "Season " + s.SeasonNumber +
                    " — " + s.StartedAt.ToString("yyyy-MM-dd") +
                    " — " + s.PlayerStats.Count + " players");

                await ctx.Message.ReplyAsync("Season History:\n" + string.Join("\n", lines));
                return;
            }

            if (string.Equals(sub, "show", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(arg, out int seasonNumber))
                {
                    await ctx.Message.ReplyAsync("Usage: !season show <number>");
                    return;
                }

                var s = _seasons.GetSeason(seasonNumber);
                if (s == null)
                {
                    await ctx.Message.ReplyAsync("Season " + seasonNumber + " does not exist.");
                    return;
                }

                await ctx.Message.ReplyAsync(
                    "Season " + s.SeasonNumber + "\n" +
                    "- Started: " + s.StartedAt.ToString("yyyy-MM-dd") + "\n" +
                    "- Players: " + s.PlayerStats.Count
                );
                return;
            }

            if (string.Equals(sub, "summary", StringComparison.OrdinalIgnoreCase))
            {
                var s = _seasons.CurrentSeason;
                var stats = s.PlayerStats.Values.ToList();

                if (!stats.Any())
                {
                    await ctx.Message.ReplyAsync("No players this season.");
                    return;
                }

                var topElo = stats.OrderByDescending(p => p.Elo).First();
                var bestWinrate = stats.Where(p => p.GamesPlayed >= 5).OrderByDescending(p => p.WinRate).FirstOrDefault();
                var mostGames = stats.OrderByDescending(p => p.GamesPlayed).First();
                var mostWins = stats.OrderByDescending(p => p.Wins).First();
                var mostLosses = stats.OrderByDescending(p => p.Losses).First();
                var mostImproved = stats.OrderByDescending(p => p.Elo - p.PreviousSeasonElo).First();

                string name(ulong id) => _registry.GetPlayer(id)?.DisplayName() ?? "<@" + id + ">";

                var lines = new List<string>
                {
                    "Season " + s.SeasonNumber + " Summary",
                    "",
                    "Top Elo: " + name(topElo.DiscordId) + " (" + topElo.Elo + ")",
                    "Best Winrate: " + (bestWinrate == null ? "N/A" : name(bestWinrate.DiscordId) + " (" + bestWinrate.WinRate.ToString("F1") + "%)"),
                    "Most Games: " + name(mostGames.DiscordId) + " (" + mostGames.GamesPlayed + ")",
                    "Most Wins: " + name(mostWins.DiscordId) + " (" + mostWins.Wins + ")",
                    "Most Losses: " + name(mostLosses.DiscordId) + " (" + mostLosses.Losses + ")",
                    "Most Improved: " + name(mostImproved.DiscordId) + " (+" + (mostImproved.Elo - mostImproved.PreviousSeasonElo) + " Elo)"
                };

                await ctx.Message.ReplyAsync(string.Join("\n", lines));
                return;
            }

            if (string.Equals(sub, "showleaderboard", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(arg, out int seasonNumber))
                {
                    await ctx.Message.ReplyAsync("Usage: !season showleaderboard <number>");
                    return;
                }

                var s = _seasons.GetSeason(seasonNumber);
                if (s == null)
                {
                    await ctx.Message.ReplyAsync("Season " + seasonNumber + " does not exist.");
                    return;
                }

                var stats = s.PlayerStats.Values
                    .OrderByDescending(p => p.Elo)
                    .Take(20)
                    .ToList();

                if (!stats.Any())
                {
                    await ctx.Message.ReplyAsync("No players in that season.");
                    return;
                }

                string name(ulong id) => _registry.GetPlayer(id)?.DisplayName() ?? "<@" + id + ">";

                var lines = stats.Select((p, i) =>
                    (i + 1) + ". " + name(p.DiscordId) + " — Elo: " + p.Elo);

                await ctx.Message.ReplyAsync("Season " + seasonNumber + " Leaderboard:\n" + string.Join("\n", lines));
                return;
            }

            var current = _seasons.CurrentSeason;

            await ctx.Message.ReplyAsync(
                "Season " + current.SeasonNumber + "\n" +
                "- Started: " + current.StartedAt.ToString("yyyy-MM-dd") + "\n" +
                "- Players this season: " + current.PlayerStats.Count
            );
        }
    }
}
