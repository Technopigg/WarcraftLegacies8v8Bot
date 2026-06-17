using LegaciesBot.Core;
using LegaciesBot.Seasons;
using LegaciesBot.Services;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace LegaciesBot.Discord
{
    public class SeasonCommands : CommandModule<CommandContext>
    {
        private readonly SeasonService _seasons;
        private readonly PlayerRegistryService _registry;
        private readonly SiteApiService _site;

        public SeasonCommands()
        {
            _seasons = GlobalServices.SeasonService;
            _registry = GlobalServices.PlayerRegistryService;
            _site = GlobalServices.SiteApiService;
        }

        [Command("season")]
        public async Task Season(string? sub = null, string? arg = null)
        {
            var ctx = this.Context;

            if (string.Equals(sub, "start", StringComparison.OrdinalIgnoreCase))
            {
                if (!GlobalServices.PermissionService.IsModeratorOrAdmin(ctx.User.Id))
                {
                    await ctx.Message.ReplyAsync("You do not have permission to use this command.");
                    return;
                }

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
                var result = await _site.GetLeaderboardAsync(pool: "discord", limit: 10);

                if (result == null || result.Entries.Count == 0)
                {
                    await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                        EmbedFactory.Warning("No data", "Could not fetch Season 5 standings from warcraftlegacies.com.")]));
                    return;
                }

                var lines = result.Entries.Take(5).Select(e =>
                {
                    string wr = (e.Winrate * 100).ToString("F0") + "%";
                    return $"`{e.Rank,2}.` **{e.DisplayName}** — {e.Rating} ({e.WinsCount}W/{e.LossesCount}L {wr})";
                });

                string desc = string.Join("\n", lines) + "\n\n[Full leaderboard](https://warcraftlegacies.com/leaderboard)";
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Info("Season 5 Summary — Top 5", desc)]));
                return;
            }

            if (string.Equals(sub, "showleaderboard", StringComparison.OrdinalIgnoreCase))
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Info("Season Leaderboard", "Season 5 ratings are on the site:\n<https://warcraftlegacies.com/leaderboard>")]));
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
