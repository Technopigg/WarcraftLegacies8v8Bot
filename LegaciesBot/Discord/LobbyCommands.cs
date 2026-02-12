using NetCord.Services.Commands;
using LegaciesBot.Services;
using System.Linq;
using System.Threading.Tasks;

namespace LegaciesBot.Discord
{
    public class LobbyCommands : CommandModule<CommandContext>
    {
        private readonly LobbyService _lobbyService;
        private readonly GameService _gameService;
        private readonly PlayerDataService _playerData;

        public LobbyCommands(LobbyService lobbyService, GameService gameService, PlayerDataService playerData)
        {
            _lobbyService = lobbyService;
            _gameService = gameService;
            _playerData = playerData;
        }
        [Command("join")]
        [Command("j")]
        public async Task JoinLobby()
        {
            var ctx = this.Context;
            var player = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == ctx.Message.Author.Id);

            if (player != null)
            {
                await ctx.Message.ReplyAsync($"{player.Name}, you are already in the lobby.");
                return;
            }

            player = _lobbyService.JoinLobby(ctx.Message.Author.Id, ctx.Message.Author.Username);
            await ctx.Message.ReplyAsync($"Welcome {player.Name}! Submit your faction preferences with !prefs <list>.");

            if (_lobbyService.CurrentLobby.IsFull && !_lobbyService.CurrentLobby.DraftStarted)
                await _gameService.StartDraft(_lobbyService.CurrentLobby, ctx.Message.ChannelId);
        }
        
        [Command("prefs")]
        public async Task ShowPreferences()
        {
            var ctx = this.Context;
            var player = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == ctx.Message.Author.Id);

            if (player == null)
            {
                await ctx.Message.ReplyAsync("You are not currently in a lobby.");
                return;
            }

            if (player.FactionPreferences.Any())
                await ctx.Message.ReplyAsync($"Your current preferences: {string.Join(", ", player.FactionPreferences)}");
            else
                await ctx.Message.ReplyAsync("You have no preferences set.");
        }

        [Command("leave")]
        [Command("l")]
        public async Task LeaveLobby()
        {
            var ctx = this.Context;
            var lobby = _lobbyService.CurrentLobby;

            var player = lobby.Players
                .FirstOrDefault(p => p.DiscordId == ctx.Message.Author.Id);

            if (player == null)
            {
                await ctx.Message.ReplyAsync("You are not currently in a lobby.");
                return;
            }

            if (lobby.DraftStarted)
            {
                await ctx.Message.ReplyAsync("Cannot leave a drafting lobby.");
                return;
            }

            lobby.Players.Remove(player);

            await ctx.Message.ReplyAsync($"{player.Name} has left the lobby.");
        }

        [Command("prefs")]
        public async Task SetPreferences(params string[] args)
        {
            var ctx = this.Context;
            var player = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == ctx.Message.Author.Id);

            if (player == null)
            {
                await ctx.Message.ReplyAsync("You are not currently in a lobby.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                await ctx.Message.ReplyAsync("You must provide at least one preference, e.g. `!prefs Dalaran, Lordaeron`.");
                return;
            }
            
            var combined = string.Join(" ", args);
            
            var newPrefs = combined
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();

            player.FactionPreferences = newPrefs;

            await ctx.Message.ReplyAsync($"Preferences updated: {string.Join(", ", player.FactionPreferences)}");
        }
        [Command("h")]
        [Command("here")]
        public async Task MarkActive()
        {
            var ctx = this.Context;
            var player = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == ctx.Message.Author.Id);

            if (player != null)
            {
                player.IsActive = true;
                player.JoinedAt = System.DateTime.UtcNow;
                await ctx.Message.ReplyAsync($"{player.Name}, All good!");
            }
            else
            {
                await ctx.Message.ReplyAsync("You are not currently in a lobby.");
            }
        }

        [Command("lobby")]
        public async Task ListLobbyMembers()
        {
            var ctx = this.Context;
            var lobby = _lobbyService.CurrentLobby;

            if (!lobby.Players.Any())
            {
                await ctx.Message.ReplyAsync("The lobby is currently empty.");
                return;
            }

            string msg = "Current lobby members:\n";
            foreach (var p in lobby.Players)
            {
                msg += $"- {p.Name}\n";
            }

            await ctx.Message.ReplyAsync(msg);
        }
    }
}