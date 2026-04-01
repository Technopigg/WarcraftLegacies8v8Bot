using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.CaptainDraft;

namespace LegaciesBot.Commands
{
    public class CaptainCommands
    {
        private readonly ILobbyService _lobby;
        private readonly ICaptainDraftService _draft;
        private readonly NicknameService _nick;
        private readonly PlayerRegistryService _registry;

        public CaptainCommands(
            ILobbyService lobby,
            ICaptainDraftService draft,
            NicknameService nick,
            PlayerRegistryService registry)
        {
            _lobby = lobby;
            _draft = draft;
            _nick = nick;
            _registry = registry;
        }

        public string ClaimCaptain(ulong userId)
        {
            var lobby = _lobby.CurrentLobby;

            if (!lobby.Players.Any(p => p.DiscordId == userId))
                return "You must join the lobby with `!j` before you can claim captain.";

            if (_draft.TryClaimCaptain(lobby, userId))
                return "You are now a captain!";

            return "Unable to claim captain.";
        }

        public string Draft(string target, ulong captainId)
        {
            var lobby = _lobby.CurrentLobby;

            var targetId = _nick.ResolvePlayerId(target);
            if (targetId == null || !lobby.Players.Any(p => p.DiscordId == targetId))
                return "That player is not in the lobby.";

            if (_draft.TryPick(lobby, captainId, targetId.Value))
                return $"Drafted <@{targetId}>.";

            return "Unable to draft player.";
        }
    }
}