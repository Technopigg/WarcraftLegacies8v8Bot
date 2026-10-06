using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Core;
using NetCord.Rest;

namespace LegaciesBot.Discord
{
    public class GameCommands : CommandModule<CommandContext>
    {
        private readonly GameService _gameService;
        private readonly LobbyService _lobbyService;
        private readonly PlayerStatsService _stats;
        private readonly PermissionService _permissions;
        private readonly PlayerDataService _playerDataService;
        private readonly MatchHistoryService _matchHistoryService;
        private readonly PlayerRegistryService _playerRegistry;
        private readonly NicknameService _nicknames;

        public GameCommands()
        {
            _gameService = GlobalServices.GameService;
            _lobbyService = GlobalServices.LobbyService;
            _playerDataService = GlobalServices.PlayerDataService;
            _stats = GlobalServices.PlayerStatsService;
            _permissions = GlobalServices.PermissionService;
            _matchHistoryService = GlobalServices.MatchHistoryService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
            _nicknames = GlobalServices.NicknameService;
        }

        [Command("register")]
        [Command("reg")]
        public async Task Register()
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;
            string name = ctx.Message.Author.Username;

            if (_playerRegistry.IsRegistered(userId))
            {
                await ctx.Message.ReplyAsync("You are already registered.");
                return;
            }

