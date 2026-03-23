using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;

namespace LegaciesBot.Tests;

public class DraftEngineTests
{
    private static List<Player> CreatePlayers(int count)
    {
        var prefs = FactionRegistry.All.Select(f => f.Name).ToList();
        var list = new List<Player>();

        var registry = new PlayerRegistryService(null);

        for (int i = 0; i < count; i++)
        {
            ulong id = (ulong)(i + 1);

            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500 + i * 10;
            p.FactionPreferences = prefs.ToList();

            list.Add(p);
        }

        return list;
    }

    [Fact]
    public void RunDraft_ThrowsIfNot16Players()
    {
        var rng = new Random(12345);
        var assignment = new FactionAssignmentService(rng);
        var engine = new DraftEngine(assignment, rng);

        var players = CreatePlayers(15);

        Assert.Throws<ArgumentException>(() => engine.RunDraft(players));
    }

    [Fact]
    public void RunDraft_AssignsEightPlayersPerTeam()
    {
        var rng = new Random(12345);
        var assignment = new FactionAssignmentService(rng);
        var engine = new DraftEngine(assignment, rng);

        var players = CreatePlayers(16);

        var (teamA, teamB) = engine.RunDraft(players);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);
    }

    [Fact]
    public void RunDraft_AssignsFactionsToAllPlayers()
    {
        var rng = new Random(12345);
        var assignment = new FactionAssignmentService(rng);
        var engine = new DraftEngine(assignment, rng);

        var players = CreatePlayers(16);

        var (teamA, teamB) = engine.RunDraft(players);

        Assert.Equal(8, teamA.AssignedFactions.Count);
        Assert.Equal(8, teamB.AssignedFactions.Count);

        foreach (var p in teamA.Players)
            Assert.False(string.IsNullOrWhiteSpace(p.AssignedFaction));

        foreach (var p in teamB.Players)
            Assert.False(string.IsNullOrWhiteSpace(p.AssignedFaction));
    }

    [Fact]
    public void RunDraft_AssignsUniqueFactionsGlobally()
    {
        var rng = new Random(12345);
        var assignment = new FactionAssignmentService(rng);
        var engine = new DraftEngine(assignment, rng);

        var players = CreatePlayers(16);

        var (teamA, teamB) = engine.RunDraft(players);

        var all = teamA.AssignedFactions.Concat(teamB.AssignedFactions).ToList();

        Assert.Equal(16, all.Count);
        Assert.Equal(16, all.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RunDraft_PlayerFactionMatchesAssignedFactionsList()
    {
        var rng = new Random(12345);
        var assignment = new FactionAssignmentService(rng);
        var engine = new DraftEngine(assignment, rng);

        var players = CreatePlayers(16);

        var (teamA, teamB) = engine.RunDraft(players);

        for (int i = 0; i < 8; i++)
            Assert.Equal(teamA.AssignedFactions[i].Name, teamA.Players[i].AssignedFaction);

        for (int i = 0; i < 8; i++)
            Assert.Equal(teamB.AssignedFactions[i].Name, teamB.Players[i].AssignedFaction);
    }
}