using LegaciesBot.Core;

namespace LegaciesBot.Services.Drafting
{
    public class CaptainDraftManualFactionStrategy : IDraftStrategy
    {
        public (Team teamA, Team teamB) RunDraft(Lobby lobby, Random rng)
        {
            throw new NotImplementedException("Captain draft logic will be added in Step 4.");
        }
    }
}