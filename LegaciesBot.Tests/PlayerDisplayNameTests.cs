using System.IO;
using LegaciesBot.Core;
using LegaciesBot.Services;
using Xunit;

public class PlayerDisplayNameTests
{
    private PlayerRegistryService CreateRegistry()
        => new PlayerRegistryService(Path.GetTempFileName());

    [Fact]
    public void DisplayName_NeverReturnsRawDiscordId()
    {
        // A record created before the fix stored Name == discordId.ToString().
        var player = new Player(738188830892359733UL, "738188830892359733");

        // With no live username, we must not leak the id.
        Assert.Equal("Unknown player", player.DisplayName());

        // With a live username available, we use it.
        Assert.Equal("Boggywoggy", player.DisplayName("Boggywoggy"));
    }

    [Fact]
    public void DisplayName_PrefersNickname_ThenName()
    {
        var player = new Player(42UL, "RealName") { Nickname = "Nick" };
        Assert.Equal("Nick", player.DisplayName("live"));

        player.Nickname = null;
        Assert.Equal("RealName", player.DisplayName("live"));
    }

    [Fact]
    public void GetOrCreate_StoresUsername_ForNewPlayer()
    {
        var registry = CreateRegistry();

        var p = registry.GetOrCreate(100UL, "Konan");

        Assert.Equal("Konan", p.Name);
        Assert.Equal("Konan", p.DisplayName());
    }

    [Fact]
    public void GetOrCreate_RepairsStaleName_WhenUsernameKnown()
    {
        var registry = CreateRegistry();

        // Simulate a legacy record whose Name is the numeric id.
        var stale = registry.GetOrCreate(500UL);
        Assert.Equal("500", stale.Name);

        // Joining again, now with a username, repairs it.
        var repaired = registry.GetOrCreate(500UL, "Helsac");

        Assert.Equal("Helsac", repaired.Name);
        Assert.Equal("Helsac", repaired.DisplayName());
    }

    [Fact]
    public void GetOrCreate_DoesNotOverwriteRealName_WithUsername()
    {
        var registry = CreateRegistry();

        var p = registry.GetOrCreate(600UL, "OriginalName");
        registry.GetOrCreate(600UL, "DifferentLogin");

        Assert.Equal("OriginalName", p.Name);
    }
}
