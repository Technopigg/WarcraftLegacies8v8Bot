using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;


public class FullMatchFlowTests
{
    private PlayerRegistryService FreshRegistry()
    {
        if (File.Exists("players.json"))
            File.Delete("players.json");

        return new PlayerRegistryService();
    }

    private MatchHistoryService FreshHistory()
    {
        if (File.Exists("match_history.json"))
            File.Delete("match_history.json");

        return new MatchHistoryService();
    }

    private readonly List<(ulong id, string name)> Players = new()
    {
        (1, "Boggywoggy"),
        (2, "Konan"),
        (3, "Dia"),
        (4, "Helsac"),
        (5, "Nick"),
        (6, "Grom"),
        (7, "Linaz"),
        (8, "Theg"),
        (9, "Technopig"),
        (10, "Enclop"),
        (11, "Lukas"),
        (12, "Alan"),
        (13, "Royce"),
        (14, "Petertros"),
        (15, "Dragozer"),
        (16, "Madsen")
    };

    [Fact]
    public void FullMatchFlow_WorksEndToEnd()
    {
        var registry = FreshRegistry();
        var lobbyService = new LobbyService();
        var history = FreshHistory();

        var elo = new EloStub();
        var factionRegistry = new FactionRegistryStub();
        var defaultPrefs = new DefaultPreferencesStub();
        var factionAssignment = new FactionAssignmentStub();

        var gameService = new GameService(
            new DummyGatewayClient(),
            new MatchHistoryAdapter(history),
            elo,
            factionAssignment,
            factionRegistry,
            defaultPrefs
        );

        foreach (var (id, name) in Players)
            registry.RegisterPlayer(id, name);

        foreach (var (id, name) in Players)
            lobbyService.JoinLobby(id, name);

        var lobby = lobbyService.CurrentLobby;

        Assert.Equal(16, lobby.Players.Count);

        var (teamA, teamB) = DraftService.CreateBalancedTeams(lobby.Players);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);

        var game = gameService.StartGame(lobby, teamA, teamB);

        var stats = new PlayerStatsService();

        var changes = gameService.SubmitScore(game, 5, 3, stats);

        Assert.True(game.Finished);
        Assert.NotEmpty(changes);
        Assert.NotEmpty(history.History);
    }
}

public class DummyGatewayClient : IGatewayClient
{
    public Task<ITextChannel?> GetTextChannelAsync(ulong id) =>
        Task.FromResult<ITextChannel?>(null);
}

public class EloStub : IEloService
{
    public Dictionary<ulong, int> ApplyTeamResult(
        List<Player> teamA,
        List<Player> teamB,
        bool teamAWon,
        PlayerStatsService stats)
    {
        return teamA.Concat(teamB)
            .ToDictionary(p => p.DiscordId, p => 5);
    }
}

public class FactionRegistryStub : IFactionRegistry
{
    public IEnumerable<Faction> All => FactionRegistry.All;
}

public class DefaultPreferencesStub : IDefaultPreferences
{
    public List<string> Factions => FactionRegistry.All.Select(f => f.Name).ToList();
}

public class FactionAssignmentStub : IFactionAssignmentService
{
    public void AssignFactionsToTeam(Team team, HashSet<TeamGroup> allowedGroups)
    {
        var factions = FactionRegistry.All
            .Where(f => allowedGroups.Contains(f.Group))
            .Take(team.Players.Count)
            .ToList();

        team.AssignedFactions.Clear();
        team.AssignedFactions.AddRange(factions);
    }
}

public class MatchHistoryAdapter : IMatchHistoryService
{
    private readonly MatchHistoryService _inner;

    public MatchHistoryAdapter(MatchHistoryService inner)
    {
        _inner = inner;
    }

    public void RecordMatch(Game game, int scoreA, int scoreB, Dictionary<ulong, int> eloChanges)
    {
        _inner.RecordMatch(game, scoreA, scoreB, eloChanges);
    }
}