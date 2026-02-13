using LegaciesBot.Core;

namespace LegaciesBot.Services;

public class RealMatchHistoryService : IMatchHistoryService
{
    private readonly MatchHistoryService _inner;

    public RealMatchHistoryService(MatchHistoryService inner)
    {
        _inner = inner;
    }

    public void RecordMatch(Game game, int scoreA, int scoreB, Dictionary<ulong, int> changes)
    {
        _inner.RecordMatch(game, scoreA, scoreB, changes);
    }
}