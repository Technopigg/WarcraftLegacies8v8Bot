using LegaciesBot.Core;

namespace LegaciesBot.Services;

public class LobbyService
{
    private readonly List<Lobby> _lobbies = new();
    private readonly TimeSpan AfkReminderDelay = TimeSpan.FromMinutes(30);
    private readonly TimeSpan AfkKickDelay = TimeSpan.FromMinutes(10);

    public Lobby CurrentLobby => _lobbies.FirstOrDefault(l => !l.DraftStarted) ?? CreateLobby();

    private Lobby CreateLobby()
    {
        var lobby = new Lobby();
        _lobbies.Add(lobby);
        return lobby;
    }

    public Player JoinLobby(ulong discordId, string name)
    {
        var lobby = CurrentLobby;

        var player = lobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
        if (player == null)
        {
            player = new Player(discordId, name) { JoinedAt = DateTime.UtcNow };
            lobby.Players.Add(player);
        }
        else
        {
            player.IsActive = true; 
        }

        lobby.AfkPingedAt[player.DiscordId] = DateTime.UtcNow.Add(AfkReminderDelay);

        return player;
    }

    public void UpdatePreferences(ulong discordId, List<string> prefs)
    {
        foreach (var lobby in _lobbies.Where(l => !l.DraftStarted))
        {
            var player = lobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
            if (player != null)
            {
                player.FactionPreferences = prefs;
                break;
            }
        }
    }

    public void CheckAfk()
    {
        foreach (var lobby in _lobbies.Where(l => !l.DraftStarted))
        {
            var now = DateTime.UtcNow;
            foreach (var player in lobby.Players.ToList())
            {
                if (lobby.AfkPingedAt.TryGetValue(player.DiscordId, out var pingTime))
                {
                    if (now > pingTime + AfkKickDelay)
                    {
                        lobby.Players.Remove(player);
                        lobby.AfkPingedAt.Remove(player.DiscordId);
                    }
                    else if (now > pingTime)
                    {
                    }
                }
            }
        }
    }
}
