using LegaciesBot.Core;
using LegaciesBot.GameData;

namespace LegaciesBot.Services
{
    public static class TeamGroupService
    {
        public static string Label(TeamGroup g) => g switch
        {
            TeamGroup.NorthAlliance => "North Alliance",
            TeamGroup.SouthAlliance => "South Alliance",
            TeamGroup.BurningLegion => "Burning Legion",
            TeamGroup.FelHorde => "Fel Horde",
            TeamGroup.Horde => "The Horde",
            TeamGroup.NightElves => "The Night Elves",
            _ => g.ToString(),
        };

        /// <summary>
        /// Number of player slots a side provides. Either/or factions (Dalaran/Gilneas,
        /// Illidari/Sunfury) share one slot, so North Alliance is 3 and Fel Horde is 2.
        /// </summary>
        public static int CountSlots(TeamGroup g) =>
            FactionRegistry.All
                .Where(f => f.Group == g)
                .Select(f => f.SlotId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        /// <summary>
        /// Every legal (Team A sides, Team B sides) matchup: every side goes to one team, no team
        /// holds an opposing pair, and both teams come to exactly TeamSize slots. Built by trying
        /// every split rather than hand-listed, so a roster or rule change can't drift out of sync.
        /// </summary>
        public static readonly IReadOnlyList<(HashSet<TeamGroup> TeamA, HashSet<TeamGroup> TeamB)> ValidSplits =
            BuildValidSplits();

        private static List<(HashSet<TeamGroup>, HashSet<TeamGroup>)> BuildValidSplits()
        {
            var all = Enum.GetValues<TeamGroup>();
            var splits = new List<(HashSet<TeamGroup>, HashSet<TeamGroup>)>();

            for (int mask = 0; mask < 1 << all.Length; mask++)
            {
                var teamA = all.Where((_, i) => (mask & (1 << i)) != 0).ToHashSet();
                var teamB = all.Except(teamA).ToHashSet();

                if (HasOpposingPair(teamA) || HasOpposingPair(teamB))
                    continue;

                if (teamA.Sum(CountSlots) == MatchFormat.TeamSize && teamB.Sum(CountSlots) == MatchFormat.TeamSize)
                    splits.Add((teamA, teamB));
            }

            return splits;
        }

        private static bool HasOpposingPair(HashSet<TeamGroup> team) =>
            team.Any(a => team.Any(b => ConstraintService.AreOpposed(a, b)));

        public static (HashSet<TeamGroup>, HashSet<TeamGroup>) GenerateValidSplit(Random? rng = null)
        {
            var chosen = ValidSplits[(rng ?? Random.Shared).Next(ValidSplits.Count)];
            return (chosen.TeamA.ToHashSet(), chosen.TeamB.ToHashSet());
        }
    }
}
