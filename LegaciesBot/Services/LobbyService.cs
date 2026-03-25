using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class LobbyService : ILobbyService
    {
        private readonly List<Lobby> _lobbies = new();
        private readonly TimeSpan AfkReminderDelay = TimeSpan.FromMinutes(30);
        private readonly TimeSpan AfkKickDelay = TimeSpan.FromMinutes(10);

        private readonly PlayerRegistryService _playerRegistry;

        public LobbyService(PlayerRegistryService playerRegistry)
        {
            _playerRegistry = playerRegistry;
        }

        public Lobby CurrentLobby =>
            _lobbies.FirstOrDefault(l => !l.DraftStarted) ?? CreateLobby();

        private Lobby CreateLobby()
        {
            var lobby = new Lobby();
            _lobbies.Add(lobby);
            return lobby;
        }

        public Player JoinLobby(ulong discordId)
        {
            var lobby = CurrentLobby;

            var player = lobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
            if (player == null)
            {
                player = _playerRegistry.GetOrCreate(discordId);
                player.JoinedAt = DateTime.UtcNow;

                lobby.Players.Add(player);
            }
            else
            {
                player.IsActive = true;
            }

            lobby.AfkPingedAt[player.DiscordId] =
                DateTime.UtcNow.Add(AfkReminderDelay);

            return player;
        }

        public bool RemovePlayer(ulong discordId)
        {
            var lobby = CurrentLobby;

            var player = lobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
            if (player == null || lobby.DraftStarted)
                return false;

            lobby.Players.Remove(player);
            lobby.AfkPingedAt.Remove(discordId);

            if (lobby.CaptainA == discordId) lobby.CaptainA = null;
            if (lobby.CaptainB == discordId) lobby.CaptainB = null;

            return true;
        }

        public bool MarkActive(ulong discordId)
        {
            var player = CurrentLobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
            if (player == null)
                return false;

            player.IsActive = true;
            player.JoinedAt = DateTime.UtcNow;
            return true;
        }

        public List<Player> GetLobbyMembers()
        {
            return CurrentLobby.Players.ToList();
        }

        public bool IsInLobby(ulong discordId)
        {
            return CurrentLobby.Players.Any(p => p.DiscordId == discordId);
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

                            if (lobby.CaptainA == player.DiscordId) lobby.CaptainA = null;
                            if (lobby.CaptainB == player.DiscordId) lobby.CaptainB = null;
                        }
                    }
                }
            }
        }
    }
}
