using LegaciesBot.Core;

namespace LegaciesBot.Services;

public interface IMatchHistoryService
{
    void RecordMatch(Game game, int scoreA, int scoreB, Dictionary<ulong, int> changes);
}