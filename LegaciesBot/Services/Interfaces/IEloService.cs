using LegaciesBot.Core;

namespace LegaciesBot.Services;

public interface IEloService
{
    Dictionary<ulong, int> ApplyTeamResult(
        List<Player> teamA,
        List<Player> teamB,
        bool teamAWon,
        PlayerStatsService stats);
}