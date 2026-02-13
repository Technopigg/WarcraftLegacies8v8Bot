using System.Collections.Generic;
using System.Linq;
using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;
using Xunit;

public class FactionAssignmentServiceTests
{
    private Player P(ulong id, params string[] prefs)
    {
        var p = new Player(id, $"P{id}");
        p.FactionPreferences = prefs.ToList();
        return p;
    }

    private Team MakeTeam(string name, params Player[] players)
    {
        var t = new Team(name);
        foreach (var p in players)
            t.AddPlayer(p);
        return t;
    }

    private Faction AnyFactionInGroups(params TeamGroup[] groups)
        => FactionRegistry.All.First(f => groups.Contains(f.Group));

    [Fact]
    public void AssignFactions_RespectsAllowedGroups()
    {
        var allowed = new HashSet<TeamGroup>
        {
            TeamGroup.NorthAlliance,
            TeamGroup.SouthAlliance
        };

        var f1 = AnyFactionInGroups(TeamGroup.NorthAlliance);
        var f2 = AnyFactionInGroups(TeamGroup.SouthAlliance);

        var team = MakeTeam("A",
            P(1, f1.Name),
            P(2, f2.Name)
        );

        FactionAssignmentService.AssignFactionsToTeam(team, allowed);

        Assert.All(team.AssignedFactions, f => Assert.Contains(f.Group, allowed));
    }

    [Fact]
    public void AssignFactions_UsesPlayerPreferencesWhenAvailable()
    {
        var allowed = new HashSet<TeamGroup>(FactionRegistry.All.Select(f => f.Group));

        var pref1 = FactionRegistry.All[0];
        var pref2 = FactionRegistry.All.First(f => f.SlotId != pref1.SlotId);

        var team = MakeTeam("A",
            P(1, pref1.Name),
            P(2, pref2.Name)
        );

        FactionAssignmentService.AssignFactionsToTeam(team, allowed);

        Assert.Contains(team.AssignedFactions, f => f.Name == pref1.Name);
        Assert.Contains(team.AssignedFactions, f => f.Name == pref2.Name);
    }

    [Fact]
    public void AssignFactions_FallsBackWhenPreferencesUnavailable()
    {
        var allowed = new HashSet<TeamGroup>(FactionRegistry.All.Select(f => f.Group));

        var team = MakeTeam("A",
            P(1, "NonexistentFaction")
        );

        FactionAssignmentService.AssignFactionsToTeam(team, allowed);

        Assert.Single(team.AssignedFactions);
    }

    [Fact]
    public void AssignFactions_EnforcesSlotExclusivity()
    {
        var sharedSlot = FactionRegistry.All
            .GroupBy(f => f.SlotId)
            .First(g => g.Count() > 1)
            .ToList();

        var f1 = sharedSlot[0];
        var f2 = sharedSlot[1];

        var allowed = new HashSet<TeamGroup>(FactionRegistry.All.Select(f => f.Group));

        var team = MakeTeam("A",
            P(1, f1.Name),
            P(2, f2.Name)
        );

        FactionAssignmentService.AssignFactionsToTeam(team, allowed);
        
        var assignedFromSharedSlot = team.AssignedFactions
            .Count(f => f.SlotId == f1.SlotId);

        Assert.Equal(1, assignedFromSharedSlot);
    }


    [Fact]
    public void AssignFactions_AssignsOneFactionPerPlayer()
    {
        var allowed = new HashSet<TeamGroup>(FactionRegistry.All.Select(f => f.Group));

        var f1 = FactionRegistry.All[0];
        var f2 = FactionRegistry.All[1];
        var f3 = FactionRegistry.All[2];

        var team = MakeTeam("A",
            P(1, f1.Name),
            P(2, f2.Name),
            P(3, f3.Name)
        );

        FactionAssignmentService.AssignFactionsToTeam(team, allowed);

        Assert.Equal(3, team.AssignedFactions.Count);
    }
}