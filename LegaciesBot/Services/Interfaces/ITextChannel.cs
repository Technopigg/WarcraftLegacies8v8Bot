using NetCord.Rest;

namespace LegaciesBot.Services;

public interface ITextChannel
{
    Task SendMessageAsync(string message);
    Task SendMessageAsync(MessageProperties message);
}