using LegaciesBot.Core;
using LTeam = LegaciesBot.Core.Team;

namespace LegaciesBot.Services;

public class GameService
{
    private readonly List<Game> _games = new();

    private readonly IGatewayClient _client;
    private readonly IMatchHistoryService _matchHistoryService;
    private readonly IEloService _eloService;
    private readonly IFactionAssignmentService _factionAssignment;
    private readonly IFactionRegistry _factionRegistry;
    private readonly IDefaultPreferences _defaultPreferences;

    private readonly DraftEngine _draftEngine;

    private int _nextGameId = 1;

    public GameService(
        IGatewayClient client,
        IMatchHistoryService matchHistoryService,
        IEloService eloService,
        IFactionAssignmentService factionAssignment,
        IFactionRegistry factionRegistry,
        IDefaultPreferences defaultPreferences,
        Random? rng = null)
    {
        _client = client;
        _matchHistoryService = matchHistoryService;
        _eloService = eloService;
        _factionAssignment = factionAssignment;
        _factionRegistry = factionRegistry;
        _defaultPreferences = defaultPreferences;

        _draftEngine = new DraftEngine(factionAssignment, rng);
    }

    public async Task StartDraft(Lobby lobby, ulong channelId)
    {
        if (lobby.Players.Count != 16)
            throw new ArgumentException("Draft requires exactly 16 players.");

        foreach (var player in lobby.Players)
        {
            if (!player.FactionPreferences.Any())
                player.FactionPreferences = _defaultPreferences.Factions.ToList();
        }

        var (teamA, teamB) = _draftEngine.RunDraft(lobby.Players);

        lobby.TeamA = teamA;
        lobby.TeamB = teamB;
        lobby.DraftStarted = true;

        var channel = await _client.GetTextChannelAsync(channelId);
        if (channel != null)
        {
            string msg = "=== DRAFT COMPLETE ===\n";
            msg += "Teams have been drafted.\n";
            await channel.SendMessageAsync(msg);
        }
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
        return game;
    }

    public Dictionary<ulong, int> SubmitScore(Game game, int scoreA, int scoreB, PlayerStatsService stats)
    {
        game.ScoreA = scoreA;
        game.ScoreB = scoreB;
        game.Finished = true;

        bool teamAWon = scoreA > scoreB;

        var changes = _eloService.ApplyTeamResult(
            game.TeamA.Players,
            game.TeamB.Players,
            teamAWon,
            stats
        );

        _matchHistoryService.RecordMatch(game, scoreA, scoreB, changes);

        game.Lobby.Players.Clear();
        game.Lobby.DraftStarted = false;

        return changes;
    }

    public List<Game> GetOngoingGames() =>
        _games.Where(g => !g.Finished).ToList();
}