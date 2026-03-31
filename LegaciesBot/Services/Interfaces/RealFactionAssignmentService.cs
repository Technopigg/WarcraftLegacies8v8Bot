using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class RealFactionAssignmentService : IFactionAssignmentService
    {
        private readonly IFactionRegistry _registry;

        public RealFactionAssignmentService(IFactionRegistry registry)
        {
            _registry = registry;
        }

        public void AssignFactionsForGame(
            Team teamA,
            Team teamB,
            HashSet<TeamGroup>? bannedGroups,
            Random? rng)
        {
            var random = rng ?? new Random();
            var factions = _registry.All.ToList();

            foreach (var p in teamA.Players)
            {
                var pick = factions[random.Next(factions.Count)];
                p.AssignedFaction = pick.Name;
                factions.Remove(pick);
            }

            foreach (var p in teamB.Players)
            {
                var pick = factions[random.Next(factions.Count)];
                p.AssignedFaction = pick.Name;
                factions.Remove(pick);
            }
        }
    }
}