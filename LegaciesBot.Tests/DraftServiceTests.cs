using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.Drafting;
using LegaciesBot.GameData;
using Moq;

namespace LegaciesBot.Tests;

public class DraftServiceTests
{
    private Lobby CreateLobbyWith16Players()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 0; i < 16; i++)
        {
            ulong id = (ulong)i;

            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i}";
            p.Elo = 1000;

            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public async Task DraftService_FullFlow_16Players_ProducesTwoValidTeams()
    {
        var lobby = CreateLobbyWith16Players();

        var gateway = new Mock<IGatewayClient>();
        var channel = new Mock<ITextChannel>();
        gateway.Setup(g => g.GetTextChannelAsync(It.IsAny<ulong>()))
               .ReturnsAsync(channel.Object);

        var history = new Mock<IMatchHistoryService>();
        var elo = new Mock<IEloService>();

        var assign = new RealFactionAssignmentService(new FactionRegistryStub());

        var registry = new Mock<IFactionRegistry>();
        registry.Setup(r => r.All).Returns(FactionRegistry.All);

        var prefs = new Mock<IDefaultPreferences>();
        prefs.Setup(p => p.Factions).Returns(FactionRegistry.All.Select(f => f.Name).ToList());

        var service = new GameService(
            gateway.Object,
            history.Object,
            elo.Object,
            assign,
            registry.Object,
            prefs.Object
        );

        await service.StartDraft(lobby, 123);

        Assert.True(lobby.DraftStarted);
        Assert.NotNull(lobby.TeamA);
        Assert.NotNull(lobby.TeamB);

        Assert.Equal(8, lobby.TeamA!.Players.Count);
        Assert.Equal(8, lobby.TeamB!.Players.Count);

        var game = service.StartGame(lobby, lobby.TeamA!, lobby.TeamB!);

        Assert.NotNull(game.TeamA);
        Assert.NotNull(game.TeamB);

        Assert.Equal(8, game.TeamA.Players.Count);
        Assert.Equal(8, game.TeamB.Players.Count);

        var allPlayers = game.TeamA.Players.Concat(game.TeamB.Players).ToList();
        Assert.Equal(16, allPlayers.Count);
        Assert.Equal(16, allPlayers.Select(p => p.DiscordId).Distinct().Count());

        channel.Verify(c => c.SendMessageAsync(It.IsAny<string>()), Times.Once);
    }
}
