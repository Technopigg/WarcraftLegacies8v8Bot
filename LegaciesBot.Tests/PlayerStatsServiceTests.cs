using System;
using System.IO;
using LegaciesBot.Services;
using Xunit;

public class PlayerStatsServiceTests
{
    private string CreateTempStatsFile(string? json = null)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, json ?? "[]");
        return path;
    }

    [Fact]
    public void GetOrCreate_CreatesNewStats_WhenNotFound()
    {
        string file = CreateTempStatsFile();
        var service = new PlayerStatsService(file);

        var stats = service.GetOrCreate(1);

        Assert.Equal((ulong)1, stats.DiscordId);
        Assert.Equal(800, stats.Elo);
    }

    [Fact]
    public void GetOrCreate_LoadsExistingStats()
    {
        string file = CreateTempStatsFile(@"[
            { ""DiscordId"": 5, ""Elo"": 800 }
        ]");

        var service = new PlayerStatsService(file);

        var stats = service.GetOrCreate(5);

        Assert.Equal((ulong)5, stats.DiscordId);
        Assert.Equal(800, stats.Elo);
    }

    [Fact]
    public void Update_SavesChangesToFile()
    {
        string file = CreateTempStatsFile();
        var service = new PlayerStatsService(file);

        var stats = service.GetOrCreate(1);
        stats.Elo = 1337;

        service.Update(stats);

        string json = File.ReadAllText(file);
        Assert.Contains("1337", json);
    }

    [Fact]
    public void MultipleUsers_AreStoredIndependently()
    {
        string file = CreateTempStatsFile();
        var service = new PlayerStatsService(file);

        var s1 = service.GetOrCreate(1);
        var s2 = service.GetOrCreate(2);

        s1.Elo = 900;
        s2.Elo = 1200;

        service.Update(s1);
        service.Update(s2);

        var loaded = new PlayerStatsService(file);

        Assert.Equal(900, loaded.GetOrCreate(1).Elo);
        Assert.Equal(1200, loaded.GetOrCreate(2).Elo);
    }

    [Fact]
    public void Save_WritesIndentedJson()
    {
        string file = CreateTempStatsFile();
        var service = new PlayerStatsService(file);

        service.GetOrCreate(1);

        string json = File.ReadAllText(file);

        Assert.Contains("\n", json);
    }

    [Fact]
    public void GetAll_ReturnsAllStats()
    {
        string file = CreateTempStatsFile();
        var service = new PlayerStatsService(file);

        service.GetOrCreate(1);
        service.GetOrCreate(2);

        var all = service.GetAll();

        Assert.Equal(2, all.Count);
    }
}