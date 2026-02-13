using LegaciesBot.Core;
using LegaciesBot.Services;

public class EloServiceTests
{
    private Player P(ulong id, int elo)
        => new Player(id, $"P{id}", elo);

    [Fact]
    public void ApplyTeamResult_WinningTeamGainsLosingTeamLoses()
    {
        var stats = new PlayerStatsService();

        var teamA = new List<Player> { P(1, 1000), P(2, 1000) };
        var teamB = new List<Player> { P(3, 1000), P(4, 1000) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats);

        Assert.True(result[1] > 0);
        Assert.True(result[2] > 0);
        Assert.True(result[3] < 0);
        Assert.True(result[4] < 0);
    }

    [Fact]
    public void ApplyTeamResult_HigherRatedTeamGainsLess()
    {
        var stats = new PlayerStatsService();

        var teamA = new List<Player> { P(1, 1200), P(2, 1200) };
        var teamB = new List<Player> { P(3, 800), P(4, 800) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats);

        Assert.True(result[1] < 10);
        Assert.True(result[2] < 10);
    }

    [Fact]
    public void ApplyTeamResult_UpsetWinGivesMorePoints()
    {
        var stats = new PlayerStatsService();

        var teamA = new List<Player> { P(1, 800), P(2, 800) };
        var teamB = new List<Player> { P(3, 1200), P(4, 1200) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats);

        Assert.True(result[1] > 20);
        Assert.True(result[2] > 20);
    }

    [Fact]
    public void ApplyTeamResult_AllPlayersReceiveSameChangeWithinTeam()
    {
        var stats = new PlayerStatsService();

        var teamA = new List<Player> { P(1, 1000), P(2, 1000), P(3, 1000) };
        var teamB = new List<Player> { P(4, 1000), P(5, 1000), P(6, 1000) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats);

        var aChanges = new[] { result[1], result[2], result[3] };
        Assert.True(aChanges.Distinct().Count() == 1);

        var bChanges = new[] { result[4], result[5], result[6] };
        Assert.True(bChanges.Distinct().Count() == 1);
    }

    [Fact]
    public void ApplyTeamResult_ReturnsDictionaryForAllPlayers()
    {
        var stats = new PlayerStatsService();

        var teamA = new List<Player> { P(1, 1000), P(2, 1000) };
        var teamB = new List<Player> { P(3, 1000), P(4, 1000) };

        var result = EloService.ApplyTeamResult(teamA, teamB, true, stats);

        Assert.Equal(4, result.Count);
        Assert.Contains(1UL, result.Keys);
        Assert.Contains(2UL, result.Keys);
        Assert.Contains(3UL, result.Keys);
        Assert.Contains(4UL, result.Keys);
    }
}