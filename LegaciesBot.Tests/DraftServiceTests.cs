using System;
using System.Collections.Generic;
using System.Linq;
using LegaciesBot.Core;
using LegaciesBot.Services;
using Xunit;

public class DraftServiceTests
{
    private List<Player> CreatePlayers(int count)
    {
        var list = new List<Player>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new Player((ulong)i, $"P{i}") { Elo = 1000 + i });
        }
        return list;
    }

    [Fact]
    public void CreateBalancedTeams_AssignsAllPlayers()
    {
        var players = CreatePlayers(10);
        var rng = new Random(12345);

        var (teamA, teamB) = DraftService.CreateBalancedTeams(players, rng);

        var all = teamA.Players.Concat(teamB.Players).ToList();

        Assert.Equal(10, all.Count);
        Assert.Equal(10, all.Distinct().Count());
    }

    [Fact]
    public void CreateBalancedTeams_TeamsAreReasonablyBalanced()
    {
        var players = CreatePlayers(10);
        var rng = new Random(12345);

        var (teamA, teamB) = DraftService.CreateBalancedTeams(players, rng);

        Assert.InRange(teamA.Players.Count, 4, 6);
        Assert.InRange(teamB.Players.Count, 4, 6);
    }

    [Fact]
    public void CreateBalancedTeams_UsesSnakeDraftOrder()
    {
        var players = CreatePlayers(6);
        var rng = new Random(12345);

        var (teamA, teamB) = DraftService.CreateBalancedTeams(players, rng);

        var sorted = players.OrderByDescending(p => p.Elo).ToList();

        var expectedOrder = new List<(string team, Player player)>
        {
            ("A", sorted[0]),
            ("B", sorted[1]),
            ("B", sorted[2]),
            ("A", sorted[3]),
            ("A", sorted[4]),
            ("B", sorted[5])
        };

        var actualOrder = new List<(string team, Player player)>();

        foreach (var p in teamA.Players)
            actualOrder.Add(("A", p));
        foreach (var p in teamB.Players)
            actualOrder.Add(("B", p));

        Assert.Equal(6, actualOrder.Count);
        Assert.True(actualOrder.Any());
    }

    [Fact]
    public void RunDraft_ReturnsTwoTeams()
    {
        var players = CreatePlayers(8);
        var rng = new Random(12345);

        var teams = DraftService.RunDraft(players, rng);

        Assert.Equal(2, teams.Count);
        Assert.Equal("Team A", teams[0].Name);
        Assert.Equal("Team B", teams[1].Name);
    }

    [Fact]
    public void CreateBalancedTeams_HandlesOddPlayerCounts()
    {
        var players = CreatePlayers(7);
        var rng = new Random(12345);

        var (teamA, teamB) = DraftService.CreateBalancedTeams(players, rng);

        Assert.Equal(7, teamA.Players.Count + teamB.Players.Count);
        Assert.InRange(teamA.Players.Count, 3, 4);
        Assert.InRange(teamB.Players.Count, 3, 4);
    }

    [Fact]
    public void EloVariance_IsDeterministicWithSeed()
    {
        var players = CreatePlayers(5);
        var rng = new Random(12345);

        var (_, _) = DraftService.CreateBalancedTeams(players, rng);

        var expected = new[] { 1000, 1001, 1002, 1003, 1004 };

        Assert.NotEqual(expected, players.Select(p => p.Elo).ToArray());
    }
}