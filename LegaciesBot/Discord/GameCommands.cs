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

        }
        [Command("recent")]
        public async Task RecentMatches()
        {
            var ctx = this.Context;
            var site = GlobalServices.SiteApiService;
            var result = await site.GetRecentMatchesAsync(limit: 5);

            if (result == null || result.Matches.Count == 0)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("No recent matches", "No ranked matches found yet, or the site is unavailable.")]));
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

        [Command("kill")]
        public async Task KillGame(params int[] args)
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

            if (args.Length == 0)
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
                int gameId = args[0];
                game = games.FirstOrDefault(g => g.Id == gameId);
                if (game == null)
                {
                    await ctx.Message.ReplyAsync("No ongoing game found with that ID.");
                    return;
                }
            }

            await _gameService.KillGame(game);

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([EmbedFactory.Warning($"Game #{game.Id} terminated", "No result recorded. Upload the replay to warcraftlegacies.com to register the match.")]));

        }

        // DISABLED (Season 5): result recording is done on the site via replay upload.
        // To re-enable: restore [Command("forcescore")] attribute.
        public async Task ForceScore(params int[] args)
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            var ongoingGames = _gameService.GetOngoingGames();
            if (!ongoingGames.Any())
            {
                await ctx.Message.ReplyAsync("There are no ongoing games.");
                return;
            }

            bool isMod = _permissions.IsMod(userId) || _permissions.IsAdmin(userId);
            bool isCaptain = ongoingGames.Any(g =>
                g.TeamA.CaptainId == userId ||
                g.TeamB.CaptainId == userId);

            if (!isMod && !isCaptain)
            {
                await ctx.Message.ReplyAsync("Only mods or game captains can score the match.");
                return;
            }

            int scoreA, scoreB;
            Game game;

            if (args.Length == 2)
            {
                scoreA = args[0];
                scoreB = args[1];

                if (ongoingGames.Count > 1)
                {
                    await ctx.Message.ReplyAsync("Multiple games active. Use: `!forcescore <gameId> <scoreA> <scoreB>`");
                    return;
                }

                game = ongoingGames.First();
            }
            else if (args.Length == 3)
            {
                int gameId = args[0];
                scoreA = args[1];
                scoreB = args[2];

                game = ongoingGames.FirstOrDefault(g => g.Id == gameId);
                if (game == null)
                {
                    await ctx.Message.ReplyAsync("No ongoing game found with that ID.");
                    return;
                }
            }
            else
            {
                await ctx.Message.ReplyAsync(
                    "Invalid arguments. Use `!forcescore <scoreA> <scoreB>` or `!forcescore <gameId> <scoreA> <scoreB>`");
                return;
            }

            if (!game.IsActive)
            {
                await ctx.Message.ReplyAsync("The game has not started yet. Factions must be locked first.");
                return;
            }

            if (!((scoreA == 0 || scoreA == 1) && (scoreB == 0 || scoreB == 1)))
            {
                await ctx.Message.ReplyAsync("Invalid score. Only 1 0, 0 1, or 0 0 are allowed.");
                return;
            }

            await FinalizeForcedScore(game, scoreA, scoreB);
        }

        private async Task FinalizeForcedScore(Game game, int scoreA, int scoreB)
        {
            var ctx = this.Context;
            var changes = await _gameService.SubmitScore(game, scoreA, scoreB, _stats);

            bool teamAWon = scoreA > scoreB;
            bool draw = scoreA == 0 && scoreB == 0;

            string resultText = draw ? "🤝 **The match ends in a draw!**" :
                teamAWon ? "🏆 **Team A wins!**" :
                "🏆 **Team B wins!**";

            string aLines = string.Join("\n", game.TeamA.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]"));
            string bLines = string.Join("\n", game.TeamB.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]"));
            string desc =
                $"{resultText}\n\n" +
                $"**Final Score:** Team A {scoreA} — Team B {scoreB}\n\n" +
                $"**Team A:**\n{aLines}\n\n" +
                $"**Team B:**\n{bLines}\n\n" +
                $"Upload the replay to [warcraftlegacies.com](https://warcraftlegacies.com) to record the result and update ratings.";

            var embed = scoreA > scoreB || scoreB > scoreA
                ? EmbedFactory.Success($"Game #{game.Id} — Result", desc)
                : EmbedFactory.Info($"Game #{game.Id} — Draw", desc);

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([embed]));
        }

        // DISABLED (Season 5): winner is determined on the site via replay, not by player vote.
        // To re-enable: restore [Command("score")] attribute.
        public async Task ScoreVote(int vote)
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            if (vote != 0 && vote != 1)
            {
                await ctx.Message.ReplyAsync("Use `!score 1` for Team A or `!score 0` for Team B.");
                return;
            }

            var games = _gameService.GetOngoingGames();
            if (!games.Any())
            {
                await ctx.Message.ReplyAsync("There are no ongoing games.");
                return;
            }

            var game = games.FirstOrDefault(g =>
                g.TeamA.Players.Any(p => p.DiscordId == userId) ||
                g.TeamB.Players.Any(p => p.DiscordId == userId));

            if (game == null)
            {
                await ctx.Message.ReplyAsync("You are not a player in this game.");
                return;
            }

            if (!game.IsActive)
            {
                await ctx.Message.ReplyAsync("The game has not started yet. Factions must be locked first.");
                return;
            }

            if (game.ScoreVotes.ContainsKey(userId))
            {
                await ctx.Message.ReplyAsync("You have already voted.");
                return;
            }

            game.ScoreVotes[userId] = vote;

            int votesA = game.ScoreVotes.Values.Count(v => v == 1);
            int votesB = game.ScoreVotes.Values.Count(v => v == 0);
            int required = 6;

            await ctx.Message.ReplyAsync(
                $"Vote recorded for **Game {game.Id}**. Team A: {votesA}/{required}, Team B: {votesB}/{required}");

            if (votesA >= required || votesB >= required)
            {
                int scoreA = votesA >= required ? 1 : 0;
                int scoreB = votesB >= required ? 1 : 0;
                await FinalizeForcedScore(game, scoreA, scoreB);
            }
        }

        // DISABLED (Season 5): see ScoreVote above.
        // To re-enable: restore [Command("scores")] attribute.
        public async Task ScoreSummary()
        {
            var ctx = this.Context;
            ulong userId = ctx.Message.Author.Id;

            var games = _gameService.GetOngoingGames();
            if (!games.Any())
            {
                await ctx.Message.ReplyAsync("There are no ongoing games.");
                return;
            }

            var game = games.FirstOrDefault(g =>
                g.TeamA.Players.Any(p => p.DiscordId == userId) ||
                g.TeamB.Players.Any(p => p.DiscordId == userId));

            if (game == null)
            {
                await ctx.Message.ReplyAsync("You are not a player in this game.");
                return;
            }

            if (!game.IsActive)
            {
                await ctx.Message.ReplyAsync("The game has not started yet. Factions must be locked first.");
                return;
            }

            var votesA = game.ScoreVotes.Where(v => v.Value == 1).Select(v => v.Key).ToList();
            var votesB = game.ScoreVotes.Where(v => v.Value == 0).Select(v => v.Key).ToList();

            var allPlayers = game.TeamA.Players.Concat(game.TeamB.Players).ToList();
            var notVoted = allPlayers.Where(p => !game.ScoreVotes.ContainsKey(p.DiscordId)).ToList();

            string msg = $"**Score Voting Summary — Game {game.Id}**\n\n";

            msg += "**Team A Votes (1):**\n";
            foreach (var id in votesA)
                msg += _playerRegistry.GetOrCreate(id).DisplayName() + "\n";

            msg += "\n**Team B Votes (0):**\n";
            foreach (var id in votesB)
                msg += _playerRegistry.GetOrCreate(id).DisplayName() + "\n";

            msg += "\n**Not Voted:**\n";
            foreach (var p in notVoted)
                msg += p.DisplayName() + "\n";

            await ctx.Message.ReplyAsync(msg);
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

            var mentions = ctx.Message.MentionedUsers;
            ulong? outId = mentions.Count >= 1 ? mentions[0].Id : _nicknames.ResolvePlayerId(outArg);
            ulong? inId  = mentions.Count >= 2 ? mentions[1].Id : _nicknames.ResolvePlayerId(inArg);

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
                _playerRegistry.SetNickname(targetPlayer.DiscordId, adminNickname);
                await ctx.Message.ReplyAsync(
                    $"Nickname for **{targetPlayer.DisplayName()}** has been changed to **{adminNickname}**.");
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
