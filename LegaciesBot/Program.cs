using LegaciesBot;
using LegaciesBot.Commands;
using LegaciesBot.Config;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Discord;
using LegaciesBot.Services.CaptainDraft;
using Microsoft.Extensions.DependencyInjection;
using LegaciesBot.Seasons;
using NetCord.Rest;

string token = Environment.GetEnvironmentVariable("WL8v8_BOT_TOKEN");

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Error: Discord bot token not set in environment variables!");
    return;
}

var client = new GatewayClient(
    new BotToken(token),
    new GatewayClientConfiguration
    {
        Intents = GatewayIntents.GuildMessages
                  | GatewayIntents.DirectMessages
                  | GatewayIntents.MessageContent
    }
);

var httpClient = new HttpClient();
var replayUploadService = new ReplayUploadService(httpClient, DiscordConfig.SiteBaseUrl, DiscordConfig.SiteApiToken);
GlobalServices.SiteApiService = new SiteApiService(httpClient, DiscordConfig.SiteBaseUrl, DiscordConfig.SiteApiToken);

var matchHistoryService = new MatchHistoryService();
var playerDataService = new PlayerDataService();
var playerStatsService = new PlayerStatsService("player_stats.json");
var playerRegistryService = new PlayerRegistryService();
var seasonService = new SeasonService();

var gateway = new RealGatewayClient(client);
var matchHistory = new RealMatchHistoryService(matchHistoryService);
var elo = new RealEloService(playerStatsService, seasonService);

var factionRegistry = new RealFactionRegistry();
var factionAssignment = new RealFactionAssignmentService(factionRegistry);
var defaultPreferences = new RealDefaultPreferences();

var nicknameService = new NicknameService(playerRegistryService);
var captainDraftService = new CaptainDraftService();

var gameService = new GameService(
    gateway,
    matchHistory,
    elo,
    factionAssignment,
    factionRegistry,
    defaultPreferences
);

var lobbyService = new LobbyService(playerRegistryService, gameService);

var factionManualAssignmentService = new FactionManualAssignmentService(
    factionRegistry,
    nicknameService,
    gameService
);

GlobalServices.PermissionService = new PermissionService("permissions.json");
GlobalServices.LobbyService = lobbyService;
GlobalServices.GameService = gameService;
GlobalServices.PlayerDataService = playerDataService;
GlobalServices.PlayerStatsService = playerStatsService;
GlobalServices.PlayerRegistryService = playerRegistryService;
GlobalServices.MatchHistoryService = matchHistoryService;
GlobalServices.NicknameService = nicknameService;
GlobalServices.FactionManualAssignmentService = factionManualAssignmentService;
GlobalServices.CaptainDraftService = captainDraftService;
GlobalServices.SeasonService = seasonService;
GlobalServices.FactionAssignmentService = factionAssignment;

var services = new ServiceCollection()
    .AddSingleton<ILobbyService>(lobbyService)
    .AddSingleton<LobbyService>(lobbyService)
    .AddSingleton<ICaptainDraftService>(captainDraftService)
    .AddSingleton(playerDataService)
    .AddSingleton(playerStatsService)
    .AddSingleton(seasonService)
    .AddSingleton(matchHistoryService)
    .AddSingleton(playerRegistryService)
    .AddSingleton(nicknameService)
    .AddSingleton(factionManualAssignmentService)
    .AddSingleton(gameService)
    .BuildServiceProvider();

var commandService = new CommandService<CommandContext>();

commandService.AddModule(typeof(AdminCommands));
commandService.AddModule(typeof(ModeCommands));
commandService.AddModule(typeof(ModerationCommands));
commandService.AddModule(typeof(LobbyCommands));
commandService.AddModule(typeof(CaptainCommands));
commandService.AddModule(typeof(StatsCommands));
commandService.AddModule(typeof(SeasonCommands));
commandService.AddModule(typeof(GameCommands));
commandService.AddModule(typeof(FactionCommands));
commandService.AddModule(typeof(DebugCommands));

client.MessageCreate += async message =>
{
    if (message.Author.IsBot || !message.Content.StartsWith('!'))
        return;

    if (message.GuildId is { } guildId && guildId != DiscordConfig.GuildId)
        return;

    if (DiscordConfig.CommandChannelId != 0 && message.ChannelId != DiscordConfig.CommandChannelId)
        return;

    var ctx = new CommandContext(message, client);
    await commandService.ExecuteAsync(1, ctx, services);
};

