using LegaciesBot.Core;

namespace LegaciesBot.Services.Drafting
{
    public class CaptainDraftAutoFactionStrategy : IDraftStrategy
    {
        private readonly IFactionAssignmentService _factionAssignment;

        public CaptainDraftAutoFactionStrategy(IFactionAssignmentService factionAssignment)
        {
            _factionAssignment = factionAssignment;
        }

        public (Team teamA, Team teamB) RunDraft(Lobby lobby, Random rng)
        {
            var teamA = new Team("Team A");
            var teamB = new Team("Team B");

            foreach (var id in lobby.TeamAPicks)
                teamA.AddPlayer(lobby.Players.First(p => p.DiscordId == id));

            foreach (var id in lobby.TeamBPicks)
                teamB.AddPlayer(lobby.Players.First(p => p.DiscordId == id));

            _factionAssignment.AssignFactionsForGame(teamA, teamB, null, rng);

            return (teamA, teamB);
        }
    }
}