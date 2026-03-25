using LegaciesBot.Core;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.GameData;

namespace LegaciesBot.Discord
{
    public class LobbyCommands : CommandModule<CommandContext>
    {
        private readonly LobbyService _lobbyService;
        private readonly GameService _gameService;
        private readonly PlayerDataService _playerData;
        private readonly PlayerStatsService _playerStats;
        private readonly PlayerRegistryService _playerRegistry;

        public LobbyCommands(
            ILobbyService lobbyService, 
            GameService gameService,
            PlayerDataService playerData,
            PlayerStatsService playerStats,
            PlayerRegistryService playerRegistry)
        {
            _lobbyService = (LobbyService)lobbyService;
            _gameService = gameService;
            _playerData = playerData;
            _playerStats = playerStats;
            _playerRegistry = playerRegistry;
        }

        [Command("join")]
        [Command("j")]
        public async Task JoinLobby()
        {
            var ctx = this.Context;
            var discordId = ctx.Message.Author.Id;
            var existing = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == discordId);

            if (existing != null)
            {
                await ctx.Message.ReplyAsync($"{existing.DisplayName()} ({existing.Elo}), you are already in the lobby.");
                return;
            }

            var player = _lobbyService.JoinLobby(discordId);
            var stats = _playerStats.GetOrCreate(player.DiscordId);
            player.Elo = stats.Elo;

            var savedPrefs = _playerData.GetPreferences(player.DiscordId);
            if (savedPrefs.Count > 0)
                player.FactionPreferences = savedPrefs.ToList();

            string display = $"{player.DisplayName()} ({player.Elo})";

            if (savedPrefs.Count > 0)
            {
                await ctx.Message.ReplyAsync(
                    $"Welcome {display}! Your saved preferences are: {string.Join(", ", savedPrefs)}.\n" +
                    $"Type `!prefs <list>` to update them."
                );
            }
            else
            {
                await ctx.Message.ReplyAsync(
                    $"Welcome {display}! Submit your faction preferences with `!prefs <list>`."
                );
            }

            if (_lobbyService.CurrentLobby.IsFull && !_lobbyService.CurrentLobby.DraftStarted)
                await _gameService.StartDraft(_lobbyService.CurrentLobby, ctx.Message.ChannelId);
        }

        [Command("lobby")]
        [Command("l")]
        public async Task ShowLobby()
        {
            var lobby = _lobbyService.CurrentLobby;
            if (lobby.Players.Count == 0)
            {
                await Context.Message.ReplyAsync("The lobby is currently empty.");
                return;
            }

            var lines = lobby.Players.Select(p => $"- {p.DisplayName()} ({p.Elo})");
            string msg = $"**Current lobby members ({lobby.Players.Count}/16):**\n" + string.Join("\n", lines);
            await Context.Message.ReplyAsync(msg);
        }

        [Command("leave")]
        [Command("quit")]
        public async Task LeaveLobby()
        {
            var ctx = this.Context;
            var userId = ctx.Message.Author.Id;
            var player = _lobbyService.CurrentLobby.Players.FirstOrDefault(p => p.DiscordId == userId);
            
            if (player == null)
            {
                await ctx.Message.ReplyAsync("You are not in the lobby.");
                return;
            }

            string display = $"{player.DisplayName()} ({player.Elo})";
            _lobbyService.RemovePlayer(userId);
            await ctx.Message.ReplyAsync($"{display} has left the lobby.");
        }

        [Command("debugfill")]
        public async Task DebugFill()
        {
            var ctx = this.Context;
            var lobby = _lobbyService.CurrentLobby;

            if (lobby.Players.Count >= 16)
            {
                await ctx.Message.ReplyAsync("Lobby is already full.");
                return;
            }

            var allFactions = FactionRegistry.All.Select(f => f.Name).ToList();
            var rand = new Random();
            int needed = 16 - lobby.Players.Count;

            for (int i = 0; i < needed; i++)
            {
                ulong fakeId = (ulong)rand.NextInt64();
                var player = _lobbyService.JoinLobby(fakeId);
                player.Name = $"TestPlayer{i + 1}";
                player.Elo = rand.Next(1000, 2000);

                int prefCount = rand.Next(1, 5);
                player.FactionPreferences = allFactions
                    .OrderBy(_ => rand.Next())
                    .Take(prefCount)
                    .ToList();
            }

            await ctx.Message.ReplyAsync($"Filled lobby with {needed} test players.");

            if (lobby.IsFull && !lobby.DraftStarted)
                await _gameService.StartDraft(lobby, ctx.Message.ChannelId);
        }

