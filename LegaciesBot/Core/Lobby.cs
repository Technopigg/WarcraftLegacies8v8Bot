namespace LegaciesBot.Core
{
    public class Lobby
    {
        public List<Player> Players { get; set; } = new();
        public bool DraftStarted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<ulong, DateTime> AfkPingedAt { get; set; } = new();
        public bool IsFull => Players.Count >= 16;
        public Team? TeamA { get; set; }
        public Team? TeamB { get; set; }
        public DraftMode DraftMode { get; set; } = DraftMode.AutoDraft_AutoFaction;
        public ulong? CaptainA { get; set; }
        public ulong? CaptainB { get; set; }
        public List<ulong> TeamAPicks { get; set; } = new();
        public List<ulong> TeamBPicks { get; set; } = new();
        public int CurrentPickIndex { get; set; } = 0;
        public bool CaptainAPassed { get; set; } = false;
        public List<ulong> DraftOrder { get; set; } = new();
        public bool FactionAssignmentStarted { get; set; } = false;
        public int FactionSubmissions { get; set; } = 0;
        public Dictionary<ulong, string> ManualFactionAssignments { get; set; } = new();
        public bool ManualFactionComplete => ManualFactionAssignments.Count == 16;
        public int GameNumber { get; set; } = 0;
        public ulong? DraftRoleId { get; set; }
        public bool IsCaptainDraft { get; set; } = false;

    }
}