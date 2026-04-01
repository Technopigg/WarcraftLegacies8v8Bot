using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;
using NetCord;
using NetCord.Rest;

public class DummyGatewayClient : IGatewayClient
{
    public Task<ITextChannel?> GetTextChannelAsync(ulong id)
        => Task.FromResult<ITextChannel?>(null);

    public Task<RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false)
        => Task.FromResult<RestGuild>(null!);

    public Task<Role> CreateRoleAsync(ulong guildId, string name)
        => Task.FromResult<Role>(null!);

    public Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId)
        => Task.CompletedTask;

    public Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId)
        => Task.CompletedTask;

    public Task DeleteRoleAsync(ulong guildId, ulong roleId)
        => Task.CompletedTask;
}

public class EloStub : IEloService
{
    public Dictionary<ulong, int> ApplyTeamResult(
        List<Player> teamA,
        List<Player> teamB,
        bool teamAWon)
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
        LegaciesBot.Core.Team teamA,
        LegaciesBot.Core.Team teamB,
        HashSet<TeamGroup>? allowedGroups,
        Random? rng)
    {
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
