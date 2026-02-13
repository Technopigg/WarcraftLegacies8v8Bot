using LegaciesBot.Services;


public class LobbyServiceTests
{
    [Fact]
    public void JoinLobby_AddsNewPlayer()
    {
        var service = new LobbyService();

        var p = service.JoinLobby(1, "Alice");

        Assert.Single(service.CurrentLobby.Players);
        Assert.Equal((ulong)1, p.DiscordId);
    }

    [Fact]
    public void RemovePlayer_RemovesExistingPlayer()
    {
        var service = new LobbyService();

        service.JoinLobby(1, "Alice");
        bool removed = service.RemovePlayer(1);

        Assert.True(removed);
        Assert.Empty(service.CurrentLobby.Players);
    }

    [Fact]
    public void RemovePlayer_Fails_WhenNotInLobby()
    {
        var service = new LobbyService();

        bool removed = service.RemovePlayer(999);

        Assert.False(removed);
    }

    [Fact]
    public void MarkActive_UpdatesPlayer()
    {
        var service = new LobbyService();

        var p = service.JoinLobby(1, "Alice");
        p.IsActive = false;

        bool success = service.MarkActive(1);

        Assert.True(success);
        Assert.True(p.IsActive);
    }

    [Fact]
    public void MarkActive_Fails_WhenNotInLobby()
    {
        var service = new LobbyService();

        bool success = service.MarkActive(999);

        Assert.False(success);
    }

    [Fact]
    public void GetLobbyMembers_ReturnsAllPlayers()
    {
        var service = new LobbyService();

        service.JoinLobby(1, "Alice");
        service.JoinLobby(2, "Bob");

        var members = service.GetLobbyMembers();

        Assert.Equal(2, members.Count);
    }

    [Fact]
    public void IsInLobby_ReturnsCorrectValue()
    {
        var service = new LobbyService();

        service.JoinLobby(1, "Alice");

        Assert.True(service.IsInLobby(1));
        Assert.False(service.IsInLobby(999));
    }

    [Fact]
    public void UpdatePreferences_UpdatesCorrectPlayer()
    {
        var service = new LobbyService();

        service.JoinLobby(1, "Alice");
        service.UpdatePreferences(1, new() { "A", "B" });

        var p = service.CurrentLobby.Players.First();

        Assert.Equal(2, p.FactionPreferences.Count);
    }

    [Fact]
    public void CheckAfk_RemovesExpiredPlayers()
    {
        var service = new LobbyService();

        var p = service.JoinLobby(1, "Alice");

        service.CurrentLobby.AfkPingedAt[1] = DateTime.UtcNow - TimeSpan.FromHours(1);

        service.CheckAfk();

        Assert.Empty(service.CurrentLobby.Players);
    }
}