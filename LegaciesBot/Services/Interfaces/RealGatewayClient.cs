using NetCord.Gateway;


namespace LegaciesBot.Services;

public class RealGatewayClient : IGatewayClient
{
    private readonly GatewayClient _client;

    public RealGatewayClient(GatewayClient client)
    {
        _client = client;
    }

    public async Task<ITextChannel?> GetTextChannelAsync(ulong channelId)
    {
        var channel = await _client.Rest.GetChannelAsync(channelId);

        if (channel is NetCord.TextChannel text)
            return new RealTextChannel(text);

        return null;
    }
}