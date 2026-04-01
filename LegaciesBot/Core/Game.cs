namespace LegaciesBot.Core
{
    public class Game
    {
        public int Id { get; set; }
        public Lobby Lobby { get; set; } = null!;
        public Team TeamA { get; set; } = null!;
        public Team TeamB { get; set; } = null!;
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public bool Finished { get; set; }
        public DateTime FinishedAt { get; set; }
        public DateTime StartedAt { get; set; }


        public bool IsActive { get; set; }
        public Dictionary<ulong, int> ScoreVotes { get; set; } = new();
        
    }
}