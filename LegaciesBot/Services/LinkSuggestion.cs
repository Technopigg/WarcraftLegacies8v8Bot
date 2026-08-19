using NetCord.Rest;

namespace LegaciesBot.Services
{
    /// <summary>
    /// Builds a one-off "is this your site profile?" suggestion for a player who is not
    /// yet linked. Never links automatically (a Discord name is not proof of owning a
    /// battletag); the player confirms with <c>!link Name#1234</c>, and the site only lets
    /// them claim an unclaimed profile. Returns null when there is nothing to suggest
    /// (already linked, no match, or the site is unavailable).
    /// </summary>
    public static class LinkSuggestion
    {
        public static async Task<EmbedProperties?> BuildAsync(SiteApiService site, ulong userId, string name)
        {
            var already = await site.GetPlayerByDiscordAsync(userId);
            if (already.IsOk)
                return null;

            var search = await site.SearchPlayersAsync(name);
            if (!search.IsOk || search.Value is null || search.Value.Count == 0)
                return null;

            var candidates = search.Value;
            if (candidates.Count == 1)
            {
                var c = candidates[0];
                return EmbedFactory.Info("Is this you?",
                    $"Found a matching profile on the site: **{c.Battletag}** ({c.DisplayName}).\n" +
                    $"If that's you, run `!link {c.Battletag}` to link it. If not, ignore this.");
            }

            var list = string.Join(", ", candidates.Take(5).Select(c => $"**{c.Battletag}**"));
            return EmbedFactory.Info("Which one is you?",
                $"Found several profiles that might be you: {list}.\nIf one is yours, run `!link Name#1234` with it.");
        }
    }
}
