using LegaciesBot;
using LegaciesBot.Commands;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Discord;
using LegaciesBot.Services.CaptainDraft;
using Microsoft.Extensions.DependencyInjection;
using LegaciesBot.Seasons;

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

var matchHistoryService = new MatchHistoryService();
var playerDataService = new PlayerDataService();
var playerStatsService = new PlayerStatsService();
var playerRegistryService = new PlayerRegistryService();
var seasonService = new SeasonService();
var lobbyService = new LobbyService(playerRegistryService);

GlobalServices.LobbyService = lobbyService;

var gateway = new RealGatewayClient(client);
var matchHistory = new RealMatchHistoryService(matchHistoryService);
var elo = new RealEloService(playerStatsService, seasonService);
var factionAssignment = new RealFactionAssignmentService();
var factionRegistry = new RealFactionRegistry();
var defaultPreferences = new RealDefaultPreferences();

var nicknameService = new NicknameService(playerRegistryService);
var captainDraftService = new CaptainDraftService();
var factionManualAssignmentService = new FactionManualAssignmentService(factionRegistry, nicknameService);

var gameService = new GameService(
    gateway,
    matchHistory,
    elo,
    factionAssignment,
    factionRegistry,
    defaultPreferences
);

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

client.MessageCreate += async message =>
{
    if (message.Author.IsBot || !message.Content.StartsWith('!'))
        return;

    var ctx = new CommandContext(message, client);
    await commandService.ExecuteAsync(1, ctx, services);
};

client.Ready += args =>
{
    Console.WriteLine("Bot is online!");
    return new ValueTask();
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
