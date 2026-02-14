namespace LegaciesBot.Core
{
    public class Player
    {
        public ulong DiscordId { get; }
        public string Name { get; set; }
        public int Elo { get; set; } = 800;
        public List<string> FactionPreferences { get; set; } = new();
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        public string? AssignedFaction { get; set; }

        public Player(ulong discordId, string name, int elo = 800)
        {
            DiscordId = discordId;
            Name = name;
            Elo = elo;
        }
    }
}