using LegaciesBot.Services;

namespace LegaciesBot.Tests;

public class BattletagMatcherTests
{
    private static PlayerSearchResult Candidate(string battletag) =>
        new(PlayerKey: battletag, DisplayName: BattletagMatcher.NameFromBattletag(battletag),
            Battletag: battletag, Rating: 1000, Sigma: 0, MatchesCount: 0,
            WinsCount: 0, LossesCount: 0, Winrate: 0, ProfileUrl: "");

    [Fact]
    public void FindStrongMatch_MatchesCloseMisspelling()
    {
        var match = BattletagMatcher.FindStrongMatch("Royke", [Candidate("Royce#1989")]);

        Assert.NotNull(match);
        Assert.Equal("Royce#1989", match!.Battletag);
    }

    [Fact]
    public void FindStrongMatch_ReturnsNull_WhenNoCandidateIsClose()
    {
        var match = BattletagMatcher.FindStrongMatch("Royke", [Candidate("Zephyrix#4402")]);

        Assert.Null(match);
    }

    [Fact]
    public void FindStrongMatch_ReturnsNull_WhenTwoCandidatesAreEquallyClose()
    {
        var match = BattletagMatcher.FindStrongMatch(
            "Royke", [Candidate("Royce#1989"), Candidate("Royle#2001")]);

        Assert.Null(match);
    }

    [Fact]
    public void FindStrongMatch_ReturnsNull_WhenNoCandidates()
    {
        var match = BattletagMatcher.FindStrongMatch("Royke", []);

        Assert.Null(match);
    }

    [Fact]
    public void NameFromBattletag_StripsDiscriminator()
    {
        Assert.Equal("Royce", BattletagMatcher.NameFromBattletag("Royce#1989"));
    }
}
