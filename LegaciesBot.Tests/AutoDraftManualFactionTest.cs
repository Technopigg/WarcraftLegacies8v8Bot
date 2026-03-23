using LegaciesBot.Core;
using LegaciesBot.Services;
using Moq;

namespace LegaciesBot.Tests;

public class AutoDraftManualFactionTests
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
    public void AutoDraftManualFaction_CreatesBalancedTeams_WithoutAssigningFactions()
    {
        var players = CreatePlayers(16);

        var lobby = new Lobby();
        lobby.Players.AddRange(players);
        lobby.DraftMode = DraftMode.AutoDraft_ManualFaction;

        var factionAssign = new Mock<IFactionAssignmentService>();

        var rng = new Random(12345);
        var engine = new DraftEngine(factionAssign.Object, rng);

        var (teamA, teamB) = engine.RunDraft(lobby);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);

        // No faction assignment should occur
        factionAssign.Verify(a => a.AssignFactionsForGame(
            It.IsAny<Team>(),
            It.IsAny<Team>(),
            It.IsAny<HashSet<TeamGroup>>(),
            It.IsAny<HashSet<TeamGroup>>()
        ), Times.Never);
    }
}