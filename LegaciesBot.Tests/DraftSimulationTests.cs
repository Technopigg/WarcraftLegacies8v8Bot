using Xunit;
using Xunit.Abstractions;
using LegaciesBot.Services;
using LegaciesBot.GameData;

public class DraftSimulationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Dictionary<ulong, List<string>> Prefs;

    public DraftSimulationTests(ITestOutputHelper output)
    {
        _output = output;

        var all = FactionRegistry.All.Select(f => f.Name).ToList();

        Prefs = new()
        {
            [1] = new() { "Fel Horde", "An'qiraj", "Stormwind", "Lordaeron", "Druids", "Scourge" },
            [2] = new() { "Warsong", "An'qiraj", "Illidari", "Sentinels", "Scourge", "Fel Horde", "Kul'tiras" },
            [3] = all.Where(f => f != "Scourge" && f != "Gilneas" && f != "Sunfury").ToList(),
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
            [15] = new() { "Dalaran", "Scourge", "Fel Horde", "Warsong", "Sentinels", "Stormwind", "Sunfury" },
            [16] = new() { "Lordaeron", "Stormwind", "Warsong", "Sunfury", "Gilneas", "Skywall", "The Exodar" }
        };
    }

    [Fact]
    public void SimulateTwentyDrafts_LogsOutput()
    {
        var history = new MatchHistoryService();
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

        var players = new List<(ulong id, string name)>
        {
            (1, "Boggywoggy"), (2, "Konan"), (3, "Dia"), (4, "Helsac"),
            (5, "Nick"), (6, "Grom"), (7, "Linaz"), (8, "Theg"),
            (9, "Technopig"), (10, "Enclop"), (11, "Lukas"), (12, "Alan"),
            (13, "Royce"), (14, "Petertros"), (15, "Dragozer"), (16, "Madsen")
        };

        for (int run = 1; run <= 20; run++)
        {
            var registry = new PlayerRegistryService();
            var lobbyService = new LobbyService();

            foreach (var (id, name) in players)
                registry.RegisterPlayer(id, name);

            foreach (var (id, name) in players)
                lobbyService.JoinLobby(id, name);

            foreach (var (id, _) in players)
                lobbyService.UpdatePreferences(id, Prefs[id]);

            var (teamA, teamB) = DraftService.CreateBalancedTeams(lobbyService.CurrentLobby.Players);

            var game = gameService.StartGame(lobbyService.CurrentLobby, teamA, teamB);

            _output.WriteLine($"=== RUN {run} ===");

            _output.WriteLine("TEAM A:");
            for (int i = 0; i < game.TeamA.Players.Count; i++)
            {
                var p = game.TeamA.Players[i];
                var f = game.TeamA.AssignedFactions[i];
                _output.WriteLine($"{p.Name} -> {f.Name}");
            }

            _output.WriteLine("TEAM B:");
            for (int i = 0; i < game.TeamB.Players.Count; i++)
            {
                var p = game.TeamB.Players[i];
                var f = game.TeamB.AssignedFactions[i];
                _output.WriteLine($"{p.Name} -> {f.Name}");
            }

            _output.WriteLine("");
        }
    }
}