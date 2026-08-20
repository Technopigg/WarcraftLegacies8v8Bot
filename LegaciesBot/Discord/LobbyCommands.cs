using LegaciesBot.Core;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Services.CaptainDraft;
using LegaciesBot.GameData;
using LegaciesBot.Moderation;
using NetCord.Rest;

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
        private readonly CaptainDraftService _captainDraft;
        private readonly SiteApiService _site;

        public LobbyCommands()
        {
            _lobbyService = GlobalServices.LobbyService;
            _gameService = GlobalServices.GameService;
            _playerData = GlobalServices.PlayerDataService;
            _playerStats = GlobalServices.PlayerStatsService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
            _moderation = GlobalServices.ModerationService;
            _nickname = GlobalServices.NicknameService;
            _captainDraft = GlobalServices.CaptainDraftService;
            _site = GlobalServices.SiteApiService;
        }

        // The lobby used to print a local "(800)" constant that meant nothing.
        // Prefer the site rating for a linked user; show "unrated" when the site
        // says they are not linked; omit the rating entirely if the site is down
        // rather than invent a number.
        private async Task<string> ResolveRatingLabelAsync(ulong discordId)
        {
            var result = await _site.GetPlayerByDiscordAsync(discordId);

            if (result.IsOk && result.Value != null)
                return result.Value.Rating.ToString();

            if (result.IsNotFound)
                return "unrated";

            return "";
        }
        
