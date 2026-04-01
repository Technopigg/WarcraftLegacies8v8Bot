using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using Moq;

namespace LegaciesBot.Tests;

public class FactionManualAssignmentTests
{
    private Lobby CreateLobbyWithCaptains(PlayerRegistryService registry)
    {
        var lobby = new Lobby();

        for (int i = 0; i < 16; i++)
        {
            ulong id = (ulong)(i + 1);
            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        lobby.DraftMode = DraftMode.CaptainDraft_ManualFaction;
        lobby.IsCaptainDraft = true; 

        lobby.CaptainA = 1;
        lobby.CaptainB = 2;

        lobby.TeamAPicks.AddRange(Enumerable.Range(1, 8).Select(i => (ulong)i));
        lobby.TeamBPicks.AddRange(Enumerable.Range(9, 8).Select(i => (ulong)i));

        return lobby;
    }

    private FactionManualAssignmentService CreateService(PlayerRegistryService registry)
    {
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

        return new FactionManualAssignmentService(
            factionRegistry.Object,
            nickname,
            gameService
        );
    }

    [Fact]
    public void TryAssignSingle_AssignsFaction_WhenValid()
    {
        var registry = new PlayerRegistryService(null);
        var lobby = CreateLobbyWithCaptains(registry);
        var service = CreateService(registry);
        var result = service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "3", "sw");

        Assert.True(result);
        Assert.Equal("Stormwind", lobby.ManualFactionAssignments[3]);
    }

    [Fact]
    public void TryAssignSingle_Fails_WhenCaptainAssignsToWrongTeam()
    {
        var registry = new PlayerRegistryService(null);
        var lobby = CreateLobbyWithCaptains(registry);
        var service = CreateService(registry);
        
        var result = service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "10", "sw");

        Assert.False(result);
    }

    [Fact]
    public void TryAssignSingle_Fails_WhenFactionAlreadyUsed()
    {
        var registry = new PlayerRegistryService(null);
        var lobby = CreateLobbyWithCaptains(registry);
        var service = CreateService(registry);

        service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "1", "sw");
        var result = service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "2", "sw");

        Assert.False(result);
    }

    [Fact]
    public void AssignBulk_AssignsMultipleFactions()
    {
        var registry = new PlayerRegistryService(null);
        var lobby = CreateLobbyWithCaptains(registry);
        var service = CreateService(registry);
        
        string bulk = """
        1 sw
        2 dal
        3 sc
        4 fh
        """;

        var errors = service.AssignBulk(lobby, lobby.CaptainA!.Value, bulk);

        Assert.Empty(errors);
        Assert.Equal("Stormwind", lobby.ManualFactionAssignments[1]);
        Assert.Equal("Dalaran", lobby.ManualFactionAssignments[2]);
        Assert.Equal("Scourge", lobby.ManualFactionAssignments[3]);
        Assert.Equal("Fel Horde", lobby.ManualFactionAssignments[4]);
    }

    [Fact]
    public void TryLockFactions_ReturnsFalse_WhenNotAllAssigned()
    {
        var registry = new PlayerRegistryService(null);
        var lobby = CreateLobbyWithCaptains(registry);
        var service = CreateService(registry);

        var result = service.TryLockFactions(lobby, lobby.CaptainA!.Value, out var message);

        Assert.False(result);
        Assert.Contains("assign all 8 factions", message);
    }
}
