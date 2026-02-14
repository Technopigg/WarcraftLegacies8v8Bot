using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;

public class TeamGroupServiceTests
{
    private static readonly HashSet<HashSet<TeamGroup>> ValidCombos = new()
    {
        new HashSet<TeamGroup> { TeamGroup.BurningLegion, TeamGroup.SouthAlliance, TeamGroup.Kalimdor },
        new HashSet<TeamGroup> { TeamGroup.BurningLegion, TeamGroup.SouthAlliance, TeamGroup.OldGods },
        new HashSet<TeamGroup> { TeamGroup.FelHorde, TeamGroup.NorthAlliance, TeamGroup.Kalimdor },
        new HashSet<TeamGroup> { TeamGroup.FelHorde, TeamGroup.NorthAlliance, TeamGroup.OldGods }
    };

    [Fact]
    public void GenerateValidSplit_EachTeamHasExactlyEightFactionSlots()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();

        int CountSlots(HashSet<TeamGroup> team) =>
            FactionRegistry.All
                .Where(f => team.Contains(f.Group))
                .Select(f => f.SlotId ?? f.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        int slotsA = CountSlots(teamA);
        int slotsB = CountSlots(teamB);

        Assert.Equal(8, slotsA);
        Assert.Equal(8, slotsB);
    }

    [Fact]
    public void GenerateValidSplit_ReturnsTwoNonNullSets()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();
        Assert.NotNull(teamA);
        Assert.NotNull(teamB);
    }

    [Fact]
    public void GenerateValidSplit_AssignsExactlyThreeGroupsPerTeam()
    {
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();
        Assert.Equal(3, teamA.Count);
        Assert.Equal(3, teamB.Count);
    }

    [Fact]
    public void GenerateValidSplit_AllGroupsAssignedExactlyOnce()
    {
        var allGroups = Enum.GetValues<TeamGroup>().ToHashSet();
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();
        var combined = teamA.Concat(teamB).ToHashSet();
        Assert.True(allGroups.SetEquals(combined));
    }

    [Fact]
    public void GenerateValidSplit_TeamAIsOneOfTheFourValidCombos()
    {
        var (teamA, _) = TeamGroupService.GenerateValidSplit();
        Assert.Contains(ValidCombos, c => c.SetEquals(teamA));
    }

    [Fact]
    public void GenerateValidSplit_TeamBIsComplementOfTeamA()
    {
        var allGroups = Enum.GetValues<TeamGroup>().ToHashSet();
        var (teamA, teamB) = TeamGroupService.GenerateValidSplit();
        Assert.True(teamB.SetEquals(allGroups.Except(teamA)));
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
    public void GenerateValidSplit_ProducesMultipleDifferentSplits()
    {
        var results = new HashSet<string>();

        for (int i = 0; i < 20; i++)
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