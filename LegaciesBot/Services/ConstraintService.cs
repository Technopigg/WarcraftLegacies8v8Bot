using System;
using System.Collections.Generic;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public static class ConstraintService
    {
     
        private static readonly Dictionary<TeamGroup, List<TeamGroup>> Incompatibilities = new()
        {
            { TeamGroup.BurningLegion, new() { TeamGroup.NorthAlliance, TeamGroup.FelHorde } },
            { TeamGroup.NorthAlliance, new() { TeamGroup.BurningLegion } },
            { TeamGroup.FelHorde, new() { TeamGroup.SouthAlliance, TeamGroup.BurningLegion } },
            { TeamGroup.SouthAlliance, new() { TeamGroup.FelHorde } },
            { TeamGroup.OldGods, new() { TeamGroup.Kalimdor } },
            { TeamGroup.Kalimdor, new() { TeamGroup.OldGods } }
        };
        
        public static bool IsCompatible(HashSet<TeamGroup> teamGroups, TeamGroup newGroup)
        {
            if (!Incompatibilities.ContainsKey(newGroup))
                return true;

            foreach (var existing in teamGroups)
            {
                if (Incompatibilities[newGroup].Contains(existing))
                    return false;
            }

            return true;
        }
    }
}