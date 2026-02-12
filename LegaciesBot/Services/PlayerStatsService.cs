using System.Text.Json;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class PlayerStatsService
    {
        private readonly string _filePath = "playerstats.json";
        private readonly Dictionary<ulong, PlayerStats> _stats = new();

        public PlayerStatsService()
        {
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

        public IReadOnlyCollection<PlayerStats> GetAll() => _stats.Values;

        public void Update(PlayerStats stats)
        {
            _stats[stats.DiscordId] = stats;
            Save();
        }
    }
}