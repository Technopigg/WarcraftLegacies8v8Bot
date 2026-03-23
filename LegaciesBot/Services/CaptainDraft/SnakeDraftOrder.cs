namespace LegaciesBot.Services.CaptainDraft
{
    public static class SnakeDraftOrder
    {
        public static List<bool> GenerateOrder(int picksPerTeam = 8)
        {
            var order = new List<bool>();
            
            order.Add(true);
            order.Add(false);
            order.Add(false);
            
            bool forward = true;

            while (order.Count < picksPerTeam * 2)
            {
                if (forward)
                {
                    order.Add(true);
                    order.Add(true);
                }
                else
                {
                    order.Add(false);
                    order.Add(false);
                }

                forward = !forward;
            }

            return order.Take(picksPerTeam * 2).ToList();
        }
    }
}