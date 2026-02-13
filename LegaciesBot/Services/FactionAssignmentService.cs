using LegaciesBot.Core;
using LegaciesBot.GameData;

namespace LegaciesBot.Services
{
    public class FactionAssignmentService : IFactionAssignmentService
    {
        private readonly Random _rng;

        public FactionAssignmentService(Random? rng = null)
        {
            _rng = rng ?? new Random();
        }

        public void AssignFactionsForGame(
            Team teamA,
            Team teamB,
            HashSet<TeamGroup> allowedGroupsA,
            HashSet<TeamGroup> allowedGroupsB)
        {
            teamA.AssignedFactions.Clear();
            teamB.AssignedFactions.Clear();

            var allowedByTeam = new Dictionary<Team, HashSet<TeamGroup>>
            {
                [teamA] = allowedGroupsA,
                [teamB] = allowedGroupsB
            };

            var allPlayers = new List<(Team team, Player player)>();
            allPlayers.AddRange(teamA.Players.Select(p => (teamA, p)));
            allPlayers.AddRange(teamB.Players.Select(p => (teamB, p)));

            allPlayers = allPlayers
                .OrderBy(_ => _rng.Next())
                .ToList();

            var usedFactionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var groupsPerTeam = new Dictionary<Team, HashSet<TeamGroup>>
            {
                [teamA] = new(),
                [teamB] = new()
            };

            foreach (var (team, player) in allPlayers)
            {
                var allowedGroups = allowedByTeam[team];

                var availableFactions = FactionRegistry.All
                    .Where(f => allowedGroups.Contains(f.Group))
                    .Where(f => !usedFactionNames.Contains(f.Name))
                    .Where(f => string.IsNullOrEmpty(f.SlotId) || !usedSlotIds.Contains(f.SlotId))
                    .ToList();

                if (!availableFactions.Any())
                    throw new Exception($"No available factions left for team {team.Name} before assigning player {player.Name}");

                Faction? assigned = null;

                foreach (var prefName in player.FactionPreferences)
                {
                    var faction = availableFactions
                        .FirstOrDefault(f => f.Name.Equals(prefName, StringComparison.OrdinalIgnoreCase));

                    if (faction == null)
                        continue;

                    if (!IsFactionValidForAssignment(faction, team, groupsPerTeam))
                        continue;

                    assigned = faction;
                    break;
                }

                if (assigned == null)
                {
                    foreach (var faction in availableFactions)
                    {
                        if (!IsFactionValidForAssignment(faction, team, groupsPerTeam))
                            continue;

                        assigned = faction;
                        break;
                    }
                }

                if (assigned == null)
                    throw new Exception($"No available faction to assign for player {player.Name} in team {team.Name}");

                team.AssignedFactions.Add(assigned);
                usedFactionNames.Add(assigned.Name);

                if (!string.IsNullOrEmpty(assigned.SlotId))
                    usedSlotIds.Add(assigned.SlotId);

                groupsPerTeam[team].Add(assigned.Group);
            }
        }

        private static bool IsFactionValidForAssignment(
            Faction faction,
            Team team,
            Dictionary<Team, HashSet<TeamGroup>> groupsPerTeam)
        {
            var teamGroups = groupsPerTeam[team];
            return ConstraintService.IsCompatible(teamGroups, faction.Group);
        }
    }
}