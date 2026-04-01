using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;
using LegaciesBot.Services.Drafting;

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

        var registry = new PlayerRegistryService(null);

        for (int i = 0; i < count; i++)
        {
            ulong id = (ulong)(i + 1);

            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            p.FactionPreferences = prefs.ToList();

            list.Add(p);
        }

        return list;
    }

    [Fact]
    public void AssignsFactionToAllPlayers()
    {
        var rng = new Random(12345);
        var service = new RealFactionAssignmentService(new FactionRegistryStub());

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, rng);

        var assignedA = teamA.Players.Where(p => !string.IsNullOrWhiteSpace(p.AssignedFaction)).ToList();
        var assignedB = teamB.Players.Where(p => !string.IsNullOrWhiteSpace(p.AssignedFaction)).ToList();

        Assert.Equal(8, assignedA.Count);
        Assert.Equal(8, assignedB.Count);
    }

    [Fact]
    public void AssignsUniqueFactionsGlobally()
    {
        var rng = new Random(12345);
        var service = new RealFactionAssignmentService(new FactionRegistryStub());

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, rng);

        var allNames = teamA.Players
            .Concat(teamB.Players)
            .Select(p => p.AssignedFaction)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        Assert.Equal(16, allNames.Count);
        Assert.Equal(16, allNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RespectsSlotExclusivity()
    {
        var rng = new Random(12345);
        var service = new RealFactionAssignmentService(new FactionRegistryStub());

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, rng);

        var all = teamA.AssignedFactions.Concat(teamB.AssignedFactions).ToList();

        var slotIds = all.Select(f => f.SlotId).ToList();

        Assert.Equal(slotIds.Count, slotIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RespectsGroupCompatibility()
    {
        var rng = new Random(12345);
        var service = new RealFactionAssignmentService(new FactionRegistryStub());

        var teamA = CreateTeam("A", CreatePlayers(8));
        var teamB = CreateTeam("B", CreatePlayers(8));

        var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

        service.AssignFactionsForGame(teamA, teamB, groupsA, rng);

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
