using NetCord.Services.Commands;
using LegaciesBot.Services;
using System.Linq;

namespace LegaciesBot.Discord;

public class GameCommands : CommandModule<CommandContext>
{
    private readonly GameService _gameService;
    private readonly LobbyService _lobbyService;

    public GameCommands(GameService gameService, LobbyService lobbyService)
    {
        _gameService = gameService;
        _lobbyService = lobbyService;
    }
    [Command("games")]
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
            msg += $"Game {game.Id}: Team A ({string.Join(", ", game.TeamA.Players.Select(p => p.Name))}) " +
                   $"vs Team B ({string.Join(", ", game.TeamB.Players.Select(p => p.Name))})\n";
        }

        await ctx.Message.ReplyAsync(msg);
    }

    [Command("score")]
    public async Task SubmitScore(int gameId, int scoreA, int scoreB)
    {
        var ctx = this.Context;

        if (scoreA < 0 || scoreB < 0)
        {
            await ctx.Message.ReplyAsync("Scores must be non-negative integers.");
            return;
        }

        var game = _gameService.GetOngoingGames().FirstOrDefault(g => g.Id == gameId);
        if (game == null)
        {
            await ctx.Message.ReplyAsync($"No ongoing game found with ID {gameId}.");
            return;
        }

        var playerId = ctx.Message.Author.Id;
        bool isParticipant = game.TeamA.Players.Any(p => p.DiscordId == playerId)
                          || game.TeamB.Players.Any(p => p.DiscordId == playerId);

        if (!isParticipant)
        {
            await ctx.Message.ReplyAsync("You are not a participant in this game and cannot submit a score.");
            return;
        }

        if (game.Finished)
        {
            await ctx.Message.ReplyAsync("This game has already been completed.");
            return;
        }

        _gameService.SubmitScore(game, scoreA, scoreB);
        await ctx.Message.ReplyAsync($"Score submitted for Game {game.Id}: Team A {scoreA} - Team B {scoreB}");
    }
}
