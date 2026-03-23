using LegaciesBot.Services;
using LegaciesBot.Services.CaptainDraft;
using NetCord.Services.Commands;

namespace LegaciesBot.Commands
{
    public class CaptainCommands
    {
        private readonly LobbyService _lobby;
        private readonly CaptainDraftService _captainDraft;

        public CaptainCommands(LobbyService lobby, CaptainDraftService captainDraft)
        {
            _lobby = lobby;
            _captainDraft = captainDraft;
        }

        [Command("captain")]
        public string ClaimCaptain(ulong userId)
        {
            var lobby = _lobby.CurrentLobby;

            if (!_captainDraft.TryClaimCaptain(lobby, userId))
                return "Two captains already exist.";

            return "You are now a captain.";
        }
        [Command("d")]
        [Command("draft")]
        public string Draft(ulong captainId, ulong targetId)
        {
            var lobby = _lobby.CurrentLobby;

            if (!_captainDraft.IsCaptainTurn(lobby, captainId))
                return "It is not your turn to pick.";

            if (!_captainDraft.TryPick(lobby, captainId, targetId))
                return "Invalid pick.";

            if (_captainDraft.DraftComplete(lobby))
                return "Draft complete! Teams are locked.";

            return "Pick successful.";
        }
    }
}