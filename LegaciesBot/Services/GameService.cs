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

    private int _nextGameId = 1;

    public GameService(
        IGatewayClient client,
        IMatchHistoryService matchHistoryService,
        IEloService eloService,
        IFactionAssignmentService factionAssignment,
        IFactionRegistry factionRegistry,
        IDefaultPreferences defaultPreferences)
    {
        _client = client;
        _matchHistoryService = matchHistoryService;
        _eloService = eloService;
        _factionAssignment = factionAssignment;
        _factionRegistry = factionRegistry;
        _defaultPreferences = defaultPreferences;
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
                player.FactionPreferences = _defaultPreferences.Factions.ToList();
        }

        var (teamA, teamB) = DraftService.CreateBalancedTeams(lobby.Players);
        var teams = new[] { teamA, teamB };
        
        foreach (var team in teams)
        {
            var allowedGroups = team.Players
                .SelectMany(p => p.FactionPreferences)
                .Select(f => _factionRegistry.All.FirstOrDefault(x => x.Name == f)?.Group)
                .Where(g => g != null)
                .Select(g => g!.Value)
                .ToHashSet<TeamGroup>();


            _factionAssignment.AssignFactionsToTeam(team, allowedGroups);
        }

        // Send draft summary to Discord
        var channel = await _client.GetTextChannelAsync(channelId);
        if (channel != null)
        {
            string msg = "=== DRAFT COMPLETE ===\n";
            int teamNum = 1;

            foreach (var team in teams)
            {
                msg += $"=== TEAM {teamNum} ===\n";
                foreach (var player in team.Players)
                {
                    var faction = team.AssignedFactions
                        .FirstOrDefault(f => player.FactionPreferences.Contains(f.Name));

                    msg += $"{player.Name} ({player.Elo}) -> {faction?.Name ?? "No Faction"}\n";
                }
                teamNum++;
            }

            var game = StartGame(lobby, teams[0], teams[1]);

            msg += $"\n=== GAME STARTED ===\n";
            msg += $"Game ID: **{game.Id}**\n";
            msg += $"Use `!score <A> <B>` to submit the final score.\n";
            msg += $"If multiple games are active, use `!score {game.Id} <A> <B>`.";

            await channel.SendMessageAsync(msg);
        }

        lobby.DraftStarted = true;
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