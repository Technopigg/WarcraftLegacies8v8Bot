using LegaciesBot.Core;
using LegaciesBot.Services;

public class ManualFactionAutoStartTests
{
    private GameService CreateGameService()
    {
        var rng = new Random(12345);
        var client = new DummyGatewayClient();
        var history = new MatchHistoryAdapter(new MatchHistoryService());
        var elo = new EloStub();
        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
        var defaults = new DefaultPreferencesStub();
        var factionRegistry = new FactionRegistryStub();

        return new GameService(
            client,
            history,
            elo,
            factionAssign,
            factionRegistry,
            defaults,
            rng
        );
    }

    private Lobby CreateLobbyWithTeams()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);
        
        for (int i = 1; i <= 16; i++)
        {
            var p = registry.GetOrCreate((ulong)i);
            p.Name = $"P{i}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }
        var teamA = new Team("Team A");
        var teamB = new Team("Team B");

        for (int i = 0; i < 8; i++)
        {
            teamA.Players.Add(lobby.Players[i]);
            lobby.ManualFactionAssignments[lobby.Players[i].DiscordId] = "A";
        }

        for (int i = 8; i < 16; i++)
        {
            teamB.Players.Add(lobby.Players[i]);
            lobby.ManualFactionAssignments[lobby.Players[i].DiscordId] = "B";
        }

        lobby.TeamA = teamA;
        lobby.TeamB = teamB;

        lobby.TeamAFactionsLocked = true;
        lobby.TeamBFactionsLocked = true;

        return lobby;
    }

    [Fact]
    public void TryAutoStartAfterManualFactions_StartsGame_And_AssignsFactions()
    {
        var service = CreateGameService();
        var lobby = CreateLobbyWithTeams();

        service.TryAutoStartAfterManualFactions(lobby);

        var game = service.GetOngoingGames().Single();

        Assert.True(game.IsActive);
        Assert.NotEqual(default, game.StartedAt);

        Assert.Same(lobby.TeamA, game.TeamA);
        Assert.Same(lobby.TeamB, game.TeamB);

        foreach (var p in lobby.Players)
            Assert.False(string.IsNullOrWhiteSpace(p.AssignedFaction));
    }
}
