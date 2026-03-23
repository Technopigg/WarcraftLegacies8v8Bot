using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public interface ILobbyService
    {
        Lobby CurrentLobby { get; }
        Player JoinLobby(ulong discordId);
        bool RemovePlayer(ulong discordId);
        bool MarkActive(ulong discordId);
        List<Player> GetLobbyMembers();
        bool IsInLobby(ulong discordId);
        void UpdatePreferences(ulong discordId, List<string> prefs);
        void CheckAfk();
    }
}