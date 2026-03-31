using NetCord.Services.Commands;

namespace LegaciesBot.Services
{
    public class DiscordUserContext : IUserContext
    {
        private readonly CommandContext _ctx;

        public DiscordUserContext(CommandContext ctx)
        {
            _ctx = ctx;
        }

        public ulong UserId => _ctx.User.Id;
    }
}