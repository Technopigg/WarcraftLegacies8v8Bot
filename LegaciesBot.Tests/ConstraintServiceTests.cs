using LegaciesBot.Core;
using LegaciesBot.Services;

public class ConstraintServiceTests
{
    [Fact]
    public void IsCompatible_ReturnsTrue_WhenTeamGroupsIsEmpty()
    {
        var teamGroups = new HashSet<TeamGroup>();
        Assert.True(ConstraintService.IsCompatible(teamGroups, TeamGroup.NorthAlliance));
        Assert.True(ConstraintService.IsCompatible(teamGroups, TeamGroup.BurningLegion));
        Assert.True(ConstraintService.IsCompatible(teamGroups, TeamGroup.Kalimdor));
    }

    [Fact]
    public void IsCompatible_RespectsDefinedIncompatibilities()
    {
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.NorthAlliance },
            TeamGroup.BurningLegion));
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.FelHorde },
            TeamGroup.BurningLegion));
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.BurningLegion },
            TeamGroup.NorthAlliance));
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.SouthAlliance },
            TeamGroup.FelHorde));
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.BurningLegion },
            TeamGroup.FelHorde));
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.Kalimdor },
            TeamGroup.OldGods));
        Assert.False(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.OldGods },
            TeamGroup.Kalimdor));
    }

    [Fact]
    public void IsCompatible_ReturnsTrue_WhenGroupsAreCompatible()
    {
        Assert.True(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.NorthAlliance },
            TeamGroup.SouthAlliance));

        Assert.True(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.FelHorde },
            TeamGroup.OldGods));

        Assert.True(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.BurningLegion },
            TeamGroup.Kalimdor));

        Assert.True(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.SouthAlliance },
            TeamGroup.Kalimdor));
    }

    [Fact]
    public void IsCompatible_MultipleExistingGroups_AllMustBeCompatible()
    {
        var teamGroups = new HashSet<TeamGroup>
        {
            TeamGroup.NorthAlliance,
            TeamGroup.SouthAlliance
        };

        Assert.False(ConstraintService.IsCompatible(teamGroups, TeamGroup.FelHorde));
        Assert.False(ConstraintService.IsCompatible(teamGroups, TeamGroup.BurningLegion));
        Assert.True(ConstraintService.IsCompatible(teamGroups, TeamGroup.Kalimdor));
    }

    [Fact]
    public void IsCompatible_GroupsWithNoIncompatibilitiesAlwaysReturnTrue()
    {
        Assert.True(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.NorthAlliance },
            TeamGroup.SouthAlliance));
        Assert.True(ConstraintService.IsCompatible(
            new HashSet<TeamGroup> { TeamGroup.NorthAlliance },
            TeamGroup.Kalimdor));
    }
}