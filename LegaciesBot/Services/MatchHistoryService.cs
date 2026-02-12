using System.Text.Json;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class MatchHistoryService
    {
        private const string FilePath = "match_history.json";

        public List<MatchRecord> History { get; private set; } = new();

        public MatchHistoryService()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                History = JsonSerializer.Deserialize<List<MatchRecord>>(json) ?? new List<MatchRecord>();
            }
            else
            {
                Save();
            }
        }

        public void RecordMatch(Game game, int scoreA, int scoreB, Dictionary<ulong, int> eloChanges)
        {
            var record = new MatchRecord
            {
                GameId = game.Id,
                Timestamp = DateTime.UtcNow,
                ScoreA = scoreA,
                ScoreB = scoreB,
                TeamA = game.TeamA.Players.Select(p => new PlayerRecord
                {
                    DiscordId = p.DiscordId,
                    Name = p.Name,
                    EloChange = eloChanges[p.DiscordId]
                }).ToList(),
                TeamB = game.TeamB.Players.Select(p => new PlayerRecord
                {
                    DiscordId = p.DiscordId,
                    Name = p.Name,
                    EloChange = eloChanges[p.DiscordId]
                }).ToList()
            };

            History.Add(record);
            Save();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(History, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }
    }

    public class MatchRecord
    {
        public int GameId { get; set; }
        public DateTime Timestamp { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public List<PlayerRecord> TeamA { get; set; } = new();
        public List<PlayerRecord> TeamB { get; set; } = new();
    }

    public class PlayerRecord
    {
        public ulong DiscordId { get; set; }
        public string Name { get; set; }
        public int EloChange { get; set; }
    }
}