namespace LegaciesBot.Discord
{
    /// <summary>
    /// Suggests the closest real command for a mistyped one. Only ever consulted when the
    /// command router reports "not found", so it can never intercept a valid command.
    ///
    /// It suggests only toward a curated list of player-facing commands and only for a close
    /// typo that keeps the first letter — so it stays quiet for another bot's commands (e.g.
    /// MEE6 also uses the `!` prefix) instead of spamming the channel.
    /// </summary>
    public static class CommandSuggester
    {
        // Player-facing commands only. Deliberately excludes mod/admin/debug commands so a
        // random user never gets nudged toward `!ban`, `!kill`, `!debugfill`, etc.
        // Every entry must be a real, implemented command — otherwise we'd nudge a user
        // toward a dead end. (`!score`/`!scores` appear in the help text but aren't wired up,
        // so they are deliberately absent here.)
        private static readonly string[] Known =
        {
            "join", "leave", "lobby", "prefs", "factions", "top", "leaderboard", "stats",
            "compare", "recent", "register", "nickname", "captain", "captains", "uncaptain",
            "draft", "pass", "mode", "games", "season", "bothelp", "link", "unlink",
        };

        public static string? Suggest(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = token.Trim().ToLowerInvariant();
            if (token.Length < 2)
                return null;

            string? best = null;
            int bestDist = int.MaxValue;

            foreach (var cmd in Known)
            {
                // Typos rarely change the first letter; this guard is what keeps us from
                // answering some other bot's command that merely looks numerically close.
                if (cmd[0] != token[0])
                    continue;

                int d = DamerauLevenshtein(token, cmd);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cmd;
                }
            }

            if (best == null)
                return null;

            int maxLen = System.Math.Max(token.Length, best.Length);
            if (bestDist == 1)
                return best;
            if (bestDist == 2 && maxLen >= 6)
                return best;

            return null;
        }

        // Optimal string alignment distance: like Levenshtein but an adjacent transposition
        // ("tpo" -> "top") counts as one edit, which is the most common kind of typo.
        public static int DamerauLevenshtein(string a, string b)
        {
            int n = a.Length, m = b.Length;
            if (n == 0) return m;
            if (m == 0) return n;

            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    int del = d[i - 1, j] + 1;
                    int ins = d[i, j - 1] + 1;
                    int sub = d[i - 1, j - 1] + cost;
                    int min = System.Math.Min(del, System.Math.Min(ins, sub));

                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        min = System.Math.Min(min, d[i - 2, j - 2] + 1);

                    d[i, j] = min;
                }
            }

            return d[n, m];
        }
    }
}
