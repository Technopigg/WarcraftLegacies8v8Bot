using LegaciesBot.Discord;

namespace LegaciesBot.Tests;

// A command called without its required arguments used to answer nothing at all: the
// router reports a parameter mismatch and nobody handled it. Nine commands behaved that
// way, so the player could not tell a dead bot from a mistyped command. The usage text
// comes from the same CommandList that !bothelp prints, and these tests hold that link:
// renaming a command in CommandList without updating it here, or the other way round,
// fails rather than silently going quiet again.
public class CommandUsageTests
{
    [Theory]
    [InlineData("compare")]
    [InlineData("nickname")]
    [InlineData("warns")]
    [InlineData("warn")]
    [InlineData("removewarn")]
    [InlineData("ban")]
    [InlineData("unban")]
    [InlineData("assignf")]
    [InlineData("draft")]
    public void Every_command_that_needs_arguments_has_a_usage_line(string command)
    {
        var usage = CommandUsage.For(command);
        Assert.False(string.IsNullOrWhiteSpace(usage), $"!{command} has no line in CommandList");
        Assert.Contains($"!{command}", usage);
    }

    [Fact]
    public void An_alias_finds_the_line_of_the_command_it_stands_for()
    {
        // CommandList spells this one "!draft <player> / !d <player>".
        Assert.Equal(CommandUsage.For("draft"), CommandUsage.For("d"));
        Assert.Contains("!draft", CommandUsage.For("d"));
    }

    [Fact]
    public void A_command_with_two_shapes_returns_both()
    {
        var usage = CommandUsage.For("nickname");
        Assert.Contains("set your own display nickname", usage);
        Assert.Contains("(mod/admin)", usage);
    }

    [Fact]
    public void Lookup_ignores_case()
    {
        Assert.Equal(CommandUsage.For("compare"), CommandUsage.For("COMPARE"));
    }

    [Fact]
    public void An_unknown_command_has_no_usage_rather_than_a_wrong_one()
    {
        Assert.Null(CommandUsage.For("zzzqqq"));
    }

    [Fact]
    public void A_command_mentioned_only_in_a_description_does_not_borrow_that_line()
    {
        // Descriptions mention other commands ("already in? !j refreshes your spot"), and
        // only the part before the dash may claim a line. !j must answer with its own.
        var usage = CommandUsage.For("j");
        Assert.NotNull(usage);
        Assert.Contains("join the lobby", usage);
    }
}
