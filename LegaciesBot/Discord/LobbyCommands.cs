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
                string name = $"TestPlayer{i + 1}";

                var player = _lobbyService.JoinLobby(fakeId, name);

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
                await ShowPreferencesForUser(mentioned.Id, mentioned.Username);
                return;
            }
            
            if (args.Length == 0)
            {
                await ShowPreferencesForUser(callerId, ctx.Message.Author.Username);
                return;
            }

            var sub = args[0].ToLowerInvariant();

            if (sub == "show")
            {
                await ShowPreferencesForUser(callerId, ctx.Message.Author.Username);
                return;
            }

            if (sub == "clear")
            {
                await ClearPreferences(callerId);
                return;
            }

            if (sub == "add")
            {
                await AddPreference(callerId, args.Skip(1).ToArray());
                return;
            }

            if (sub == "remove")
            {
                await RemovePreference(callerId, args.Skip(1).ToArray());
                return;
            }
            
            await SetPreferencesList(callerId, args);
        }

        private async Task ShowPreferencesForUser(ulong userId, string username)
        {
            var prefs = _playerData.GetPreferences(userId);

            if (prefs.Count == 0)
            {
                await Context.Message.ReplyAsync(
                    userId == Context.Message.Author.Id
                        ? "You have no faction preferences set."
                        : $"{username} has no faction preferences set."
                );
            }
            else
            {
                await Context.Message.ReplyAsync(
                    userId == Context.Message.Author.Id
                        ? $"Your current faction preferences are: {string.Join(", ", prefs)}"
                        : $"{username}'s current faction preferences are: {string.Join(", ", prefs)}"
                );
            }
        }

        private async Task ClearPreferences(ulong userId)
        {
            var current = _playerData.GetPreferences(userId);
            if (current.Count == 0)
            {
                await Context.Message.ReplyAsync("You have no preferences to clear.");
                return;
            }

            _playerData.SetPreferences(userId, new System.Collections.Generic.List<string>());
            await Context.Message.ReplyAsync("Your faction preferences have been cleared.");
        }

        private async Task AddPreference(ulong userId, string[] args)
        {
            if (args.Length == 0)
            {
                await Context.Message.ReplyAsync("Usage: `!prefs add <Faction> [Position]`");
                return;
            }
            
            string factionName = args[0];
            int? position = null;

            if (args.Length >= 2 && int.TryParse(args[1], out int pos))
                position = pos;

            var validNames = FactionRegistry.All
                .Select(f => f.Name.ToLowerInvariant())
                .ToHashSet();

            if (!validNames.Contains(factionName.ToLowerInvariant()))
            {
                await Context.Message.ReplyAsync(
                    $"`{factionName}` is not a valid faction.\n" +
                    $"Valid factions are:\n" +
                    $"{string.Join(", ", FactionRegistry.All.Select(x => x.Name))}"
                );
                return;
            }

            var prefs = _playerData.GetPreferences(userId);
            
            prefs = prefs
                .Where(p => !p.Equals(factionName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (position.HasValue && position.Value > 0)
            {
                int index = position.Value - 1;
                if (index >= prefs.Count)
                    prefs.Add(factionName);
                else
                    prefs.Insert(index, factionName);
            }
            else
            {
                prefs.Add(factionName);
            }

            _playerData.SetPreferences(userId, prefs);

            await Context.Message.ReplyAsync(
                $"Your faction preferences have been updated to: {string.Join(", ", prefs)}"
            );
        }

        private async Task RemovePreference(ulong userId, string[] args)
        {
            if (args.Length == 0)
            {
                await Context.Message.ReplyAsync("Usage: `!prefs remove <Faction>`");
                return;
            }

            string factionName = args[0];

            var prefs = _playerData.GetPreferences(userId);

            var existing = prefs
                .FirstOrDefault(p => p.Equals(factionName, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                await Context.Message.ReplyAsync(
                    $"You don't have `{factionName}` in your faction preferences."
                );
                return;
            }

            prefs = prefs
                .Where(p => !p.Equals(factionName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _playerData.SetPreferences(userId, prefs);

            await Context.Message.ReplyAsync(
                $"Your faction preferences have been updated to: {string.Join(", ", prefs)}"
            );
        }

        private async Task SetPreferencesList(ulong userId, string[] rawInput)
        {
            if (rawInput.Length == 0)
            {
                await Context.Message.ReplyAsync("You must specify at least one faction.");
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
                    await Context.Message.ReplyAsync(
                        $"`{f}` is not a valid faction.\n" +
                        $"Valid factions are:\n" +
                        $"{string.Join(", ", FactionRegistry.All.Select(x => x.Name))}"
                    );
                    return;
                }
            }

            _playerData.SetPreferences(userId, factions);

            await Context.Message.ReplyAsync(
                $"Your faction preferences have been updated to: {string.Join(", ", factions)}"
            );
        }
        [Command("leave")]
        [Command("l")]
        public async Task LeaveLobby()
        {
            var ctx = this.Context;

            bool success = _lobbyService.RemovePlayer(ctx.Message.Author.Id);

            if (!success)
            {
                await ctx.Message.ReplyAsync("You are not currently in a lobby or the draft has already started.");
                return;
            }

            await ctx.Message.ReplyAsync($"{ctx.Message.Author.Username} has left the lobby.");
        }

        [Command("here")]
        [Command("h")]
        public async Task MarkActive()
        {
            var ctx = this.Context;

            bool success = _lobbyService.MarkActive(ctx.Message.Author.Id);

            if (!success)
            {
                await ctx.Message.ReplyAsync("You are not currently in a lobby.");
                return;
            }

            await ctx.Message.ReplyAsync($"{ctx.Message.Author.Username}, all good!");
        }
        
       
        [Command("lobby")]
        public async Task ListLobbyMembers()
        {
            var ctx = this.Context;

            var members = _lobbyService.GetLobbyMembers();

            if (members.Count == 0)
            {
                await ctx.Message.ReplyAsync("The lobby is currently empty.");
                return;
            }

            string msg = "Current lobby members:\n" +
                         string.Join("\n", members.Select(p => $"- {p.Name}"));

            await ctx.Message.ReplyAsync(msg);
        }

    }
}