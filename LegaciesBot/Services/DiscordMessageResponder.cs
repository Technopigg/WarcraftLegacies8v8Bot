using NetCord.Services.Commands;

namespace LegaciesBot.Services
{
    public class DiscordMessageResponder : IMessageResponder
    {
        private readonly CommandContext _ctx;

        public DiscordMessageResponder(CommandContext ctx)
        {
            _ctx = ctx;
        }

        public Task ReplyAsync(string message) => _ctx.Message.ReplyAsync(message);
    }
}
