using LegaciesBot.Core;

namespace LegaciesBot.Services;

public class RealFactionAssignmentService : IFactionAssignmentService
{
    public void AssignFactionsToTeam(Team team, HashSet<TeamGroup> allowedGroups)
    {
        FactionAssignmentService.AssignFactionsToTeam(team, allowedGroups);
    }
}