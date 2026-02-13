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
        (8, "TheG"),
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
        gameService.StartDraft(lobby, 123).Wait();

        Assert.NotNull(lobby.TeamA);
        Assert.NotNull(lobby.TeamB);
        Assert.Equal(8, lobby.TeamA!.Players.Count);
        Assert.Equal(8, lobby.TeamB!.Players.Count);
        
        var game = gameService.StartGame(lobby, lobby.TeamA!, lobby.TeamB!);
        var stats = new PlayerStatsService();
        var changes = gameService.SubmitScore(game, 5, 3, stats);

        Assert.True(game.Finished);
        Assert.NotEmpty(changes);
        Assert.NotEmpty(history.History);
    }
}