using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.GameData;
using NetCord;
using NetCord.Rest;
using LTeam = LegaciesBot.Core.Team;

public class SubstitutePlayerTests
{
    private class TrackingGatewayClient : IGatewayClient
    {
        public List<(ulong userId, ulong roleId)> AddedRoles { get; } = new();
        public List<(ulong userId, ulong roleId)> RemovedRoles { get; } = new();

        public Task<ITextChannel?> GetTextChannelAsync(ulong id) => Task.FromResult<ITextChannel?>(null);
        public Task<RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false) => Task.FromResult<RestGuild>(null!);
        public Task<Role> CreateRoleAsync(ulong guildId, string name) => Task.FromResult<Role>(null!);
        public Task DeleteRoleAsync(ulong guildId, ulong roleId) => Task.CompletedTask;

        public Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId)
        {
            AddedRoles.Add((userId, roleId));
            return Task.CompletedTask;
        }

        public Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId)
        {
            RemovedRoles.Add((userId, roleId));
            return Task.CompletedTask;
        }
    }

    private static GameService CreateService(TrackingGatewayClient? client = null)
    {
        return new GameService(
            client ?? new TrackingGatewayClient(),
            new MatchHistoryAdapter(new MatchHistoryService()),
            new EloStub(),
            new FactionAssignmentStub(),
            new FactionRegistryStub(),
            new DefaultPreferencesStub());
    }

    private static Player MakePlayer(ulong id, string name = "")
    {
        return new Player { DiscordId = id, Name = name.Length > 0 ? name : $"Player{id}" };
    }

    // ── FindGameByPlayer ──────────────────────────────────────────────────────

    [Fact]
    public void FindGameByPlayer_ReturnsNull_WhenNoGames()
    {
        var svc = CreateService();
        Assert.Null(svc.FindGameByPlayer(1UL));
    }

    [Fact]
    public void FindGameByPlayer_FindsPlayer_ByLobbyDuringCaptainDraft()
    {
        var svc = CreateService();
        var lobby = new Lobby();
        var p = MakePlayer(42UL);
        lobby.Players.Add(p);
        lobby.IsLocked = true;

        var game = svc.CreatePendingGameIfMissing(lobby);

        // TeamA/TeamB are not yet set — player is only in lobby.Players.
        Assert.Null(game.TeamA);
        var found = svc.FindGameByPlayer(42UL);
        Assert.Same(game, found);
    }

    [Fact]
    public void FindGameByPlayer_FindsPlayer_ByTeamAfterDraft()
    {
        var svc = CreateService();
        var lobby = new Lobby();
        var p = MakePlayer(7UL);
        lobby.Players.Add(p);
        lobby.IsLocked = true;

        var game = svc.CreatePendingGameIfMissing(lobby);
        var team = new LTeam("Team A");
        team.AddPlayer(p);
        game.TeamA = team;

        var found = svc.FindGameByPlayer(7UL);
        Assert.Same(game, found);
    }

    // ── SubstitutePlayer: active game ─────────────────────────────────────────

    [Fact]
    public async Task Sub_ActiveGame_ReplacesInTeamAndTransfersTeamRole()
    {
        var client = new TrackingGatewayClient();
        var svc = CreateService(client);

        var outP = MakePlayer(1UL);
        outP.AssignedFaction = "Lordaeron";
        var inP = MakePlayer(2UL);

        var lobby = new Lobby();
        lobby.Players.Add(outP);
        lobby.IsLocked = true;

        var game = svc.CreatePendingGameIfMissing(lobby);
        var teamA = new LTeam("Team A");
        teamA.AddPlayer(outP);
        game.TeamA = teamA;
        game.TeamB = new LTeam("Team B");
        game.IsActive = true;

        var (team, faction) = await svc.SubstitutePlayer(game, outP, inP);

        Assert.NotNull(team);
        Assert.Equal("Team A", team!.Name);
        Assert.Equal("Lordaeron", faction);
        Assert.Contains(inP, teamA.Players);
        Assert.DoesNotContain(outP, teamA.Players);
        // Team role transferred.
        Assert.Contains(client.RemovedRoles, r => r.userId == 1UL);
        Assert.Contains(client.AddedRoles, r => r.userId == 2UL);
    }

    // ── SubstitutePlayer: undrafted player in lobby ───────────────────────────

    [Fact]
    public async Task Sub_DraftPhase_UndraftedPlayer_ReplacesInLobby()
    {
        var svc = CreateService();

        var outP = MakePlayer(10UL);
        var inP  = MakePlayer(20UL);

        var lobby = new Lobby();
        lobby.Players.Add(outP);
        lobby.IsLocked = true;
        lobby.DraftStarted = true;
        lobby.IsCaptainDraft = true;
        // TeamA/TeamB are null — draft still in progress.

        var game = svc.CreatePendingGameIfMissing(lobby);

        var (team, _) = await svc.SubstitutePlayer(game, outP, inP);

        Assert.Null(team);
        Assert.Contains(inP, lobby.Players);
        Assert.DoesNotContain(outP, lobby.Players);
    }

    // ── SubstitutePlayer: already-picked player in TeamAPicks ────────────────

    [Fact]
    public async Task Sub_DraftPhase_PickedPlayerTeamA_ReplacesInPicksAndLobby()
    {
        var svc = CreateService();

        var outP = MakePlayer(10UL);
        var inP  = MakePlayer(20UL);

        var lobby = new Lobby();
        lobby.Players.Add(outP);
        lobby.IsLocked = true;
        lobby.DraftStarted = true;
        lobby.IsCaptainDraft = true;
        lobby.TeamAPicks.Add(outP.DiscordId);

        var game = svc.CreatePendingGameIfMissing(lobby);

        await svc.SubstitutePlayer(game, outP, inP);

        Assert.Contains(inP.DiscordId, lobby.TeamAPicks);
        Assert.DoesNotContain(outP.DiscordId, lobby.TeamAPicks);
        Assert.Contains(inP, lobby.Players);
        Assert.DoesNotContain(outP, lobby.Players);
    }

    [Fact]
    public async Task Sub_DraftPhase_PickedPlayerTeamB_ReplacesInPicksAndLobby()
    {
        var svc = CreateService();

        var outP = MakePlayer(10UL);
        var inP  = MakePlayer(20UL);

        var lobby = new Lobby();
        lobby.Players.Add(outP);
        lobby.IsLocked = true;
        lobby.DraftStarted = true;
        lobby.IsCaptainDraft = true;
        lobby.TeamBPicks.Add(outP.DiscordId);

        var game = svc.CreatePendingGameIfMissing(lobby);

        await svc.SubstitutePlayer(game, outP, inP);

        Assert.Contains(inP.DiscordId, lobby.TeamBPicks);
        Assert.DoesNotContain(outP.DiscordId, lobby.TeamBPicks);
    }

    // ── SubstitutePlayer: DraftRole is transferred during captain draft ───────

    [Fact]
    public async Task Sub_DraftPhase_TransfersDraftRole()
    {
        var client = new TrackingGatewayClient();
        var svc = CreateService(client);

        var outP = MakePlayer(10UL);
        var inP  = MakePlayer(20UL);

        ulong draftRoleId = 999UL;

        var lobby = new Lobby();
        lobby.Players.Add(outP);
        lobby.IsLocked = true;
        lobby.DraftStarted = true;
        lobby.IsCaptainDraft = true;
        lobby.DraftRoleId = draftRoleId;

        var game = svc.CreatePendingGameIfMissing(lobby);

        await svc.SubstitutePlayer(game, outP, inP);

        Assert.Contains(client.RemovedRoles, r => r.userId == 10UL && r.roleId == draftRoleId);
        Assert.Contains(client.AddedRoles, r => r.userId == 20UL && r.roleId == draftRoleId);
    }

    // ── No role transfer without DraftRoleId and game not active ─────────────

    [Fact]
    public async Task Sub_DraftPhase_NoDraftRole_NoRoleOperations()
    {
        var client = new TrackingGatewayClient();
        var svc = CreateService(client);

        var outP = MakePlayer(10UL);
        var inP  = MakePlayer(20UL);

        var lobby = new Lobby();
        lobby.Players.Add(outP);
        lobby.IsLocked = true;
        lobby.DraftRoleId = null;

        var game = svc.CreatePendingGameIfMissing(lobby);

        await svc.SubstitutePlayer(game, outP, inP);

        Assert.Empty(client.AddedRoles);
        Assert.Empty(client.RemovedRoles);
    }
}
