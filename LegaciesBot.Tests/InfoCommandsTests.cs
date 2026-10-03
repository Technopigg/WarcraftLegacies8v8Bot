using LegaciesBot.Discord;

namespace LegaciesBot.Tests;

public class InfoCommandsTests
{
    [Fact]
    public void Version_says_7v7()
    {
        Assert.Contains("7v7", InfoCommands.Version);
    }

    [Fact]
    public void Version_text_shows_build_and_start_times_as_discord_timestamps()
    {
        var built = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var started = new DateTime(2026, 10, 3, 12, 5, 0, DateTimeKind.Utc);

        var text = InfoCommands.BuildVersionText(built, started);

        Assert.Contains(InfoCommands.Version, text);
        Assert.Contains("<t:1791028800:f>", text);
        Assert.Contains("<t:1791029100:R>", text);
    }

    [Fact]
    public void Typo_of_version_is_suggested()
    {
        Assert.Equal("version", CommandSuggester.Suggest("verison"));
    }
}
