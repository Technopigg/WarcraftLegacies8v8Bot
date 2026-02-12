using LegaciesBot.Core;
using LTeam = LegaciesBot.Core.Team;
using LegaciesBot.GameData;
using NetCord.Gateway;

namespace LegaciesBot.Services;

public class GameService
{
    private readonly List<Game> _games = new();
    private readonly GatewayClient _client;
    private int _nextGameId = 1; 

    public GameService(GatewayClient client)
    {
        _client = client;
    }

    public Game StartGame(Lobby lobby, LTeam teamA, LTeam teamB)
    {
        var game = new Game
        {
            Id = _nextGameId++, 
            Lobby = lobby,
            TeamA = teamA,
            TeamB = teamB
        };
        _games.Add(game);
        lobby.DraftStarted = true;
        return game;
    }

    public async Task StartDraft(Lobby lobby, ulong channelId)
    {
        foreach (var player in lobby.Players)
        {
            if (!player.FactionPreferences.Any())
                player.FactionPreferences = DefaultPreferences.Factions.ToList();
        }

        var draftTeamsTuple = DraftService.CreateBalancedTeams(lobby.Players);
        var teams = new LTeam[] { draftTeamsTuple.Item1, draftTeamsTuple.Item2 };

        foreach (var team in teams)
        {
            var allowedGroups = team.Players
                .SelectMany(p => p.FactionPreferences)
                .Select(f => FactionRegistry.All.FirstOrDefault(x => x.Name == f)?.Group)
                .Where(g => g != null)
                .Select(g => g.Value)
                .ToHashSet();

            FactionAssignmentService.AssignFactionsToTeam(team, allowedGroups);
        }

        var channelModel = await _client.Rest.GetChannelAsync(channelId);
        if (channelModel is NetCord.TextChannel textChannel)
        {
            string msg = "=== DRAFT COMPLETE ===\n";
            int teamNum = 1;
            foreach (var team in teams)
            {
                msg += $"=== TEAM {teamNum} ===\n";
                foreach (var player in team.Players)
                {
                    var faction = team.AssignedFactions.FirstOrDefault(f => player.FactionPreferences.Contains(f.Name));
                    msg += $"{player.Name} ({player.Elo}) -> {faction?.Name ?? "No Faction"}\n";
                }
                teamNum++;
            }

            await textChannel.SendMessageAsync(msg);
        }

        lobby.DraftStarted = true;
    }

    public void SubmitScore(Game game, int scoreA, int scoreB)
    {
        game.ScoreA = scoreA;
        game.ScoreB = scoreB;
        game.Finished = true;
    }

    public List<Game> GetOngoingGames() => _games.Where(g => !g.Finished).ToList();
}
