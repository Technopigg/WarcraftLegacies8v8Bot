using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using Moq;

namespace LegaciesBot.Tests;

public class FactionManualAssignmentTests
{
    private Lobby CreateLobbyWithCaptains()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);
        for (int i = 0; i < 16; i++)
        {
            ulong id = (ulong)(i + 1);
            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }
        lobby.DraftMode = DraftMode.CaptainDraft_ManualFaction;
        
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

        return new FactionManualAssignmentService(factionRegistry.Object, nickname);
    }

    [Fact]
    public void TryAssignSingle_AssignsFaction_WhenValid()
    {
        var lobby = CreateLobbyWithCaptains();
        var registry = new PlayerRegistryService(null);
        var service = CreateService(registry);

        var result = service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "Player3", "sw");

        Assert.True(result);
        Assert.Equal("Stormwind", lobby.ManualFactionAssignments[3]);
    }

    [Fact]
    public void TryAssignSingle_Fails_WhenCaptainAssignsToWrongTeam()
    {
        var lobby = CreateLobbyWithCaptains();
        var registry = new PlayerRegistryService(null);
        var service = CreateService(registry);

        var result = service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "Player10", "sw");

        Assert.False(result);
    }

    [Fact]
    public void TryAssignSingle_Fails_WhenFactionAlreadyUsed()
    {
        var lobby = CreateLobbyWithCaptains();
        var registry = new PlayerRegistryService(null);
        var service = CreateService(registry);

        service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "Player1", "sw");
        var result = service.TryAssignSingle(lobby, lobby.CaptainA!.Value, "Player2", "sw");

        Assert.False(result);
    }

    [Fact]
    public void AssignBulk_AssignsMultipleFactions()
    {
        var lobby = CreateLobbyWithCaptains();
        var registry = new PlayerRegistryService(null);
        var service = CreateService(registry);

        string bulk = """
        Player1 sw
        Player2 dal
        Player3 sc
        Player4 fh
        """;

        var errors = service.AssignBulk(lobby, lobby.CaptainA!.Value, bulk);

        Assert.Empty(errors);
        Assert.Equal("Stormwind", lobby.ManualFactionAssignments[1]);
        Assert.Equal("Dalaran", lobby.ManualFactionAssignments[2]);
        Assert.Equal("Scourge", lobby.ManualFactionAssignments[3]);
        Assert.Equal("Fel Horde", lobby.ManualFactionAssignments[4]);
    }

    [Fact]
    public void TryFinalize_ReturnsFalse_WhenNotAllAssigned()
    {
        var lobby = CreateLobbyWithCaptains();
        var registry = new PlayerRegistryService(null);
        var service = CreateService(registry);

        var result = service.TryFinalize(lobby);

        Assert.False(result);
    }
}