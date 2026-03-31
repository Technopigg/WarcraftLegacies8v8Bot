using System.Text.Json.Serialization;

namespace LegaciesBot.Seasons
{
    public class Season
    {
        [JsonInclude]
        public int SeasonNumber { get; set; }

        [JsonInclude]
        public DateTime StartedAt { get; set; }

        [JsonInclude]
        public DateTime? EndedAt { get; set; }

        [JsonInclude]
        public Dictionary<ulong, PlayerSeasonStats> PlayerStats { get; set; } = new();
    }
}