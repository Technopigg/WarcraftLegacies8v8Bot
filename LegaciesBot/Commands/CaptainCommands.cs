using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.CaptainDraft;
using NetCord.Services.Commands;

namespace LegaciesBot.Commands
{
    public class CaptainCommands : CommandModule<CommandContext>
    {
        private readonly ILobbyService _lobby;
        private readonly ICaptainDraftService _captainDraft;
        private readonly NicknameService _nicknames;
        private readonly PlayerRegistryService _playerRegistry;

        public CaptainCommands(
            ILobbyService lobby, 
            ICaptainDraftService captainDraft, 
            NicknameService nicknames,
            PlayerRegistryService playerRegistry)
        {
            _lobby = lobby;
            _captainDraft = captainDraft;
            _nicknames = nicknames;
            _playerRegistry = playerRegistry;
        }

        [Command("captains")]
        public string ListCaptains()
        {
            var lobby = _lobby.CurrentLobby;
            var names = GetCaptainNames(lobby);

            if (names.Length == 0)
                return "There are currently no captains claimed.";

            return $"Current captains: {string.Join(", ", names)}";
        }

        [Command("captain")]
        public string ClaimCaptain(ulong testUserId = 0)
        {
            var userId = testUserId == 0 ? Context.User.Id : testUserId;
            var lobby = _lobby.CurrentLobby;
            
            if (lobby.CaptainA == userId || lobby.CaptainB == userId)
                return "You are already a captain.";
            
            if (!_captainDraft.TryClaimCaptain(lobby, userId))
            {
                var names = GetCaptainNames(lobby);
                return $"Two captains already exist: {string.Join(", ", names)}.";
            }
            
            var currentNames = GetCaptainNames(lobby);
            return $"You are now a captain! Current captains: {string.Join(", ", currentNames)}.";
        }

        [Command("uncaptain", "drop")]
        public string UnclaimCaptain(ulong testUserId = 0)
        {
            var userId = testUserId == 0 ? Context.User.Id : testUserId;
            var lobby = _lobby.CurrentLobby;

            if (lobby.CaptainA == userId) { lobby.CaptainA = null; return "You dropped Captain A."; }
            if (lobby.CaptainB == userId) { lobby.CaptainB = null; return "You dropped Captain B."; }

            return "You are not a captain.";
        }

        [Command("d", "draft")]
        public string Draft(string input, ulong testCaptainId = 0)
        {
            var captainId = testCaptainId == 0 ? Context.User.Id : testCaptainId;
            var lobby = _lobby.CurrentLobby;

            ulong? targetId = (Context != null && Context.Message.MentionedUsers.Count > 0) 
                ? Context.Message.MentionedUsers.First().Id 
                : _nicknames.ResolvePlayerId(input);

            if (!targetId.HasValue)
                return $"`{input}` is not a valid player or mention.";

            if (!_captainDraft.IsCaptainTurn(lobby, captainId))
                return "It is not your turn to pick.";

            if (!_captainDraft.TryPick(lobby, captainId, targetId.Value))
                return "Invalid pick (player might already be picked or not in lobby).";

            if (_captainDraft.DraftComplete(lobby))
                return "Draft complete! Teams are locked.";

            var pickedName = _playerRegistry.GetPlayer(targetId.Value)?.DisplayName() ?? targetId.Value.ToString();
            return $"Successfully picked **{pickedName}**.";
        }

        private string[] GetCaptainNames(Core.Lobby lobby)
        {
            var list = new List<string>();
            if (lobby.CaptainA.HasValue) 
                list.Add(_playerRegistry.GetPlayer(lobby.CaptainA.Value)?.DisplayName() ?? "Unknown");
            if (lobby.CaptainB.HasValue) 
                list.Add(_playerRegistry.GetPlayer(lobby.CaptainB.Value)?.DisplayName() ?? "Unknown");
            return list.ToArray();
        }
    }
}
