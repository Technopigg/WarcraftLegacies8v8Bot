using LegaciesBot.Core;
using LegaciesBot.GameData;

namespace LegaciesBot.Services
{
    public static class TeamGroupService
    {
        private static readonly List<(TeamGroup, TeamGroup, TeamGroup)> ValidTeamACombos =
            new()
            {
                (TeamGroup.BurningLegion, TeamGroup.SouthAlliance, TeamGroup.Kalimdor),
                (TeamGroup.BurningLegion, TeamGroup.SouthAlliance, TeamGroup.OldGods),
                (TeamGroup.FelHorde, TeamGroup.NorthAlliance, TeamGroup.Kalimdor),
                (TeamGroup.FelHorde, TeamGroup.NorthAlliance, TeamGroup.OldGods)
            };

        private static int CountSlots(TeamGroup g)
        {
            return FactionRegistry.All
                .Where(f => f.Group == g)
                .Select(f => f.SlotId ?? f.Name)   
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private static readonly List<(TeamGroup, TeamGroup, TeamGroup)> PrecomputedValid =
            ValidTeamACombos
                .Where(c =>
                {
                    var teamA = new HashSet<TeamGroup> { c.Item1, c.Item2, c.Item3 };
                    var teamB = Enum.GetValues(typeof(TeamGroup))
                        .Cast<TeamGroup>()
                        .Where(g => !teamA.Contains(g))
                        .ToHashSet();

                    var slotsA = teamA.Sum(CountSlots);
                    var slotsB = teamB.Sum(CountSlots);

                    return slotsA == 8 && slotsB == 8;
                })
                .ToList();

        public static (HashSet<TeamGroup>, HashSet<TeamGroup>) GenerateValidSplit()
        {
            var rng = new Random();
            var chosen = PrecomputedValid[rng.Next(PrecomputedValid.Count)];

            var teamA = new HashSet<TeamGroup> { chosen.Item1, chosen.Item2, chosen.Item3 };
            var teamB = Enum.GetValues(typeof(TeamGroup))
                .Cast<TeamGroup>()
                .Where(g => !teamA.Contains(g))
                .ToHashSet();

            return (teamA, teamB);
        }
    }
}