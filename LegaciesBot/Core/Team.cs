namespace LegaciesBot.Core;

public class Team
{
    public string Name { get; }
    public List<Player> Players { get; } = new();
    public List<Faction> AssignedFactions { get; } = new();

    public Team(string name)
    {
        Name = name;
    }

    public int TotalElo()
    {
        return Players.Sum(p => p.Elo);
    }

    public void AddPlayer(Player player)
    {
        Players.Add(player);
    }
}