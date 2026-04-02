using LegaciesBot.Services;

public class LobbyLockingTests
{
    private GameService CreateGameService()
    {
        var rng = new Random(12345);
        var client = new DummyGatewayClient();
        var history = new MatchHistoryAdapter(new MatchHistoryService());
        var elo = new EloStub();
        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
        var defaults = new DefaultPreferencesStub();
        var factionRegistry = new FactionRegistryStub();

        return new GameService(
            client,
            history,
            elo,
            factionAssign,
            factionRegistry,
            defaults,
            rng
        );
    }

    private LobbyService CreateService()
    {
        var registry = new PlayerRegistryService(null);
        var gameService = CreateGameService();
        return new LobbyService(registry, gameService);
    }

    [Fact]
    public void Lobby_Locks_At_16_Players_And_Spawns_New_Lobby()
    {
        var service = CreateService();

        for (int i = 1; i <= 16; i++)
            service.JoinLobby((ulong)i);

        var firstLobby = service.CurrentLobby;
        Assert.True(firstLobby.IsLocked);
        Assert.Equal(16, firstLobby.Players.Count);

        service.JoinLobby(999);

        var secondLobby = service.CurrentLobby;
        Assert.NotSame(firstLobby, secondLobby);
        Assert.False(secondLobby.IsLocked);
        Assert.Single(secondLobby.Players);
        Assert.Equal((ulong)999, secondLobby.Players[0].DiscordId);
    }

    [Fact]
    public void RemovePlayer_Fails_When_Lobby_Locked()
    {
        var service = CreateService();

        for (int i = 1; i <= 16; i++)
            service.JoinLobby((ulong)i);

        var lobby = service.CurrentLobby;
        Assert.True(lobby.IsLocked);

        var removed = service.RemovePlayer(1);
        Assert.False(removed);
        Assert.Equal(16, lobby.Players.Count);
    }

    [Fact]
    public void CheckAfk_DoesNothing_When_Lobby_Locked()
    {
        var service = CreateService();

        for (int i = 1; i <= 16; i++)
            service.JoinLobby((ulong)i);

        var lobby = service.CurrentLobby;
        Assert.True(lobby.IsLocked);

        var id = lobby.Players[0].DiscordId;
        lobby.AfkPingedAt[id] = DateTime.UtcNow - TimeSpan.FromHours(1);

        service.CheckAfk();

        Assert.Equal(16, lobby.Players.Count);
    }
}
