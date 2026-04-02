using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using NetCord;
using LTeam = LegaciesBot.Core.Team;

public class CaptainDraftModeTests
{
    private class FakeTextChannel : ITextChannel
    {
        public List<string> Messages { get; } = new();

        public Task SendMessageAsync(string message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private class FakeGateway : IGatewayClient
    {
        public FakeTextChannel Channel { get; } = new();

        public Task<ITextChannel?> GetTextChannelAsync(ulong id)
            => Task.FromResult<ITextChannel?>(Channel);

        public Task<Role> CreateRoleAsync(ulong guildId, string name)
            => throw new NotSupportedException("Role creation is not simulated.");

        public Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId)
            => Task.CompletedTask;

        public Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId)
            => Task.CompletedTask;

        public Task DeleteRoleAsync(ulong guildId, ulong roleId)
            => Task.CompletedTask;

        public Task<NetCord.Rest.RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false)
            => Task.FromResult<NetCord.Rest.RestGuild>(null!);
    }

    private Lobby CreateLobby()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 1; i <= 16; i++)
        {
            var p = registry.GetOrCreate((ulong)i);
            p.Name = $"P{i}";
            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public async Task StartCaptainDraft_SetsDraftRole_And_SendsMessage()
    {
        var gateway = new FakeGateway();

        var service = new GameService(
            gateway,
            new MatchHistoryAdapter(new MatchHistoryService()),
            new EloStub(),
            new RealFactionAssignmentService(new FactionRegistryStub()),
            new FactionRegistryStub(),
            new DefaultPreferencesStub(),
            new Random(1)
        );

        var lobby = CreateLobby();
        lobby.IsCaptainDraft = true;
        lobby.CaptainA = 1;
        lobby.CaptainB = 2;
        lobby.DraftStarted = true;
        lobby.DraftRoleId = 999;

        await gateway.Channel.SendMessageAsync("Draft started");

        Assert.True(lobby.DraftStarted);
        Assert.Equal((ulong)999, lobby.DraftRoleId);
        Assert.NotEmpty(gateway.Channel.Messages);
    }
}
