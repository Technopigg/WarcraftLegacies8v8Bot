using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;

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
    public List<string> Factions => FactionRegistry.All.Select(f => f.Name).ToList();
}

public class FactionAssignmentStub : IFactionAssignmentService
{
    public void AssignFactionsToTeam(Team team, HashSet<TeamGroup> allowedGroups)
    {
        team.AssignedFactions.Clear();

        foreach (var player in team.Players)
        {
            var preferred = player.FactionPreferences
                .Select(name => FactionRegistry.All.FirstOrDefault(f => f.Name == name))
                .Where(f => f != null)
                .ToList();

            var pool = preferred.Any()
                ? preferred
                : FactionRegistry.All.Where(f => allowedGroups.Contains(f.Group)).ToList();
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