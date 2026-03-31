using LegaciesBot.Core;

namespace LegaciesBot.Services.Drafting
{
    public class AutoDraftAutoFactionStrategy : IDraftStrategy
    {
        private readonly IFactionAssignmentService _factionAssignment;

        public AutoDraftAutoFactionStrategy(IFactionAssignmentService factionAssignment)
        {
            _factionAssignment = factionAssignment;
        }

        public (Team teamA, Team teamB) RunDraft(Lobby lobby, Random rng)
        {
            var players = lobby.Players.ToList();

            var (teamA, teamB) = DraftService.CreateBalancedTeams(players, rng);

            _factionAssignment.AssignFactionsForGame(teamA, teamB, null, rng);

            return (teamA, teamB);
        }
    }
}