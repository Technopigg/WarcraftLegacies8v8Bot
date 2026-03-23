using LegaciesBot.Core;

namespace LegaciesBot.Services.Drafting
{
    public interface IDraftStrategy
    {
        (Team teamA, Team teamB) RunDraft(Lobby lobby, Random rng);
    }
}