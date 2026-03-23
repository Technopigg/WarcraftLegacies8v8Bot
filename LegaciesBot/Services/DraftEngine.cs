using LegaciesBot.Core;
using LegaciesBot.Services.Drafting;

namespace LegaciesBot.Services
{
    public class DraftEngine
    {
        private readonly Random _rng;
        private readonly Dictionary<DraftMode, IDraftStrategy> _strategies;

        public DraftEngine(
            IFactionAssignmentService factionAssignment,
            Random? rng = null)
        {
            _rng = rng ?? new Random();

            _strategies = new()
            {
                [DraftMode.AutoDraft_AutoFaction] = new AutoDraftAutoFactionStrategy(factionAssignment),
                [DraftMode.AutoDraft_ManualFaction] = new AutoDraftManualFactionStrategy(),
                [DraftMode.CaptainDraft_AutoFaction] = new CaptainDraftAutoFactionStrategy(factionAssignment),
                [DraftMode.CaptainDraft_ManualFaction] = new CaptainDraftManualFactionStrategy()
            };
        }

        public (Team teamA, Team teamB) RunDraft(Lobby lobby)
        {
            return _strategies[lobby.DraftMode].RunDraft(lobby, _rng);
        }
    }
}