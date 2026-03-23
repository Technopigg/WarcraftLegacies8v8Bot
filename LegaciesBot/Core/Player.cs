using System.Text.Json.Serialization;
using LegaciesBot.Services;

namespace LegaciesBot.Core
{
    public class Player
    {
        public Player() { }

        public Player(ulong discordId, string name, int elo = 800)
        {
            DiscordId = discordId;
            Name = name;
            Elo = elo;
        }

        [JsonInclude]
        public ulong DiscordId { get; set; }

        [JsonInclude]
        public string? Nickname { get; set; }

        private string _name = "";

        [JsonInclude]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PlayerRegistryService.OnPlayerMutated?.Invoke(this);
            }
        }

        [JsonInclude]
        public int Elo { get; set; } = 800;

        [JsonInclude]
        public List<string> FactionPreferences { get; set; } = new();

        [JsonInclude]
        public DateTime JoinedAt { get; set; }

        [JsonInclude]
        public bool IsActive { get; set; } = true;

        [JsonInclude]
        public string? AssignedFaction { get; set; }
    }
}