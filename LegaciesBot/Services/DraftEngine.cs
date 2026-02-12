using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public static class DraftEngine
    {
        /// <summary>
        /// Runs a full 8v8 draft:
        /// 1. Balances players into teams
        /// 2. Generates valid TeamGroup split
        /// 3. Assigns factions based on preferences
        /// </summary>
        public static (Team teamA, Team teamB) RunDraft(List<Player> players)
        {
            if (players.Count != 16)
                throw new ArgumentException("Draft requires exactly 16 players.");

            var (teamA, teamB) = DraftService.CreateBalancedTeams(players);
            var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

            FactionAssignmentService.AssignFactionsToTeam(teamA, groupsA);
            FactionAssignmentService.AssignFactionsToTeam(teamB, groupsB);

            return (teamA, teamB);
        }
    }
}