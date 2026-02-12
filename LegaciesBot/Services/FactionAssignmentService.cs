using System;
using System.Collections.Generic;
using System.Linq;
using LegaciesBot.Core;
using LegaciesBot.GameData;

namespace LegaciesBot.Services
{
    public static class FactionAssignmentService
    {
        private static readonly Random Rng = new();

        /// <summary>
        /// Assigns factions to players on a team.
        /// Randomly selects a player each turn, assigns their highest-preference available faction.
        /// Factions that share a slot (e.g., Dalaran/Gilneas) are mutually exclusive per team.
        /// </summary>
        public static void AssignFactionsToTeam(Team team, HashSet<TeamGroup> allowedGroups)
        {

            var availableFactions = FactionRegistry.All
                .Where(f => allowedGroups.Contains(f.Group))
                .ToList();
            
            var unassignedPlayers = team.Players.ToList();
            var usedSlots = new HashSet<string>();

            while (unassignedPlayers.Any())
            {
                var index = Rng.Next(unassignedPlayers.Count);
                var player = unassignedPlayers[index];

                Faction assigned = null;

                foreach (var prefName in player.FactionPreferences)
                {
                    var faction = availableFactions.FirstOrDefault(f => f.Name == prefName && !usedSlots.Contains(f.SlotId));
                    if (faction != null)
                    {
                        assigned = faction;
                        break;
                    }
                }
                
                if (assigned == null)
                {
                    assigned = availableFactions.FirstOrDefault(f => !usedSlots.Contains(f.SlotId));
                }

                if (assigned != null)
                {
                    team.AssignedFactions.Add(assigned);
                    usedSlots.Add(assigned.SlotId); 
                }
                else
                {
                    throw new Exception($"No available faction to assign for player {player.Name} in team {team.Name}");
                }
                unassignedPlayers.RemoveAt(index);
            }
        }
    }
}
