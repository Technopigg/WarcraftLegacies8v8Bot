using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.CaptainDraft;
using LegaciesBot.Config;
using NetCord.Services.Commands;

namespace LegaciesBot.Commands
{
    public class CaptainCommands : CommandModule<CommandContext>
    {
        private readonly ILobbyService _lobby;
        private readonly ICaptainDraftService _captainDraft;
        private readonly NicknameService _nicknames;
        private readonly PlayerRegistryService _playerRegistry;
        private readonly IGatewayClient _client;

        private const ulong GuildId = 1218338908216229979;

        public CaptainCommands()
        {
            _lobby = GlobalServices.LobbyService;
            _captainDraft = GlobalServices.CaptainDraftService;
            _nicknames = GlobalServices.NicknameService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
            _client = GlobalServices.GameService.Client;
        }

        [Command("captains")]
        public async Task ListCaptains()
        {
            var lobby = _lobby.CurrentLobby;
            var names = GetCaptainNames(lobby);

            if (names.Length == 0)
            {
                await Context.Message.ReplyAsync("There are currently no captains claimed.");
                return;
            }

            await Context.Message.ReplyAsync("Current captains: " + string.Join(", ", names));
        }

        [Command("captain")]
        public async Task ClaimCaptain()
        {
            var userId = Context.User.Id;
            var lobby = _lobby.CurrentLobby;

            if (!lobby.Players.Any(p => p.DiscordId == userId))
            {
                await Context.Message.ReplyAsync("You must join the lobby with !j before you can claim captain.");
                return;
            }

            if (lobby.CaptainA == userId || lobby.CaptainB == userId)
            {
                await Context.Message.ReplyAsync("You are already a captain.");
                return;
            }

            if (!_captainDraft.TryClaimCaptain(lobby, userId))
            {
                var names = GetCaptainNames(lobby);
                await Context.Message.ReplyAsync("Two captains already exist: " + string.Join(", ", names));
                return;
            }

            var currentNames = GetCaptainNames(lobby);
            await Context.Message.ReplyAsync("You are now a captain! Current captains: " + string.Join(", ", currentNames));
        }

        [Command("uncaptain", "drop")]
        public async Task UnclaimCaptain()
        {
            var userId = Context.User.Id;
            var lobby = _lobby.CurrentLobby;

            if (lobby.CaptainA == userId)
            {
                lobby.CaptainA = null;
                await Context.Message.ReplyAsync("You dropped Captain A.");
                return;
            }

            if (lobby.CaptainB == userId)
            {
                lobby.CaptainB = null;
                await Context.Message.ReplyAsync("You dropped Captain B.");
                return;
            }

            await Context.Message.ReplyAsync("You are not a captain.");
        }

        [Command("pass")]
        public async Task Pass()
        {
            var captainId = Context.User.Id;
            var lobby = _lobby.CurrentLobby;

            if (lobby.CaptainA != captainId)
            {
                await Context.Message.ReplyAsync("Only Captain A may pass the first pick.");
                return;
            }

            if (lobby.CurrentPickIndex != 0)
            {
                await Context.Message.ReplyAsync("You may only pass on the first pick.");
                return;
            }

            lobby.CaptainAPassed = true;
            _captainDraft.BuildDraftOrder(lobby);

            await Context.Message.ReplyAsync("Captain A has passed. Captain B picks first.");
        }

        [Command("d", "draft")]
        public async Task Draft(string input)
        {
            var captainId = Context.User.Id;
            var lobby = _lobby.CurrentLobby;

            ulong? targetId = (Context.Message.MentionedUsers.Count > 0)
                ? Context.Message.MentionedUsers.First().Id
                : _nicknames.ResolvePlayerId(input);

            if (!targetId.HasValue)
            {
                await Context.Message.ReplyAsync(input + " is not a valid player.");
                return;
            }

            if (!lobby.Players.Any(p => p.DiscordId == targetId.Value))
            {
                await Context.Message.ReplyAsync("That player is not in the lobby.");
                return;
            }

            if (!_captainDraft.IsCaptainTurn(lobby, captainId))
            {
                await Context.Message.ReplyAsync("It is not your turn to pick.");
                return;
            }

            if (!_captainDraft.TryPick(lobby, captainId, targetId.Value))
            {
                await Context.Message.ReplyAsync("Invalid pick.");
                return;
            }

            var pickedName = _playerRegistry.GetPlayer(targetId.Value)?.DisplayName() ?? targetId.Value.ToString();
            await Context.Message.ReplyAsync("Picked " + pickedName);

            if (_captainDraft.DraftComplete(lobby))
                await FinalizeDraft(lobby);
        }

        private async Task FinalizeDraft(Lobby lobby)
        {
            var teamAPlayers = lobby.TeamAPicks.Select(id => _playerRegistry.GetPlayer(id)).Where(p => p != null).ToList();
            var teamBPlayers = lobby.TeamBPicks.Select(id => _playerRegistry.GetPlayer(id)).Where(p => p != null).ToList();

            foreach (var p in teamAPlayers)
                await _client.AddRoleToMemberAsync(GuildId, p!.DiscordId, RoleConfig.Team1Role);

            foreach (var p in teamBPlayers)
                await _client.AddRoleToMemberAsync(GuildId, p!.DiscordId, RoleConfig.Team2Role);

            lobby.FactionAssignmentStarted = true;

            await Context.Message.ReplyAsync("Draft complete! Team roles assigned.\nProceed to faction assignment.");
        }

        private string[] GetCaptainNames(Lobby lobby)
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
