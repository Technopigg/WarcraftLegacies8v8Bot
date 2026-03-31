using LegaciesBot.Discord;
using LegaciesBot.Moderation;
using LegaciesBot.Services;
using Moq;

namespace LegaciesBot.Tests;

public class TestResponder : IMessageResponder
{
    public List<string> Messages { get; } = new();

    public Task ReplyAsync(string message)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}

public class ModerationCommandsTests
{
    private void CleanFiles()
    {
        if (File.Exists("moderation.json"))
            File.Delete("moderation.json");

        if (File.Exists("players.json"))
            File.Delete("players.json");
    }

    private (ModerationCommands commands,
             TestResponder responder,
             ModerationService mod,
             Mock<PermissionService> permMock,
             NicknameService nickname,
             PlayerRegistryService registry)
        CreateCommands()
    {
        CleanFiles();

        var responder = new TestResponder();

        var mod = new ModerationService(
            TimeSpan.FromMinutes(5),
            3
        );

        var permMock = new Mock<PermissionService>() { CallBase = true };
        permMock.Setup(p => p.IsModeratorOrAdmin(It.IsAny<ulong>())).Returns(true);

        var registry = new PlayerRegistryService("players.json");
        var nickname = new NicknameService(registry);

        var userContext = new TestUserContext { UserId = 1 };

        var commands = new ModerationCommands(
            mod,
            permMock.Object,
            nickname,
            responder,
            userContext
        );

        return (commands, responder, mod, permMock, nickname, registry);
    }

    private void RegisterPlayer(PlayerRegistryService registry, ulong id, string name)
    {
        registry.RegisterPlayer(id, name);
    }

    [Fact]
    public async Task Warn_SendsMessage_WhenValid()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        await commands.WarnAsync("5", "test");

        Assert.Equal("Warned <@5>: test", responder.Messages[0]);
    }

    [Fact]
    public async Task Warn_TriggersAutoBan()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        mod.AddWarning(5, 1, "a");
        mod.AddWarning(5, 1, "b");

        await commands.WarnAsync("5", "c");

        Assert.Equal("Warned <@5>: c", responder.Messages[0]);
        Assert.Equal("<@5> has been automatically banned: Reached warning threshold", responder.Messages[1]);
    }

    [Fact]
    public async Task Warn_RejectsInvalidUser()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        await commands.WarnAsync("bad", "test");

        Assert.Equal("Could not resolve user.", responder.Messages[0]);
    }

    [Fact]
    public async Task Unwarn_RemovesWarning_AndUnbansIfNeeded()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        mod.AddWarning(5, 1, "a");
        mod.AddWarning(5, 1, "b");
        mod.AddWarning(5, 1, "c");

        Assert.True(mod.IsBanned(5));

        await commands.RemoveWarnAsync("5", 2);

        Assert.Equal("Removed warning 2 from <@5>.", responder.Messages[0]);
        Assert.Equal("Unbanned <@5>.", responder.Messages[1]);
    }

    [Fact]
    public async Task Unwarn_ShowsUsage_WhenMissingArgs()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        await commands.RemoveWarnUsageAsync("5");

        Assert.Equal("Usage: !removewarn <user> <index>", responder.Messages[0]);
    }

    [Fact]
    public async Task Ban_SendsMessage()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        await commands.BanAsync("5", "bad");

        Assert.Equal("Banned <@5>: bad", responder.Messages[0]);
    }

    [Fact]
    public async Task Unban_SendsMessage_WhenSuccessful()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        mod.AddBan(5, 1, "bad");

        await commands.UnbanAsync("5");

        Assert.Equal("Unbanned <@5>.", responder.Messages[0]);
    }

    [Fact]
    public async Task Warns_ShowsBanStatus_AndWarnings()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        mod.AddBan(5, 1, "bad");
        mod.AddWarning(5, 1, "test");

        await commands.WarningsAsync("5");

        Assert.Contains("is currently **banned**", responder.Messages[0]);
        Assert.Contains("Warnings for <@5>:", responder.Messages[1]);
        Assert.Contains("test", responder.Messages[1]);
    }

    [Fact]
    public async Task Warns_ShowsNoWarnings()
    {
        var (commands, responder, mod, perm, nick, registry) = CreateCommands();

        RegisterPlayer(registry, 5, "5");

        await commands.WarningsAsync("5");

        Assert.Equal("<@5> is **not banned**.", responder.Messages[0]);
        Assert.Equal("No active warnings.", responder.Messages[1]);
    }
}
