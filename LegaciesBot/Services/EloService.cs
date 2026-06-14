using LegaciesBot.Core;
using LegaciesBot.Seasons;

namespace LegaciesBot.Services
{
    public static class EloService
    {
        private const int K = 32;

        public static Dictionary<ulong, int> ApplyTeamResult(
            IEnumerable<Player> teamA,
            IEnumerable<Player> teamB,
            MatchResult result,
            PlayerStatsService lifetimeStats,
            SeasonService seasonStats)
        {
            var changes = new Dictionary<ulong, int>();

            var teamAList = teamA.ToList();
            var teamBList = teamB.ToList();

            double avgA = teamAList.Average(p => lifetimeStats.GetOrCreate(p.DiscordId).Elo);
            double avgB = teamBList.Average(p => lifetimeStats.GetOrCreate(p.DiscordId).Elo);

            double expectedA = 1.0 / (1.0 + Math.Pow(10, (avgB - avgA) / 400.0));
            double expectedB = 1.0 - expectedA;

            double scoreA = result switch
            {
                MatchResult.TeamAWin => 1.0,
                MatchResult.TeamBWin => 0.0,
                _ => 0.5
            };
            double scoreB = 1.0 - scoreA;

            int deltaA = (int)Math.Round(K * (scoreA - expectedA));
            int deltaB = (int)Math.Round(K * (scoreB - expectedB));

            ApplyToTeam(teamAList, deltaA, result, MatchResult.TeamAWin, lifetimeStats, seasonStats, changes);
            ApplyToTeam(teamBList, deltaB, result, MatchResult.TeamBWin, lifetimeStats, seasonStats, changes);

            return changes;
        }

        private static void ApplyToTeam(
            List<Player> team,
            int delta,
            MatchResult result,
            MatchResult winResult,
            PlayerStatsService lifetimeStats,
            SeasonService seasonStats,
            Dictionary<ulong, int> changes)
        {
            foreach (var p in team)
            {
                var lifetime = lifetimeStats.GetOrCreate(p.DiscordId);
                var seasonal = seasonStats.GetOrCreateSeasonStats(p.DiscordId);

                int oldElo = lifetime.Elo;

                lifetime.GamesPlayed++;
                seasonal.GamesPlayed++;

                if (result == MatchResult.Draw)
                {
                    lifetime.Draws++;
                    seasonal.Draws++;
                }
                else if (result == winResult)
                {
                    lifetime.Wins++;
                    seasonal.Wins++;
                }
                else
                {
                    lifetime.Losses++;
                    seasonal.Losses++;
                }

                lifetime.Elo += delta;
                seasonal.Elo += delta;

                // Lifetime FactionHistory is updated separately by
                // GameService.UpdateFactionStats (uses the same MatchResult).
                if (p.AssignedFaction != null)
                {
                    if (!seasonal.FactionHistory.TryGetValue(p.AssignedFaction, out var seasonalRecord))
                        seasonal.FactionHistory[p.AssignedFaction] = seasonalRecord = new FactionRecord();

                    if (result == MatchResult.Draw)
                        seasonalRecord.Draws++;
                    else if (result == winResult)
                        seasonalRecord.Wins++;
                    else
                        seasonalRecord.Losses++;
                }

                changes[p.DiscordId] = lifetime.Elo - oldElo;

                lifetimeStats.Update(lifetime);
                seasonStats.Save();
            }
        }
    }
}
