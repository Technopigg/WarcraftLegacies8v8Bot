namespace LegaciesBot.Services
{
    public enum LobbyAfkKind
    {
        Reminder, // "still here? you'll be dropped soon"
        Removed,  // dropped for inactivity
    }

    /// <summary>
    /// Something the AFK check wants the bot to say. CheckAfk stays free of Discord
    /// so it is testable; Program posts these to <see cref="ChannelId"/>.
    /// </summary>
    public sealed record LobbyAfkNotice(
        LobbyAfkKind Kind,
        ulong DiscordId,
        string DisplayName,
        ulong ChannelId,
        int LobbyCount);
}
