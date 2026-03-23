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
            var (groupsA, groupsB) = TeamGroupService.GenerateValidSplit();

            _factionAssignment.AssignFactionsForGame(teamA, teamB, groupsA, groupsB);

            for (int i = 0; i < teamA.Players.Count; i++)
                teamA.Players[i].AssignedFaction = teamA.AssignedFactions[i].Name;

            for (int i = 0; i < teamB.Players.Count; i++)
                teamB.Players[i].AssignedFaction = teamB.AssignedFactions[i].Name;

            return (teamA, teamB);
        }
    }
}