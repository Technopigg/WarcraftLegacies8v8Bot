using NetCord.Services.Commands;
using LegaciesBot.Services;
using System.Linq;
using System.Threading.Tasks;
using LegaciesBot.GameData;

namespace LegaciesBot.Discord
{
    public class LobbyCommands : CommandModule<CommandContext>
    {
        private readonly LobbyService _lobbyService;
        private readonly GameService _gameService;
        private readonly PlayerDataService _playerData;
        private readonly PlayerStatsService _playerStats;

        public LobbyCommands(
            LobbyService lobbyService,
            GameService gameService,
            PlayerDataService playerData,
            PlayerStatsService playerStats)
        {
            _lobbyService = lobbyService;
            _gameService = gameService;
            _playerData = playerData;
            _playerStats = playerStats;
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

            var savedPrefs = _playerData.GetPreferences(player.DiscordId);
            if (savedPrefs.Count > 0)
                player.FactionPreferences = savedPrefs.ToList();

            var stats = _playerStats.GetOrCreate(player.DiscordId);
            player.Elo = stats.Elo;

            if (savedPrefs.Count > 0)
            {
                await ctx.Message.ReplyAsync(
                    $"Welcome {player.Name}! Your saved preferences are: {string.Join(", ", savedPrefs)}.\n" +
                    $"Type `!prefs <list>` to update them."
                );
            }
            else
            {
                await ctx.Message.ReplyAsync(
                    $"Welcome {player.Name}! Submit your faction preferences with `!prefs <list>`."
                );
            }

            if (_lobbyService.CurrentLobby.IsFull && !_lobbyService.CurrentLobby.DraftStarted)
                await _gameService.StartDraft(_lobbyService.CurrentLobby, ctx.Message.ChannelId);
        }
        [Command("prefs")]
        public async Task ShowPreferences()
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            var current = _playerData.GetPreferences(userId);

            if (current.Count == 0)
            {
                await ctx.Message.ReplyAsync("You have no faction preferences set.");
            }
            else
            {
                await ctx.Message.ReplyAsync(
                    $"Your current faction preferences are: {string.Join(", ", current)}"
                );
            }
        }
        
        [Command("prefs")]
        public async Task SetPreferences(params string[] rawInput)
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            if (rawInput.Length == 0)
            {
                await ctx.Message.ReplyAsync("You must specify at least one faction.");
                return;
            }
            
            var combined = string.Join(" ", rawInput);

            var factions = combined
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToList();
            
            var validNames = FactionRegistry.All
                .Select(f => f.Name.ToLowerInvariant())
                .ToHashSet();

            foreach (var f in factions)
            {
                if (!validNames.Contains(f.ToLowerInvariant()))
                {
                    await ctx.Message.ReplyAsync(
                        $"`{f}` is not a valid faction.\n" +
                        $"Valid factions are:\n" +
                        $"{string.Join(", ", FactionRegistry.All.Select(x => x.Name))}"
                    );
                    return;
                }
            }
            _playerData.SetPreferences(userId, factions);

            await ctx.Message.ReplyAsync(
                $"Your faction preferences have been updated to: {string.Join(", ", factions)}"
            );
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