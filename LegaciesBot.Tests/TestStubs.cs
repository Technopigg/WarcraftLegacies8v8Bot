using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;

public class DummyGatewayClient : IGatewayClient
{
    public Task<ITextChannel?> GetTextChannelAsync(ulong id) =>
        Task.FromResult<ITextChannel?>(null);
}

public class EloStub : IEloService
{
    public Dictionary<ulong, int> ApplyTeamResult(
        List<Player> teamA,
        List<Player> teamB,
        bool teamAWon,
        PlayerStatsService stats)
    {
        return teamA.Concat(teamB)
            .ToDictionary(p => p.DiscordId, p => 5);
    }
}

public class FactionRegistryStub : IFactionRegistry
{
    public IEnumerable<Faction> All => FactionRegistry.All;
}

public class DefaultPreferencesStub : IDefaultPreferences
{
    public List<string> Factions => FactionRegistry.All
        .Select(f => f.Name)
        .ToList();
}

public class FactionAssignmentStub : IFactionAssignmentService
{
    public HashSet<TeamGroup> AllowedGroupsA { get; private set; } = new();
    public HashSet<TeamGroup> AllowedGroupsB { get; private set; } = new();

    private readonly Random _rng = new Random(12345);

    public void AssignFactionsForGame(
        Team teamA,
        Team teamB,
        HashSet<TeamGroup> allowedGroupsA,
        HashSet<TeamGroup> allowedGroupsB)
    {
        AllowedGroupsA = new HashSet<TeamGroup>(allowedGroupsA);
        AllowedGroupsB = new HashSet<TeamGroup>(allowedGroupsB);

        teamA.AssignedFactions.Clear();
        teamB.AssignedFactions.Clear();

        var allowedByTeam = new Dictionary<Team, HashSet<TeamGroup>>
        {
            [teamA] = AllowedGroupsA,
            [teamB] = AllowedGroupsB
        };

        var allPlayers = new List<(Team team, Player player)>();
        allPlayers.AddRange(teamA.Players.Select(p => (teamA, p)));
        allPlayers.AddRange(teamB.Players.Select(p => (teamB, p)));

        allPlayers = allPlayers
            .OrderBy(_ => _rng.Next())
            .ToList();

        var usedFactionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var groupsPerTeam = new Dictionary<Team, HashSet<TeamGroup>>
        {
            [teamA] = new(),
            [teamB] = new()
        };

        foreach (var (team, player) in allPlayers)
        {
            var allowedGroups = allowedByTeam[team];

            var availableFactions = FactionRegistry.All
                .Where(f => allowedGroups.Contains(f.Group))
                .Where(f => !usedFactionNames.Contains(f.Name))
                .Where(f => string.IsNullOrEmpty(f.SlotId) || !usedSlotIds.Contains(f.SlotId))
                .ToList();

            Faction? assigned = null;

            foreach (var prefName in player.FactionPreferences)
            {
                var faction = availableFactions
                    .FirstOrDefault(f => f.Name.Equals(prefName, StringComparison.OrdinalIgnoreCase));

                if (faction == null)
                    continue;

                if (!ConstraintService.IsCompatible(groupsPerTeam[team], faction.Group))
                    continue;

                assigned = faction;
                break;
            }

            if (assigned == null)
            {
                foreach (var faction in availableFactions)
                {
                    if (!ConstraintService.IsCompatible(groupsPerTeam[team], faction.Group))
                        continue;

                    assigned = faction;
                    break;
                }
            }

            team.AssignedFactions.Add(assigned);
            usedFactionNames.Add(assigned.Name);

            if (!string.IsNullOrEmpty(assigned.SlotId))
                usedSlotIds.Add(assigned.SlotId);

            groupsPerTeam[team].Add(assigned.Group);
        }
    }
}

public class MatchHistoryAdapter : IMatchHistoryService
{
    private readonly MatchHistoryService _inner;

    public MatchHistoryAdapter(MatchHistoryService inner)
    {
        _inner = inner;
    }

    public void RecordMatch(Game game, int scoreA, int scoreB, Dictionary<ulong, int> eloChanges)
    {
        _inner.RecordMatch(game, scoreA, scoreB, eloChanges);
    }
}