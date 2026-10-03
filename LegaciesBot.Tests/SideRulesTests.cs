using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;
using Moq;

namespace LegaciesBot.Tests;

// How real games are set up: captains pick whole sides, and these always end up on
// opposite teams: Legion/North, Fel Horde/South, North/South, Legion/Fel Horde, Horde/Night Elves.
public class SideRulesTests
{
    private static readonly Dictionary<string, Faction> ByName = FactionRegistry.All.ToDictionary(f => f.Name);

    [Fact]
    public void Only_Legion_South_vs_North_FelHorde_with_Horde_and_Elves_either_way()
    {
        var expected = new[]
        {
            new[] { TeamGroup.BurningLegion, TeamGroup.SouthAlliance, TeamGroup.Horde },
            new[] { TeamGroup.BurningLegion, TeamGroup.SouthAlliance, TeamGroup.NightElves },
            new[] { TeamGroup.NorthAlliance, TeamGroup.FelHorde, TeamGroup.Horde },
            new[] { TeamGroup.NorthAlliance, TeamGroup.FelHorde, TeamGroup.NightElves },
        };

        Assert.Equal(4, TeamGroupService.ValidSplits.Count);
        foreach (var combo in expected)
            Assert.Contains(TeamGroupService.ValidSplits, s => s.TeamA.SetEquals(combo));
    }

    [Theory]
    [InlineData(TeamGroup.BurningLegion, TeamGroup.NorthAlliance)]
    [InlineData(TeamGroup.FelHorde, TeamGroup.SouthAlliance)]
    [InlineData(TeamGroup.NorthAlliance, TeamGroup.SouthAlliance)]
    [InlineData(TeamGroup.BurningLegion, TeamGroup.FelHorde)]
    [InlineData(TeamGroup.Horde, TeamGroup.NightElves)]
    public void Opposing_sides(TeamGroup a, TeamGroup b)
    {
        Assert.True(ConstraintService.AreOpposed(a, b));
        Assert.True(ConstraintService.AreOpposed(b, a));
    }

    private static (Team A, Team B) AutoAssign(int seed, Action<Player>? setPrefs = null)
    {
        var a = new Team("A");
        var b = new Team("B");
        for (int i = 0; i < MatchFormat.TeamSize; i++)
        {
            a.AddPlayer(new Player((ulong)(i + 1), $"a{i}"));
            b.AddPlayer(new Player((ulong)(i + 100), $"b{i}"));
        }
        foreach (var p in a.Players.Concat(b.Players))
            setPrefs?.Invoke(p);

        new RealFactionAssignmentService(new FactionRegistryStub()).AssignFactionsForGame(a, b, null, new Random(seed));
        return (a, b);
    }

    [Fact]
    public void Auto_faction_always_gives_each_team_whole_legal_sides()
    {
        for (int seed = 0; seed < 500; seed++)
        {
            var (a, b) = AutoAssign(seed);
            var all = a.Players.Concat(b.Players).ToList();

            Assert.All(all, p => Assert.False(string.IsNullOrEmpty(p.AssignedFaction)));

            var groupsA = a.Players.Select(p => ByName[p.AssignedFaction!].Group).ToHashSet();
            var groupsB = b.Players.Select(p => ByName[p.AssignedFaction!].Group).ToHashSet();
            Assert.Contains(TeamGroupService.ValidSplits, s => s.TeamA.SetEquals(groupsA) && s.TeamB.SetEquals(groupsB));

            // One faction per slot: never both Dalaran and Gilneas, or both Illidari and Sunfury.
            var slots = all.Select(p => ByName[p.AssignedFaction!].SlotId).ToList();
            Assert.Equal(slots.Count, slots.Distinct().Count());

            Assert.Equal(MatchFormat.TeamSize, a.AssignedFactions.Count);
            Assert.Equal(MatchFormat.TeamSize, b.AssignedFactions.Count);
        }
    }

    [Fact]
    public void Auto_faction_gives_players_their_preference_when_it_fits()
    {
        // Only player 1 has a preference; a matchup exists where they get it, so they always do.
        for (int seed = 0; seed < 50; seed++)
        {
            var (a, _) = AutoAssign(seed, p => p.FactionPreferences = p.DiscordId == 1 ? new() { "Druids" } : new());
            Assert.Equal("Druids", a.Players.First(p => p.DiscordId == 1).AssignedFaction);
        }
    }

