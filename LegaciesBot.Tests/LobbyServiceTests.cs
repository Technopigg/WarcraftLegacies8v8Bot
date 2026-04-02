using LegaciesBot.Services;

public class LobbyServiceTests
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
    public void JoinLobby_AddsNewPlayer()
    {
        var service = CreateService();

        var p = service.JoinLobby(1);

        Assert.Single(service.CurrentLobby.Players);
        Assert.Equal((ulong)1, p.DiscordId);
    }

    [Fact]
    public void RemovePlayer_RemovesExistingPlayer()
    {
        var service = CreateService();

        service.JoinLobby(1);
        bool removed = service.RemovePlayer(1);

        Assert.True(removed);
        Assert.Empty(service.CurrentLobby.Players);
    }

    [Fact]
    public void RemovePlayer_ClearsCaptainSlots()
    {
        var service = CreateService();
        var lobby = service.CurrentLobby;
        
        service.JoinLobby(1);
        lobby.CaptainA = 1;
        lobby.CaptainB = 2;

        service.RemovePlayer(1);

        Assert.Null(lobby.CaptainA);
        Assert.Equal((ulong)2, lobby.CaptainB); 
    }

    [Fact]
    public void RemovePlayer_Fails_WhenNotInLobby()
    {
        var service = CreateService();

        bool removed = service.RemovePlayer(999);

        Assert.False(removed);
    }

    [Fact]
    public void MarkActive_UpdatesPlayer()
    {
        var service = CreateService();

        var p = service.JoinLobby(1);
        p.IsActive = false;

        bool success = service.MarkActive(1);

        Assert.True(success);
        Assert.True(p.IsActive);
    }

    [Fact]
    public void MarkActive_Fails_WhenNotInLobby()
    {
        var service = CreateService();

        bool success = service.MarkActive(999);

        Assert.False(success);
    }

    [Fact]
    public void GetLobbyMembers_ReturnsAllPlayers()
    {
        var service = CreateService();

        service.JoinLobby(1);
        service.JoinLobby(2);

        var members = service.GetLobbyMembers();

        Assert.Equal(2, members.Count);
    }

    [Fact]
    public void IsInLobby_ReturnsCorrectValue()
    {
        var service = CreateService();

        service.JoinLobby(1);

        Assert.True(service.IsInLobby(1));
        Assert.False(service.IsInLobby(999));
    }

    [Fact]
    public void UpdatePreferences_UpdatesCorrectPlayer()
    {
        var service = CreateService();

        service.JoinLobby(1);
        service.UpdatePreferences(1, new() { "A", "B" });

        var p = service.CurrentLobby.Players.First();

        Assert.Equal(2, p.FactionPreferences.Count);
    }

    [Fact]
    public void CheckAfk_RemovesExpiredPlayers_AndClearsCaptains()
    {
        var service = CreateService();
        var lobby = service.CurrentLobby;

        service.JoinLobby(1);
        lobby.CaptainA = 1;
        
        lobby.AfkPingedAt[1] = DateTime.UtcNow - TimeSpan.FromHours(1);
        service.CheckAfk();

        Assert.Empty(lobby.Players);
        Assert.Null(lobby.CaptainA);
    }
}
