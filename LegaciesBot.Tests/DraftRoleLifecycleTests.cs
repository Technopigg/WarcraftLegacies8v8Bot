using LegaciesBot.Core;
using LegaciesBot.Services;
using NetCord;
using LTeam = LegaciesBot.Core.Team;

public class DraftRoleLifecycleTests
{
    private class RoleTrackingGateway : IGatewayClient
    {
        public List<(ulong GuildId, ulong UserId, ulong RoleId)> RemovedRoles { get; } = new();
        public List<(ulong GuildId, ulong RoleId)> DeletedRoles { get; } = new();

        public Task<ITextChannel?> GetTextChannelAsync(ulong id)
            => Task.FromResult<ITextChannel?>(null);

        public Task<Role> CreateRoleAsync(ulong guildId, string name)
            => throw new NotSupportedException("Role creation is not simulated.");

        public Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId)
            => Task.CompletedTask;

        public Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId)
        {
            RemovedRoles.Add((guildId, userId, roleId));
            return Task.CompletedTask;
        }

        public Task DeleteRoleAsync(ulong guildId, ulong roleId)
        {
            DeletedRoles.Add((guildId, roleId));
            return Task.CompletedTask;
        }

        public Task<NetCord.Rest.RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false)
            => Task.FromResult<NetCord.Rest.RestGuild>(null!);
    }

    private Lobby CreateLobbyWithPlayers(int count)
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 1; i <= count; i++)
        {
            var p = registry.GetOrCreate((ulong)i);
            p.Name = $"P{i}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        return lobby;
    }

    private GameService CreateGameService(RoleTrackingGateway gateway)
    {
        var history = new MatchHistoryAdapter(new MatchHistoryService());
        var elo = new EloStub();
        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
        var defaults = new DefaultPreferencesStub();
        var factionRegistry = new FactionRegistryStub();
        var rng = new Random(12345);

        return new GameService(
            gateway,
            history,
            elo,
            factionAssign,
            factionRegistry,
            defaults,
            rng
        );
    }

    [Fact]
    public async Task SubmitScore_RemovesDraftRole_And_ResetsLobby()
    {
        var gateway = new RoleTrackingGateway();
        var service = CreateGameService(gateway);

        var lobby = CreateLobbyWithPlayers(16);
        lobby.IsCaptainDraft = true;
        lobby.DraftRoleId = 999;
        lobby.DraftStarted = true;

        var teamA = new LTeam("Team A");
        var teamB = new LTeam("Team B");

        for (int i = 0; i < 8; i++)
        {
            teamA.Players.Add(lobby.Players[i]);
            lobby.Players[i].AssignedFaction = "A";
        }

        for (int i = 8; i < 16; i++)
        {
            teamB.Players.Add(lobby.Players[i]);
            lobby.Players[i].AssignedFaction = "B";
        }

        lobby.TeamA = teamA;
        lobby.TeamB = teamB;

        var game = service.CreatePendingGameIfMissing(lobby);
        game.TeamA = teamA;
        game.TeamB = teamB;
        game.StartedAt = DateTime.UtcNow;
        game.IsActive = true;

        var stats = new PlayerStatsService();
        await service.SubmitScore(game, 10, 5, stats);

        Assert.True(game.Finished);
        Assert.False(game.IsActive);
        Assert.Empty(lobby.Players);
        Assert.False(lobby.DraftStarted);
        Assert.False(lobby.IsLocked);
        Assert.Equal(0, lobby.GameNumber);
        Assert.False(lobby.DraftRoleId.HasValue);
        Assert.NotEmpty(gateway.RemovedRoles);
        Assert.NotEmpty(gateway.DeletedRoles);
    }
}
