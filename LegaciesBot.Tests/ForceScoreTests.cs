using LegaciesBot.Discord;

namespace LegaciesBot.Tests;

public class ForceScoreTests
{
    [Theory]
    [InlineData(new[] { 1, 0 }, null, 1, 0)]
    [InlineData(new[] { 0, 1 }, null, 0, 1)]
    [InlineData(new[] { 0, 0 }, null, 0, 0)]
    [InlineData(new[] { 3, 1, 0 }, 3, 1, 0)]
    public void Accepts_win_loss_or_draw_with_optional_game_number(int[] args, int? game, int a, int b)
    {
        Assert.True(GameCommands.TryParseForceScore(args, out var gameId, out var scoreA, out var scoreB, out _));
        Assert.Equal(game, gameId);
        Assert.Equal(a, scoreA);
        Assert.Equal(b, scoreB);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 1 })]
    [InlineData(new[] { 2, 0 })]
    [InlineData(new[] { 5, 3 })]
    [InlineData(new[] { 1, 2, 0, 1 })]
    public void Rejects_anything_else_with_usage_help(int[] args)
    {
        Assert.False(GameCommands.TryParseForceScore(args, out _, out _, out _, out var error));
        Assert.Contains("!forcescore 1 0", error);
    }
}