    // --- !assignf (manual faction) ---

    private static (Lobby Lobby, FactionManualAssignmentService Service) ManualSetup()
    {
        var registry = new PlayerRegistryService(null);
        var lobby = new Lobby { DraftMode = DraftMode.CaptainDraft_ManualFaction, IsCaptainDraft = true, CaptainA = 1, CaptainB = 8 };
        for (int i = 1; i <= MatchFormat.LobbySize; i++)
            lobby.Players.Add(registry.GetOrCreate((ulong)i));
        lobby.TeamAPicks.AddRange(Enumerable.Range(1, MatchFormat.TeamSize).Select(i => (ulong)i));
        lobby.TeamBPicks.AddRange(Enumerable.Range(MatchFormat.TeamSize + 1, MatchFormat.TeamSize).Select(i => (ulong)i));

        var factionRegistry = new Mock<IFactionRegistry>();
        factionRegistry.Setup(r => r.All).Returns(FactionRegistry.All);
        var gameService = new GameService(new DummyGatewayClient(), new MatchHistoryAdapter(new MatchHistoryService()),
            new EloStub(), new FactionAssignmentStub(), new FactionRegistryStub(), new DefaultPreferencesStub(), new Random(1));
        return (lobby, new FactionManualAssignmentService(factionRegistry.Object, new NicknameService(registry), gameService));
    }

    [Fact]
    public void Assignf_blocks_opposing_sides_on_the_same_team()
    {
        var (lobby, service) = ManualSetup();
        Assert.True(service.TryAssignSingle(lobby, 1, "2", "Scourge", out _));

        Assert.False(service.TryAssignSingle(lobby, 1, "3", "Lordaeron", out var reason));
        Assert.Contains("opposite teams", reason);

        Assert.False(service.TryAssignSingle(lobby, 1, "3", "Fel Horde", out reason));
        Assert.Contains("opposite teams", reason);
    }

    [Fact]
    public void Assignf_blocks_taking_the_other_teams_side()
    {
        var (lobby, service) = ManualSetup();
        Assert.True(service.TryAssignSingle(lobby, 1, "2", "Scourge", out _));

        Assert.False(service.TryAssignSingle(lobby, 8, "9", "Legion", out var reason));
        Assert.Contains("other team's side", reason);
    }

    [Fact]
    public void Assignf_allows_only_one_of_an_either_or_pair()
    {
        var (lobby, service) = ManualSetup();
        Assert.True(service.TryAssignSingle(lobby, 1, "2", "Dalaran", out _));

        Assert.False(service.TryAssignSingle(lobby, 1, "3", "Gilneas", out var reason));
        Assert.Contains("Only one of", reason);
    }

    [Fact]
    public void Assignf_lets_a_captain_change_a_players_faction()
    {
        var (lobby, service) = ManualSetup();
        Assert.True(service.TryAssignSingle(lobby, 1, "2", "Dalaran", out _));
        Assert.True(service.TryAssignSingle(lobby, 1, "2", "Gilneas", out _));
        Assert.Equal("Gilneas", lobby.ManualFactionAssignments[2]);
    }

    [Fact]
    public void Assignf_full_legal_7v7_locks_and_starts()
    {
        var (lobby, service) = ManualSetup();
        var a = new[] { "Scourge", "Legion", "Stormwind", "Ironforge", "Kul'tiras", "Orcish Horde", "Tauren Tribes" };
        var b = new[] { "Lordaeron", "Quel'thalas", "Gilneas", "Fel Horde", "Sunfury", "Sentinels", "Druids" };

        for (int i = 0; i < MatchFormat.TeamSize; i++)
        {
            Assert.True(service.TryAssignSingle(lobby, 1, (i + 1).ToString(), a[i], out var ra), ra);
            Assert.True(service.TryAssignSingle(lobby, 8, (i + 8).ToString(), b[i], out var rb), rb);
        }

        Assert.True(service.TryLockFactions(lobby, 1, out _));
        Assert.True(service.TryLockFactions(lobby, 8, out _));
    }
}
