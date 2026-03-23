using LegaciesBot.Core;
using LegaciesBot.Services;
using Moq;

namespace LegaciesBot.Tests;

public class AutoDraftAutoFactionTests
{
    private List<Player> CreatePlayers(int count)
    {
        var registry = new PlayerRegistryService(null);
        var list = new List<Player>();

        for (int i = 0; i < count; i++)
        {
            ulong id = (ulong)(i + 1);
            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            list.Add(p);
        }

        return list;
    }

    [Fact]
    public void AutoDraftAutoFaction_CreatesBalancedTeams_AndAssignsFactions()
    {
        var players = CreatePlayers(16);

        var lobby = new Lobby();
        lobby.Players.AddRange(players);
        lobby.DraftMode = DraftMode.AutoDraft_AutoFaction;

        var factionAssign = new Mock<IFactionAssignmentService>();
        factionAssign.Setup(a => a.AssignFactionsForGame(
            It.IsAny<Team>(),
            It.IsAny<Team>(),
            It.IsAny<HashSet<TeamGroup>>(),
            It.IsAny<HashSet<TeamGroup>>()
        ));

        var rng = new Random(12345);
        var engine = new DraftEngine(factionAssign.Object, rng);

        var (teamA, teamB) = engine.RunDraft(lobby);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);

        factionAssign.Verify(a => a.AssignFactionsForGame(
            It.IsAny<Team>(),
            It.IsAny<Team>(),
            It.IsAny<HashSet<TeamGroup>>(),
            It.IsAny<HashSet<TeamGroup>>()
        ), Times.Once);
    }
}