using LegaciesBot.Services;
using Xunit;

public class NicknameTests
{
    [Fact]
    public void Nickname_Lookup_Works()
    {
        var reg = new PlayerRegistryService(null);
        reg.RegisterPlayer(1, "Boggywoggy");
        reg.SetNickname(1, "Boggy");

        var p1 = reg.Resolve("Boggy");
        var p2 = reg.FindByNameOrNickname("Boggywoggy");

        Assert.Equal(1UL, p1.DiscordId);
        Assert.Equal(1UL, p2.DiscordId);
    }
}