[Command("join")]
[Command("j")]
public async Task JoinLobby()
{
    var ctx = this.Context;
    var discordId = ctx.Message.Author.Id;
    var username = ctx.Message.Author.Username;
    // !j registers you on the fly; remember whether this is your first time so we only
    // offer the site-profile link once, not on every join.
    bool wasRegistered = _playerRegistry.IsRegistered(discordId);

    if (_moderation.IsBanned(discordId))
    {
        await ctx.Message.ReplyAsync("You are banned and cannot join the lobby.");
        return;
    }

    var existing = _lobbyService.CurrentLobby.Players
        .FirstOrDefault(p => p.DiscordId == discordId);

    if (existing != null)
    {
        // Re-arm the inactivity timer: the AFK warning tells people to "Type !j to stay",
        // so an already-joined !j must actually keep them in, not silently do nothing.
        _lobbyService.KeepAlive(discordId);
        _lobbyService.CurrentLobby.LastActiveChannelId = ctx.Message.ChannelId;
        await ctx.Message.ReplyAsync(
            $"{existing.DisplayName(username)}, you're already in the lobby. Your spot is refreshed, you won't be dropped for inactivity.");
        return;
    }

    var player = _lobbyService.JoinLobby(discordId, username);
    _lobbyService.CurrentLobby.LastActiveChannelId = ctx.Message.ChannelId;

    var savedPrefs = _playerData.GetPreferences(player.DiscordId);
    if (savedPrefs.Count > 0)
        player.FactionPreferences = savedPrefs.ToList();

    string rating = await ResolveRatingLabelAsync(discordId);
    string name = player.DisplayName(username);
    string display = string.IsNullOrEmpty(rating) ? name : $"{name} ({rating})";

    int lobbyCount = _lobbyService.CurrentLobby.Players.Count;
    string lobbyLine = $"\nLobby: {lobbyCount}/16.";

    if (savedPrefs.Count > 0)
    {
        await ctx.Message.ReplyAsync(
            $"Welcome {display}! Your saved preferences are: {string.Join(", ", savedPrefs)}.{lobbyLine}\n" +
            $"To change them: `!prefs Scourge Fel Horde Dalaran` (order = priority). `!bothelp` for the full list."
        );
    }
    else
    {
        await ctx.Message.ReplyAsync(
            $"Welcome {display}! Lobby: {lobbyCount}/16.\n" +
            $"Set your faction preferences: `!prefs Scourge Fel Horde Dalaran` (order = priority). `!bothelp` for the full list."
        );
    }

    if (!wasRegistered)
    {
        var suggestion = await LinkSuggestion.BuildAsync(_site, discordId, name);
        if (suggestion is not null)
            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([suggestion]));
    }

    var lobby = _lobbyService.CurrentLobby;

    if (lobby.IsFull && !lobby.DraftStarted)
    {
        if (lobby.CaptainA != null && lobby.CaptainB != null)
        {
            // Captains might have already set a mode with !mode before the lobby
            // filled. Only default to Captain Draft (Manual Faction) if they never touched it.
            if (lobby.DraftMode == DraftMode.AutoDraft_AutoFaction)
                lobby.DraftMode = DraftMode.CaptainDraft_ManualFaction;

            if (lobby.DraftMode is DraftMode.CaptainDraft_ManualFaction or DraftMode.CaptainDraft_AutoFaction)
            {
                lobby.IsCaptainDraft = true;

                string modeLabel = lobby.DraftMode == DraftMode.CaptainDraft_AutoFaction ? "Auto Faction" : "Manual Faction";
                await ctx.Message.ReplyAsync(
                    $"Two captains detected — switching to **Captain Draft ({modeLabel})**.\n" +
                    "Drafting will take place in the dedicated draft channel."
                );

                _captainDraft.BuildDraftOrder(lobby);
                await _gameService.StartCaptainDraft(lobby, lobby.DraftChannelId);

                return;
            }

            await ctx.Message.ReplyAsync(
                "Two captains detected — switching to **AutoDraft (Manual Faction)**.\n" +
                "Teams will be auto-balanced; captains assign factions afterward."
            );

            await _gameService.StartDraft(lobby, ctx.Message.ChannelId);

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
                await Context.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([EmbedFactory.Info("Lobby", "The lobby is currently empty.")]));
                return;
            }

            string title = lobby.GameNumber > 0
                ? $"Lobby #{lobby.GameNumber} — {lobby.Players.Count}/16"
                : $"Lobby — {lobby.Players.Count}/16";

            var lines = lobby.Players.Select((p, i) => $"`{i + 1,2}.` {p.DisplayName()}");
            var embed = EmbedFactory.Info(title, string.Join("\n", lines));

            await Context.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([embed]));
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
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([EmbedFactory.Warning("Not in lobby", "You are not in the lobby.")]));
                return;
            }

            string display = player.DisplayName(ctx.Message.Author.Username);
            _lobbyService.RemovePlayer(userId);

            int remaining = _lobbyService.CurrentLobby.Players.Count;
            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([EmbedFactory.Neutral("Left lobby", $"{display} has left. ({remaining}/16)")]));
        }

        [Command("prefs")]
        [Command("p")]
        public async Task Preferences([CommandParameter(Remainder = true)] string? argsText = null)
        {
            var args = string.IsNullOrWhiteSpace(argsText)
                ? Array.Empty<string>()
                : argsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

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

            if (sub == "show" || sub == "list" || sub == "help")
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

            if (sub == "all")
            {
                var everything = FactionParser.CanonicalNames.ToList();
                _playerData.SetPreferences(callerId, everything);
                await Context.Message.ReplyAsync(
                    $"You'll play anything. Preferences set to all {everything.Count} factions: {string.Join(", ", everything)}");
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
                await Context.Message.ReplyAsync("Usage: `!prefs add Fel Horde` (one or more factions).");
                return;
            }

            var result = FactionParser.Parse(string.Join(' ', args));
            if (result.Accepted.Count == 0)
            {
                await Context.Message.ReplyAsync(FactionUnknownMessage(result.Unknown));
                return;
            }

            var prefs = _playerData.GetPreferences(userId);
            var added = new List<string>();
            foreach (var faction in result.Accepted)
            {
                if (!prefs.Contains(faction, StringComparer.OrdinalIgnoreCase))
                {
                    prefs.Add(faction);
                    added.Add(faction);
                }
            }

            _playerData.SetPreferences(userId, prefs);

            var reply = added.Count > 0
                ? $"Added: {string.Join(", ", added)}. Your preferences are now: {string.Join(", ", prefs)}"
                : $"Those are already in your preferences: {string.Join(", ", result.Accepted)}";
            if (result.Unknown.Count > 0)
                reply += $"\nIgnored (not a faction): {string.Join(", ", result.Unknown)}. Type `!bothelp` for valid names.";
            await Context.Message.ReplyAsync(reply);
        }

        private async Task RemovePreference(ulong userId, string[] args)
        {
            if (args.Length == 0)
            {
                await Context.Message.ReplyAsync("Usage: `!prefs remove Fel Horde` (one or more factions).");
                return;
            }

            var result = FactionParser.Parse(string.Join(' ', args));
            if (result.Accepted.Count == 0)
            {
                await Context.Message.ReplyAsync(FactionUnknownMessage(result.Unknown));
                return;
            }

            var prefs = _playerData.GetPreferences(userId);
            var removed = result.Accepted
                .Where(faction => prefs.RemoveAll(p => p.Equals(faction, StringComparison.OrdinalIgnoreCase)) > 0)
                .ToList();

            _playerData.SetPreferences(userId, prefs);

            var reply = removed.Count > 0
                ? $"Removed: {string.Join(", ", removed)}. Your preferences are now: {(prefs.Count > 0 ? string.Join(", ", prefs) : "none")}"
                : "None of those were in your preferences.";
            await Context.Message.ReplyAsync(reply);
        }

        private async Task SetPreferencesList(ulong userId, string[] args)
        {
            var result = FactionParser.Parse(string.Join(' ', args));

            if (result.Accepted.Count == 0)
            {
                await Context.Message.ReplyAsync(FactionUnknownMessage(result.Unknown));
                return;
            }

            _playerData.SetPreferences(userId, result.Accepted.ToList());

            var reply = $"Preferences updated to: {string.Join(", ", result.Accepted)}";
            if (result.Unknown.Count > 0)
                reply += $"\nIgnored (not a faction): {string.Join(", ", result.Unknown)}. Type `!bothelp` for valid names.";
            await Context.Message.ReplyAsync(reply);
        }

        private static string FactionUnknownMessage(IReadOnlyList<string> unknown)
        {
            var head = unknown.Count > 0
                ? $"Couldn't match any faction in: {string.Join(", ", unknown)}."
                : "Couldn't find any faction there.";
            return head +
                "\nExample: `!prefs Scourge Fel Horde Dalaran`. Type `!bothelp` for the full list.";
        }

        [Command("bothelp")]
        [Command("h")]
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
