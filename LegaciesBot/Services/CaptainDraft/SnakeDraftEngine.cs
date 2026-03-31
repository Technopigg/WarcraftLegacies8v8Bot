namespace LegaciesBot.Services.CaptainDraft
{
    public class SnakeDraftEngine
    {
        public List<ulong> BuildOrder(ulong captainA, ulong captainB, bool aPassed)
        {
            var order = new List<ulong>();

            if (!aPassed)
            {
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
            }
            else
            {
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
                order.Add(captainB);
                order.Add(captainA);
                order.Add(captainA);
                order.Add(captainB);
            }

            return order;
        }
    }
}