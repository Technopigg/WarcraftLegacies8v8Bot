using NetCord;
using NetCord.Gateway;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Discord;

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

var lobbyService = new LobbyService();
var gameService = new GameService(client);
var playerDataService = new PlayerDataService();
var playerStatsService = new PlayerStatsService();




var commandService = new CommandService<CommandContext>();
commandService.AddModule<LobbyCommands>();
commandService.AddModule<GameCommands>();
commandService.AddModule<StatsCommands>();

client.MessageCreate += async message =>
{
    if (message.Author.IsBot || !message.Content.StartsWith('!'))
        return;

    var ctx = new CommandContext(message, client);
    await commandService.ExecuteAsync(
        1,
        ctx,
        new SimpleServiceProvider(lobbyService, gameService, playerDataService, playerStatsService)
    );
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

public class SimpleServiceProvider : IServiceProvider
{
    private readonly LobbyService _lobbyService;
    private readonly GameService _gameService;
    private readonly PlayerDataService _playerDataService;
    private readonly PlayerStatsService _playerStatsService;

    
    public SimpleServiceProvider(LobbyService lobbyService, GameService gameService, PlayerDataService playerDataService, PlayerStatsService playerStatsService)
    {
        _lobbyService = lobbyService;
        _gameService = gameService;
        _playerDataService = playerDataService;
        _playerStatsService = playerStatsService;

    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(LobbyService)) return _lobbyService;
        if (serviceType == typeof(GameService)) return _gameService;
        if (serviceType == typeof(PlayerDataService)) return _playerDataService;
        if (serviceType == typeof(PlayerStatsService)) return _playerStatsService;
        return null;
    }
}