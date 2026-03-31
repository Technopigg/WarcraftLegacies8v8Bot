using LegaciesBot.Services;
using NetCord.Services.Commands;

namespace LegaciesBot.Commands
{
    public class FactionCommands : CommandModule<CommandContext>
    {
        private readonly ILobbyService _lobby;
        private readonly FactionManualAssignmentService _manual;

        public FactionCommands()
        {
            _lobby = GlobalServices.LobbyService;
            _manual = GlobalServices.FactionManualAssignmentService;
        }

        [Command("assignf")]
        public async Task AssignFaction(string player, string faction)
        {
            var lobby = _lobby.CurrentLobby;
            var captainId = Context.User.Id;

            if (!_manual.TryAssignSingle(lobby, captainId, player, faction))
            {
                await Context.Message.ReplyAsync("Invalid faction assignment.");
                return;
            }

            await Context.Message.ReplyAsync("Faction assigned.");
        }

        [Command("assignfactions")]
        public async Task AssignFactions([CommandParameter(Remainder = true)] string bulk)
        {
            var lobby = _lobby.CurrentLobby;
            var captainId = Context.User.Id;

            var errors = _manual.AssignBulk(lobby, captainId, bulk);

            if (errors.Count == 0)
            {
                await Context.Message.ReplyAsync("Bulk assignment complete.");
                return;
            }

            await Context.Message.ReplyAsync("Some assignments failed:\n" + string.Join("\n", errors));
        }

        [Command("lockfactions")]
        public async Task LockFactions()
        {
            var lobby = _lobby.CurrentLobby;
            var captainId = Context.User.Id;

            if (!_manual.TryLockFactions(lobby, captainId, out var message))
            {
                await Context.Message.ReplyAsync(message);
                return;
            }

            await Context.Message.ReplyAsync(message);

            if (lobby.TeamAFactionsLocked && lobby.TeamBFactionsLocked)
                await Context.Message.ReplyAsync("Both teams have locked factions. The game is starting.");
        }
    }
}
