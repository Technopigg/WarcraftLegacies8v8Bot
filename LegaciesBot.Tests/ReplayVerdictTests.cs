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

/// <summary>
/// The other two vocabularies the site speaks: why it would not take a file,
/// and why it already had it. A player reading "replay_parse_error" or
/// "same_game_shorter_or_equal" learns nothing.
/// </summary>
public class ReplayReasonTests
{
    [Theory]
    [InlineData("replay_parse_error")]
    [InlineData("empty_replay_file")]
    [InlineData("replay_file_too_large")]
    [InlineData("invalid_content_length")]
    [InlineData("missing_map_info")]
    [InlineData("unsupported_map_family")]
    [InlineData("map_family_not_registered")]
    [InlineData("unregistered_map_version")]
    [InlineData("rate_limit_exceeded")]
    [InlineData("storage_unavailable")]
    public void EveryRejectionCodeTheSiteSendsBecomesASentence(string code)
    {
        var text = ReplayVerdict.DescribeRejection(code);

        Assert.NotEqual(code, text);
        Assert.DoesNotContain("_", text);
    }

    [Theory]
    [InlineData("file_sha256_exists")]
    [InlineData("parsed_replay_exists")]
    [InlineData("discord_ids_exist")]
    [InlineData("same_game_shorter_or_equal")]
    [InlineData("duplicate_replay")]
    public void EveryDuplicateCodeTheSiteSendsBecomesASentence(string code)
    {
        var text = ReplayVerdict.DescribeDuplicate(code);

        Assert.NotEqual(code, text);
        Assert.DoesNotContain("_", text);
    }

    [Fact]
    public void AMissingReasonStillReads()
    {
        Assert.Equal("no reason given", ReplayVerdict.DescribeRejection(null));
        Assert.Equal("this replay is already uploaded", ReplayVerdict.DescribeDuplicate(null));
    }

    [Fact]
    public void AnUnknownCodeIsShownRatherThanHidden()
    {
        Assert.Equal("brand_new_code", ReplayVerdict.DescribeRejection("brand_new_code"));
        Assert.Equal("brand_new_code", ReplayVerdict.DescribeDuplicate("brand_new_code"));
    }
}
