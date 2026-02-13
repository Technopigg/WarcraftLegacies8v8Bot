using LegaciesBot.Core;


// <summary>
/// Runs a full 8v8 draft:
/// 1. Balances players into teams
/// 2. Generates valid TeamGroup split
/// 3. Assigns factions based on preferences
/// </summary>
/// 
namespace LegaciesBot.Services
{
    public class DraftEngine
    {
        private readonly IFactionAssignmentService _factionAssignment;
        private readonly Random _rng;

        public DraftEngine(IFactionAssignmentService factionAssignment, Random? rng = null)
        {
            _factionAssignment = factionAssignment;
            _rng = rng ?? new Random();
        }

        public (Team teamA, Team teamB) RunDraft(List<Player> players)
        {
            if (players.Count != 16)
                throw new ArgumentException("Draft requires exactly 16 players.");

            var (teamA, teamB) = DraftService.CreateBalancedTeams(players);
            var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

            _factionAssignment.AssignFactionsForGame(teamA, teamB, groupsA, groupsB);

            return (teamA, teamB);
        }
    }
}