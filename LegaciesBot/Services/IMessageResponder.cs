namespace LegaciesBot.Services
{
    public interface IMessageResponder
    {
        Task ReplyAsync(string message);
    }
}