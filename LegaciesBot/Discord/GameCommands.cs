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

            for (int i = 0; i < lastMatches.Count; i++)
            {
                var match = lastMatches[i];

                bool teamAWon = match.ScoreA > match.ScoreB;
                bool draw = match.ScoreA == 0 && match.ScoreB == 0;

                string result;
                if (draw)
                    result = "Draw";
                else if (teamAWon)
                    result = "Team A Win";
                else
                    result = "Team B Win";

                msg += "**Game " + match.GameId + "** — " + result + "\n";
                msg += "Score: **" + match.ScoreA + " - " + match.ScoreB + "**\n";


                msg += "Team A Elo: ";
                for (int j = 0; j < match.TeamA.Count; j++)
                {
                    var p = match.TeamA[j];
                    string sign = p.EloChange >= 0 ? "+" : "";
                    msg += p.DisplayName + " (" + sign + p.EloChange + ")";
                    if (j < match.TeamA.Count - 1)
                        msg += ", ";
                }

                msg += "\n";


                msg += "Team B Elo: ";
                for (int j = 0; j < match.TeamB.Count; j++)
                {
                    var p = match.TeamB[j];
                    string sign = p.EloChange >= 0 ? "+" : "";
                    msg += p.DisplayName + " (" + sign + p.EloChange + ")";
                    if (j < match.TeamB.Count - 1)
                        msg += ", ";
                }

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

            if (!_permissions.IsMod(userId))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use this command.");
                return;
            }

            var ongoingGames = _gameService.GetOngoingGames();
            if (!ongoingGames.Any())
            {
                await ctx.Message.ReplyAsync("There are no ongoing games.");
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
                    await ctx.Message.ReplyAsync(
                        "Multiple games active. Use: `!forcescore <gameId> <scoreA> <scoreB>`");
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
            var changes = _gameService.SubmitScore(game, scoreA, scoreB, _stats);

            bool teamAWon = scoreA > scoreB;
            bool draw = scoreA == 0 && scoreB == 0;

            string resultText = draw ? "🤝 **The match ends in a draw!**" :
                teamAWon ? "🏆 **Team A wins!**" :
                "🏆 **Team B wins!**";

            string msg = $"{resultText}\n\n";
            msg += $"**Final Score:** Team A {scoreA} — Team B {scoreB}\n\n";
            msg += "**Elo changes:**\n\n";

            msg += "**Team A:**\n";
            foreach (var p in game.TeamA.Players)
            {
                int delta = changes[p.DiscordId];
                string sign = delta >= 0 ? "+" : "";
                msg += $"{p.DisplayName()} {sign}{delta}\n";
            }

            msg += "\n**Team B:**\n";
            foreach (var p in game.TeamB.Players)
            {
                int delta = changes[p.DiscordId];
                string sign = delta >= 0 ? "+" : "";
                msg += $"{p.DisplayName()} {sign}{delta}\n";
            }

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
                    $"Game {game.Id}: Team A ({string.Join(", ", game.TeamA.Players.Select(p => p.DisplayName()))}) " +
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