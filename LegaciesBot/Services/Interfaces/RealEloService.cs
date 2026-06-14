using LegaciesBot.Core;
using LegaciesBot.Seasons;

namespace LegaciesBot.Services
{
    public class RealEloService : IEloService
    {
        private readonly PlayerStatsService _lifetime;
        private readonly SeasonService _seasons;

        public RealEloService(PlayerStatsService lifetime, SeasonService seasons)
        {
            _lifetime = lifetime;
            _seasons = seasons;
        }

        public Dictionary<ulong, int> ApplyTeamResult(
            List<Player> teamA,
            List<Player> teamB,
            MatchResult result)
        {
            return EloService.ApplyTeamResult(
                teamA,
                teamB,
                result,
                _lifetime,
                _seasons
            );
        }
    }
}