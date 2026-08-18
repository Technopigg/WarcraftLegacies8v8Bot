namespace LegaciesBot.Services
{
    public abstract record BattletagMatch
    {
        public sealed record None : BattletagMatch;
        public sealed record Exact(PlayerSearchResult Player) : BattletagMatch;
        public sealed record Suggested(PlayerSearchResult Player) : BattletagMatch;
        public sealed record Ambiguous(IReadOnlyList<PlayerSearchResult> Candidates) : BattletagMatch;
    }

    public static class BattletagMatcher
    {
        private const double MinScore = 0.6;
        private const double MinMargin = 0.15;
        private const double ExactScore = 0.999;

        public static BattletagMatch Evaluate(string discordUsername, IReadOnlyList<PlayerSearchResult> candidates)
        {
            if (candidates.Count == 0)
                return new BattletagMatch.None();

            var ranked = candidates
                .Select(c => (Candidate: c, Score: Similarity(discordUsername, NameFromBattletag(c.Battletag))))
                .OrderByDescending(x => x.Score)
                .ToList();

            var exact = ranked.Where(r => r.Score >= ExactScore).ToList();
            if (exact.Count == 1)
                return new BattletagMatch.Exact(exact[0].Candidate);
            if (exact.Count > 1)
                return new BattletagMatch.Ambiguous(exact.Select(e => e.Candidate).ToList());

            var plausible = ranked.Where(r => r.Score >= MinScore).ToList();
            if (plausible.Count == 0)
                return new BattletagMatch.None();

            if (plausible.Count == 1 || plausible[0].Score - plausible[1].Score >= MinMargin)
                return new BattletagMatch.Suggested(plausible[0].Candidate);

            return new BattletagMatch.Ambiguous(plausible.Select(p => p.Candidate).ToList());
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
