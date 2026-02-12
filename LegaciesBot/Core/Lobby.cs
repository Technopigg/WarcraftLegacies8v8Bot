namespace LegaciesBot.Core;

public class Lobby
{
    public List<Player> Players { get; set; } = new();
    public bool DraftStarted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<ulong, DateTime> AfkPingedAt { get; set; } = new(); // AFK reminders

    public bool IsFull => Players.Count >= 16;
}
