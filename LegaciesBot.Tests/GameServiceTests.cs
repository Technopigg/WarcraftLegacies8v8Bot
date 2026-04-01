using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using NetCord;
using NetCord.Rest;

public class GameServiceTests
{
    private class FakeTextChannel : ITextChannel
    {
        public List<string> SentMessages { get; } = new();

        public Task SendMessageAsync(string message)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private class FakeGatewayClient : IGatewayClient
    {
        public ITextChannel? Channel { get; set; }

        public Task<ITextChannel?> GetTextChannelAsync(ulong channelId)
            => Task.FromResult(Channel);

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

    private class FakeMatchHistoryService : IMatchHistoryService
    {
        public void RecordMatch(Game game, int scoreA, int scoreB, Dictionary<ulong, int> changes)
        {
        }
    }

    private class FakeEloService : IEloService
    {
        public Dictionary<ulong, int> ApplyTeamResult(
            List<Player> teamA,
            List<Player> teamB,
            bool teamAWon)
        {
            return teamA.Concat(teamB)
                .ToDictionary(p => p.DiscordId, p => 0);
        }
    }

    private class FakeDefaultPreferences : IDefaultPreferences
    {
        public List<string> Factions { get; } =
            FactionRegistry.All.Select(f => f.Name).ToList();
    }

    private class FakeFactionRegistry : IFactionRegistry
    {
        public IEnumerable<Faction> All => FactionRegistry.All;
    }

    private static Lobby CreateLobbyWithPlayers(int count)
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 0; i < count; i++)
        {
            ulong id = (ulong)(i + 1);

            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            p.FactionPreferences = new List<string>();

            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public async Task StartDraft_SetsTeams_And_SendsMessage()
    {
        var rng = new Random(12345);

        var client = new FakeGatewayClient();
        var channel = new FakeTextChannel();
        client.Channel = channel;

        var matchHistory = new FakeMatchHistoryService();
        var elo = new FakeEloService();
        var factionAssignment = new RealFactionAssignmentService(new FakeFactionRegistry());
        var factionRegistry = new FakeFactionRegistry();
        var defaults = new FakeDefaultPreferences();

        var service = new GameService(
            client,
            matchHistory,
            elo,
            factionAssignment,
            factionRegistry,
            defaults,
            rng
        );

        var lobby = CreateLobbyWithPlayers(16);

        await service.StartDraft(lobby, 123UL);

        Assert.NotNull(lobby.TeamA);
        Assert.NotNull(lobby.TeamB);
        Assert.True(lobby.DraftStarted);

        Assert.NotEmpty(channel.SentMessages);
        Assert.Contains("DRAFT COMPLETE", channel.SentMessages[0]);
    }

    [Fact]
    public async Task StartDraft_Throws_When_Not_16_Players()
    {
        var rng = new Random(12345);

        var client = new FakeGatewayClient();
        var matchHistory = new FakeMatchHistoryService();
        var elo = new FakeEloService();
        var factionAssignment = new RealFactionAssignmentService(new FakeFactionRegistry());
        var factionRegistry = new FakeFactionRegistry();
        var defaults = new FakeDefaultPreferences();

        var service = new GameService(
            client,
            matchHistory,
            elo,
            factionAssignment,
            factionRegistry,
            defaults,
            rng
        );

        var lobby = CreateLobbyWithPlayers(15);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.StartDraft(lobby, 123UL)
        );
    }

    [Fact]
    public async Task StartDraft_AssignsFactionsToPlayers()
    {
        var rng = new Random(12345);

        var client = new FakeGatewayClient();
        var matchHistory = new FakeMatchHistoryService();
        var elo = new FakeEloService();
        var factionAssignment = new RealFactionAssignmentService(new FakeFactionRegistry());
        var factionRegistry = new FakeFactionRegistry();
        var defaults = new FakeDefaultPreferences();

        var service = new GameService(
            client,
            matchHistory,
            elo,
            factionAssignment,
            factionRegistry,
            defaults,
            rng
        );

        var lobby = CreateLobbyWithPlayers(16);

        await service.StartDraft(lobby, 0);

        foreach (var p in lobby.TeamA.Players)
            Assert.False(string.IsNullOrWhiteSpace(p.AssignedFaction));

        foreach (var p in lobby.TeamB.Players)
            Assert.False(string.IsNullOrWhiteSpace(p.AssignedFaction));
    }

    [Fact]
    public void SubmitScore_UpdatesFactionStats()
    {
        var rng = new Random(12345);

        var client = new FakeGatewayClient();
        var matchHistory = new FakeMatchHistoryService();
        var elo = new FakeEloService();
        var factionAssignment = new RealFactionAssignmentService(new FakeFactionRegistry());
        var factionRegistry = new FakeFactionRegistry();
        var defaults = new FakeDefaultPreferences();

        var path = Path.GetTempFileName();
        File.WriteAllText(path, "[]");
        var statsService = new PlayerStatsService(path);

        var service = new GameService(
            client,
            matchHistory,
            elo,
            factionAssignment,
            factionRegistry,
            defaults,
            rng
        );

        var lobby = CreateLobbyWithPlayers(16);

        var (teamA, teamB) = DraftService.CreateBalancedTeams(lobby.Players, rng);

        factionAssignment.AssignFactionsForGame(
            teamA,
            teamB,
            null,
            rng
        );

        var game = service.StartGame(lobby, teamA, teamB);

        service.SubmitScore(game, 10, 5, statsService);

        foreach (var p in teamA.Players)
        {
            var s = statsService.GetOrCreate(p.DiscordId);
            Assert.True(s.FactionHistory[p.AssignedFaction].Wins == 1);
        }

        foreach (var p in teamB.Players)
        {
            var s = statsService.GetOrCreate(p.DiscordId);
            Assert.True(s.FactionHistory[p.AssignedFaction].Losses == 1);
        }
    }
}
