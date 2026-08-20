using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public interface ILobbyService
    {
        Lobby CurrentLobby { get; }
        Player JoinLobby(ulong discordId, string? username = null);
        bool RemovePlayer(ulong discordId);
        bool MarkActive(ulong discordId);
        // Re-arm the inactivity timer for a player already in the lobby (the "Type !j to stay" keep-alive).
        bool KeepAlive(ulong discordId);
        List<Player> GetLobbyMembers();
        bool IsInLobby(ulong discordId);
        void UpdatePreferences(ulong discordId, List<string> prefs);
        IReadOnlyList<LobbyAfkNotice> CheckAfk();
    }
}