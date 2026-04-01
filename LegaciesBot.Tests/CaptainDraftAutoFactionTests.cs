using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.Drafting;
using Moq;

namespace LegaciesBot.Tests;

public class CaptainDraftAutoFactionTests
{
    private Lobby CreateCaptainDraftLobby()
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

        // Pre‑selected picks for captain draft
        lobby.TeamAPicks.AddRange(Enumerable.Range(1, 8).Select(i => (ulong)i));
        lobby.TeamBPicks.AddRange(Enumerable.Range(9, 8).Select(i => (ulong)i));

        lobby.DraftMode = DraftMode.CaptainDraft_AutoFaction;

        return lobby;
    }

    [Fact]
    public void CaptainDraftAutoFaction_AssignsTeams_AndFactions()
    {
        var lobby = CreateCaptainDraftLobby();

        var factionAssign = new Mock<IFactionAssignmentService>();

        factionAssign.Setup(a => a.AssignFactionsForGame(
            It.IsAny<Team>(),
            It.IsAny<Team>(),
            It.IsAny<HashSet<TeamGroup>>(),
            It.IsAny<Random>()
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
            It.IsAny<Random>()
        ), Times.Once);
    }
}