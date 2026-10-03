using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    /// <summary>
    /// The side-picking rules. In a real game one captain picks a side (e.g. Burning Legion),
    /// then the other captain picks, and these pairs always end up on opposite teams.
    /// Together they leave only Burning Legion + South Alliance vs North Alliance + Fel Horde,
    /// with The Horde and The Night Elves split either way (see TeamGroupService).
    /// </summary>
    public static class ConstraintService
    {
        public static readonly IReadOnlyList<(TeamGroup, TeamGroup)> OpposingPairs = new[]
        {
            (TeamGroup.BurningLegion, TeamGroup.NorthAlliance),
            (TeamGroup.FelHorde, TeamGroup.SouthAlliance),
            (TeamGroup.NorthAlliance, TeamGroup.SouthAlliance),
            (TeamGroup.BurningLegion, TeamGroup.FelHorde),
            (TeamGroup.Horde, TeamGroup.NightElves),
        };

        public static bool AreOpposed(TeamGroup a, TeamGroup b) =>
            OpposingPairs.Any(p => (p.Item1 == a && p.Item2 == b) || (p.Item1 == b && p.Item2 == a));

        public static bool IsCompatible(HashSet<TeamGroup> teamGroups, TeamGroup newGroup) =>
            teamGroups.All(existing => !AreOpposed(existing, newGroup));
    }
}
