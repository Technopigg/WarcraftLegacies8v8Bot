using NetCord.Services.Commands;
using LegaciesBot.Services;

using LegaciesBot.Core;


namespace LegaciesBot.Discord;

public class GameCommands : CommandModule<CommandContext>
{
    private readonly GameService _gameService;
    private readonly LobbyService _lobbyService;
    private readonly PlayerStatsService _stats;
    private readonly PermissionService _permissions;

    public GameCommands(GameService gameService, LobbyService lobbyService, PlayerStatsService stats, PermissionService permissions)
    {
        _gameService = gameService;
        _lobbyService = lobbyService;
        _stats = stats;
        _permissions = permissions;
    }
    [Command("makemod")]
    public async Task MakeMod(ulong targetUserId)
    {
        var ctx = this.Context;
        ulong userId = ctx.Message.Author.Id;

        if (!_permissions.IsAdmin(userId))
        {
            await ctx.Message.ReplyAsync("Only admins can use this command.");
            return;
        }

        _permissions.AddMod(targetUserId);

        await ctx.Message.ReplyAsync($"User <@{targetUserId}> has been added as a **Mod**.");
    }

    [Command("kill")]
    public async Task KillGame()
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
            await ctx.Message.ReplyAsync("There are no ongoing games to kill.");
            return;
        }

        Game game = games.Count == 1 ? games.First() : null;

        if (game == null)
        {
            await ctx.Message.ReplyAsync("Multiple games active. Use: `!kill <gameId>`");
            return;
        }

        game.Lobby.Players.Clear();
        game.Lobby.DraftStarted = false;
        game.Finished = true;

        await ctx.Message.ReplyAsync($"Game {game.Id} has been terminated with no Elo changes.");
    }

    [Command("forcescore")]
    public async Task ForceScore(int scoreA, int scoreB)
    {
        var ctx = this.Context;
        ulong userId = ctx.Message.Author.Id;

        if (!_permissions.IsMod(userId))
        {
            await ctx.Message.ReplyAsync("You do not have permission to use this command.");
            return;
        }

        if (!((scoreA == 0 || scoreA == 1) && (scoreB == 0 || scoreB == 1)))
        {
            await ctx.Message.ReplyAsync("Invalid score. Only 1 0, 0 1, or 0 0 are allowed.");
            return;
        }

        var games = _gameService.GetOngoingGames();
        if (!games.Any())
        {
            await ctx.Message.ReplyAsync("There are no ongoing games.");
            return;
        }

        Game game = games.Count == 1 ? games.First() : null;

        if (game == null)
        {
            await ctx.Message.ReplyAsync("Multiple games active. Use: `!forcescore <gameId> <scoreA> <scoreB>`");
            return;
        }

        var changes = _gameService.SubmitScore(game, scoreA, scoreB, _stats);

        bool teamAWon = scoreA > scoreB;
        bool draw = scoreA == 0 && scoreB == 0;

        string resultText = draw
            ? "🤝 **The match ends in a draw!**"
            : teamAWon ? "🏆 **Team A wins!**" : "🏆 **Team B wins!**";

        string msg = $"{resultText}\n\n";
        msg += $"**Final Score:** Team A {scoreA} — Team B {scoreB}\n\n";
        msg += "**Elo changes:**\n\n";

        msg += "**Team A:**\n";
        foreach (var p in game.TeamA.Players)
        {
            int delta = changes[p.DiscordId];
            string sign = delta >= 0 ? "+" : "";
            msg += $"{p.Name} {sign}{delta}\n";
        }

        msg += "\n**Team B:**\n";
        foreach (var p in game.TeamB.Players)
        {
            int delta = changes[p.DiscordId];
            string sign = delta >= 0 ? "+" : "";
            msg += $"{p.Name} {sign}{delta}\n";
        }

        await ctx.Message.ReplyAsync(msg);
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
public async Task SubmitScore(int scoreA, int scoreB)
{
    var ctx = this.Context;


    if (!((scoreA == 0 || scoreA == 1) && (scoreB == 0 || scoreB == 1)))
    {
        await ctx.Message.ReplyAsync("Invalid score. Only 1 0, 0 1, or 0 0 are allowed.");
        return;
    }


    var games = _gameService.GetOngoingGames();

    if (!games.Any())
    {
        await ctx.Message.ReplyAsync("There are no ongoing games.");
        return;
    }

    Game game;

 
    if (games.Count == 1)
    {
        game = games.First();
    }
    else
    {
        await ctx.Message.ReplyAsync(
            "Multiple games are active. Please specify the game ID: `!score <gameId> <scoreA> <scoreB>`"
        );
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
    
    game.ScoreSubmissions[playerId] = (scoreA, scoreB);
    
    int matchingVotes = game.ScoreSubmissions
        .Count(v => v.Value.scoreA == scoreA && v.Value.scoreB == scoreB);

    int totalPlayers = game.TeamA.Players.Count + game.TeamB.Players.Count;
    int required = (totalPlayers / 2) + 1; 
    if (matchingVotes < required)
    {
        await ctx.Message.ReplyAsync(
            $"Score recorded. {matchingVotes}/{required} votes for {scoreA}-{scoreB}."
        );
        return;
    }

   
    var changes = _gameService.SubmitScore(game, scoreA, scoreB, _stats);

    bool teamAWon = scoreA > scoreB;
    bool teamBWon = scoreB > scoreA;
    bool draw = scoreA == 0 && scoreB == 0;

    string resultText = draw
        ? "🤝 **The match ends in a draw!**"
        : teamAWon ? "🏆 **Team A wins!**" : "🏆 **Team B wins!**";

    string msg = $"{resultText}\n\n";
    msg += $"**Final Score:** Team A {scoreA} — Team B {scoreB}\n\n";
    msg += "**Elo changes:**\n\n";

    msg += "**Team A:**\n";
    foreach (var p in game.TeamA.Players)
    {
        int delta = changes[p.DiscordId];
        string sign = delta >= 0 ? "+" : "";
        msg += $"{p.Name} {sign}{delta}\n";
    }

    msg += "\n**Team B:**\n";
    foreach (var p in game.TeamB.Players)
    {
        int delta = changes[p.DiscordId];
        string sign = delta >= 0 ? "+" : "";
        msg += $"{p.Name} {sign}{delta}\n";
    }

    await ctx.Message.ReplyAsync(msg);
}


}