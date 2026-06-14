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

        private static ulong GetUlongEnv(string name, ulong fallback)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            return ulong.TryParse(raw, out var value) ? value : fallback;
        }
    }
}
