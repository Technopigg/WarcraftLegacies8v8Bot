using LegaciesBot.Services;

namespace LegaciesBot.Tests;

/// <summary>
/// The sentence every uploader reads. It was wrong for the whole of Season 5's
/// first weeks — the embed read the rating pool and told people their game was
/// ranked when the site had recorded it as not ranked.
/// </summary>
public class ReplayVerdictTests
{
    [Fact]
    public void RankedMatchNamesThePool()
    {
        Assert.Equal("Ranked (discord pool)", ReplayVerdict.Describe("ranked", "discord", null));
        Assert.Equal("Ranked (public pool)", ReplayVerdict.Describe("ranked", "public", null));
    }

    [Fact]
    public void APoolAloneNeverMakesAMatchRanked()
    {
        // The exact confusion this replaced: discord pool, but the site did not
        // count the game.
        var verdict = ReplayVerdict.Describe("not_ranked", "discord", "unregistered_map_version");

        Assert.StartsWith("Not ranked", verdict);
        Assert.DoesNotContain("Ranked (", verdict);
        Assert.True(ReplayVerdict.IsNotRanked("not_ranked"));
    }

    [Fact]
    public void AMatchAwaitingReviewIsNotCalledRanked()
    {
        var verdict = ReplayVerdict.Describe("pending_result", "discord", null);

        Assert.Equal("Awaiting review (discord pool)", verdict);
        Assert.False(ReplayVerdict.IsNotRanked("pending_result"));
    }

    [Fact]
    public void AnUnknownStatusIsTreatedAsAwaitingReview()
    {
        // Better to under-promise than to announce a result the site has not given.
        Assert.Equal("Awaiting review (discord pool)", ReplayVerdict.Describe(null, "discord", null));
        Assert.Equal("Awaiting review (discord pool)", ReplayVerdict.Describe("something_new", "discord", null));
    }

    [Theory]
    [InlineData("unregistered_map_version", "this map version is not part of the season")]
    [InlineData("map_version_not_ranked", "this map version does not count towards the rating")]
    [InlineData("single_player_replay", "single player replays do not count")]
    [InlineData("merged_profile_duplicate_participation", "two merged accounts played this match; it is back in review")]
    public void EveryReasonTheSiteCanSendIsSpelledOut(string code, string expected)
    {
        Assert.Equal(expected, ReplayVerdict.DescribeExclusion(code));
        Assert.Contains(expected, ReplayVerdict.Describe("not_ranked", "discord", code));
    }

    [Fact]
    public void AMissingReasonPointsAtTheMatchPage()
    {
        Assert.Equal("see the match page for details", ReplayVerdict.DescribeExclusion(null));
    }

    [Fact]
    public void AnUnknownReasonIsPassedThroughRatherThanSwallowed()
    {
        // If the site grows a new code, the player sees it verbatim instead of
        // a blank — and we find out from the report rather than from silence.
        Assert.Equal("some_new_code", ReplayVerdict.DescribeExclusion("some_new_code"));
    }
}
