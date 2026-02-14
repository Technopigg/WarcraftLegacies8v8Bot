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
            // no-op
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
}