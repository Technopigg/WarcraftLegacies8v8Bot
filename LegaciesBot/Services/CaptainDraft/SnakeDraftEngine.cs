using LegaciesBot.Core;

namespace LegaciesBot.Services.CaptainDraft
{
    public class SnakeDraftEngine
    {
        // Snake order: first picker gets 1 pick, then the sides alternate in pairs
        // (A B B A A B B A ...) until each team has TeamSize picks. Captains pick
        // themselves too, so the order covers the whole lobby.
        public List<ulong> BuildOrder(ulong captainA, ulong captainB, bool aPassed)
        {
            var first = aPassed ? captainB : captainA;
            var second = aPassed ? captainA : captainB;

            var order = new List<ulong>(MatchFormat.LobbySize);
            for (int i = 0; i < MatchFormat.LobbySize; i++)
                order.Add((i + 1) / 2 % 2 == 0 ? first : second);

            return order;
        }
    }
}
