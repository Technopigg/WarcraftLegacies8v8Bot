using System.Collections.Generic;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public static class ConstraintService
    {
        private static readonly Dictionary<TeamGroup, List<TeamGroup>> Incompatibilities = new()
        {
            { TeamGroup.BurningLegion, new() { TeamGroup.NorthAlliance, TeamGroup.FelHorde } },
            { TeamGroup.NorthAlliance, new() { TeamGroup.BurningLegion, TeamGroup.SouthAlliance } },
            { TeamGroup.FelHorde, new() { TeamGroup.BurningLegion, TeamGroup.SouthAlliance } },
            { TeamGroup.SouthAlliance, new() { TeamGroup.FelHorde, TeamGroup.NorthAlliance } },
            { TeamGroup.OldGods, new() { TeamGroup.Kalimdor } },
            { TeamGroup.Kalimdor, new() { TeamGroup.OldGods } }
        };

        private static bool Incompat(TeamGroup a, TeamGroup b)
        {
            return Incompatibilities.TryGetValue(a, out var blocked) && blocked.Contains(b);
        }

        public static bool IsCompatible(HashSet<TeamGroup> teamGroups, TeamGroup newGroup)
        {
            foreach (var existing in teamGroups)
            {
                if (Incompat(existing, newGroup) || Incompat(newGroup, existing))
                    return false;
            }

            return true;
        }
    }
}