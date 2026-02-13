using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using Moq;

public class DraftServiceTests
{
    private Lobby CreateLobbyWith16Players()
    {
        var lobby = new Lobby();
        for (int i = 0; i < 16; i++)
        {
            lobby.Players.Add(new Player((ulong)i, $"Player{i}") { Elo = 1000 });
        }
        return lobby;
    }

    [Fact]
    public async Task DraftService_FullFlow_16Players_ProducesTwoTeamsOf8()
    {
        var lobby = CreateLobbyWith16Players();

        var gateway = new Mock<IGatewayClient>();
        var channel = new Mock<ITextChannel>();
        gateway.Setup(g => g.GetTextChannelAsync(It.IsAny<ulong>()))
               .ReturnsAsync(channel.Object);

        var history = new Mock<IMatchHistoryService>();
        var elo = new Mock<IEloService>();
        var assign = new Mock<IFactionAssignmentService>();

        var registry = new Mock<IFactionRegistry>();
        registry.Setup(r => r.All).Returns(FactionRegistry.All);

        var prefs = new Mock<IDefaultPreferences>();
        prefs.Setup(p => p.Factions).Returns(FactionRegistry.All.Select(f => f.Name).ToList());

        var service = new GameService(gateway.Object, history.Object, elo.Object, assign.Object, registry.Object, prefs.Object);

        await service.StartDraft(lobby, 123);

        Assert.True(lobby.DraftStarted);

        var teamA = new Team("Team A");
        var teamB = new Team("Team B");

        for (int i = 0; i < 8; i++)
            teamA.AddPlayer(lobby.Players[i]);

        for (int i = 8; i < 16; i++)
            teamB.AddPlayer(lobby.Players[i]);

        var game = service.StartGame(lobby, teamA, teamB);

        Assert.Equal(8, game.TeamA.Players.Count);
        Assert.Equal(8, game.TeamB.Players.Count);

        var allPlayers = game.TeamA.Players.Concat(game.TeamB.Players).ToList();
        Assert.Equal(16, allPlayers.Count);
        Assert.Equal(16, allPlayers.Select(p => p.DiscordId).Distinct().Count());

        channel.Verify(c => c.SendMessageAsync(It.IsAny<string>()), Times.Once);
    }
}