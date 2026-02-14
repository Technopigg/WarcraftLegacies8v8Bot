using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;

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

        public Task<ITextChannel?> GetTextChannelAsync(ulong id)
            => Task.FromResult(Channel);
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
            bool teamAWon,
            PlayerStatsService stats)
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

        for (int i = 0; i < count; i++)
        {
            var p = new Player((ulong)(i + 1), $"Player{i + 1}", 1500);
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
        var factionAssignment = new FactionAssignmentService(rng);
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
        var factionAssignment = new FactionAssignmentService(rng);
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
        var factionAssignment = new FactionAssignmentService(rng);
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
        var factionAssignment = new FactionAssignmentService(rng);
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

        factionAssignment.AssignFactionsForGame(teamA, teamB,
            TeamGroupService.GenerateValidSplit().Item1,
            TeamGroupService.GenerateValidSplit().Item2);

        for (int i = 0; i < teamA.Players.Count; i++)
            teamA.Players[i].AssignedFaction = teamA.AssignedFactions[i].Name;

        for (int i = 0; i < teamB.Players.Count; i++)
            teamB.Players[i].AssignedFaction = teamB.AssignedFactions[i].Name;

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