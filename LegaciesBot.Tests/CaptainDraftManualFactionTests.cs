using LegaciesBot.Core;
using LegaciesBot.Services;
using Moq;

namespace LegaciesBot.Tests;

public class CaptainDraftManualFactionTests
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

        lobby.TeamAPicks.AddRange(Enumerable.Range(1, 8).Select(i => (ulong)i));
        lobby.TeamBPicks.AddRange(Enumerable.Range(9, 8).Select(i => (ulong)i));

        lobby.DraftMode = DraftMode.CaptainDraft_ManualFaction;

        return lobby;
    }

    [Fact]
    public void CaptainDraftManualFaction_ThrowsNotImplemented()
    {
        var lobby = CreateCaptainDraftLobby();

        var factionAssign = new Mock<IFactionAssignmentService>();
        var rng = new Random(12345);

        var engine = new DraftEngine(factionAssign.Object, rng);

        Assert.Throws<NotImplementedException>(() => engine.RunDraft(lobby));
    }
}