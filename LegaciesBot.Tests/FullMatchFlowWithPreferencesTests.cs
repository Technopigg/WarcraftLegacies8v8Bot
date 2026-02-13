using LegaciesBot.GameData;
using LegaciesBot.Services;

public class FullMatchFlowWithPreferencesTests
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

    private readonly Dictionary<ulong, List<string>> Prefs;

    public FullMatchFlowWithPreferencesTests()
    {
        var all = FactionRegistry.All.Select(f => f.Name).ToList();

        Prefs = new()
        {
            [1] = new() { "Fel Horde", "An'qiraj", "Stormwind", "Lordaeron", "Druids", "Scourge" },

            [2] = new() { "Warsong", "An'qiraj", "Illidari", "Sentinels", "Scourge", "Fel Horde", "Kul'tiras" },

            [3] = all
                .Where(f => f != "Scourge" && f != "Gilneas" && f != "Sunfury")
                .ToList(),

            [4] = new() { "Lordaeron", "Skywall", "Stormwind" },
            
            [5] = new() { "Dalaran", "Legion", "Druids" },

            [6] = new()
            {
                "Ironforge", "Stormwind", "The Exodar", "Druids", "Lordaeron",
                "Kul'tiras", "Illidari", "Gilneas", "Sentinels", "Black Empire", "Legion"
            },

            [7] = new()
            {
                "Skywall", "Scourge", "An'qiraj", "Sentinels", "The Exodar",
                "Quel'thalas", "Illidari", "Fel Horde", "Dalaran", "Ironforge", "Kul'tiras"
            },

            [8] = new() { "Lordaeron", "Sentinels", "Ironforge" },

            [9] = new() { "Dalaran", "Quel'thalas", "Kul'tiras", "Illidari", "Stormwind" },

            [10] = new() { "Warsong", "Skywall", "Dalaran", "Scourge", "Kul'tiras" },

            [11] = new() { "Gilneas", "Lordaeron", "Quel'thalas", "Frostwolf", "Fel Horde", "Kul'tiras" },

            [12] = new() { "Illidari", "Legion", "Druids", "Quel'thalas", "Lordaeron" },

            [13] = new() { "Fel Horde", "Scourge", "Frostwolf", "Kul'tiras", "Lordaeron" },

            [14] = new() { "Kul'tiras", "Lordaeron", "Stormwind", "The Exodar", "Quel'thalas" },

            [15] = new()
            {
                "Dalaran", "Scourge", "Fel Horde", "Warsong",
                "Sentinels", "Stormwind", "Sunfury"
            },

            [16] = new()
            {
                "Lordaeron", "Stormwind", "Warsong", "Sunfury",
                "Gilneas", "Skywall", "The Exodar"
            }
        };
    }

    [Fact]
    public void FullMatchFlow_WithPreferences_WorksEndToEnd()
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

        foreach (var (id, _) in Players)
            lobbyService.UpdatePreferences(id, Prefs[id]);

        var lobby = lobbyService.CurrentLobby;

        var (teamA, teamB) = DraftService.CreateBalancedTeams(lobby.Players);

        var game = gameService.StartGame(lobby, teamA, teamB);

        var stats = new PlayerStatsService();

        var changes = gameService.SubmitScore(game, 4, 2, stats);
        var teamAInGame = game.TeamA;
        var teamBInGame = game.TeamB;

        for (int i = 0; i < teamAInGame.Players.Count; i++)
        {
            var player = teamAInGame.Players[i];
            var faction = teamAInGame.AssignedFactions[i];

            Assert.Contains(faction.Name, Prefs[player.DiscordId]);
        }

        for (int i = 0; i < teamBInGame.Players.Count; i++)
        {
            var player = teamBInGame.Players[i];
            var faction = teamBInGame.AssignedFactions[i];

            Assert.Contains(faction.Name, Prefs[player.DiscordId]);
        }

        
        Assert.True(game.Finished);
        Assert.NotEmpty(changes);
        Assert.NotEmpty(history.History);
    }
}