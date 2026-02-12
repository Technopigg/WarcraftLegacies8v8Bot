namespace LegaciesBot.Core;

public class Game
{
    public Lobby Lobby { get; set; }          
    public Team TeamA { get; set; }
    public Team TeamB { get; set; }
    public bool Finished { get; set; } = false;
    public int ScoreA { get; set; } = 0;
    public int ScoreB { get; set; } = 0;
    public int Id { get; set; }
    public Dictionary<ulong, (int scoreA, int scoreB)> ScoreSubmissions { get; set; } 
        = new();

}