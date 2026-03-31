using LegaciesBot.Core;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using LegaciesBot.Moderation;

namespace LegaciesBot.Discord
{
    public class LobbyCommands : CommandModule<CommandContext>
    {
        private readonly LobbyService _lobbyService;
        private readonly GameService _gameService;
        private readonly PlayerDataService _playerData;
        private readonly PlayerStatsService _playerStats;
        private readonly PlayerRegistryService _playerRegistry;
        private readonly ModerationService _moderation;
        private readonly NicknameService _nickname;

        public LobbyCommands()
        {
            _lobbyService = GlobalServices.LobbyService;
            _gameService = GlobalServices.GameService;
            _playerData = GlobalServices.PlayerDataService;
            _playerStats = GlobalServices.PlayerStatsService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
            _moderation = GlobalServices.ModerationService;
            _nickname = GlobalServices.NicknameService;
        }

        [Command("join")]
        [Command("j")]
        public async Task JoinLobby()
        {
            var ctx = this.Context;
            var discordId = ctx.Message.Author.Id;

            if (_moderation.IsBanned(discordId))
            {
                await ctx.Message.ReplyAsync("You are banned and cannot join the lobby.");
                return;
            }

            var existing = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == discordId);

            if (existing != null)
            {
                await ctx.Message.ReplyAsync(
                    $"{existing.DisplayName()} ({existing.Elo}), you are already in the lobby.");
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

            var lobby = _lobbyService.CurrentLobby;

            if (lobby.IsFull && !lobby.DraftStarted)
            {
                if (lobby.CaptainA != null && lobby.CaptainB != null)
                {
                    lobby.DraftMode = DraftMode.CaptainDraft_ManualFaction;

                    await ctx.Message.ReplyAsync(
                        "Two captains detected — switching to **Captain Draft (Manual Faction)**.\n" +
                        "Captains, begin drafting using `!draft @player`."
                    );

                    return;
                }

                lobby.DraftMode = DraftMode.AutoDraft_AutoFaction;

                await ctx.Message.ReplyAsync("Draft mode: **AutoDraft (Auto Faction)**");

                await _gameService.StartDraft(lobby, ctx.Message.ChannelId);
            }
        }

        [Command("lobby")]
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
        [Command("l")]
        public async Task LeaveLobby()
        {
            var ctx = this.Context;
            var userId = ctx.Message.Author.Id;

            var player = _lobbyService.CurrentLobby.Players
                .FirstOrDefault(p => p.DiscordId == userId);

            if (player == null)
            {
                await ctx.Message.ReplyAsync("You are not in the lobby.");
                return;
            }

            string display = $"{player.DisplayName()} ({player.Elo})";
            _lobbyService.RemovePlayer(userId);

            await ctx.Message.ReplyAsync($"{display} has left the lobby.");
        }

        [Command("prefs")]
        [Command("p")]
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

        private async Task ShowPreferencesForUser(ulong userId, string displayName)
        {
            var prefs = _playerData.GetPreferences(userId);

            if (prefs.Count == 0)
            {
                await Context.Message.ReplyAsync(
                    userId == Context.Message.Author.Id
                        ? "You have no faction preferences set."
                        : $"{displayName} has no faction preferences set."
                );
            }
            else
            {
                await Context.Message.ReplyAsync(
                    userId == Context.Message.Author.Id
                        ? $"Your current faction preferences are: {string.Join(", ", prefs)}"
                        : $"{displayName}'s current faction preferences are: {string.Join(", ", prefs)}"
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

            _playerData.SetPreferences(userId, new List<string>());
            await Context.Message.ReplyAsync("Your faction preferences have been cleared.");
        }

        private async Task AddPreference(ulong userId, string[] args)
        {
            if (args.Length == 0)
            {
                await Context.Message.ReplyAsync("Usage: `!prefs add <Faction>`");
                return;
            }

            string factionName = args[0];
            var validNames = FactionRegistry.All
                .Select(f => f.Name.ToLowerInvariant())
                .ToHashSet();

            if (!validNames.Contains(factionName.ToLowerInvariant()))
            {
                await Context.Message.ReplyAsync(
                    $"`{factionName}` is not a valid faction.\nValid factions are:\n{string.Join(", ", FactionRegistry.All.Select(x => x.Name))}"
                );
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
            if (args.Length == 0)
            {
                await Context.Message.ReplyAsync("Usage: `!prefs remove <Faction>`");
                return;
            }

            var prefs = _playerData.GetPreferences(userId);

            if (prefs.RemoveAll(p => p.Equals(args[0], StringComparison.OrdinalIgnoreCase)) > 0)
            {
                _playerData.SetPreferences(userId, prefs);
                await Context.Message.ReplyAsync($"Removed `{args[0]}` from your preferences.");
            }
        }

        private async Task SetPreferencesList(ulong userId, string[] args)
        {
            var validNames = FactionRegistry.All
                .ToDictionary(f => f.Name.ToLowerInvariant(), f => f.Name);

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

        [Command("help")]
        public async Task Help()
        {
            var ctx = this.Context;

            string path = Path.Combine(AppContext.BaseDirectory, "CommandList");

            if (!File.Exists(path))
            {
                await ctx.Message.ReplyAsync("Help file not found.");
                return;
            }

            var text = await File.ReadAllTextAsync(path);

            if (text.Length <= 2000)
            {
                await ctx.Message.ReplyAsync(text);
                return;
            }

            int index = 0;

            while (index < text.Length)
            {
                int length = Math.Min(2000, text.Length - index);
                string chunk = text.Substring(index, length);

                await ctx.Message.ReplyAsync(chunk);
                index += length;
            }
        }
    }
}
