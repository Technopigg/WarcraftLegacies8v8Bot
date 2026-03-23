using System;
using System.IO;
using System.Linq;
using LegaciesBot.Core;
using LegaciesBot.Services;
using Xunit;

public class PlayerRegistryServiceTests
{
    private PlayerRegistryService CreateRegistry()
    {
        return new PlayerRegistryService(Path.GetTempFileName());
    }

    [Fact]
    public void GetPlayer_ReturnsNull_WhenNotRegistered()
    {
        var registry = CreateRegistry();

        var result = registry.GetPlayer(999);

        Assert.Null(result);
    }

    [Fact]
    public void GetOrCreate_CreatesNewPlayer()
    {
        var registry = CreateRegistry();

        var p = registry.GetOrCreate(1);
        p.Name = "Techno";

        var registry2 = new PlayerRegistryService(registry.FilePath);
        var loaded = registry2.GetPlayer(1);

        Assert.NotNull(p);
        Assert.Equal((ulong)1, p.DiscordId);
        Assert.Equal("Techno", p.DisplayName());
        Assert.True(p.IsActive);
        Assert.True(p.JoinedAt <= DateTime.UtcNow);
        Assert.Equal("Techno", loaded.Name);
    }

    [Fact]
    public void GetOrCreate_ReturnsExistingPlayer_WhenAlreadyRegistered()
    {
        var registry = CreateRegistry();

        var p1 = registry.GetOrCreate(2);
        p1.Name = "Nick";

        var registry2 = new PlayerRegistryService(registry.FilePath);
        var p2 = registry2.GetOrCreate(2);

        Assert.Equal("Nick", p2.Name);
        Assert.Single(registry2.GetAllPlayers());
    }

    [Fact]
    public void IsRegistered_WorksCorrectly()
    {
        var registry = CreateRegistry();

        var p = registry.GetOrCreate(3);
        p.Name = "Vamp";

        var registry2 = new PlayerRegistryService(registry.FilePath);

        Assert.True(registry2.IsRegistered(3));
        Assert.False(registry2.IsRegistered(4));
    }

    [Fact]
    public void GetAllPlayers_ReturnsAllRegisteredPlayers()
    {
        var registry = CreateRegistry();

        registry.GetOrCreate(1).Name = "Techno";
        registry.GetOrCreate(2).Name = "Nick";
        registry.GetOrCreate(3).Name = "Vamp";
        registry.GetOrCreate(4).Name = "Madsen";
        registry.GetOrCreate(5).Name = "Yak";

        var registry2 = new PlayerRegistryService(registry.FilePath);
        var all = registry2.GetAllPlayers();

        Assert.Equal(5, all.Count);
        Assert.Contains(all, p => p.DisplayName() == "Techno");
        Assert.Contains(all, p => p.DisplayName() == "Nick");
        Assert.Contains(all, p => p.DisplayName() == "Vamp");
        Assert.Contains(all, p => p.DisplayName() == "Madsen");
        Assert.Contains(all, p => p.DisplayName() == "Yak");
    }

    [Fact]
    public void Registry_PersistsSinglePlayer()
    {
        string path = Path.GetTempFileName();

        {
            var registry = new PlayerRegistryService(path);
            var p = registry.GetOrCreate(1);
            p.Name = "Techno";
        }

        var registry2 = new PlayerRegistryService(path);
        var loaded = registry2.GetPlayer(1);

        Assert.NotNull(loaded);
        Assert.Equal("Techno", loaded.Name);
    }

    [Fact]
    public void Registry_PersistsMultiplePlayers()
    {
        string path = Path.GetTempFileName();

        {
            var registry = new PlayerRegistryService(path);
            registry.GetOrCreate(1).Name = "Techno";
            registry.GetOrCreate(2).Name = "Nick";
            registry.GetOrCreate(3).Name = "Vamp";
        }

        var registry2 = new PlayerRegistryService(path);
        var all = registry2.GetAllPlayers();

        Assert.Equal(3, all.Count);
        Assert.NotNull(registry2.GetPlayer(1));
        Assert.NotNull(registry2.GetPlayer(2));
        Assert.NotNull(registry2.GetPlayer(3));
    }
}