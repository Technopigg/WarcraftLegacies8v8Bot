using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using Moq;

public class ManualFactionAssignmentAutoStartTests
{
    private Lobby CreateLobby(PlayerRegistryService registry)
    {
        var lobby = new Lobby();
        lobby.DraftMode = DraftMode.CaptainDraft_ManualFaction;
        lobby.IsCaptainDraft = true;

        for (int i = 1; i <= 16; i++)
        {
            var p = registry.GetOrCreate((ulong)i);
            p.Name = $"P{i}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        lobby.CaptainA = 1;
        lobby.CaptainB = 2;

        lobby.TeamAPicks = Enumerable.Range(1, 8).Select(i => (ulong)i).ToList();
        lobby.TeamBPicks = Enumerable.Range(9, 8).Select(i => (ulong)i).ToList();

        return lobby;
    }

    [Fact]
    public void ManualFactionAssignment_AutoStartsGame_WhenBothTeamsLocked()
    {
        var registry = new PlayerRegistryService(null);

        var factionRegistry = new Mock<IFactionRegistry>();
        factionRegistry.Setup(r => r.All).Returns(FactionRegistry.All);

        var nickname = new NicknameService(registry);

        var gameService = new GameService(
            new DummyGatewayClient(),
            new MatchHistoryAdapter(new MatchHistoryService()),
            new EloStub(),
            new FactionAssignmentStub(),
            new FactionRegistryStub(),
            new DefaultPreferencesStub(),
            new Random(12345)
        );

        var service = new FactionManualAssignmentService(
            factionRegistry.Object,
            nickname,
            gameService
        );

        var lobby = CreateLobby(registry);

        lobby.TeamA = new Team("Team A");
        foreach (var id in lobby.TeamAPicks)
            lobby.TeamA.AddPlayer(registry.GetPlayer(id));

        lobby.TeamB = new Team("Team B");
        foreach (var id in lobby.TeamBPicks)
            lobby.TeamB.AddPlayer(registry.GetPlayer(id));

        var aFactions = new[] { "sw", "dala", "sc", "fel", "sents", "if", "leg", "quel" };
        int ai = 0;
        foreach (var id in lobby.TeamAPicks)
        {
            service.TryAssignSingle(lobby, lobby.CaptainA!.Value, id.ToString(), aFactions[ai]);
            ai++;
        }

        var bFactions = new[] { "aq", "exo", "ws", "fw", "sun", "dru", "kt", "be" };
        int bi = 0;
        foreach (var id in lobby.TeamBPicks)
        {
            service.TryAssignSingle(lobby, lobby.CaptainB!.Value, id.ToString(), bFactions[bi]);
            bi++;
        }

        var resultA = service.TryLockFactions(lobby, lobby.CaptainA!.Value, out _);
        var resultB = service.TryLockFactions(lobby, lobby.CaptainB!.Value, out _);

        Assert.True(resultA);
        Assert.True(resultB);

        Assert.NotNull(lobby.TeamA);
        Assert.NotNull(lobby.TeamB);

        Assert.True(lobby.TeamA.Players.All(p => !string.IsNullOrWhiteSpace(p.AssignedFaction)));
        Assert.True(lobby.TeamB.Players.All(p => !string.IsNullOrWhiteSpace(p.AssignedFaction)));
    }
}
