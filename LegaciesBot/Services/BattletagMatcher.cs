namespace LegaciesBot.Services
{
    public static class BattletagMatcher
    {
        private const double MinScore = 0.6;
        private const double MinMargin = 0.15;

        public static PlayerSearchResult? FindStrongMatch(
            string discordUsername, IReadOnlyList<PlayerSearchResult> candidates)
        {
            if (candidates.Count == 0)
                return null;

            var ranked = candidates
                .Select(c => (Candidate: c, Score: Similarity(discordUsername, NameFromBattletag(c.Battletag))))
                .OrderByDescending(x => x.Score)
                .ToList();

            var best = ranked[0];
            if (best.Score < MinScore)
                return null;

            if (ranked.Count > 1 && best.Score - ranked[1].Score < MinMargin)
                return null;

            return best.Candidate;
        }

        public static string NameFromBattletag(string battletag)
        {
            var hashIndex = battletag.IndexOf('#');
            return hashIndex < 0 ? battletag : battletag[..hashIndex];
        }

        public static double Similarity(string a, string b)
        {
            var maxLen = Math.Max(a.Length, b.Length);
            if (maxLen == 0)
                return 1.0;

            return 1.0 - (double)LevenshteinDistance(a.ToLowerInvariant(), b.ToLowerInvariant()) / maxLen;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];

            for (var j = 0; j <= b.Length; j++)
                previous[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }
    }
}
