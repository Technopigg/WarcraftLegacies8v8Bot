using LegaciesBot.Core;

namespace LegaciesBot.Services.Drafting
{
    public class AutoDraftManualFactionStrategy : IDraftStrategy
    {
        public (Team teamA, Team teamB) RunDraft(Lobby lobby, Random rng)
        {
            var players = lobby.Players.ToList();
            return DraftService.CreateBalancedTeams(players, rng);
        }
    }
}