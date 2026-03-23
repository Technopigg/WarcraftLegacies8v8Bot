using LegaciesBot.Core;
using LegaciesBot.Commands;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using Moq;

namespace LegaciesBot.Tests;

public class FactionCommandsTests
{
    private Lobby CreateLobby()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        // Create 16 players
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

    private FactionCommands CreateCommands(Lobby lobby, PlayerRegistryService registry)
    {
        var factionRegistry = new Mock<IFactionRegistry>();
        factionRegistry.Setup(r => r.All).Returns(FactionRegistry.All);

        var nickname = new NicknameService(registry);
        var manual = new FactionManualAssignmentService(factionRegistry.Object, nickname);

        var lobbyService = new Mock<LobbyService>();
        lobbyService.Setup(l => l.CurrentLobby).Returns(lobby);

        return new FactionCommands(lobbyService.Object, manual);
    }

    [Fact]
    public void SetFaction_Succeeds_WhenValid()
    {
        var lobby = CreateLobby();
        var registry = new PlayerRegistryService(null);
        var commands = CreateCommands(lobby, registry);

        var result = commands.SetFaction(1, "Player3", "sw");

        Assert.Equal("Faction assigned.", result);
        Assert.Equal("Stormwind", lobby.ManualFactionAssignments[3]);
    }

    [Fact]
    public void SetFaction_Fails_WhenInvalid()
    {
        var lobby = CreateLobby();
        var registry = new PlayerRegistryService(null);
        var commands = CreateCommands(lobby, registry);

        var result = commands.SetFaction(1, "Player10", "sw");

        Assert.Equal("Invalid faction assignment.", result);
    }

    [Fact]
    public void SetFactions_Bulk_Succeeds()
    {
        var lobby = CreateLobby();
        var registry = new PlayerRegistryService(null);
        var commands = CreateCommands(lobby, registry);

        string bulk = """
        Player1 sw
        Player2 dal
        Player3 sc
        """;

        var result = commands.SetFactions(1, bulk);

        Assert.Equal("Bulk assignment complete.", result);
        Assert.Equal("Stormwind", lobby.ManualFactionAssignments[1]);
        Assert.Equal("Dalaran", lobby.ManualFactionAssignments[2]);
        Assert.Equal("Scourge", lobby.ManualFactionAssignments[3]);
    }

    [Fact]
    public void SetFactions_Bulk_ReportsErrors()
    {
        var lobby = CreateLobby();
        var registry = new PlayerRegistryService(null);
        var commands = CreateCommands(lobby, registry);

        string bulk = """
        Player1 sw
        PlayerX dal
        """;

        var result = commands.SetFactions(1, bulk);

        Assert.Contains("Some assignments failed:", result);
        Assert.Contains("Failed: PlayerX dal", result);
    }

    [Fact]
    public void Finalize_Fails_WhenNotAllAssigned()
    {
        var lobby = CreateLobby();
        var registry = new PlayerRegistryService(null);
        var commands = CreateCommands(lobby, registry);

        var result = commands.Finalize(1);

        Assert.Equal("Not all factions have been assigned.", result);
    }
}