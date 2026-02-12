using System.Text.Json;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class PlayerRegistryService
    {
        private const string FilePath = "players.json";

        private readonly Dictionary<ulong, Player> _players = new();

        public PlayerRegistryService()
        {
            Load();
        }

        public Player? GetPlayer(ulong discordId)
        {
            return _players.TryGetValue(discordId, out var p) ? p : null;
        }

        public bool IsRegistered(ulong discordId)
        {
            return _players.ContainsKey(discordId);
        }

        public Player RegisterPlayer(ulong discordId, string name)
        {
            if (_players.ContainsKey(discordId))
                return _players[discordId];

            var player = new Player(discordId, name)
            {
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _players[discordId] = player;
            Save();

            return player;
        }

        public IReadOnlyCollection<Player> GetAllPlayers()
        {
            return _players.Values;
        }

        private void Load()
        {
            if (!File.Exists(FilePath))
                return;

            var json = File.ReadAllText(FilePath);
            var list = JsonSerializer.Deserialize<List<Player>>(json);

            if (list != null)
            {
                foreach (var p in list)
                    _players[p.DiscordId] = p;
            }
        }

        private void Save()
        {
            var list = _players.Values.ToList();

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }
    }
}