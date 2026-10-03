namespace LegaciesBot.Core;

/// <summary>
/// Lobby/team sizing in one place. Season 5 moved from 8v8 to 7v7 when the Old Gods team
/// was removed; every "lobby full", draft-complete and faction-lock check reads from here.
/// </summary>
public static class MatchFormat
{
    public const int TeamSize = 7;
    public const int LobbySize = TeamSize * 2;
}
