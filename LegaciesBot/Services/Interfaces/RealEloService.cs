using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    // Season 5+: the site is the single source of truth for all ratings. The local
    // Elo engine has been retired. This implementation is a no-op kept only to
    // satisfy the GameService constructor; it never mutates any local rating store.
    public class RealEloService : IEloService
    {
        public Dictionary<ulong, int> ApplyTeamResult(
            List<Player> teamA,
            List<Player> teamB,
            MatchResult result)
        {
            return new Dictionary<ulong, int>();
        }
    }
}
