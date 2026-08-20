using LegaciesBot.Core;
using LegaciesBot.Services;

public class LobbyBoardServiceTests
{
    private static Lobby LobbyWith(params (ulong id, string name)[] players)
    {
        var lobby = new Lobby();
        foreach (var (id, name) in players)
            lobby.Players.Add(new Player(id, name));
        return lobby;
    }

    [Fact]
    public void Empty_lobby_renders_a_join_hint()
    {
        var embed = LobbyBoardService.Render(new Lobby());
        Assert.Contains("Live Lobby", embed.Title);
        Assert.Contains("!j", embed.Description);
    }

    [Fact]
    public void Populated_lobby_lists_every_player_and_the_count()
    {
        var embed = LobbyBoardService.Render(LobbyWith((1, "Alice"), (2, "Bob")));
        Assert.Contains("2/16", embed.Title);
        Assert.Contains("Alice", embed.Description);
        Assert.Contains("Bob", embed.Description);
    }

    [Fact]
    public void Signature_is_stable_for_the_same_roster_and_changes_when_it_differs()
    {
        var a = LobbyWith((1, "Alice"));
        var same = LobbyWith((1, "Alice"));
        var bigger = LobbyWith((1, "Alice"), (2, "Bob"));

        Assert.Equal(LobbyBoardService.Signature(a), LobbyBoardService.Signature(same));
        Assert.NotEqual(LobbyBoardService.Signature(a), LobbyBoardService.Signature(bigger));
    }

    [Fact]
    public void Locked_lobby_signature_differs_from_open_one()
    {
        var open = LobbyWith((1, "Alice"));
        var locked = LobbyWith((1, "Alice"));
        locked.IsLocked = true;
        Assert.NotEqual(LobbyBoardService.Signature(open), LobbyBoardService.Signature(locked));
    }

    [Fact]
    public void Board_is_disabled_when_channel_id_is_zero()
    {
        var svc = new LobbyBoardService(null!, 0, () => new Lobby());
        Assert.False(svc.Enabled);
    }
}
