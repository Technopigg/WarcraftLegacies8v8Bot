using LegaciesBot.Core;



namespace LegaciesBot.Services

// <summary>
/// Runs a full 8v8 draft:
/// 1. Balances players into teams
/// 2. Generates valid TeamGroup split
/// 3. Assigns factions based on preferences
/// </summary>
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

            for (int i = 0; i < teamA.Players.Count && i < teamA.AssignedFactions.Count; i++)
                teamA.Players[i].AssignedFaction = teamA.AssignedFactions[i].Name;

            for (int i = 0; i < teamB.Players.Count && i < teamB.AssignedFactions.Count; i++)
                teamB.Players[i].AssignedFaction = teamB.AssignedFactions[i].Name;

            return (teamA, teamB);
        }
    }
}