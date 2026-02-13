using LegaciesBot.Core;

namespace LegaciesBot.Services;

public interface IFactionAssignmentService
{
    void AssignFactionsToTeam(Team team, HashSet<TeamGroup> allowedGroups);
}