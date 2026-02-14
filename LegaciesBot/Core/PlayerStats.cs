namespace LegaciesBot.Core
{
    public class PlayerStats
    {
        public ulong DiscordId { get; set; }
        public int Elo { get; set; } = 800;
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        
        public Dictionary<string, FactionRecord> FactionHistory { get; set; } = new();

        public double WinRate =>
            GamesPlayed == 0 ? 0 : (double)Wins / GamesPlayed * 100.0;
    }

    public class FactionRecord
    {
        public int Wins { get; set; }
        public int Losses { get; set; }

        public double WinRate =>
            Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses) * 100.0;
    }
}