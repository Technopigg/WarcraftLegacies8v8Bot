using LegaciesBot.Core;

namespace LegaciesBot.Services;

public class RealFactionAssignmentService : IFactionAssignmentService
{
    private readonly FactionAssignmentService _inner = new();

    public void AssignFactionsForGame(
        Team teamA,
        Team teamB,
        HashSet<TeamGroup> allowedGroupsA,
        HashSet<TeamGroup> allowedGroupsB)
    {
        _inner.AssignFactionsForGame(
            teamA,
            teamB,
            allowedGroupsA,
            allowedGroupsB
        );
    }
}