        [Command("prefs")]
        public async Task Preferences(params string[] args)
        {
            var ctx = this.Context;
            ulong callerId = ctx.Message.Author.Id;

            if (ctx.Message.MentionedUsers.Count > 0)
            {
                var mentioned = ctx.Message.MentionedUsers[0];
                var reg = _playerRegistry.GetPlayer(mentioned.Id);
                string display = reg?.DisplayName() ?? mentioned.Username;
                await ShowPreferencesForUser(mentioned.Id, display);
                return;
            }

            if (args.Length == 0)
            {
                var reg = _playerRegistry.GetPlayer(callerId);
                string display = reg?.DisplayName() ?? ctx.Message.Author.Username;
                await ShowPreferencesForUser(callerId, display);
                return;
            }

            var sub = args[0].ToLowerInvariant();
            if (sub == "show")
            {
                var reg = _playerRegistry.GetPlayer(callerId);
                string display = reg?.DisplayName() ?? ctx.Message.Author.Username;
                await ShowPreferencesForUser(callerId, display);
                return;
            }
            if (sub == "clear") { await ClearPreferences(callerId); return; }
            if (sub == "add") { await AddPreference(callerId, args.Skip(1).ToArray()); return; }
            if (sub == "remove") { await RemovePreference(callerId, args.Skip(1).ToArray()); return; }

            await SetPreferencesList(callerId, args);
        }

        private async Task ShowPreferencesForUser(ulong userId, string displayName)
        {
            var prefs = _playerData.GetPreferences(userId);
            if (prefs.Count == 0)
            {
                await Context.Message.ReplyAsync(userId == Context.Message.Author.Id ? "You have no faction preferences set." : $"{displayName} has no faction preferences set.");
            }
            else
            {
                await Context.Message.ReplyAsync(userId == Context.Message.Author.Id ? $"Your current faction preferences are: {string.Join(", ", prefs)}" : $"{displayName}'s current faction preferences are: {string.Join(", ", prefs)}");
            }
        }

        private async Task ClearPreferences(ulong userId)
        {
            var current = _playerData.GetPreferences(userId);
            if (current.Count == 0) { await Context.Message.ReplyAsync("You have no preferences to clear."); return; }
            _playerData.SetPreferences(userId, new List<string>());
            await Context.Message.ReplyAsync("Your faction preferences have been cleared.");
        }

        private async Task AddPreference(ulong userId, string[] args)
        {
            if (args.Length == 0) { await Context.Message.ReplyAsync("Usage: `!prefs add <Faction> [Position]`"); return; }
            string factionName = args[0];
            var validNames = FactionRegistry.All.Select(f => f.Name.ToLowerInvariant()).ToHashSet();
            if (!validNames.Contains(factionName.ToLowerInvariant()))
            {
                await Context.Message.ReplyAsync($"`{factionName}` is not a valid faction.\nValid factions are:\n{string.Join(", ", FactionRegistry.All.Select(x => x.Name))}");
                return;
            }
            var prefs = _playerData.GetPreferences(userId);
            if (!prefs.Contains(factionName, StringComparer.OrdinalIgnoreCase))
            {
                prefs.Add(factionName);
                _playerData.SetPreferences(userId, prefs);
                await Context.Message.ReplyAsync($"Added `{factionName}` to your preferences.");
            }
        }

        private async Task RemovePreference(ulong userId, string[] args)
        {
            if (args.Length == 0) { await Context.Message.ReplyAsync("Usage: `!prefs remove <Faction>`"); return; }
            var prefs = _playerData.GetPreferences(userId);
            if (prefs.RemoveAll(p => p.Equals(args[0], StringComparison.OrdinalIgnoreCase)) > 0)
            {
                _playerData.SetPreferences(userId, prefs);
                await Context.Message.ReplyAsync($"Removed `{args[0]}` from your preferences.");
            }
        }

        private async Task SetPreferencesList(ulong userId, string[] args)
        {
            var validNames = FactionRegistry.All.ToDictionary(f => f.Name.ToLowerInvariant(), f => f.Name);
            var newPrefs = new List<string>();
            foreach (var arg in args)
            {
                if (validNames.TryGetValue(arg.ToLowerInvariant(), out var correctName))
                    newPrefs.Add(correctName);
            }
            if (newPrefs.Count > 0)
            {
                _playerData.SetPreferences(userId, newPrefs);
                await Context.Message.ReplyAsync($"Preferences updated to: {string.Join(", ", newPrefs)}");
            }
        }
    }
}
