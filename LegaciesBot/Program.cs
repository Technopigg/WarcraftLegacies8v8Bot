using NetCord;
using NetCord.Gateway;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Discord;

var token = "MTQ3MTUyOTIyMDAxNzc1MDA4OA.GrESoW.Beh1GiFbCzSg9oDPKHiE-gLVEnV2MSGwYGnzMM";

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

var commandService = new CommandService<CommandContext>();
commandService.AddModule<LobbyCommands>();
commandService.AddModule<GameCommands>();

client.MessageCreate += async message =>
{
    if (message.Author.IsBot || !message.Content.StartsWith('!'))
        return;

    var ctx = new CommandContext(message, client);
    await commandService.ExecuteAsync(
        1,
        ctx,
        new SimpleServiceProvider(lobbyService, gameService)
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

    public SimpleServiceProvider(LobbyService lobbyService, GameService gameService)
    {
        _lobbyService = lobbyService;
        _gameService = gameService;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(LobbyService)) return _lobbyService;
        if (serviceType == typeof(GameService)) return _gameService;
        return null;
    }
}