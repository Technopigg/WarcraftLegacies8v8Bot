using System.Text.Json;
using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class PlayerRegistryService
    {
        private readonly string? _filePath;
        public string FilePath => _filePath;
        private readonly Dictionary<ulong, Player> _players = new();

        public static Action<Player>? OnPlayerMutated;

        public PlayerRegistryService(string? filePath = null)
        {
            _filePath = filePath ?? "players.json";
            Load();
            OnPlayerMutated = _ => Save();
        }

        public Player? Resolve(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var p = FindByNameOrNickname(input);
            if (p != null)
                return p;

            if (input.StartsWith("<@") && input.EndsWith(">"))
            {
                var trimmed = input.Trim('<', '>', '@', '!');
                if (ulong.TryParse(trimmed, out ulong idFromMention))
                    return GetPlayer(idFromMention);
            }

            if (ulong.TryParse(input, out ulong id))
                return GetPlayer(id);

            return null;
        }

        public Player? FindByNameOrNickname(string input)
        {
            return _players.Values.FirstOrDefault(p =>
                p.Name.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                (p.Nickname != null &&
                 p.Nickname.Equals(input, StringComparison.OrdinalIgnoreCase)));
        }

        public Player? GetPlayer(ulong discordId)
        {
            return _players.TryGetValue(discordId, out var p) ? p : null;
        }

        public Player GetOrCreate(ulong discordId, string? name = null)
        {
            if (_players.TryGetValue(discordId, out var existing))
                return existing;

            var player = new Player(discordId, name ?? discordId.ToString())
            {
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _players[discordId] = player;
            Save();

            return player;
        }

        public bool SetNickname(ulong discordId, string nickname)
        {
            if (!_players.TryGetValue(discordId, out var player))
                return false;

            if (_players.Values.Any(p =>
                    p.Nickname != null &&
                    p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Nickname already taken.");

            player.Nickname = nickname;
            Save();
            return true;
        }

        public bool IsRegistered(ulong discordId)
        {
            return _players.ContainsKey(discordId);
        }

        public Player RegisterPlayer(ulong discordId, string name)
        {
            if (_players.ContainsKey(discordId))
                return _players[discordId];

            var player = new Player(discordId, name)
            {
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _players[discordId] = player;
            Save();

            return player;
        }

        public IReadOnlyCollection<Player> GetAllPlayers()
        {
            return _players.Values;
        }

        private void Load()
        {
            if (_filePath == null || !File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var list = JsonSerializer.Deserialize<List<Player>>(json);
            if (list != null)
            {
                foreach (var p in list)
                    _players[p.DiscordId] = p;
            }
        }

        private void Save()
        {
            if (_filePath == null)
                return;

            var list = _players.Values.ToList();

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }
        
    }
}