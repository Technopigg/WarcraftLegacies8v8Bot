using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;


public class TeamGroupServiceTests
{
    [Fact]
    public void GenerateValidSplit_ReturnsTwoNonNullSets()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();

        Assert.NotNull(teamA);
        Assert.NotNull(teamB);
    }

    [Fact]
    public void GenerateValidSplit_AllGroupsAreAssignedExactlyOnce()
    {
        var allGroups = Enum.GetValues<TeamGroup>().ToHashSet();

        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();

        var combined = teamA.Concat(teamB).ToHashSet();

        Assert.Equal(allGroups.Count, combined.Count);
        Assert.True(allGroups.SetEquals(combined));
        Assert.Empty(teamA.Intersect(teamB));
    }

    [Fact]
    public void GenerateValidSplit_NoIncompatibleGroupsAppearTogether()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();

        foreach (var g in teamA)
            foreach (var other in teamA)
                if (g != other)
                    Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));

        foreach (var g in teamB)
            foreach (var other in teamB)
                if (g != other)
                    Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));
    }

    [Fact]
    public void GenerateValidSplit_EachTeamHasEnoughFactionsForEightPlayers()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();

        int factionsA = FactionRegistry.All.Count(f => teamA.Contains(f.Group));
        int factionsB = FactionRegistry.All.Count(f => teamB.Contains(f.Group));

        Assert.True(factionsA >= 8);
        Assert.True(factionsB >= 8);
    }

    [Fact]
    public void GenerateValidSplit_ProducesDifferentSplitsOverMultipleRuns()
    {
        var results = new HashSet<string>();

        for (int i = 0; i < 10; i++)
        {
            var (teamA, teamB) = TeamGroupService.GenerateValidSplit();
            string signature = string.Join(",", teamA.OrderBy(x => x)) + "|" +
                               string.Join(",", teamB.OrderBy(x => x));
            results.Add(signature);
        }

        Assert.True(results.Count > 1);
    }

    [Fact]
    public void GenerateValidSplit_NoTeamIsEmpty()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();

        Assert.NotEmpty(teamA);
        Assert.NotEmpty(teamB);
    }
}