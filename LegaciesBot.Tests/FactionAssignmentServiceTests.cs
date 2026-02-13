using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;

public class FactionAssignmentServiceTests
{
    private static Team CreateTeam(string name, IEnumerable<Player> players)
    {
        var t = new Team(name);
        foreach (var p in players)
            t.AddPlayer(p);
        return t;
    }

    private static List<Player> CreatePlayers(int count)
    {
        var prefs = FactionRegistry.All.Select(f => f.Name).ToList();
        var list = new List<Player>();

        for (int i = 0; i < count; i++)
        {
            var p = new Player((ulong)(i + 1), $"Player{i + 1}", 1500);
            p.FactionPreferences = prefs.ToList();
            list.Add(p);
        }

        return list;
    }

    [Fact]
    public void AssignsFactionToAllPlayers()
    {
        var rng = new Random(12345);
        var service = new FactionAssignmentService(rng);

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, groupsB);

        Assert.Equal(8, teamA.AssignedFactions.Count);
        Assert.Equal(8, teamB.AssignedFactions.Count);
    }

    [Fact]
    public void AssignsUniqueFactionsGlobally()
    {
        var rng = new Random(12345);
        var service = new FactionAssignmentService(rng);

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, groupsB);

        var all = teamA.AssignedFactions.Concat(teamB.AssignedFactions).ToList();

        Assert.Equal(16, all.Count);
        Assert.Equal(16, all.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RespectsSlotExclusivity()
    {
        var rng = new Random(12345);
        var service = new FactionAssignmentService(rng);

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, groupsB);

        var all = teamA.AssignedFactions.Concat(teamB.AssignedFactions).ToList();

        var slotIds = all.Select(f => f.SlotId).ToList();

        Assert.Equal(slotIds.Count, slotIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RespectsGroupCompatibility()
    {
        var rng = new Random(12345);
        var service = new FactionAssignmentService(rng);

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, groupsB);

        var groupsAUsed = teamA.AssignedFactions.Select(f => f.Group).ToHashSet();
        var groupsBUsed = teamB.AssignedFactions.Select(f => f.Group).ToHashSet();

        foreach (var g in groupsAUsed)
            foreach (var other in groupsAUsed)
                Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));

        foreach (var g in groupsBUsed)
            foreach (var other in groupsBUsed)
                Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));
    }
}