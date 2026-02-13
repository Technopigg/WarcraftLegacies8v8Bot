using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;


public class DraftEngineTests
{
    private Player P(int id)
    {
        var p = new Player((ulong)id, $"P{id}");
        p.FactionPreferences = new List<string>();
        return p;
    }

    private List<Player> SixteenPlayers()
    {
        var list = new List<Player>();
        for (int i = 1; i <= 16; i++)
            list.Add(P(i));
        return list;
    }

    [Fact]
    public void RunDraft_ThrowsIfNot16Players()
    {
        Assert.Throws<ArgumentException>(() => DraftEngine.RunDraft(new List<Player>()));
        Assert.Throws<ArgumentException>(() => DraftEngine.RunDraft(new List<Player> { P(1) }));
        Assert.Throws<ArgumentException>(() => DraftEngine.RunDraft(new List<Player>(Enumerable.Range(1, 15).Select(P))));
        Assert.Throws<ArgumentException>(() => DraftEngine.RunDraft(new List<Player>(Enumerable.Range(1, 17).Select(P))));
    }

    [Fact]
    public void RunDraft_ReturnsTwoTeamsOfEightPlayers()
    {
        var players = SixteenPlayers();

        var (teamA, teamB) = DraftEngine.RunDraft(players);

        Assert.Equal(8, teamA.Players.Count);
        Assert.Equal(8, teamB.Players.Count);
    }

    [Fact]
    public void RunDraft_AssignsValidTeamGroups()
    {
        var players = SixteenPlayers();

        var (teamA, teamB) = DraftEngine.RunDraft(players);

     
        var groupsA = teamA.AssignedFactions.Select(f => f.Group).ToHashSet();
        var groupsB = teamB.AssignedFactions.Select(f => f.Group).ToHashSet();

 
        foreach (var g in groupsA)
        {
            foreach (var other in groupsA)
            {
                if (g == other) continue;
                Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));
            }
        }

        foreach (var g in groupsB)
        {
            foreach (var other in groupsB)
            {
                if (g == other) continue;
                Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));
            }
        }
    }

    [Fact]
    public void RunDraft_AssignsEightFactionsPerTeam()
    {
        var players = SixteenPlayers();

        var (teamA, teamB) = DraftEngine.RunDraft(players);

        Assert.Equal(8, teamA.AssignedFactions.Count);
        Assert.Equal(8, teamB.AssignedFactions.Count);
    }

    [Fact]
    public void RunDraft_NoDuplicateSlotsWithinTeam()
    {
        var players = SixteenPlayers();

        var (teamA, teamB) = DraftEngine.RunDraft(players);

        var slotsA = teamA.AssignedFactions.Select(f => f.SlotId).ToList();
        var slotsB = teamB.AssignedFactions.Select(f => f.SlotId).ToList();

        Assert.Equal(slotsA.Count, slotsA.Distinct().Count());
        Assert.Equal(slotsB.Count, slotsB.Distinct().Count());
    }

    [Fact]
    public void RunDraft_AssignedFactionsBelongToAllowedGroups()
    {
        var players = SixteenPlayers();

        var (teamA, teamB) = DraftEngine.RunDraft(players);

        foreach (var f in teamA.AssignedFactions)
            Assert.Contains(f.Group, Enum.GetValues<TeamGroup>());

        foreach (var f in teamB.AssignedFactions)
            Assert.Contains(f.Group, Enum.GetValues<TeamGroup>());
    }

    [Fact]
    public void RunDraft_RespectsPreferencesForSomePlayers()
    {
        var players = SixteenPlayers();
        
        var prefFaction1 = FactionRegistry.All[0];
        var prefFaction2 = FactionRegistry.All.First(f => f.SlotId != prefFaction1.SlotId);

        players[0].FactionPreferences = new List<string> { prefFaction1.Name };
        players[1].FactionPreferences = new List<string> { prefFaction2.Name };

        var (teamA, teamB) = DraftEngine.RunDraft(players);

        var allAssigned = teamA.AssignedFactions.Concat(teamB.AssignedFactions).ToList();

        Assert.Contains(allAssigned, f => f.Name == prefFaction1.Name);
        Assert.Contains(allAssigned, f => f.Name == prefFaction2.Name);
    }
}