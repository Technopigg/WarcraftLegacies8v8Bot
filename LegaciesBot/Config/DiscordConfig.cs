namespace LegaciesBot.Config
{
    public static class DiscordConfig
    {
        public static ulong GuildId { get; } =
            GetUlongEnv("WL_DISCORD_GUILD_ID", 1218338908216229979);

        public static ulong DraftChannelId { get; } =
            GetUlongEnv("WL_DISCORD_DRAFT_CHANNEL_ID", 1488958363361349908);

        public static ulong Team1RoleId { get; } =
            GetUlongEnv("WL_DISCORD_TEAM1_ROLE_ID", 1488594785500397698);

        public static ulong Team2RoleId { get; } =
            GetUlongEnv("WL_DISCORD_TEAM2_ROLE_ID", 1488594866970824904);

        /// <summary>
        /// Channel commands are restricted to. 0 means no restriction (commands work in any channel).
        /// </summary>
        public static ulong CommandChannelId { get; } =
            GetUlongEnv("WL_DISCORD_COMMAND_CHANNEL_ID", 0);

        /// <summary>
        /// Channel used for replay uploads. 0 means not configured — uploads are ignored.
        /// </summary>
        public static ulong ReplayChannelId { get; } =
            GetUlongEnv("WL_DISCORD_REPLAY_CHANNEL_ID", 0);

        /// <summary>
        /// Base URL of the warcraftlegacies.com site used for replay upload and bot API.
        /// </summary>
        public static string SiteBaseUrl { get; } =
            Environment.GetEnvironmentVariable("WL_SITE_BASE_URL") ?? "https://warcraftlegacies.com";

        /// <summary>
        /// Bearer token for /api/bot/* endpoints. Empty means no auth required.
        /// </summary>
        public static string SiteApiToken { get; } =
            Environment.GetEnvironmentVariable("WL_SITE_API_TOKEN") ?? "";

        private static ulong GetUlongEnv(string name, ulong fallback)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            return ulong.TryParse(raw, out var value) ? value : fallback;
        }
    }
}
