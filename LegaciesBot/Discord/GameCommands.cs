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
            await this.Context.Message.ReplyAsync(
                "Match history is available on the site: **https://warcraftlegacies.com/replays**\n" +
                "Results are recorded automatically when a replay is uploaded.");
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

            await ctx.Message.ReplyAsync($"Game {game.Id} has been terminated with no Elo changes.");
        }

        [Command("forcescore")]
        public async Task ForceScore(params int[] args)
        {
            await this.Context.Message.ReplyAsync(
                "Result recording is handled on the site via replay upload. " +
                "To close a stuck game without recording a result, use `!kill [gameId]`.");
        }

        [Command("score")]
        public async Task ScoreVote(int vote)
        {
            await this.Context.Message.ReplyAsync(
                "Voting is no longer used. Upload the replay to record the result: **https://warcraftlegacies.com/upload**");
        }

        [Command("scores")]
        public async Task ScoreSummary()
        {
            await this.Context.Message.ReplyAsync(
                "Voting is no longer used. Upload the replay to record the result: **https://warcraftlegacies.com/upload**");
        }

        [Command("g")]
        [Command("games")]
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
                string status = game.IsActive ? "In Progress" : "Drafting/Factions";

                string teamA = string.Join(", ",
                    game.TeamA?.Players.Select(p =>
                        $"{p.DisplayName()} [{p.AssignedFaction}]"
                    ) ?? Array.Empty<string>()
                );

                string teamB = string.Join(", ",
                    game.TeamB?.Players.Select(p =>
                        $"{p.DisplayName()} [{p.AssignedFaction}]"
                    ) ?? Array.Empty<string>()
                );

                msg += $"**Game {game.Id}** ({status}) — Team A ({teamA}) vs Team B ({teamB})\n";
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
