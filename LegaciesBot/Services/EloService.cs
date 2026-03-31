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
            bool teamAWon,
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

            double scoreA = teamAWon ? 1.0 : 0.0;
            double scoreB = 1.0 - scoreA;

            int deltaA = (int)Math.Round(K * (scoreA - expectedA));
            int deltaB = (int)Math.Round(K * (scoreB - expectedB));
            
            foreach (var p in teamAList)
            {
                var lifetime = lifetimeStats.GetOrCreate(p.DiscordId);
                var seasonal = seasonStats.GetOrCreateSeasonStats(p.DiscordId);

                int oldElo = lifetime.Elo;
                
                lifetime.GamesPlayed++;
                if (teamAWon) lifetime.Wins++; else lifetime.Losses++;
                lifetime.Elo += deltaA;
                seasonal.GamesPlayed++;
                if (teamAWon) seasonal.Wins++; else seasonal.Losses++;
                seasonal.Elo += deltaA;
                if (p.AssignedFaction != null)
                {
                    if (!lifetime.FactionHistory.TryGetValue(p.AssignedFaction, out var record))
                        lifetime.FactionHistory[p.AssignedFaction] = record = new FactionRecord();

                    if (teamAWon) record.Wins++; else record.Losses++;
                }
                if (p.AssignedFaction != null)
                {
                    if (!seasonal.FactionHistory.TryGetValue(p.AssignedFaction, out var record))
                        seasonal.FactionHistory[p.AssignedFaction] = record = new FactionRecord();

                    if (teamAWon) record.Wins++; else record.Losses++;
                }

                changes[p.DiscordId] = lifetime.Elo - oldElo;

                lifetimeStats.Update(lifetime);
                seasonStats.Save(); 
            }
            
            foreach (var p in teamBList)
            {
                var lifetime = lifetimeStats.GetOrCreate(p.DiscordId);
                var seasonal = seasonStats.GetOrCreateSeasonStats(p.DiscordId);

                int oldElo = lifetime.Elo;
                lifetime.GamesPlayed++;
                if (!teamAWon) lifetime.Wins++; else lifetime.Losses++;
                lifetime.Elo += deltaB;
                
                seasonal.GamesPlayed++;
                if (!teamAWon) seasonal.Wins++; else seasonal.Losses++;
                seasonal.Elo += deltaB;

                if (p.AssignedFaction != null)
                {
                    if (!lifetime.FactionHistory.TryGetValue(p.AssignedFaction, out var record))
                        lifetime.FactionHistory[p.AssignedFaction] = record = new FactionRecord();

                    if (!teamAWon) record.Wins++; else record.Losses++;
                }

                if (p.AssignedFaction != null)
                {
                    if (!seasonal.FactionHistory.TryGetValue(p.AssignedFaction, out var record))
                        seasonal.FactionHistory[p.AssignedFaction] = record = new FactionRecord();

                    if (!teamAWon) record.Wins++; else record.Losses++;
                }

                changes[p.DiscordId] = lifetime.Elo - oldElo;

                lifetimeStats.Update(lifetime);
                seasonStats.Save();
            }

            return changes;
        }
    }
}
