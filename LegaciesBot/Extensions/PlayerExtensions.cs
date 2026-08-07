namespace LegaciesBot.Core;

public static class PlayerExtensions
{
    /// <summary>
    /// A human-readable name for the player. Never returns the raw Discord id.
    /// Prefers the nickname, then a real stored name, then the live Discord
    /// username passed in by the caller, and only as a last resort a neutral
    /// placeholder.
    /// </summary>
    public static string DisplayName(this Player player, string? liveUsername = null)
    {
        if (!string.IsNullOrWhiteSpace(player.Nickname))
            return player.Nickname!;

        if (HasRealName(player))
            return player.Name;

        if (!string.IsNullOrWhiteSpace(liveUsername))
            return liveUsername!;

        return "Unknown player";
    }

    /// <summary>
    /// True when the stored Name is something other than the numeric Discord id
    /// (older records were created with Name = discordId.ToString()).
    /// </summary>
    public static bool HasRealName(this Player player)
    {
        return !string.IsNullOrWhiteSpace(player.Name)
            && player.Name != player.DiscordId.ToString();
    }
}
