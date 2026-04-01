using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.Drafting;

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

        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
        var engine = new DraftEngine(factionAssign, rng);

        var (teamA, teamB) = engine.RunDraft(lobby);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);

        Assert.Empty(teamA.Players.Intersect(teamB.Players));

        var allPlayers = teamA.Players.Concat(teamB.Players).ToList();

        Assert.All(allPlayers, p => Assert.False(string.IsNullOrWhiteSpace(p.AssignedFaction)));

        Assert.Equal(16, allPlayers.Select(p => p.AssignedFaction).Distinct().Count());
    }

    [Fact]
    public void AutoDraftAutoFaction_IsDeterministic_WithSeed()
    {
        var lobby1 = CreateLobbyWithPlayers(16);
        lobby1.DraftMode = DraftMode.AutoDraft_AutoFaction;

        var lobby2 = CreateLobbyWithPlayers(16);
        lobby2.DraftMode = DraftMode.AutoDraft_AutoFaction;

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        var factionAssign1 = new RealFactionAssignmentService(new FactionRegistryStub());
        var factionAssign2 = new RealFactionAssignmentService(new FactionRegistryStub());

        var engine1 = new DraftEngine(factionAssign1, rng1);
        var engine2 = new DraftEngine(factionAssign2, rng2);

        var (teamA1, teamB1) = engine1.RunDraft(lobby1);
        var (teamA2, teamB2) = engine2.RunDraft(lobby2);

        Assert.Equal(teamA1.Players.Select(p => p.DiscordId), teamA2.Players.Select(p => p.DiscordId));
        Assert.Equal(teamB1.Players.Select(p => p.DiscordId), teamB2.Players.Select(p => p.DiscordId));
    }
}
