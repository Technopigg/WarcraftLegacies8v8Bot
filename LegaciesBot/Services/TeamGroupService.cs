using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;

namespace LegaciesBot.Services
{
    public static class TeamGroupService
    {
        private static readonly List<TeamGroup> AllGroups = Enum.GetValues(typeof(TeamGroup))
            .Cast<TeamGroup>()
            .ToList();

        public static (HashSet<TeamGroup>, HashSet<TeamGroup>) GenerateValidSplit()
        {
            var rng = new Random();
            while (true)
            {
                var teamA = new HashSet<TeamGroup>();
                var teamB = new HashSet<TeamGroup>();

                var shuffledGroups = AllGroups.OrderBy(_ => rng.Next()).ToList();

                foreach (var group in shuffledGroups)
                {
                    if (ConstraintService.IsCompatible(teamA, group))
                        teamA.Add(group);
                    else
                        teamB.Add(group);
                }
                
                int teamAPlayerCount = 8; // or team.Players.Count
                int teamBPlayerCount = 8;

                int teamAFactions = FactionRegistry.All.Count(f => teamA.Contains(f.Group));
                int teamBFactions = FactionRegistry.All.Count(f => teamB.Contains(f.Group));

                if (teamAFactions >= teamAPlayerCount && teamBFactions >= teamBPlayerCount)
                    return (teamA, teamB);
              
            }
        }


        private static int CountFactions(HashSet<TeamGroup> groups)
        {
            return FactionRegistry.All.Count(f => groups.Contains(f.Group));
        }
    }
}