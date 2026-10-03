using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services.CaptainDraft;

namespace LegaciesBot.Tests;

// Season 5: Old Gods team removed, Kalimdor split into The Horde vs The Night Elves,
// The Exodar dropped, and the game went from 8v8 to 7v7.
public class Season5RosterTests
{
    [Fact]
    public void Format_is_7v7()
    {
        Assert.Equal(7, MatchFormat.TeamSize);
        Assert.Equal(14, MatchFormat.LobbySize);
    }

    [Fact]
    public void Horde_is_Orcish_Horde_and_Tauren_Tribes()
    {
        var horde = FactionRegistry.All.Where(f => f.Group == TeamGroup.Horde).Select(f => f.Name);
        Assert.Equal(new[] { "Orcish Horde", "Tauren Tribes" }, horde);
    }

    [Fact]
    public void Night_Elves_are_Sentinels_and_Druids()
    {
        var elves = FactionRegistry.All.Where(f => f.Group == TeamGroup.NightElves).Select(f => f.Name);
        Assert.Equal(new[] { "Sentinels", "Druids" }, elves);
    }

    [Theory]
    [InlineData("The Exodar")]
    [InlineData("An'qiraj")]
    [InlineData("Black Empire")]
    [InlineData("Skywall")]
    [InlineData("Warsong")]
    [InlineData("Frostwolf")]
    public void Removed_factions_are_gone_and_not_accepted_by_prefs(string removed)
    {
        Assert.DoesNotContain(FactionRegistry.All, f => f.Name == removed);

        var result = FactionParser.Parse(removed);
        Assert.DoesNotContain(removed, result.Accepted);
    }

    [Fact]
    public void Saved_prefs_for_removed_factions_are_dropped()
    {
        var saved = new[] { "Skywall", "Scourge", "The Exodar", "druids", "Warsong" };
        Assert.Equal(new[] { "Scourge", "druids" }, FactionParser.OnlyCurrent(saved));
    }

    [Fact]
    public void Default_preferences_are_all_real_factions()
    {
        Assert.Equal(DefaultPreferences.Factions, FactionParser.OnlyCurrent(DefaultPreferences.Factions));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Snake_order_covers_the_lobby_with_TeamSize_picks_each(bool aPassed)
    {
        const ulong a = 1, b = 2;
        var order = new SnakeDraftEngine().BuildOrder(a, b, aPassed);

        Assert.Equal(MatchFormat.LobbySize, order.Count);
        Assert.Equal(MatchFormat.TeamSize, order.Count(id => id == a));
        Assert.Equal(MatchFormat.TeamSize, order.Count(id => id == b));

        var first = aPassed ? b : a;
        var second = aPassed ? a : b;
        Assert.Equal(new[] { first, second, second, first, first, second }, order.Take(6));
    }
}
