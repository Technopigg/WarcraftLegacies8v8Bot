using System.Text.Json;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class PlayerStatsService
    {
        private readonly string _filePath;
        private readonly Dictionary<ulong, PlayerStats> _stats = new();
        private readonly object _lock = new();

        public PlayerStatsService(string? filePath = null)
        {
            _filePath = filePath ?? "player_stats.json";

            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<PlayerStats>>(json);
                if (loaded != null)
                {
                    foreach (var s in loaded)
                        _stats[s.DiscordId] = s;
                }
            }
        }

        private void Save()
        {
            var list = new List<PlayerStats>(_stats.Values);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }

        public PlayerStats GetOrCreate(ulong discordId)
        {
            lock (_lock)
            {
                if (!_stats.TryGetValue(discordId, out var stats))
                {
                    stats = new PlayerStats
                    {
                        DiscordId = discordId,
                        Elo = 800
                    };
                    _stats[discordId] = stats;
                    Save();
                }

                return stats;
            }
        }

        public IReadOnlyCollection<PlayerStats> GetAll()
        {
            lock (_lock)
                return _stats.Values.ToList();
        }

        public void Update(PlayerStats stats)
        {
            lock (_lock)
            {
                _stats[stats.DiscordId] = stats;
                Save();
            }
        }
    }
}