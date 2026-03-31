using System.Collections.Generic;
using System.Linq;
using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Seasons;
using Xunit;

public class EloServiceTests
{
    private Player P(ulong id, int elo, PlayerStatsService stats)
    {
        var registry = new PlayerRegistryService(null);

        var p = registry.GetOrCreate(id);
        p.Name = $"P{id}";
        p.Elo = elo;

        var s = stats.GetOrCreate(id);
        s.Elo = elo;
        stats.Update(s);

        return p;
    }

    private SeasonService CreateSeasonService()
    {
        const string path = "test_seasons.json";

        if (File.Exists(path))
            File.Delete(path);

        return new SeasonService(path);
    }

    [Fact]
    public void ApplyTeamResult_WinningTeamGainsLosingTeamLoses()
    {
        var stats = new PlayerStatsService();
        var seasons = CreateSeasonService();

        var teamA = new List<Player> { P(1, 1000, stats), P(2, 1000, stats) };
        var teamB = new List<Player> { P(3, 1000, stats), P(4, 1000, stats) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats, seasons);

        Assert.True(result[1] > 0);
        Assert.True(result[2] > 0);
        Assert.True(result[3] < 0);
        Assert.True(result[4] < 0);
    }

    [Fact]
    public void ApplyTeamResult_HigherRatedTeamGainsLess()
    {
        var stats = new PlayerStatsService();
        var seasons = CreateSeasonService();

        var teamA = new List<Player> { P(1, 1200, stats), P(2, 1200, stats) };
        var teamB = new List<Player> { P(3, 800, stats), P(4, 800, stats) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats, seasons);

        Assert.True(result[1] < 10);
        Assert.True(result[2] < 10);
    }

    [Fact]
    public void ApplyTeamResult_UpsetWinGivesMorePoints()
    {
        var stats = new PlayerStatsService();
        var seasons = CreateSeasonService();

        var teamA = new List<Player> { P(1, 800, stats), P(2, 800, stats) };
        var teamB = new List<Player> { P(3, 1200, stats), P(4, 1200, stats) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats, seasons);

        Assert.True(result[1] > 20);
        Assert.True(result[2] > 20);
    }

    [Fact]
    public void ApplyTeamResult_AllPlayersReceiveSameChangeWithinTeam()
    {
        var stats = new PlayerStatsService();
        var seasons = CreateSeasonService();

        var teamA = new List<Player> { P(1, 1000, stats), P(2, 1000, stats), P(3, 1000, stats) };
        var teamB = new List<Player> { P(4, 1000, stats), P(5, 1000, stats), P(6, 1000, stats) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats, seasons);

        var aChanges = new[] { result[1], result[2], result[3] };
        Assert.Single(aChanges.Distinct());

        var bChanges = new[] { result[4], result[5], result[6] };
        Assert.Single(bChanges.Distinct());
    }

    [Fact]
    public void ApplyTeamResult_ReturnsDictionaryForAllPlayers()
    {
        var stats = new PlayerStatsService();
        var seasons = CreateSeasonService();

        var teamA = new List<Player> { P(1, 1000, stats), P(2, 1000, stats) };
        var teamB = new List<Player> { P(3, 1000, stats), P(4, 1000, stats) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats, seasons);

        Assert.Equal(4, result.Count);
        Assert.Contains(1UL, result.Keys);
        Assert.Contains(2UL, result.Keys);
        Assert.Contains(3UL, result.Keys);
        Assert.Contains(4UL, result.Keys);
    }
}
