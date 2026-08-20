using LegaciesBot.Discord;

public class CommandSuggesterTests
{
    [Theory]
    [InlineData("leaderbord", "leaderboard")] // dropped a letter
    [InlineData("prefss", "prefs")]           // doubled a letter
    [InlineData("stat", "stats")]             // missing a letter
    [InlineData("tpo", "top")]                // adjacent transposition
    [InlineData("joim", "join")]              // substitution
    [InlineData("loby", "lobby")]
    [InlineData("registr", "register")]
    public void Suggests_closest_command_for_obvious_typos(string typo, string expected)
    {
        Assert.Equal(expected, CommandSuggester.Suggest(typo));
    }

    [Theory]
    [InlineData("rank")]   // MEE6-style, not close to any of ours
    [InlineData("mute")]   // 'm' near "mode" but distance 2 on a short word
    [InlineData("levels")]
    [InlineData("help")]   // first letter never matches "bothelp" -> stays quiet (MEE6 owns !help)
    [InlineData("asdf")]
    [InlineData("xp")]
    [InlineData("")]
    [InlineData("j")]      // too short to disambiguate
    public void Stays_quiet_for_non_typos_and_other_bots(string token)
    {
        Assert.Null(CommandSuggester.Suggest(token));
    }

    [Fact]
    public void Exact_command_is_not_suggested()
    {
        // An exact hit is distance 0; the suggester only fires for a near-miss.
        Assert.Null(CommandSuggester.Suggest("top"));
        Assert.Null(CommandSuggester.Suggest("leaderboard"));
    }

    [Fact]
    public void Transposition_counts_as_one_edit()
    {
        Assert.Equal(1, CommandSuggester.DamerauLevenshtein("tpo", "top"));
        Assert.Equal(1, CommandSuggester.DamerauLevenshtein("prefs", "pref"));
        Assert.Equal(0, CommandSuggester.DamerauLevenshtein("stats", "stats"));
    }
}
