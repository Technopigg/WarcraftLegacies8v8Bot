using LegaciesBot.Core;
using LegaciesBot.Services;

public class MatchHistoryServiceTests
{
    private void ResetFile()
    {
        if (File.Exists("match_history.json"))
            File.Delete("match_history.json");
    }

    private Game CreateGame()
    {
        var teamA = new Team("A");
        var teamB = new Team("B");

        for (int i = 1; i <= 2; i++)
        {
            var p = new Player((ulong)i, $"P{i}", 800);
            p.AssignedFaction = $"Faction{i}";
            teamA.Players.Add(p);
            teamA.AssignedFactions.Add(new Faction($"Faction{i}", TeamGroup.NorthAlliance));
        }

        for (int i = 3; i <= 4; i++)
        {
            var p = new Player((ulong)i, $"P{i}", 800);
            p.AssignedFaction = $"Faction{i}";
            teamB.Players.Add(p);
            teamB.AssignedFactions.Add(new Faction($"Faction{i}", TeamGroup.SouthAlliance));
        }

        return new Game
        {
            Id = 1,
            TeamA = teamA,
            TeamB = teamB,
            Lobby = new Lobby()
        };
    }

    [Fact]
    public void LoadsEmptyHistory_WhenFileIsEmpty()
    {
        ResetFile();
        File.WriteAllText("match_history.json", "[]");

        var service = new MatchHistoryService();

        Assert.Empty(service.History);
    }

    [Fact]
    public void RecordMatch_AddsEntryToHistory()
    {
        ResetFile();
        var service = new MatchHistoryService();

        var game = CreateGame();
        var changes = new Dictionary<ulong, int>
        {
            [1] = 10,
            [2] = -5,
            [3] = 0,
            [4] = 0
        };

        service.RecordMatch(game, 10, 5, changes);

        Assert.Single(service.History);
        var record = service.History[0];

        Assert.Equal(1, record.GameId);
        Assert.Equal(10, record.ScoreA);
        Assert.Equal(5, record.ScoreB);
        Assert.Equal(2, record.TeamA.Count);
        Assert.Equal(2, record.TeamB.Count);
    }

    [Fact]
    public void RecordMatch_SavesFactionForEachPlayer()
    {
        ResetFile();
        var service = new MatchHistoryService();

        var game = CreateGame();
        var changes = new Dictionary<ulong, int>
        {
            [1] = 10,
            [2] = -5,
            [3] = 0,
            [4] = 0
        };

        service.RecordMatch(game, 10, 5, changes);

        var record = service.History[0];

        Assert.Equal("Faction1", record.TeamA[0].Faction);
        Assert.Equal("Faction2", record.TeamA[1].Faction);
        Assert.Equal("Faction3", record.TeamB[0].Faction);
        Assert.Equal("Faction4", record.TeamB[1].Faction);
    }

    [Fact]
    public void SaveAndReload_PreservesHistory()
    {
        ResetFile();
        var service = new MatchHistoryService();

        var game = CreateGame();
        var changes = new Dictionary<ulong, int>
        {
            [1] = 10,
            [2] = -5,
            [3] = 0,
            [4] = 0
        };

        service.RecordMatch(game, 10, 5, changes);

        var loaded = new MatchHistoryService();

        Assert.Single(loaded.History);
        Assert.Equal(1, loaded.History[0].GameId);
    }

    [Fact]
    public void LoadsOldJson_WithoutFactionField()
    {
        ResetFile();

        string oldJson = @"
        [
            {
                ""GameId"": 1,
                ""Timestamp"": ""2024-01-01T00:00:00Z"",
                ""ScoreA"": 10,
                ""ScoreB"": 5,
                ""TeamA"": [
                    { ""DiscordId"": 1, ""Name"": ""P1"", ""EloChange"": 10 }
                ],
                ""TeamB"": [
                    { ""DiscordId"": 2, ""Name"": ""P2"", ""EloChange"": -5 }
                ]
            }
        ]";

        File.WriteAllText("match_history.json", oldJson);

        var service = new MatchHistoryService();

        Assert.Single(service.History);
        Assert.Null(service.History[0].TeamA[0].Faction);
        Assert.Null(service.History[0].TeamB[0].Faction);
    }
}