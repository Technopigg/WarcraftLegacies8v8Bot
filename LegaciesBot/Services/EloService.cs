using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public static class EloService
    {
        private const int K = 32;

        public static Dictionary<ulong, int> ApplyTeamResult(
            IEnumerable<Player> teamA,
            IEnumerable<Player> teamB,
            bool teamAWon,
            PlayerStatsService statsService)
        {
            var changes = new Dictionary<ulong, int>();

            var teamAList = teamA.ToList();
            var teamBList = teamB.ToList();

            double avgA = teamAList.Average(p => statsService.GetOrCreate(p.DiscordId).Elo);
            double avgB = teamBList.Average(p => statsService.GetOrCreate(p.DiscordId).Elo);

            double expectedA = 1.0 / (1.0 + Math.Pow(10, (avgB - avgA) / 400.0));
            double expectedB = 1.0 - expectedA;

            double scoreA = teamAWon ? 1.0 : 0.0;
            double scoreB = 1.0 - scoreA;

            int deltaA = (int)Math.Round(K * (scoreA - expectedA));
            int deltaB = (int)Math.Round(K * (scoreB - expectedB));
            
            foreach (var p in teamAList)
            {
                var s = statsService.GetOrCreate(p.DiscordId);
                int oldElo = s.Elo;

                s.GamesPlayed++;
                if (teamAWon) s.Wins++; else s.Losses++;
                s.Elo += deltaA;

                changes[p.DiscordId] = s.Elo - oldElo;
                statsService.Update(s);
            }
            
            foreach (var p in teamBList)
            {
                var s = statsService.GetOrCreate(p.DiscordId);
                int oldElo = s.Elo;

                s.GamesPlayed++;
                if (!teamAWon) s.Wins++; else s.Losses++;
                s.Elo += deltaB;

                changes[p.DiscordId] = s.Elo - oldElo;
                statsService.Update(s);
            }

            return changes;
        }
    }
}