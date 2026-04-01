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

        public Lobby CurrentLobby
        {
            get
            {
                if (_lobbies.Count == 0)
                    return CreateLobby();

                return _lobbies.Last();
            }
        }

        private Lobby CreateLobby()
        {
            var lobby = new Lobby();
            _lobbies.Add(lobby);
            return lobby;
        }

        public Player JoinLobby(ulong discordId)
        {
            var lobby = CurrentLobby;

            if (lobby.IsLocked || lobby.Players.Count >= 16)
                lobby = CreateLobby();

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

            if (lobby.Players.Count >= 16)
                lobby.IsLocked = true;

            return player;
        }

        public bool RemovePlayer(ulong discordId)
        {
            var lobby = CurrentLobby;

            var player = lobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
            if (player == null || lobby.IsLocked)
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
            var lobby = CurrentLobby;

            var player = lobby.Players.FirstOrDefault(p => p.DiscordId == discordId);
            if (player != null)
                player.FactionPreferences = prefs;
        }

        public void CheckAfk()
        {
            var lobby = CurrentLobby;

            if (lobby.IsLocked)
                return;

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
