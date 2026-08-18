using LegaciesBot.GameData;

namespace LegaciesBot.Tests;

public class FactionParserTests
{
    // The exact case from the live channel: commas + a space-separated faction ("fel horde")
    // used to collapse to just "Illidari". It must now keep every faction.
    [Fact]
    public void Parse_HandlesCommasAndMultiWordFactions()
    {
        var result = FactionParser.Parse("quel'thalas, Dalaran, fel horde, illidari");

        Assert.Equal(new[] { "Quel'thalas", "Dalaran", "Fel Horde", "Illidari" }, result.Accepted);
        Assert.Empty(result.Unknown);
    }

    [Theory]
    [InlineData("Fel Horde", "Fel Horde")]
    [InlineData("The Exodar", "The Exodar")]
    [InlineData("Black Empire", "Black Empire")]
    [InlineData("\"Fel Horde\"", "Fel Horde")]
    public void Parse_MatchesMultiWordNames(string input, string expected)
    {
        var result = FactionParser.Parse(input);
        Assert.Equal(new[] { expected }, result.Accepted);
        Assert.Empty(result.Unknown);
    }

    [Theory]
    [InlineData("illidan", "Illidari")]
    [InlineData("illidani", "Illidari")]
    [InlineData("draenei", "The Exodar")]
    [InlineData("quel", "Quel'thalas")]
    [InlineData("fel", "Fel Horde")]
    public void Parse_ResolvesAliases(string input, string expected)
    {
        var result = FactionParser.Parse(input);
        Assert.Equal(new[] { expected }, result.Accepted);
    }

    // Issue #13: short prefixes like "!pref Quel dal storm".
    [Fact]
    public void Parse_ResolvesUniquePrefixes()
    {
        var result = FactionParser.Parse("quel dal storm");
        Assert.Equal(new[] { "Quel'thalas", "Dalaran", "Stormwind" }, result.Accepted);
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void Parse_DoesNotGuessTooShortOrAmbiguous()
    {
        // "s" is a prefix of many factions and is too short: it must not be accepted.
        var result = FactionParser.Parse("s");
        Assert.Empty(result.Accepted);
        Assert.Equal(new[] { "s" }, result.Unknown);
    }

    [Fact]
    public void Parse_ForgivesOneCharTypo()
    {
        var result = FactionParser.Parse("iroforge");
        Assert.Equal(new[] { "Ironforge" }, result.Accepted);
    }

    [Fact]
    public void Parse_GreedyDoesNotSplitAdjacentFactions()
    {
        var result = FactionParser.Parse("Fel Horde Illidari");
        Assert.Equal(new[] { "Fel Horde", "Illidari" }, result.Accepted);
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void Parse_ReportsUnknownTokensInsteadOfDropping()
    {
        var result = FactionParser.Parse("Scourge fish Legion");
        Assert.Equal(new[] { "Scourge", "Legion" }, result.Accepted);
        Assert.Equal(new[] { "fish" }, result.Unknown);
    }

    [Fact]
    public void Parse_PreservesOrderAndDeduplicates()
    {
        var result = FactionParser.Parse("Dalaran Scourge dalaran");
        Assert.Equal(new[] { "Dalaran", "Scourge" }, result.Accepted);
    }

    // Adversarial cases proven in the live test — locked here so they cannot regress.
    [Fact]
    public void Parse_StripsEmojiAndPunctuationAroundAName()
    {
        var result = FactionParser.Parse("🔥illidan🔥");
        Assert.Equal(new[] { "Illidari" }, result.Accepted);
    }

    [Fact]
    public void Parse_ReportsEveryUnknownToken()
    {
        var result = FactionParser.Parse("fish chips banana");
        Assert.Empty(result.Accepted);
        Assert.Equal(new[] { "fish", "chips", "banana" }, result.Unknown);
    }

    [Fact]
    public void Parse_NumbersAndSymbolsAreNeverFactions()
    {
        var result = FactionParser.Parse("12345 !@#$%");
        Assert.Empty(result.Accepted);
    }

    [Fact]
    public void Parse_HandlesFiveMultiWordAndApostropheFactionsAtOnce()
    {
        var result = FactionParser.Parse("kul'tiras an'qiraj black empire the exodar fel horde");
        Assert.Equal(
            new[] { "Kul'tiras", "An'qiraj", "Black Empire", "The Exodar", "Fel Horde" },
            result.Accepted);
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void Parse_DeduplicatesCaseInsensitively()
    {
        var result = FactionParser.Parse("Scourge scourge SCOURGE");
        Assert.Equal(new[] { "Scourge" }, result.Accepted);
    }

    [Fact]
    public void Parse_EmptyInputYieldsNothing()
    {
        var result = FactionParser.Parse("   ");
        Assert.Empty(result.Accepted);
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void Parse_AllTwentyCanonicalNamesRoundTrip()
    {
        foreach (var name in FactionParser.CanonicalNames)
        {
            var result = FactionParser.Parse(name);
            Assert.Equal(new[] { name }, result.Accepted);
        }
    }
}