            var player = _playerRegistry.RegisterPlayer(userId, name);
            _stats.GetOrCreate(userId);

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Success("Registered", $"Welcome, **{player.DisplayName(name)}**!\nPlay games and upload replays to [warcraftlegacies.com](https://warcraftlegacies.com) to build your ranked rating.")]));

            var suggestion = await LinkSuggestion.BuildAsync(GlobalServices.SiteApiService, userId, name);
            if (suggestion is not null)
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([suggestion]));
        }

        [Command("recent")]
        public async Task RecentMatches()
        {
            var ctx = this.Context;
            var site = GlobalServices.SiteApiService;
            var result = await site.GetRecentMatchesAsync(limit: 5);

            // Same distinction as !leaderboard: "nothing to show yet" is not a fault.
            if (result == null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Recent matches unavailable", "Could not reach warcraftlegacies.com — try again later.")]));
                return;
            }

            if (result.Matches.Count == 0)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Info("No recent matches", "No match has been reviewed yet this season.")]));
                return;
            }

            var lines = new List<string>();
            foreach (var match in result.Matches)
            {
                var winners = match.TeamA.Any(p => p.IsWinner) ? match.TeamA : match.TeamB;
                var losers = match.TeamA.Any(p => p.IsWinner) ? match.TeamB : match.TeamA;

                bool isDraw = !match.TeamA.Any(p => p.IsWinner) && !match.TeamB.Any(p => p.IsWinner);

                static string FormatTeam(IReadOnlyList<RecentMatchParticipant> team)
                {
                    var names = team.Select(p => p.Faction != null ? $"{p.Battletag} [{p.Faction}]" : p.Battletag);
                    return string.Join(", ", names);
                }

                string resultLine = isDraw
                    ? $"Draw — {FormatTeam(match.TeamA)} vs {FormatTeam(match.TeamB)}"
                    : $"**{FormatTeam(winners)}** def. {FormatTeam(losers)}";

                string dateStr = match.PlayedAt != null && DateTime.TryParse(match.PlayedAt, out var dt)
                    ? dt.ToLocalTime().ToString("dd MMM yyyy")
                    : "unknown date";

                lines.Add($"[{dateStr}]({match.MatchUrl}) — {resultLine}");
            }

            string desc = string.Join("\n\n", lines) + $"\n\n[Full match archive](https://warcraftlegacies.com/matches)";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Info("Recent Matches — Discord Pool", desc)]));
        }

        private string FormatAgo(DateTime time)
        {
            var span = DateTime.UtcNow - time;

            if (span.TotalMinutes < 1)
                return "just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hours ago";

            return $"{(int)span.TotalDays} days ago";
        }

        // Records a result in the bot only: ends the game, saves it to the bot's match history
        // and faction win/loss records, and clears roles/lobby. Site rankings still come from the
        // replay upload, since the site has no way to receive a result from the bot yet, so local
        // Elo stays off and nothing is counted twice.
        [Command("forcescore")]
        [Command("fs")]
        public async Task ForceScore(params int[] args)
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            if (!TryParseForceScore(args, out int? gameId, out int scoreA, out int scoreB, out string error))
            {
                await ctx.Message.ReplyAsync(error);
                return;
            }

            var ongoing = _gameService.GetOngoingGames();
            Game? game;
            if (gameId != null)
            {
                game = ongoing.FirstOrDefault(g => g.Id == gameId.Value);
                if (game == null)
                {
                    await ctx.Message.ReplyAsync($"No ongoing game #{gameId}. `!games` lists them.");
                    return;
                }
            }
            else if (ongoing.Count == 1)
            {
                game = ongoing[0];
            }
            else
            {
                await ctx.Message.ReplyAsync(ongoing.Count == 0
                    ? "There are no ongoing games."
                    : "Multiple games active. Use `!forcescore <gameId> <scoreA> <scoreB>`.");
                return;
            }

            bool isMod = _permissions.IsMod(userId);
            bool isCaptain = game.Lobby.CaptainA == userId || game.Lobby.CaptainB == userId;
            if (!isMod && !isCaptain)
            {
                await ctx.Message.ReplyAsync("Only mods or this game's captains can score it.");
                return;
            }

            if (!game.IsActive || game.TeamA == null || game.TeamB == null)
            {
                await ctx.Message.ReplyAsync("That game hasn't started yet (factions must be locked first). Use `!kill` to cancel it instead.");
                return;
            }

            await _gameService.SubmitScore(game, scoreA, scoreB, _stats);

            string result = scoreA == scoreB ? "It's a draw." : scoreA > scoreB ? "**Team A wins!**" : "**Team B wins!**";
            string Roster(Team t) => string.Join("\n", t.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]"));
            string desc =
                $"{result}\n\n**Team A**\n{Roster(game.TeamA)}\n\n**Team B**\n{Roster(game.TeamB)}\n\n" +
                "Recorded in the bot. For site rankings, upload the replay to [warcraftlegacies.com](https://warcraftlegacies.com).";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Success($"Game #{game.Id} scored", desc)]));
        }

        // `!forcescore 1 0` (only game) or `!forcescore 3 1 0` (game #3). 1 0 = Team A won,
        // 0 1 = Team B won, 0 0 = draw. Pure so it can be unit-tested.
        public static bool TryParseForceScore(int[] args, out int? gameId, out int scoreA, out int scoreB, out string error)
        {
            gameId = null;
            scoreA = scoreB = 0;
            error = "Usage: `!forcescore 1 0` (Team A won), `!forcescore 0 1` (Team B won), `!forcescore 0 0` (draw). " +
                    "Add the game number first if several games are running, e.g. `!forcescore 3 1 0`.";

            if (args.Length == 3)
                gameId = args[0];
            else if (args.Length != 2)
                return false;

            scoreA = args[^2];
            scoreB = args[^1];
            if (scoreA is not (0 or 1) || scoreB is not (0 or 1) || (scoreA == 1 && scoreB == 1))
                return false;

            error = "";
            return true;
        }

        [Command("kill")]
        public async Task KillGame(int? gameId = null)
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            if (!_permissions.IsMod(userId))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use this command.");
                return;
            }

            var games = _gameService.GetOngoingGames();
            if (!games.Any())
            {
                await ctx.Message.ReplyAsync("There are no ongoing games.");
                return;
            }

            Game game;

            if (gameId == null)
            {
                if (games.Count > 1)
                {
                    await ctx.Message.ReplyAsync("Multiple games active. Use: `!kill <gameId>`");
                    return;
                }

                game = games.First();
            }
            else
            {
                game = games.FirstOrDefault(g => g.Id == gameId.Value);
                if (game == null)
                {
                    await ctx.Message.ReplyAsync("No ongoing game found with that ID.");
                    return;
                }
            }

            await _gameService.KillGame(game);

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([EmbedFactory.Warning($"Game #{game.Id} terminated", "No result recorded. Upload the replay to warcraftlegacies.com to register the match.")]));

        }


        [Command("g")]
        [Command("games")]
        public async Task ListGames()
        {
            var ctx = this.Context;
            var games = _gameService.GetOngoingGames();

            if (!games.Any())
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([EmbedFactory.Info("Games", "No ongoing games.")]));
                return;
            }

            var fields = new List<EmbedFieldProperties>();
            foreach (var game in games)
            {
                string status = game.IsActive ? "In Progress" : "Drafting / Factions";

                string teamA = game.TeamA?.Players.Any() == true
                    ? string.Join("\n", game.TeamA.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]"))
                    : "—";
                string teamB = game.TeamB?.Players.Any() == true
                    ? string.Join("\n", game.TeamB.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]"))
                    : "—";

                fields.Add(new EmbedFieldProperties().WithName($"Game #{game.Id} — {status}").WithValue($"**Team A**\n{teamA}\n\n**Team B**\n{teamB}").WithInline(false));
            }

            var embed = EmbedFactory.Info($"Ongoing Games ({games.Count})").WithFields(fields);
            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([embed]));
        }

        [Command("sub")]
        public async Task SubstitutePlayer(string outArg, string inArg)
        {
            var ctx = this.Context;
            var callerId = ctx.Message.Author.Id;

            if (!_permissions.IsMod(callerId) && !_permissions.IsAdmin(callerId))
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Permission denied", "Only moderators can substitute players.")]));
                return;
            }

            ulong? outId = _nicknames.ResolvePlayerId(outArg);
            ulong? inId  = _nicknames.ResolvePlayerId(inArg);

            if (!outId.HasValue)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Player not found", $"Could not resolve out-player: `{outArg}`")]));
                return;
            }

            if (!inId.HasValue)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Player not found", $"Could not resolve in-player: `{inArg}`")]));
                return;
            }

            var game = _gameService.FindGameByPlayer(outId.Value);
            if (game == null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("No active game", $"<@{outId.Value}> is not in any ongoing game.")]));
                return;
            }

            var outPlayer = _playerRegistry.GetOrCreate(outId.Value);
            var inPlayer  = _playerRegistry.GetOrCreate(inId.Value);

            if (_gameService.FindGameByPlayer(inId.Value) != null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Already in game", $"{inPlayer.DisplayName()} is already in an ongoing game.")]));
                return;
            }

            var inStats = _stats.GetOrCreate(inId.Value);
            inPlayer.Elo = inStats.Elo;

            var (team, faction) = await _gameService.SubstitutePlayer(game, outPlayer, inPlayer);

            string factionStr = string.IsNullOrEmpty(faction) ? "—" : faction;
            string teamStr = team?.Name ?? "Undrafted";
            string desc = $"**{outPlayer.DisplayName()}** → **{inPlayer.DisplayName()}**\nTeam: **{teamStr}** | Faction: **{factionStr}**";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Success($"Sub — Game #{game.Id}", desc)]));
        }

        [Command("nickname")]
        public async Task NicknameAsync(string targetOrNickname, string? newNickname = null)
        {
            var ctx = this.Context;
            var callerId = ctx.Message.Author.Id;
            bool isAdmin = _permissions.IsMod(callerId) || _permissions.IsAdmin(callerId);

            if (newNickname == null)
            {
                if (!_playerRegistry.IsRegistered(callerId))
                {
                    await ctx.Message.ReplyAsync("You are not registered. Use `!register` first.");
                    return;
                }

                string nickname = targetOrNickname;
                if (!ValidateNickname(nickname, out var error))
                {
                    await ctx.Message.ReplyAsync(error);
                    return;
                }

                try
                {
                    _playerRegistry.SetNickname(callerId, nickname);
                    await ctx.Message.ReplyAsync($"Your nickname has been set to **{nickname}**.");
                }
                catch (InvalidOperationException)
                {
                    await ctx.Message.ReplyAsync("That nickname is already taken by another player.");
                }

                return;
            }

            if (!isAdmin)
            {
                await ctx.Message.ReplyAsync("You do not have permission to change other players' nicknames.");
                return;
            }

            string targetName = targetOrNickname;
            string adminNickname = newNickname!;
            var targetPlayer = _playerRegistry.Resolve(targetName);

            if (targetPlayer == null)
            {
                await ctx.Message.ReplyAsync($"No player found with name, nickname, ID, or mention **{targetName}**.");
                return;
            }

            if (!ValidateNickname(adminNickname, out string adminError))
            {
                await ctx.Message.ReplyAsync(adminError);
                return;
            }

            try
            {
                string previousName = targetPlayer.DisplayName();
                _playerRegistry.SetNickname(targetPlayer.DiscordId, adminNickname);
                await ctx.Message.ReplyAsync(
                    $"Nickname for **{previousName}** has been changed to **{adminNickname}**.");
            }
            catch (InvalidOperationException)
            {
                await ctx.Message.ReplyAsync("That nickname is already taken by another player.");
            }
        }

        private bool ValidateNickname(string nickname, out string error)
        {
            error = "";
            if (nickname.Length < 2 || nickname.Length > 20)
            {
                error = "Nickname must be between 2 and 20 characters.";
                return false;
            }

            if (!nickname.All(char.IsLetterOrDigit))
            {
                error = "Nickname can only contain letters and numbers.";
                return false;
            }

            return true;
        }
    }
}
