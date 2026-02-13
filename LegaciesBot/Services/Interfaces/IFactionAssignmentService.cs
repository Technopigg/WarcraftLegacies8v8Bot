using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public interface IFactionAssignmentService
    {
        void AssignFactionsForGame(
            Team teamA,
            Team teamB,
            HashSet<TeamGroup> allowedGroupsA,
            HashSet<TeamGroup> allowedGroupsB);
    }
}