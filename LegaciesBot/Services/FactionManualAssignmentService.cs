using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class FactionManualAssignmentService
    {
        private readonly IFactionRegistry _registry;
        private readonly NicknameService _nicknames;

        public static readonly Dictionary<string, string> FactionShortcodes = new()
        {
            ["sw"] = "Stormwind",
            ["dal"] = "Dalaran",
            ["sc"] = "Scourge",
            ["fh"] = "Fel Horde",
            ["sen"] = "Sentinels",
            ["if"] = "Ironforge",
            ["leg"] = "Legion",
            ["qt"] = "Quel'thalas"
        };

        public FactionManualAssignmentService(IFactionRegistry registry, NicknameService nicknames)
        {
            _registry = registry;
            _nicknames = nicknames;
        }

        private ulong? ResolvePlayer(string input)
        {
            return _nicknames.ResolvePlayerId(input);
        }

        private string? ResolveFaction(string input)
        {
            if (FactionShortcodes.TryGetValue(input.ToLower(), out var full))
                return full;

            var faction = _registry.All.FirstOrDefault(f =>
                f.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

            return faction?.Name;
        }

        public bool TryAssignSingle(Lobby lobby, ulong captainId, string playerInput, string factionInput)
        {
            if (!lobby.IsCaptainDraft)
                return false;

            if (captainId != lobby.CaptainA && captainId != lobby.CaptainB)
                return false;

            var targetId = ResolvePlayer(playerInput);
            if (targetId == null)
                return false;

            bool isA = lobby.TeamAPicks.Contains(targetId.Value);
            bool isB = lobby.TeamBPicks.Contains(targetId.Value);

            if (captainId == lobby.CaptainA && !isA) return false;
            if (captainId == lobby.CaptainB && !isB) return false;

            var faction = ResolveFaction(factionInput);
            if (faction == null)
                return false;

            if (lobby.ManualFactionAssignments.Values.Contains(faction))
                return false;

            lobby.ManualFactionAssignments[targetId.Value] = faction;
            return true;
        }

        public List<string> AssignBulk(Lobby lobby, ulong captainId, string bulkText)
        {
            var errors = new List<string>();

            var lines = bulkText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    errors.Add($"Invalid line: {line}");
                    continue;
                }

                var player = parts[0];
                var faction = parts[1];

                if (!TryAssignSingle(lobby, captainId, player, faction))
                    errors.Add($"Failed: {player} {faction}");
            }

            return errors;
        }

        public bool TryFinalize(Lobby lobby)
        {
            return lobby.ManualFactionAssignments.Count == 16;
        }
    }
}