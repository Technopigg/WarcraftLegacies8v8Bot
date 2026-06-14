namespace LegaciesBot.Moderation
{
    public class ModerationData
    {
        public Dictionary<ulong, UserModerationEntry> Users { get; set; } = new();
    }

    public class UserModerationEntry
    {
        public List<WarningEntry> Warnings { get; set; } = new();
        public List<BanEntry> Bans { get; set; } = new();
    }

    public class WarningEntry
    {
        public ulong ModeratorId { get; set; }
        public string Reason { get; set; } = "";
        public DateTime IssuedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }

    public class BanEntry
    {
        public ulong ModeratorId { get; set; }
        public string Reason { get; set; } = "";
        public DateTime IssuedAtUtc { get; set; }
    }
}