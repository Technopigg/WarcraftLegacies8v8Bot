using System;
using System.IO;
using System.Linq;
using LegaciesBot.Services;
using Xunit;

public class PlayerRegistryServiceTests
{
    private void ResetFile()
    {
        if (File.Exists("players.json"))
            File.Delete("players.json");
    }

    [Fact]
    public void GetPlayer_ReturnsNull_WhenNotRegistered()
    {
        ResetFile();
        var registry = new PlayerRegistryService();

        var result = registry.GetPlayer(999);

        Assert.Null(result);
    }

    [Fact]
    public void RegisterPlayer_CreatesNewPlayer()
    {
        ResetFile();
        var registry = new PlayerRegistryService();

        var p = registry.RegisterPlayer(1, "Techno");

        Assert.NotNull(p);
        Assert.Equal((ulong)1, p.DiscordId);
        Assert.Equal("Techno", p.Name);
        Assert.True(p.IsActive);
        Assert.True(p.JoinedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void RegisterPlayer_ReturnsExistingPlayer_WhenAlreadyRegistered()
    {
        ResetFile();
        var registry = new PlayerRegistryService();

        var p1 = registry.RegisterPlayer(2, "Nick");
        var p2 = registry.RegisterPlayer(2, "Nick");

        Assert.Same(p1, p2);
        Assert.Single(registry.GetAllPlayers());
    }

    [Fact]
    public void IsRegistered_WorksCorrectly()
    {
        ResetFile();
        var registry = new PlayerRegistryService();

        registry.RegisterPlayer(3, "Vamp");

        Assert.True(registry.IsRegistered(3));
        Assert.False(registry.IsRegistered(4));
    }

    [Fact]
    public void GetAllPlayers_ReturnsAllRegisteredPlayers()
    {
        ResetFile();
        var registry = new PlayerRegistryService();

        registry.RegisterPlayer(1, "Techno");
        registry.RegisterPlayer(2, "Nick");
        registry.RegisterPlayer(3, "Vamp");
        registry.RegisterPlayer(4, "Madsen");
        registry.RegisterPlayer(5, "Yak");

        var all = registry.GetAllPlayers();

        Assert.Equal(5, all.Count);
        Assert.Contains(all, p => p.Name == "Techno");
        Assert.Contains(all, p => p.Name == "Nick");
        Assert.Contains(all, p => p.Name == "Vamp");
        Assert.Contains(all, p => p.Name == "Madsen");
        Assert.Contains(all, p => p.Name == "Yak");
    }

    [Fact]
    public void Registry_PersistsSinglePlayer()
    {
        ResetFile();

        {
            var registry = new PlayerRegistryService();
            registry.RegisterPlayer(1, "Techno");
        }

        var registry2 = new PlayerRegistryService();
        var loaded = registry2.GetPlayer(1);

        Assert.NotNull(loaded);
        Assert.Equal("Techno", loaded.Name);
    }

    [Fact]
    public void Registry_PersistsMultiplePlayers()
    {
        ResetFile();

        {
            var registry = new PlayerRegistryService();
            registry.RegisterPlayer(1, "Techno");
            registry.RegisterPlayer(2, "Nick");
            registry.RegisterPlayer(3, "Vamp");
        }

        var registry2 = new PlayerRegistryService();
        var all = registry2.GetAllPlayers();

        Assert.Equal(3, all.Count);
        Assert.NotNull(registry2.GetPlayer(1));
        Assert.NotNull(registry2.GetPlayer(2));
        Assert.NotNull(registry2.GetPlayer(3));
    }
}