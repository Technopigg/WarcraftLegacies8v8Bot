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

        [Command("compare")]
        public async Task Compare(string name1, string name2)
        {
            var ctx = this.Context;
            var results = await Task.WhenAll(_site.GetPlayerAsync(name1), _site.GetPlayerAsync(name2));
            var r1 = results[0];
            var r2 = results[1];

            if (r1 == null && r2 == null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Not found", $"Neither `{name1}` nor `{name2}` found in the discord pool.")]));
                return;
            }

            static string Row(PlayerRatingResult? r, string key) => r == null
                ? $"`{key}` — not found"
                : $"**{r.DisplayName}** — {r.Rating} ({r.WinsCount}W/{r.LossesCount}L {(r.Winrate * 100):F0}%)";

            string desc = Row(r1, name1) + "\n" + Row(r2, name2);
            if (r1 != null && r2 != null)
            {
                int diff = r1.Rating - r2.Rating;
                string leader = diff > 0 ? r1.DisplayName : r2.DisplayName;
                desc += $"\n\n{leader} leads by **{Math.Abs(diff)}** rating points.";
            }

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info("Compare", desc)]));
        }
    }
}
