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

            ["orc"]  = "Orcish Horde",
            ["tt"]   = "Tauren Tribes",
            ["tauren"] = "Tauren Tribes",

            ["sents"] = "Sentinels",
            ["dru"]  = "Druids"
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
            input = input.Trim();
            if (FactionShortcodes.TryGetValue(input.ToLower(), out var full))
                return full;

            var faction = _registry.All.FirstOrDefault(f =>
                f.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (faction != null)
                return faction.Name;

            // Same forgiving matching as !prefs: phone apostrophes (Kul’tiras), missing
            // apostrophes (kultiras), aliases (illidan) and one-letter typos. Must resolve
            // to exactly one faction, so "!assignf bob scourge legion" is still rejected.
            var parsed = GameData.FactionParser.Parse(input);
            if (parsed.Accepted.Count == 1 && parsed.Unknown.Count == 0
                && _registry.All.Any(f => f.Name == parsed.Accepted[0]))
                return parsed.Accepted[0];

            return null;
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
            => TryAssignSingle(lobby, captainId, playerInput, factionInput, out _);

        public bool TryAssignSingle(Lobby lobby, ulong captainId, string playerInput, string factionInput, out string reason)
        {
            // Just need captains to exist here - doesn't matter if this lobby ran
            // an actual captain draft or not.
            if (lobby.CaptainA == null || lobby.CaptainB == null)
            {
                reason = "This lobby has no captains.";
                return false;
            }

            if (!IsCaptain(lobby, captainId))
            {
                reason = "Only captains can assign factions.";
                return false;
            }

            if (IsTeamLocked(lobby, captainId))
            {
                reason = "Your team's factions are already locked.";
                return false;
            }

            var targetId = ResolvePlayer(playerInput);
            if (targetId == null)
            {
                reason = $"Couldn't find a player called **{playerInput}**.";
                return false;
            }

            if (!IsOnCaptainsTeam(lobby, captainId, targetId.Value))
            {
                reason = $"**{playerInput}** isn't on your team.";
                return false;
            }

            var faction = ResolveFaction(factionInput);
            if (faction == null)
            {
                reason = $"**{factionInput}** isn't a faction. `!factions` lists them.";
                return false;
            }

            if (!FitsTheSideRules(lobby, captainId, targetId.Value, faction, out reason))
                return false;

            lobby.ManualFactionAssignments[targetId.Value] = faction;
            reason = "";
            return true;
        }

        // Captains pick whole sides, like in a real game: a side belongs to one team, the
        // opposing pairs (Legion/North, Fel Horde/South, Horde/Night Elves) are never on the
        // same team, each slot is used once (one of Dalaran/Gilneas, one of Illidari/Sunfury),
        // and the team's sides must be able to add up to exactly 7.
        private bool FitsTheSideRules(Lobby lobby, ulong captainId, ulong targetId, string factionName, out string reason)
        {
            reason = "";
            var faction = _registry.All.First(f => f.Name == factionName);
            ulong otherCaptain = captainId == lobby.CaptainA ? lobby.CaptainB!.Value : lobby.CaptainA!.Value;
            var ownPlayers = GetTeamPlayers(lobby, captainId).ToHashSet();
            var otherPlayers = GetTeamPlayers(lobby, otherCaptain).ToHashSet();

            // Everyone else's assignments; the target's current one is about to be replaced.
            var others = lobby.ManualFactionAssignments
                .Where(kv => kv.Key != targetId)
                .Select(kv => (Player: kv.Key, Faction: _registry.All.FirstOrDefault(f => f.Name == kv.Value)))
                .Where(x => x.Faction != null)
                .ToList();

            var slotTaken = others.FirstOrDefault(x => x.Faction!.SlotId.Equals(faction.SlotId, StringComparison.OrdinalIgnoreCase));
            if (slotTaken.Faction != null)
            {
                reason = slotTaken.Faction.Name == faction.Name
                    ? $"**{faction.Name}** is already taken."
                    : $"Only one of **{slotTaken.Faction.Name}** / **{faction.Name}** can be played, and {slotTaken.Faction.Name} is already taken.";
                return false;
            }

            var ownGroups = others.Where(x => ownPlayers.Contains(x.Player)).Select(x => x.Faction!.Group).ToHashSet();
            var otherGroups = others.Where(x => otherPlayers.Contains(x.Player)).Select(x => x.Faction!.Group).ToHashSet();

            if (otherGroups.Contains(faction.Group))
            {
                reason = $"**{TeamGroupService.Label(faction.Group)}** is the other team's side.";
                return false;
            }

            var clash = ownGroups.FirstOrDefault(g => ConstraintService.AreOpposed(g, faction.Group));
            if (ownGroups.Any(g => ConstraintService.AreOpposed(g, faction.Group)))
            {
                reason = $"**{TeamGroupService.Label(faction.Group)}** and **{TeamGroupService.Label(clash)}** must be on opposite teams.";
                return false;
            }

            var newOwn = ownGroups.Append(faction.Group).ToHashSet();
            bool possible = TeamGroupService.ValidSplits.Any(s =>
                (newOwn.IsSubsetOf(s.TeamA) && otherGroups.IsSubsetOf(s.TeamB)) ||
                (newOwn.IsSubsetOf(s.TeamB) && otherGroups.IsSubsetOf(s.TeamA)));
            if (!possible)
            {
                reason = $"Your team can't have both {string.Join(" and ", newOwn.Select(g => $"**{TeamGroupService.Label(g)}**"))}: " +
                         $"a {MatchFormat.TeamSize}-player team is Burning Legion + South Alliance or North Alliance + Fel Horde, " +
                         "plus The Horde or The Night Elves.";
                return false;
            }

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

                if (!TryAssignSingle(lobby, captainId, player, faction, out var reason))
                    errors.Add($"{player} {faction}: {reason}");
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
            if (teamPlayers.Count != MatchFormat.TeamSize)
            {
                message = $"Your team does not have exactly {MatchFormat.TeamSize} players.";
                return false;
            }

            var missing = teamPlayers.Where(p => !lobby.ManualFactionAssignments.ContainsKey(p)).ToList();
            if (missing.Any())
            {
                message = $"You must assign all {MatchFormat.TeamSize} factions before locking.";
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
