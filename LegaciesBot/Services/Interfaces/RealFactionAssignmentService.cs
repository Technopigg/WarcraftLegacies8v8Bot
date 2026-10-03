using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    /// <summary>
    /// Auto-faction assignment. Mirrors how captains pick in a real game: each team gets
    /// whole sides from one legal matchup (TeamGroupService.ValidSplits), every slot is used
    /// once (so only one of Dalaran/Gilneas, one of Illidari/Sunfury), and players get their
    /// highest !prefs choice that's still free on their own team. The matchup that satisfies
    /// preferences best is used; ties are broken at random.
    ///
    /// It used to hand out random factions from the whole list, so teams mixed opposing
    /// sides in practically every game.
    /// </summary>
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

            List<(Player Player, Faction? Faction)>? bestA = null, bestB = null;
            int bestScore = -1;

            foreach (var (groupsA, groupsB) in TeamGroupService.ValidSplits.OrderBy(_ => random.Next()))
            {
                var (picksA, scoreA) = AssignTeam(teamA.Players, groupsA, random);
                var (picksB, scoreB) = AssignTeam(teamB.Players, groupsB, random);

                if (scoreA + scoreB > bestScore)
                {
                    bestScore = scoreA + scoreB;
                    bestA = picksA;
                    bestB = picksB;
                }
            }

            Apply(teamA, bestA!);
            Apply(teamB, bestB!);
        }

        private static void Apply(Team team, List<(Player Player, Faction? Faction)> picks)
        {
            team.AssignedFactions.Clear();
            foreach (var (player, faction) in picks)
            {
                player.AssignedFaction = faction?.Name;
                if (faction != null)
                    team.AssignedFactions.Add(faction);
            }
        }

        private (List<(Player, Faction?)> Picks, int Score) AssignTeam(
            IReadOnlyList<Player> players, HashSet<TeamGroup> groups, Random random)
        {
            // Free slots for this team's sides: slot id -> the faction(s) that can fill it.
            var freeSlots = _registry.All
                .Where(f => groups.Contains(f.Group))
                .GroupBy(f => f.SlotId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var picks = new Dictionary<Player, Faction?>();
            int score = 0;

            // Random order so nobody is always first in line for a contested faction.
            var order = players.OrderBy(_ => random.Next()).ToList();

            foreach (var player in order)
            {
                var prefs = player.FactionPreferences ?? new List<string>();
                for (int rank = 0; rank < prefs.Count; rank++)
                {
                    var wanted = _registry.All.FirstOrDefault(f =>
                        f.Name.Equals(prefs[rank], StringComparison.OrdinalIgnoreCase));
                    if (wanted == null || !freeSlots.ContainsKey(wanted.SlotId))
                        continue;

                    picks[player] = wanted;
                    freeSlots.Remove(wanted.SlotId);
                    score += Math.Max(1, 10 - rank); // a 1st choice is worth more than a 5th
                    break;
                }
            }

            foreach (var player in order.Where(p => !picks.ContainsKey(p)))
            {
                if (freeSlots.Count == 0)
                {
                    picks[player] = null; // more players than slots; shouldn't happen in a full lobby
                    continue;
                }

                var slot = freeSlots.Keys.ElementAt(random.Next(freeSlots.Count));
                var options = freeSlots[slot];
                picks[player] = options[random.Next(options.Count)];
                freeSlots.Remove(slot);
            }

            return (players.Select(p => (p, picks[p])).ToList(), score);
        }
    }
}
