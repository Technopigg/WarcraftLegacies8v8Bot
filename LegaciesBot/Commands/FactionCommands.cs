using LegaciesBot.Services;
using NetCord.Services.Commands;

namespace LegaciesBot.Commands
{
    public class FactionCommands
    {
        private readonly ILobbyService _lobby;
        private readonly FactionManualAssignmentService _manual;

        public FactionCommands(ILobbyService lobby, FactionManualAssignmentService manual)
        {
            _lobby = lobby;
            _manual = manual;
        }

        [Command("setfaction")]
        public string SetFaction(ulong captainId, string player, string faction)
        {
            var lobby = _lobby.CurrentLobby;

            if (!_manual.TryAssignSingle(lobby, captainId, player, faction))
                return "Invalid faction assignment.";

            if (lobby.ManualFactionAssignments.Count == 16)
                return "All factions assigned. Use !submit.";

            return "Faction assigned.";
        }

        [Command("setfactions")]
        public string SetFactions(ulong captainId, [CommandParameter(Remainder = true)] string bulk)
        {
            var lobby = _lobby.CurrentLobby;

            var errors = _manual.AssignBulk(lobby, captainId, bulk);

            if (errors.Count == 0)
            {
                if (lobby.ManualFactionAssignments.Count == 16)
                    return "All factions assigned. Use !submit.";

                return "Bulk assignment complete.";
            }

            return "Some assignments failed:\n" + string.Join("\n", errors);
        }

        [Command("submit")]
        public string Finalize(ulong captainId)
        {
            var lobby = _lobby.CurrentLobby;

            if (!_manual.TryFinalize(lobby))
                return "Not all factions have been assigned.";

            return "Factions finalized. Ready to start the game.";
        }
    }
}