using LegaciesBot.Discord;
using LegaciesBot.GameData;

public class FactionListTests
{
    [Fact]
    public void Lists_every_registered_faction()
    {
        var text = LobbyCommands.BuildFactionList();
        foreach (var faction in FactionRegistry.All)
            Assert.Contains(faction.Name, text);
    }

    [Fact]
    public void Groups_factions_under_readable_headers()
    {
        var text = LobbyCommands.BuildFactionList();
        foreach (var label in new[] { "North Alliance", "South Alliance", "Burning Legion", "Fel Horde", "Kalimdor", "Old Gods" })
            Assert.Contains(label, text);
    }

    [Fact]
    public void Points_players_at_prefs()
    {
        Assert.Contains("!prefs", LobbyCommands.BuildFactionList());
    }
}
