using LegaciesBot.Core;

namespace LegaciesBot.Services;

public class RealEloService : IEloService
{
    public Dictionary<ulong, int> ApplyTeamResult(
        List<Player> teamA,
        List<Player> teamB,
        bool teamAWon,
        PlayerStatsService stats)
    {
        return EloService.ApplyTeamResult(teamA, teamB, teamAWon, stats);
    }
}