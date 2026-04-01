using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.Drafting;

public class DraftEngineDebugModeTests
{
    private Lobby CreateLobby()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 1; i <= 16; i++)
        {
            var p = registry.GetOrCreate((ulong)i);
            p.Name = $"P{i}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public void DebugDraft_RunsDraftEngineNormally()
    {
        var rng = new Random(12345);
        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
        var engine = new DraftEngine(factionAssign, rng);

        var lobby = CreateLobby();
        lobby.DraftStarted = true;
        lobby.DraftMode = DraftMode.AutoDraft_AutoFaction;

        var (teamA, teamB) = engine.RunDraft(lobby);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);

        Assert.True(teamA.Players.All(p => !string.IsNullOrWhiteSpace(p.AssignedFaction)));
        Assert.True(teamB.Players.All(p => !string.IsNullOrWhiteSpace(p.AssignedFaction)));
    }
}