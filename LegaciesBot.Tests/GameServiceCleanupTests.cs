using LegaciesBot.Core;
using LegaciesBot.Services;
using NetCord;
using NetCord.Rest;
using System.Runtime.Serialization;

public class GameServiceCleanupTests
{
    private class FakeGateway : IGatewayClient
    {
        public Task<ITextChannel?> GetTextChannelAsync(ulong id)
            => Task.FromResult<ITextChannel?>(null);

        public Task<RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false)
        {
            var g = (RestGuild)FormatterServices.GetUninitializedObject(typeof(RestGuild));
            return Task.FromResult(g);
        }

        public Task<Role> CreateRoleAsync(ulong guildId, string name)
        {
            var r = (Role)FormatterServices.GetUninitializedObject(typeof(Role));
            return Task.FromResult(r);
        }

        public Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId)
            => Task.CompletedTask;

        public Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId)
            => Task.CompletedTask;

        public Task DeleteRoleAsync(ulong guildId, ulong roleId)
            => Task.CompletedTask;
    }

    private Lobby CreateLobby()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 1; i <= 16; i++)
        {
            var p = registry.GetOrCreate((ulong)i);
            p.Name = $"P{i}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public void SubmitScore_CleansUpRoles_And_ResetsLobby()
    {
        var gateway = new FakeGateway();
        var history = new MatchHistoryAdapter(new MatchHistoryService());
        var elo = new EloStub();
        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
        var defaults = new DefaultPreferencesStub();
        var rng = new Random(12345);

        var service = new GameService(
            gateway,
            history,
            elo,
            factionAssign,
            new FactionRegistryStub(),
            defaults,
            rng
        );

        var lobby = CreateLobby();

        var (teamA, teamB) = DraftService.CreateBalancedTeams(lobby.Players, rng);
        factionAssign.AssignFactionsForGame(teamA, teamB, null, rng);

        var game = service.CreatePendingGameIfMissing(lobby);
        game.TeamA = teamA;
        game.TeamB = teamB;
        game.StartedAt = DateTime.UtcNow;
        game.IsActive = true;

        service.SubmitScore(game, 10, 5, new PlayerStatsService()).GetAwaiter().GetResult();

        Assert.True(game.Finished);
        Assert.Empty(lobby.Players);
        Assert.False(lobby.DraftStarted);
    }
}