client.MessageCreate += async message =>
{
    if (message.Author.IsBot) return;
    if (DiscordConfig.ReplayChannelId == 0) return;
    if (message.ChannelId != DiscordConfig.ReplayChannelId) return;

    var replays = message.Attachments
        .Where(a => a.FileName.EndsWith(".w3g", StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (replays.Count == 0) return;

    foreach (var attachment in replays)
    {
        byte[] data;
        try
        {
            data = await httpClient.GetByteArrayAsync(attachment.Url);
        }
        catch (Exception ex)
        {
            await client.Rest.SendMessageAsync(message.ChannelId,
                new MessageProperties().WithContent($"Failed to download `{attachment.FileName}`: {ex.Message}"));
            continue;
        }

        var discordCtx = new DiscordUploadContext(
            GuildId: message.GuildId ?? 0,
            ChannelId: message.ChannelId,
            MessageId: message.Id,
            AttachmentId: attachment.Id,
            UploaderId: message.Author.Id
        );
        var result = await replayUploadService.UploadAsync(data, attachment.FileName, discordCtx);
        var embed = BuildReplayEmbed(result, attachment.FileName, DiscordConfig.SiteBaseUrl);

        await client.Rest.SendMessageAsync(message.ChannelId,
            new MessageProperties().WithEmbeds([embed]).WithMessageReference(
                MessageReferenceProperties.Reply(message.Id, failIfNotExists: false)));
    }
};

static EmbedProperties BuildReplayEmbed(ReplayUploadResult result, string filename, string siteBaseUrl)
{
    if (result.NetworkError != null)
        return EmbedFactory.Error("Upload failed", $"`{filename}`\n{result.NetworkError}");

    if (result.RateLimited)
        return EmbedFactory.Warning("Rate limit", $"`{filename}`\nToo many uploads — try again in a minute.");

    if (result.TooLarge)
        return EmbedFactory.Error("File too large", $"`{filename}`");

    if (result.IsSuccess)
    {
        // The rating pool says which ladder a match would belong to, not whether it counts.
        // Only the site's match status does — a game on a map outside the season is recorded
        // but not ranked, and telling the player otherwise is a lie.
        string verdict = ReplayVerdict.Describe(result.MatchStatus, result.RatingPool, result.RankedExclusionReason);
        string desc = $"`{filename}`\n**{verdict}** — {result.PlayersCount} players — {result.MapVersion}";
        if (result.MatchPublicId != null)
            desc += $"\n{siteBaseUrl}/replays/{result.MatchPublicId}";

        return ReplayVerdict.IsNotRanked(result.MatchStatus)
            ? EmbedFactory.Warning("Match recorded, not ranked", desc)
            : EmbedFactory.Success("Match recorded", desc);
    }

    if (result.IsDuplicate)
    {
        string desc = $"`{filename}`\n{ReplayVerdict.DescribeDuplicate(result.Reason)}.";
        if (result.MatchPublicId != null)
            desc += $"\n{siteBaseUrl}/replays/{result.MatchPublicId}";
        return EmbedFactory.Warning("Duplicate", desc);
    }

    if (result.IsRejected)
        return EmbedFactory.Error("Rejected", $"`{filename}`\n{ReplayVerdict.DescribeRejection(result.Reason)}");

    return EmbedFactory.Error("Unexpected response", $"`{filename}` — {result.Status}");
}

client.Ready += args =>
{
    Console.WriteLine($"Bot is online! Guild={DiscordConfig.GuildId} Team1Role={DiscordConfig.Team1RoleId} Team2Role={DiscordConfig.Team2RoleId}");
    _ = ValidateRoleConfigAsync();
    return new ValueTask();
};

async Task ValidateRoleConfigAsync()
{
    try
    {
        var guild = await client.Rest.GetGuildAsync(DiscordConfig.GuildId);
        var roleIds = guild.Roles.Keys.ToHashSet();

        if (!roleIds.Contains(DiscordConfig.Team1RoleId))
            Console.WriteLine($"[WARN] Team1 role {DiscordConfig.Team1RoleId} not found in guild. Set WL_DISCORD_TEAM1_ROLE_ID to override.");

        if (!roleIds.Contains(DiscordConfig.Team2RoleId))
            Console.WriteLine($"[WARN] Team2 role {DiscordConfig.Team2RoleId} not found in guild. Set WL_DISCORD_TEAM2_ROLE_ID to override.");

        if (roleIds.Contains(DiscordConfig.Team1RoleId) && roleIds.Contains(DiscordConfig.Team2RoleId))
            Console.WriteLine("[OK] Team1 and Team2 roles verified in guild.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Could not validate role config: {ex.Message}");
    }
};

_ = Task.Run(async () =>
{
    while (true)
    {
        lobbyService.CheckAfk();
        await Task.Delay(TimeSpan.FromMinutes(1));
    }
});

await client.StartAsync();
await Task.Delay(-1);
