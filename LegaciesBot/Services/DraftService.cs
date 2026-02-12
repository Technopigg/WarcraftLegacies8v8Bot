using LegaciesBot.Core;

namespace LegaciesBot.Services;

public static class DraftService
{
    public static (Team, Team) CreateBalancedTeams(List<Player> players)
    {
        var rng = new Random();
        
        foreach (var player in players)
        {
            player.Elo += rng.Next(-10, 11);
        }

        var sorted = players.OrderByDescending(p => p.Elo).ToList();

        var teamA = new Team("Team A");
        var teamB = new Team("Team B");

        bool snakeForward = true;

        for (int i = 0; i < sorted.Count; i++)
        {
            if (snakeForward)
            {
                teamA.AddPlayer(sorted[i]);
                if (i + 1 < sorted.Count)
                    teamB.AddPlayer(sorted[++i]);
            }
            else
            {
                teamB.AddPlayer(sorted[i]);
                if (i + 1 < sorted.Count)
                    teamA.AddPlayer(sorted[++i]);
            }

            snakeForward = !snakeForward;
        }

        return (teamA, teamB);
    }

    public static List<Team> RunDraft(List<Player> players)
    {
        var (teamA, teamB) = CreateBalancedTeams(players);
        return new List<Team> { teamA, teamB };
    }
}