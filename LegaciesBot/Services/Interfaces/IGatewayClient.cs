namespace LegaciesBot.Services;

public interface IGatewayClient
{
    Task<ITextChannel?> GetTextChannelAsync(ulong channelId);
}