using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Core;

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

        public GameCommands()
        {
            _gameService = GlobalServices.GameService;
            _lobbyService = GlobalServices.LobbyService;
            _playerDataService = GlobalServices.PlayerDataService;
            _stats = GlobalServices.PlayerStatsService;
            _permissions = GlobalServices.PermissionService;
            _matchHistoryService = GlobalServices.MatchHistoryService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
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

            await ctx.Message.ReplyAsync(
                $"Registration complete. Welcome, **{player.DisplayName()}**! Your starting Elo is **{player.Elo}**.");
        }

        [Command("recent")]
        public async Task RecentMatches()
        {
            var ctx = this.Context;
            var history = _matchHistoryService.History;

            if (history.Count == 0)
            {
                await ctx.Message.ReplyAsync("No matches have been recorded yet.");
                return;
            }

            var lastMatches = history.OrderByDescending(m => m.Timestamp).Take(5).ToList();
            string msg = "=== RECENT MATCHES ===\n\n";

            foreach (var match in lastMatches)
            {
                bool teamAWon = match.ScoreA > match.ScoreB;
                bool draw = match.ScoreA == 0 && match.ScoreB == 0;

                string result = draw ? "Draw" : teamAWon ? "Team A Win" : "Team B Win";

                msg += $"**Game {match.GameId}** — {result}\n";
                msg += $"Score: **{match.ScoreA} - {match.ScoreB}**\n";

                msg += "Team A Elo: ";
                msg += string.Join(", ", match.TeamA.Select(p =>
                {
                    string sign = p.EloChange >= 0 ? "+" : "";
                    return $"{p.DisplayName} ({sign}{p.EloChange})";
                }));

                msg += "\nTeam B Elo: ";
                msg += string.Join(", ", match.TeamB.Select(p =>
                {
                    string sign = p.EloChange >= 0 ? "+" : "";
                    return $"{p.DisplayName} ({sign}{p.EloChange})";
                }));

                msg += "\n\n";
            }

            await ctx.Message.ReplyAsync(msg);
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

            game.Lobby.Players.Clear();
            game.Lobby.DraftStarted = false;
            game.Finished = true;

            await ctx.Message.ReplyAsync($"Game {game.Id} has been terminated with no Elo changes.");
        }

        [Command("forcescore")]
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

            string msg = $"**Game {game.Id}**\n\n";
            msg += $"{resultText}\n\n";
            msg += $"**Final Score:** Team A {scoreA} — Team B {scoreB}\n\n";
            msg += "**Elo changes:**\n\n";

            msg += "**Team A:**\n";
            foreach (var p in game.TeamA.Players)
            {
                int delta = changes[p.DiscordId];
                var stats = _stats.GetOrCreate(p.DiscordId);
                int oldElo = stats.Elo - delta;
                string sign = delta >= 0 ? "+" : "";
                msg += $"{p.DisplayName()} ({oldElo}) {sign}{delta}\n";
            }

            msg += "\n**Team B:**\n";
            foreach (var p in game.TeamB.Players)
            {
                int delta = changes[p.DiscordId];
                var stats = _stats.GetOrCreate(p.DiscordId);
                int oldElo = stats.Elo - delta;
                string sign = delta >= 0 ? "+" : "";
                msg += $"{p.DisplayName()} ({oldElo}) {sign}{delta}\n";
            }

            await ctx.Message.ReplyAsync(msg);
        }

        [Command("score")]
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

        [Command("scores")]
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
        public async Task ListGames()
        {
            var ctx = this.Context;
            var games = _gameService.GetOngoingGames();

            if (!games.Any())
            {
                await ctx.Message.ReplyAsync("There are no ongoing games.");
                return;
            }

            string msg = "=== ONGOING GAMES ===\n";
            foreach (var game in games)
            {
                msg +=
                    $"**Game {game.Id}** — " +
                    $"Team A ({string.Join(", ", game.TeamA.Players.Select(p => p.DisplayName()))}) " +
                    $"vs Team B ({string.Join(", ", game.TeamB.Players.Select(p => p.DisplayName()))})\n";
            }

            await ctx.Message.ReplyAsync(msg);
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
