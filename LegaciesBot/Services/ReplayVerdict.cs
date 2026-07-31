namespace LegaciesBot.Services
{
    /// <summary>
    /// Turns the site's answer about an uploaded replay into the sentence a
    /// player reads in Discord.
    ///
    /// It lived in Program.cs as a pair of local functions, which meant the one
    /// piece of text every uploader sees could not be tested. The rule it
    /// encodes is easy to get wrong and was wrong until recently: the rating
    /// pool says which ladder a match would join, never whether it counted.
    /// Only the match status does.
    /// </summary>
    public static class ReplayVerdict
    {
        public const string Ranked = "ranked";
        public const string NotRanked = "not_ranked";

        /// <summary>The headline: did this game count, and if not, why not.</summary>
        public static string Describe(string? matchStatus, string? ratingPool, string? exclusionReason)
        {
            string pool = ratingPool == "discord" ? "discord pool" : "public pool";

            return matchStatus switch
            {
                NotRanked => $"Not ranked — {DescribeExclusion(exclusionReason)}",
                Ranked => $"Ranked ({pool})",
                // Anything else means the winners have not been marked yet.
                _ => $"Awaiting review ({pool})",
            };
        }

        /// <summary>True when the embed should warn rather than congratulate.</summary>
        public static bool IsNotRanked(string? matchStatus) => matchStatus == NotRanked;

        /// <summary>
        /// Every code the site can put in ranked_exclusion_reason. The last one
        /// cannot reach an upload — it appears when two accounts that both
        /// played a match are merged into one profile — but leaving it to the
        /// fallback would print a raw identifier at a player.
        /// </summary>
        public static string DescribeExclusion(string? reason) => reason switch
        {
            "unregistered_map_version" => "this map version is not part of the season",
            "map_version_not_ranked" => "this map version does not count towards the rating",
            "single_player_replay" => "single player replays do not count",
            "merged_profile_duplicate_participation" => "two merged accounts played this match; it is back in review",
            null => "see the match page for details",
            _ => reason,
        };
    }
}
