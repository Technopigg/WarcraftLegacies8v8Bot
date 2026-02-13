using NetCord;

namespace LegaciesBot.Services;

public class RealTextChannel : ITextChannel
{
    private readonly TextChannel _channel;

    public RealTextChannel(TextChannel channel)
    {
        _channel = channel;
    }

    public Task SendMessageAsync(string message)
    {
        return _channel.SendMessageAsync(message);
    }
}