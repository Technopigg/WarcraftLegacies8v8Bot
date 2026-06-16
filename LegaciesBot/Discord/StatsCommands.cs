using NetCord.Services.Commands;
using LegaciesBot.Services;
using NetCord.Rest;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private readonly SiteApiService _site;

        public StatsCommands()
        {
            _site = GlobalServices.SiteApiService;
        }

        [Command("stats")]
        public async Task Stats(string? playerKey = null)
        {
            var ctx = this.Context;

            if (string.IsNullOrWhiteSpace(playerKey))
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Info("Player Stats", "Usage: `!stats <battletag>` (e.g. `!stats Nick#1234`)\nFull profile: <https://warcraftlegacies.com/players>")]));
                return;
            }

            var result = await _site.GetPlayerAsync(playerKey);

            if (result == null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Not found", $"No data for `{playerKey}` in the discord pool.\nCheck your battletag or visit <https://warcraftlegacies.com/players>")]));
                return;
            }

            string winratePct = (result.Winrate * 100).ToString("F1") + "%";
            string desc =
                $"**Rating:** {result.Rating} (σ {result.Sigma:F0})\n" +
                $"**Matches:** {result.MatchesCount} — {result.WinsCount}W / {result.LossesCount}L — {winratePct}\n" +
                $"[Full profile]({result.ProfileUrl})";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info($"Stats — {result.DisplayName}", desc)]));
        }

        [Command("leaderboard")]
        public async Task Leaderboard(int count = 10)
        {
            var ctx = this.Context;

            if (count <= 0) count = 10;
            if (count > 25) count = 25;

            var result = await _site.GetLeaderboardAsync(limit: count);

            if (result == null || result.Entries.Count == 0)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Leaderboard unavailable", "Could not reach warcraftlegacies.com — try again later.")]));
                return;
            }

            var lines = result.Entries.Select(e =>
            {
                string wr = (e.Winrate * 100).ToString("F0") + "%";
                return $"`{e.Rank,2}.` **{e.DisplayName}** — {e.Rating} ({e.WinsCount}W/{e.LossesCount}L {wr})";
            });

            string desc = string.Join("\n", lines) + $"\n\n[Full leaderboard](https://warcraftlegacies.com/leaderboard)";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info($"Top {result.Entries.Count} — Discord Pool", desc)]));
        }

        // DISABLED (Season 5): compare comes from site API — requires battletag lookup, not yet wired.
        // To re-enable: restore [Command("compare")] attribute and implement two GetPlayerAsync calls.
        public async Task Compare(string name1, string name2)
        {
            await Context.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info("Compare players", $"Visit: <https://warcraftlegacies.com/players>")]));
        }
    }
}
