namespace LegaciesBot.Core
{
    public class PlayerStats
    {
        public ulong DiscordId { get; set; }
        public int Elo { get; set; } = 800;
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        public double WinRate =>
            GamesPlayed == 0 ? 0 : (double)Wins / GamesPlayed * 100.0;
    }
}