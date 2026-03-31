using System.Text.Json.Serialization;
using LegaciesBot.Core;

namespace LegaciesBot.Seasons
{
    public class PlayerSeasonStats
    {
        [JsonInclude]
        public ulong DiscordId { get; set; }

        [JsonInclude]
        public int Elo { get; set; } = 800;

        [JsonInclude]
        public int PreviousSeasonElo { get; set; }

        [JsonInclude]
        public int GamesPlayed { get; set; }

        [JsonInclude]
        public int Wins { get; set; }

        [JsonInclude]
        public int Losses { get; set; }

        [JsonInclude]
        public Dictionary<string, FactionRecord> FactionHistory { get; set; } = new();

        [JsonIgnore]
        public double WinRate =>
            GamesPlayed == 0 ? 0 : (double)Wins / GamesPlayed * 100.0;
    }
}