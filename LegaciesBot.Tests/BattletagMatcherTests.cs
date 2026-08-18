using LegaciesBot.Services;

namespace LegaciesBot.Tests;

public class BattletagMatcherTests
{
    private static PlayerSearchResult Candidate(string battletag) =>
        new(PlayerKey: battletag, DisplayName: BattletagMatcher.NameFromBattletag(battletag),
            Battletag: battletag, Rating: 1000, Sigma: 0, MatchesCount: 0,
            WinsCount: 0, LossesCount: 0, Winrate: 0, ProfileUrl: "");

    [Fact]
    public void Evaluate_ReturnsExact_WhenNameMatchesExactly()
    {
        var match = BattletagMatcher.Evaluate("Technopig", [Candidate("Technopig#1234")]);

        var exact = Assert.IsType<BattletagMatch.Exact>(match);
        Assert.Equal("Technopig#1234", exact.Player.Battletag);
    }

    [Fact]
    public void Evaluate_ReturnsSuggested_ForCloseMisspelling()
    {
        var match = BattletagMatcher.Evaluate("Royke", [Candidate("Royce#1989")]);

        var suggested = Assert.IsType<BattletagMatch.Suggested>(match);
        Assert.Equal("Royce#1989", suggested.Player.Battletag);
    }

    [Fact]
    public void Evaluate_ReturnsAmbiguous_WhenTwoExactMatchesTie()
    {
        var match = BattletagMatcher.Evaluate(
            "Technopig", [Candidate("Technopig#1234"), Candidate("Technopig#5678")]);

        var ambiguous = Assert.IsType<BattletagMatch.Ambiguous>(match);
        Assert.Equal(2, ambiguous.Candidates.Count);
    }

    [Fact]
    public void Evaluate_ReturnsAmbiguous_WhenTwoFuzzyMatchesTie()
    {
        var match = BattletagMatcher.Evaluate(
            "Technopig", [Candidate("Technopigg#1234"), Candidate("Technodig#5678")]);

        var ambiguous = Assert.IsType<BattletagMatch.Ambiguous>(match);
        Assert.Equal(2, ambiguous.Candidates.Count);
    }

    [Fact]
    public void Evaluate_ReturnsNone_WhenNoCandidateIsClose()
    {
        var match = BattletagMatcher.Evaluate("Royke", [Candidate("Zephyrix#4402")]);

        Assert.IsType<BattletagMatch.None>(match);
    }

    [Fact]
    public void Evaluate_ReturnsNone_WhenNoCandidates()
    {
        var match = BattletagMatcher.Evaluate("Royke", []);

        Assert.IsType<BattletagMatch.None>(match);
    }

    [Fact]
    public void NameFromBattletag_StripsDiscriminator()
    {
        Assert.Equal("Royce", BattletagMatcher.NameFromBattletag("Royce#1989"));
    }
}
