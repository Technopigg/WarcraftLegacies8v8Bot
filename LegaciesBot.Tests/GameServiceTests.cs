using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using Moq;
using Xunit;

public class GameServiceTests
{
    private Lobby CreateLobbyWithPlayers(int count)
    {
        var lobby = new Lobby();
        for (int i = 0; i < count; i++)
        {
            lobby.Players.Add(new Player((ulong)i, $"P{i}") { Elo = 1000 });
        }
        return lobby;
    }

    [Fact]
    public void StartGame_CreatesGameAndSetsDraftStarted()
    {
        var gateway = new Mock<IGatewayClient>();
        var history = new Mock<IMatchHistoryService>();
        var elo = new Mock<IEloService>();
        var assign = new Mock<IFactionAssignmentService>();
        var registry = new Mock<IFactionRegistry>();
        var prefs = new Mock<IDefaultPreferences>();

        var service = new GameService(gateway.Object, history.Object, elo.Object, assign.Object, registry.Object, prefs.Object);

        var lobby = new Lobby();
        var teamA = new Team("Team A");
        var teamB = new Team("Team B");

        var game = service.StartGame(lobby, teamA, teamB);

        Assert.Equal(1, game.Id);
        Assert.True(lobby.DraftStarted);
        Assert.Equal(teamA, game.TeamA);
        Assert.Equal(teamB, game.TeamB);
    }

    [Fact]
    public async Task StartDraft_AssignsDefaultPreferences_WhenMissing()
    {
        var lobby = CreateLobbyWithPlayers(4);
        lobby.Players[0].FactionPreferences.Clear();

        var gateway = new Mock<IGatewayClient>();
        var channel = new Mock<ITextChannel>();
        gateway.Setup(g => g.GetTextChannelAsync(It.IsAny<ulong>()))
               .ReturnsAsync(channel.Object);

        var history = new Mock<IMatchHistoryService>();
        var elo = new Mock<IEloService>();
        var assign = new Mock<IFactionAssignmentService>();

        var registry = new Mock<IFactionRegistry>();
        registry.Setup(r => r.All).Returns(new List<Faction>
        {
            new Faction("Lordaeron", TeamGroup.NorthAlliance),
            new Faction("Scourge", TeamGroup.BurningLegion),
            new Faction("Stormwind", TeamGroup.SouthAlliance),
            new Faction("Warsong", TeamGroup.Kalimdor)
        });

        var prefs = new Mock<IDefaultPreferences>();
        prefs.Setup(p => p.Factions).Returns(new List<string>
        {
            "Lordaeron",
            "Scourge",
            "Stormwind",
            "Warsong"
        });

        var service = new GameService(gateway.Object, history.Object, elo.Object, assign.Object, registry.Object, prefs.Object);

        await service.StartDraft(lobby, 123);

        Assert.Equal(4, lobby.Players[0].FactionPreferences.Count);
        channel.Verify(c => c.SendMessageAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void SubmitScore_UpdatesGameAndCallsDependencies()
    {
        var gateway = new Mock<IGatewayClient>();
        var history = new Mock<IMatchHistoryService>();
        var elo = new Mock<IEloService>();
        var assign = new Mock<IFactionAssignmentService>();
        var registry = new Mock<IFactionRegistry>();
        var prefs = new Mock<IDefaultPreferences>();

        var service = new GameService(gateway.Object, history.Object, elo.Object, assign.Object, registry.Object, prefs.Object);

        var lobby = CreateLobbyWithPlayers(4);
        var teamA = new Team("Team A");
        var teamB = new Team("Team B");

        teamA.AddPlayer(lobby.Players[0]);
        teamB.AddPlayer(lobby.Players[1]);

        var game = service.StartGame(lobby, teamA, teamB);

        elo.Setup(e => e.ApplyTeamResult(
            It.IsAny<List<Player>>(),
            It.IsAny<List<Player>>(),
            true,
            It.IsAny<PlayerStatsService>()))
            .Returns(new Dictionary<ulong, int> { { 0, 10 }, { 1, -10 } });

        var stats = new PlayerStatsService();

        var result = service.SubmitScore(game, 5, 3, stats);

        Assert.True(game.Finished);
        Assert.Empty(lobby.Players);
        Assert.False(lobby.DraftStarted);
        Assert.Equal(10, result[0]);
        Assert.Equal(-10, result[1]);

        history.Verify(h => h.RecordMatch(game, 5, 3, result), Times.Once);
    }

    [Fact]
    public void GetOngoingGames_ReturnsOnlyUnfinished()
    {
        var gateway = new Mock<IGatewayClient>();
        var history = new Mock<IMatchHistoryService>();
        var elo = new Mock<IEloService>();
        var assign = new Mock<IFactionAssignmentService>();
        var registry = new Mock<IFactionRegistry>();
        var prefs = new Mock<IDefaultPreferences>();

        var service = new GameService(gateway.Object, history.Object, elo.Object, assign.Object, registry.Object, prefs.Object);

        var lobby = new Lobby();
        var teamA = new Team("Team A");
        var teamB = new Team("Team B");

        var g1 = service.StartGame(lobby, teamA, teamB);
        var g2 = service.StartGame(lobby, teamA, teamB);

        g1.Finished = true;

        var ongoing = service.GetOngoingGames();

        Assert.Single(ongoing);
        Assert.Equal(g2, ongoing[0]);
    }
}