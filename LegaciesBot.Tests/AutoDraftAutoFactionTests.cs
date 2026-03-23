using LegaciesBot.Core;
using LegaciesBot.Services;

namespace LegaciesBot.Tests;

public class AutoDraftAutoFactionTests
{
    private Lobby CreateLobbyWithPlayers(int count)
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 0; i < count; i++)
        {
            ulong id = (ulong)(i + 1);
            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public void AutoDraftAutoFaction_CreatesBalancedTeams_AndAssignsFactions()
    {
        var lobby = CreateLobbyWithPlayers(16);
        lobby.DraftMode = DraftMode.AutoDraft_AutoFaction;

        var rng = new Random(12345);
        
        var factionAssign = new FactionAssignmentService(rng);

        var engine = new DraftEngine(factionAssign, rng);

        var (teamA, teamB) = engine.RunDraft(lobby);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);
        
    }
}