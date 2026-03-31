using System.Text.Json;

namespace LegaciesBot.Seasons
{
    public class SeasonService
    {
        private readonly string _filePath;

        public Season CurrentSeason { get; private set; }
        public List<Season> ArchivedSeasons { get; private set; } = new();

        public SeasonService(string filePath = "seasons.json")
        {
            _filePath = filePath;
            Load();
        }

        public PlayerSeasonStats GetOrCreateSeasonStats(ulong discordId)
        {
            if (!CurrentSeason.PlayerStats.TryGetValue(discordId, out var stats))
            {
                stats = new PlayerSeasonStats
                {
                    DiscordId = discordId,
                    Elo = 800,
                    PreviousSeasonElo = 800
                };

                CurrentSeason.PlayerStats[discordId] = stats;
                Save();
            }

            return stats;
        }

        public void StartNewSeason()
        {
            if (CurrentSeason != null)
            {
                foreach (var p in CurrentSeason.PlayerStats.Values)
                    p.PreviousSeasonElo = p.Elo;

                CurrentSeason.EndedAt = DateTime.UtcNow;
                ArchivedSeasons.Add(CurrentSeason);
            }

            CurrentSeason = new Season
            {
                SeasonNumber = ArchivedSeasons.Count + 1,
                StartedAt = DateTime.UtcNow
            };

            Save();
        }

        public IEnumerable<Season> GetAllSeasons()
        {
            return ArchivedSeasons.Concat(new[] { CurrentSeason });
        }

        public Season? GetSeason(int seasonNumber)
        {
            if (CurrentSeason.SeasonNumber == seasonNumber)
                return CurrentSeason;

            return ArchivedSeasons.FirstOrDefault(s => s.SeasonNumber == seasonNumber);
        }

        private class SeasonFileModel
        {
            public Season? CurrentSeason { get; set; }
            public List<Season> ArchivedSeasons { get; set; } = new();
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                CurrentSeason = new Season
                {
                    SeasonNumber = 1,
                    StartedAt = DateTime.UtcNow
                };
                ArchivedSeasons = new List<Season>();
                Save();
                return;
            }

            var json = File.ReadAllText(_filePath);
            var model = JsonSerializer.Deserialize<SeasonFileModel>(json);

            if (model == null)
            {
                CurrentSeason = new Season
                {
                    SeasonNumber = 1,
                    StartedAt = DateTime.UtcNow
                };
                ArchivedSeasons = new List<Season>();
                Save();
                return;
            }

            CurrentSeason = model.CurrentSeason ?? new Season
            {
                SeasonNumber = 1,
                StartedAt = DateTime.UtcNow
            };

            ArchivedSeasons = model.ArchivedSeasons ?? new List<Season>();
        }

        public void Save()
        {
            var model = new SeasonFileModel
            {
                CurrentSeason = CurrentSeason,
                ArchivedSeasons = ArchivedSeasons
            };

            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }
    }
}
