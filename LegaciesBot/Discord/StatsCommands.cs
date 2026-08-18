using NetCord.Services.Commands;
using LegaciesBot.Services;
using NetCord.Rest;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private const string RankingsUrl = "https://warcraftlegacies.com/rankings";

        private readonly SiteApiService _site;

        public StatsCommands()
        {
            _site = GlobalServices.SiteApiService;
        }

        [Command("stats")]
        public async Task Stats([CommandParameter(Remainder = true)] string? query = null)
        {
            var ctx = this.Context;

            // A @mention or a raw Discord id names a *linked profile*, not a battletag —
            // route it to the by-discord lookup instead of the name search (which always
            // returned "Not found", even right after a mod linked the user).
            var mentionedId = ResolveMentionedDiscordId(ctx, query);
            if (mentionedId != null)
            {
                var byDiscord = await _site.GetPlayerByDiscordAsync(mentionedId.Value);
                if (byDiscord.IsUnavailable)
                {
                    await ReplySiteUnavailable();
                    return;
                }
                if (byDiscord.IsNotFound)
                {
                    await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                        EmbedFactory.Warning("Not linked",
                            $"<@{mentionedId}> is not linked to a site profile yet.\n" +
                            "A mod can link them with `!link @user Name#1234`, or search by name with `!stats <name>`.")]));
                    return;
                }
                var linked = byDiscord.Value!;
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    BuildStatsEmbed(linked.DisplayName, linked.Rating, linked.Sigma, linked.MatchesCount,
                        linked.WinsCount, linked.LossesCount, linked.Winrate, linked.ProfileUrl)]));
                return;
            }

            // Bare `!stats` — "about me" via the caller's linked Discord id.
            if (string.IsNullOrWhiteSpace(query))
            {
                var me = await _site.GetPlayerByDiscordAsync(ctx.Message.Author.Id);

                if (me.IsUnavailable)
                {
                    await ReplySiteUnavailable();
                    return;
                }

                if (me.IsNotFound)
                {
                    await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                        EmbedFactory.Info("Not linked yet",
                            "Your Discord account is not linked to a site profile.\n" +
                            "Ask a mod to link you with `!link @you Name#1234`, or look someone up with `!stats <name>`.")]));
                    return;
                }

                var r = me.Value!;
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    BuildStatsEmbed(r.DisplayName, r.Rating, r.Sigma, r.MatchesCount,
                        r.WinsCount, r.LossesCount, r.Winrate, r.ProfileUrl)]));
                return;
            }

            // `!stats <query>` — fuzzy search.
            var search = await _site.SearchPlayersAsync(query);

            if (search.IsUnavailable)
            {
                await ReplySiteUnavailable();
                return;
            }

            var results = search.Value!;

            if (results.Count == 0)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Not found",
                        $"No player matching `{query}` in the discord pool.\nEvery player: <{RankingsUrl}>")]));
                return;
            }

            if (results.Count > 1)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Info("Which one did you mean?",
                        $"Found: {string.Join(", ", results.Select(r => r.Battletag))} — which one did you mean?")]));
                return;
            }

            var hit = results[0];
            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                BuildStatsEmbed(hit.DisplayName, hit.Rating, hit.Sigma, hit.MatchesCount,
                    hit.WinsCount, hit.LossesCount, hit.Winrate, hit.ProfileUrl,
                    prefixLine: $"Showing stats for {hit.Battletag}")]));
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

            string desc = string.Join("\n", lines) + $"\n\n[Full leaderboard]({RankingsUrl})";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info($"Top {result.Entries.Count} — Discord Pool", desc)]));
        }

        [Command("compare")]
        public async Task Compare(string name1, string name2)
        {
            var ctx = this.Context;

            var left = await ResolveOneAsync(name1);
            var right = await ResolveOneAsync(name2);

            if (left.State == Resolution.Unavailable || right.State == Resolution.Unavailable)
            {
                await ReplySiteUnavailable();
                return;
            }

            // Ambiguity beats missing: tell the user how to disambiguate first.
            if (left.State == Resolution.Many || right.State == Resolution.Many)
            {
                var parts = new List<string>();
                if (left.State == Resolution.Many)
                    parts.Add($"`{name1}` matches: {string.Join(", ", left.Many!.Select(r => r.Battletag))}");
                if (right.State == Resolution.Many)
                    parts.Add($"`{name2}` matches: {string.Join(", ", right.Many!.Select(r => r.Battletag))}");

                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Info("Which one did you mean?",
                        string.Join("\n", parts) + "\n\nBe more specific and try again.")]));
                return;
            }

            if (left.State == Resolution.None && right.State == Resolution.None)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Not found", $"Neither `{name1}` nor `{name2}` found in the discord pool.")]));
                return;
            }

            var r1 = left.Player;
            var r2 = right.Player;

            static string Row(PlayerSearchResult? r, string key) => r == null
                ? $"`{key}` — not found"
                : $"**{r.DisplayName}** — {r.Rating} ({r.WinsCount}W/{r.LossesCount}L {(r.Winrate * 100):F0}%)";

            string desc = Row(r1, name1) + "\n" + Row(r2, name2);
            if (r1 != null && r2 != null)
            {
                int diff = r1.Rating - r2.Rating;
                string leader = diff >= 0 ? r1.DisplayName : r2.DisplayName;
                desc += $"\n\n{leader} leads by **{Math.Abs(diff)}** rating points.";
            }

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info("Compare", desc)]));
        }

        // A @mention (parsed by Discord into MentionedUsers) or a bare 17-20 digit snowflake
        // both identify a Discord user, i.e. a linked profile. Anything else is a name.
        private static ulong? ResolveMentionedDiscordId(CommandContext ctx, string? query)
        {
            if (ctx.Message.MentionedUsers.Count > 0)
                return ctx.Message.MentionedUsers[0].Id;

            var trimmed = query?.Trim();
            if (trimmed is { Length: >= 17 and <= 20 } && trimmed.All(char.IsDigit) && ulong.TryParse(trimmed, out var id))
                return id;

            return null;
        }

        private enum Resolution { Ok, None, Many, Unavailable }

        private async Task<(Resolution State, PlayerSearchResult? Player, IReadOnlyList<PlayerSearchResult>? Many)>
            ResolveOneAsync(string query)
        {
            var search = await _site.SearchPlayersAsync(query);

            if (search.IsUnavailable)
                return (Resolution.Unavailable, null, null);

            var results = search.Value!;

            return results.Count switch
            {
                0 => (Resolution.None, null, null),
                1 => (Resolution.Ok, results[0], null),
                _ => (Resolution.Many, null, results),
            };
        }

        private Task ReplySiteUnavailable() =>
            Context.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Error("Site unavailable",
                    "Could not reach warcraftlegacies.com — please try again in a minute.")]));

        private static NetCord.Rest.EmbedProperties BuildStatsEmbed(
            string displayName, int rating, double sigma, int matches,
            int wins, int losses, double winrate, string profileUrl, string? prefixLine = null)
        {
            string winratePct = (winrate * 100).ToString("F1") + "%";
            string desc =
                (prefixLine != null ? prefixLine + "\n\n" : "") +
                $"**Rating:** {rating} (σ {sigma:F0})\n" +
                $"**Matches:** {matches} — {wins}W / {losses}L — {winratePct}\n" +
                $"[Full profile]({profileUrl})";

            return EmbedFactory.Info($"Stats — {displayName}", desc);
        }
    }
}
