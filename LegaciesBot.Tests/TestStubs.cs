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
    public void AssignFactionsForGame(
        Team teamA,
        Team teamB,
        HashSet<TeamGroup> allowedGroupsA,
        HashSet<TeamGroup> allowedGroupsB)
    {
        teamA.AssignedFactions.Clear();
        teamB.AssignedFactions.Clear();

        AssignForSingleTeam(teamA, allowedGroupsA);
        AssignForSingleTeam(teamB, allowedGroupsB);
    }

    private void AssignForSingleTeam(Team team, HashSet<TeamGroup> allowedGroups)
    {
        var all = FactionRegistry.All
            .Where(f => allowedGroups.Contains(f.Group))
            .ToList();

        for (int i = 0; i < team.Players.Count; i++)
        {
            var player = team.Players[i];

            var preferred = player.FactionPreferences
                .Select(name => all.FirstOrDefault(f =>
                    f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => f != null)
                .ToList();

            var pool = preferred.Any() ? preferred! : all;

            var faction = pool.First();

            team.AssignedFactions.Add(faction);
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