using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;

public class DraftSimulationTests
{
    private static List<Player> CreatePlayers(int count, int seed)
    {
        var rng = new Random(seed);
        var prefs = FactionRegistry.All.Select(f => f.Name).ToList();
        var list = new List<Player>();

        for (int i = 0; i < count; i++)
        {
            var p = new Player(
                (ulong)(i + 1),
                $"Player{i + 1}",
                1400 + rng.Next(-200, 201)
            );

            p.FactionPreferences = prefs
                .OrderBy(_ => rng.Next())
                .ToList();

            list.Add(p);
        }

        return list;
    }

    [Fact]
    public void DraftRuns_MultipleTimes_NoFailures_And_ValidAssignments()
    {
        for (int run = 0; run < 20; run++)
        {
            var rng = new Random(1000 + run);

            var assignment = new FactionAssignmentService(rng);
            var engine = new DraftEngine(assignment, rng);

            var players = CreatePlayers(16, 5000 + run);

            var (teamA, teamB) = engine.RunDraft(players);

            Assert.Equal(8, teamA.Players.Count);
            Assert.Equal(8, teamB.Players.Count);

            Assert.Equal(8, teamA.AssignedFactions.Count);
            Assert.Equal(8, teamB.AssignedFactions.Count);

            var all = teamA.AssignedFactions.Concat(teamB.AssignedFactions).ToList();
            Assert.Equal(16, all.Count);
            Assert.Equal(16, all.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());

            var slotIds = all.Select(f => f.SlotId).ToList();
            Assert.Equal(slotIds.Count, slotIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            var groupsA = teamA.AssignedFactions.Select(f => f.Group).ToHashSet();
            var groupsB = teamB.AssignedFactions.Select(f => f.Group).ToHashSet();

            foreach (var g in groupsA)
                foreach (var other in groupsA)
                    Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));

            foreach (var g in groupsB)
                foreach (var other in groupsB)
                    Assert.True(ConstraintService.IsCompatible(new HashSet<TeamGroup> { g }, other));
        }
    }
}