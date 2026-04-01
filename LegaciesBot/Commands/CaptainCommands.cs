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
        private const ulong DraftChannelId = 1488958363361349908;
        private const string CaptainRoleName = "Captain";

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

            await Context.Message.ReplyAsync(
                $"Lobby #{lobby.GameNumber} — Current captains: {string.Join(", ", names)}");
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
                await Context.Message.ReplyAsync(
                    $"Lobby #{lobby.GameNumber} — Two captains already exist: {string.Join(", ", names)}");
                return;
            }

            var guild = await _client.GetGuildAsync(GuildId);
            var role = guild.Roles.Values.FirstOrDefault(r => r.Name == CaptainRoleName);

            if (role is null)
                role = await _client.CreateRoleAsync(GuildId, CaptainRoleName);

            lobby.CaptainRoleId = role.Id;

            await _client.AddRoleToMemberAsync(GuildId, userId, role.Id);

            var currentNames = GetCaptainNames(lobby);
            await Context.Message.ReplyAsync(
                $"Lobby #{lobby.GameNumber} — You are now a captain! Current captains: {string.Join(", ", currentNames)}");
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
                await SendDraftMessage("Only Captain A may pass the first pick.");
                return;
            }

            if (lobby.CurrentPickIndex != 0)
            {
                await SendDraftMessage("You may only pass on the first pick.");
                return;
            }

            lobby.CaptainAPassed = true;
            _captainDraft.BuildDraftOrder(lobby);

            await SendDraftMessage(
                $"Lobby #{lobby.GameNumber} — Captain A has passed. Captain B picks first.");
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
                await SendDraftMessage(input + " is not a valid player.");
                return;
            }

            if (!lobby.Players.Any(p => p.DiscordId == targetId.Value))
            {
                await SendDraftMessage("That player is not in the lobby.");
                return;
            }

            if (!_captainDraft.IsCaptainTurn(lobby, captainId))
            {
                await SendDraftMessage("It is not your turn to pick.");
                return;
            }

            if (!_captainDraft.TryPick(lobby, captainId, targetId.Value))
            {
                await SendDraftMessage("Invalid pick.");
                return;
            }

            var pickedName = _playerRegistry.GetPlayer(targetId.Value)?.DisplayName() ?? targetId.Value.ToString();
            await SendDraftMessage($"Lobby #{lobby.GameNumber} — Picked {pickedName}");

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

            if (lobby.CaptainRoleId.HasValue)
            {
                if (lobby.CaptainA.HasValue)
                    await _client.RemoveRoleFromMemberAsync(GuildId, lobby.CaptainA.Value, lobby.CaptainRoleId.Value);

                if (lobby.CaptainB.HasValue)
                    await _client.RemoveRoleFromMemberAsync(GuildId, lobby.CaptainB.Value, lobby.CaptainRoleId.Value);
            }

            lobby.FactionAssignmentStarted = true;

            await SendDraftMessage(
                $"Lobby #{lobby.GameNumber} — Draft complete! Team roles assigned.\nProceed to faction assignment.");
        }

        private async Task SendDraftMessage(string message)
        {
            var channel = await _client.GetTextChannelAsync(DraftChannelId);
            if (channel != null)
                await channel.SendMessageAsync(message);
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
