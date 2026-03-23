namespace LegaciesBot.Core;

public static class PlayerExtensions
{
    public static string DisplayName(this Player player)
    {
        return player.Nickname ?? player.Name;
    }
}