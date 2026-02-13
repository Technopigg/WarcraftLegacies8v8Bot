namespace LegaciesBot.Services;

public interface ITextChannel
{
    Task SendMessageAsync(string message);
}