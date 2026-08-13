using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class FactionManualAssignmentService
    {
        private readonly IFactionRegistry _registry;
        private readonly NicknameService _nicknames;
        private readonly GameService _gameService;

        public static readonly Dictionary<string, string> FactionShortcodes = new()
        {
            ["lord"] = "Lordaeron",
            ["quel"] = "Quel'thalas",
            ["dala"] = "Dalaran",
            ["giln"] = "Gilneas",

            ["sc"]   = "Scourge",
            ["leg"]  = "Legion",

            ["sw"]   = "Stormwind",
            ["if"]   = "Ironforge",
            ["kt"]   = "Kul'tiras",

            ["fel"]  = "Fel Horde",
            ["illi"] = "Illidari",
            ["sun"]  = "Sunfury",

            ["ws"]   = "Warsong",
            ["fw"]   = "Frostwolf",
            ["sents"] = "Sentinels",
            ["exo"]  = "The Exodar",
            ["dru"]  = "Druids",

            ["aq"]   = "An'qiraj",
            ["be"]   = "Black Empire",
            ["sky"]  = "Skywall"
        };


        public FactionManualAssignmentService(
            IFactionRegistry registry,
            NicknameService nicknames,
            GameService gameService)
        {
            _registry = registry;
            _nicknames = nicknames;
            _gameService = gameService;
        }

        private ulong? ResolvePlayer(string input)
        {
            if (input.StartsWith("<@") && input.EndsWith(">"))
            {
                var inner = input.Trim('<', '@', '!', '>');
                if (ulong.TryParse(inner, out var id))
                    return id;
            }

            if (ulong.TryParse(input, out var raw))
                return raw;

            var nick = _nicknames.ResolvePlayerId(input);
            if (nick.HasValue)
                return nick.Value;

            return null;
        }

        private string? ResolveFaction(string input)
        {
            if (FactionShortcodes.TryGetValue(input.ToLower(), out var full))
                return full;

            var faction = _registry.All.FirstOrDefault(f =>
                f.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

            return faction?.Name;
        }

        private bool IsCaptain(Lobby lobby, ulong id)
            => id == lobby.CaptainA || id == lobby.CaptainB;

        private bool IsTeamLocked(Lobby lobby, ulong captainId)
        {
            if (captainId == lobby.CaptainA)
                return lobby.TeamAFactionsLocked;

            if (captainId == lobby.CaptainB)
                return lobby.TeamBFactionsLocked;

            return false;
        }

        // Team membership can come from two places: picks made during an actual
        // captain draft (TeamAPicks/TeamBPicks), or the auto-balanced teams when
        // captains skip the draft and just assign factions (lobby.TeamA/TeamB).
        private bool IsOnCaptainsTeam(Lobby lobby, ulong captainId, ulong playerId)
        {
            if (lobby.IsCaptainDraft)
            {
                if (captainId == lobby.CaptainA)
                    return lobby.TeamAPicks.Contains(playerId);

                if (captainId == lobby.CaptainB)
                    return lobby.TeamBPicks.Contains(playerId);

                return false;
            }

            if (captainId == lobby.CaptainA)
                return lobby.TeamA?.Players.Any(p => p.DiscordId == playerId) == true;

            if (captainId == lobby.CaptainB)
                return lobby.TeamB?.Players.Any(p => p.DiscordId == playerId) == true;

            return false;
        }

        private IEnumerable<ulong> GetTeamPlayers(Lobby lobby, ulong captainId)
        {
            if (lobby.IsCaptainDraft)
            {
                if (captainId == lobby.CaptainA)
                    return lobby.TeamAPicks;

                if (captainId == lobby.CaptainB)
                    return lobby.TeamBPicks;

                return Enumerable.Empty<ulong>();
            }

            if (captainId == lobby.CaptainA)
                return lobby.TeamA?.Players.Select(p => p.DiscordId) ?? Enumerable.Empty<ulong>();

            if (captainId == lobby.CaptainB)
                return lobby.TeamB?.Players.Select(p => p.DiscordId) ?? Enumerable.Empty<ulong>();

            return Enumerable.Empty<ulong>();
        }

        public bool TryAssignSingle(Lobby lobby, ulong captainId, string playerInput, string factionInput)
        {
            // Just need captains to exist here - doesn't matter if this lobby ran
            // an actual captain draft or not.
            if (lobby.CaptainA == null || lobby.CaptainB == null)
                return false;

            if (!IsCaptain(lobby, captainId))
                return false;

            if (IsTeamLocked(lobby, captainId))
                return false;

            var targetId = ResolvePlayer(playerInput);
            if (targetId == null)
                return false;

            if (!IsOnCaptainsTeam(lobby, captainId, targetId.Value))
                return false;

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
                var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
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

        public bool TryLockFactions(Lobby lobby, ulong captainId, out string message)
        {
            message = "";

            if (lobby.CaptainA == null || lobby.CaptainB == null)
            {
                message = "No captains assigned for this lobby.";
                return false;
            }

            if (!IsCaptain(lobby, captainId))
            {
                message = "Only captains can lock factions.";
                return false;
            }

            if (IsTeamLocked(lobby, captainId))
            {
                message = "Your team is already locked.";
                return false;
            }

            var teamPlayers = GetTeamPlayers(lobby, captainId).ToList();
            if (teamPlayers.Count != 8)
            {
                message = "Your team does not have exactly 8 players.";
                return false;
            }

            var missing = teamPlayers.Where(p => !lobby.ManualFactionAssignments.ContainsKey(p)).ToList();
            if (missing.Any())
            {
                message = "You must assign all 8 factions before locking.";
                return false;
            }

            if (captainId == lobby.CaptainA)
                lobby.TeamAFactionsLocked = true;
            else
                lobby.TeamBFactionsLocked = true;

            var summary = new List<string>();
            foreach (var pid in teamPlayers)
            {
                var faction = lobby.ManualFactionAssignments[pid];
                summary.Add($"{pid} → {faction}");
            }

            message =
                "Your team’s factions are now locked.\n\n" +
                string.Join("\n", summary);

            if (lobby.TeamAFactionsLocked && lobby.TeamBFactionsLocked)
                FinalizeAndStartGame(lobby);

            return true;
        }

        private void FinalizeAndStartGame(Lobby lobby)
        {
            foreach (var p in lobby.Players)
            {
                if (lobby.ManualFactionAssignments.TryGetValue(p.DiscordId, out var faction))
                    p.AssignedFaction = faction;
            }
            _gameService.TryAutoStartAfterManualFactions(lobby);
        }
    }
}
