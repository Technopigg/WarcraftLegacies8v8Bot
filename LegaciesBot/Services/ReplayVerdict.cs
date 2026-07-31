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
        /// Why the site would not take the file at all. These are the codes it
        /// answers a rejected or errored upload with; a player reading
        /// "replay_parse_error" learns nothing.
        /// </summary>
        public static string DescribeRejection(string? reason) => reason switch
        {
            "replay_parse_error" => "this file is not a Warcraft III replay the site can read",
            "empty_replay_file" => "the file is empty",
            "replay_file_too_large" => "the file is too large",
            "invalid_content_length" => "the upload was malformed",
            "missing_map_info" => "the replay does not say which map it was played on",
            "unsupported_map_family" => "that map is not tracked here",
            "map_family_not_registered" => "that map is not tracked here",
            "unregistered_map_version" => "this map version is not part of the season",
            "rate_limit_exceeded" => "too many uploads just now, try again shortly",
            "storage_unavailable" => "replay storage is unavailable, try again shortly",
            null => "no reason given",
            _ => reason,
        };

        /// <summary>
        /// Why the site already had this replay. Every one of these means the
        /// game is on the site already, so the player wants the link, not an
        /// error.
        /// </summary>
        public static string DescribeDuplicate(string? reason) => reason switch
        {
            "file_sha256_exists" => "this exact file is already uploaded",
            "parsed_replay_exists" => "this replay is already uploaded",
            "discord_ids_exist" => "this attachment was already uploaded",
            "same_game_shorter_or_equal" => "a longer recording of this game is already uploaded",
            "duplicate_replay" => "this replay is already uploaded",
            null => "this replay is already uploaded",
            _ => reason,
        };

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
