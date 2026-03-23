using LegaciesBot.Core;
using LegaciesBot.Services;

namespace LegaciesBot.Tests;

[Collection("NicknameTests")]
public class NicknameServiceTests
{
    private PlayerRegistryService CreateRegistry()
    {
        var registry = new PlayerRegistryService(null);

        var p1 = registry.GetOrCreate(1, "NicknameTest_Alice");
        p1.Nickname = "ali_test";

        var p2 = registry.GetOrCreate(2, "NicknameTest_Bob");
        p2.Nickname = "bobby_test";

        return registry;
    }

    [Fact]
    public void ResolvePlayerId_ByNickname_Works()
    {
        var registry = CreateRegistry();
        var service = new NicknameService(registry);

        var id = service.ResolvePlayerId("ali_test");

        Assert.Equal((ulong)1, id);
    }

    [Fact]
    public void ResolvePlayerId_ByName_Works()
    {
        var registry = CreateRegistry();
        var service = new NicknameService(registry);

        var id = service.ResolvePlayerId("NicknameTest_Bob");

        Assert.Equal((ulong)2, id);
    }

    [Fact]
    public void ResolvePlayerId_ByMention_Works()
    {
        var registry = CreateRegistry();
        var service = new NicknameService(registry);

        var id = service.ResolvePlayerId("<@2>");

        Assert.Equal((ulong)2, id);
    }

    [Fact]
    public void ResolvePlayerId_ByRawId_Works()
    {
        var registry = CreateRegistry();
        var service = new NicknameService(registry);

        var id = service.ResolvePlayerId("1");

        Assert.Equal((ulong)1, id);
    }

    [Fact]
    public void ResolvePlayerId_Unknown_ReturnsNull()
    {
        var registry = CreateRegistry();
        var service = new NicknameService(registry);

        var id = service.ResolvePlayerId("unknown");

        Assert.Null(id);
    }
    
    public NicknameServiceTests()
    {
        PlayerRegistryService.ResetForTests();
    }
    [Fact]
    public void ResolvePlayerId_Empty_ReturnsNull()
    {
        var registry = CreateRegistry();
        var service = new NicknameService(registry);

        var id = service.ResolvePlayerId("");

        Assert.Null(id);
    }
}

[CollectionDefinition("NicknameTests", DisableParallelization = true)]
public class NicknameTestCollection { }