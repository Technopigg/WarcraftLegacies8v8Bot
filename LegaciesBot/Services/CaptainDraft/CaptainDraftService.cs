using LegaciesBot.Core;

namespace LegaciesBot.Services.CaptainDraft
{
    public class CaptainDraftService
    {
        private readonly List<bool> _order;

        public CaptainDraftService()
        {
            _order = SnakeDraftOrder.GenerateOrder();
        }

        public bool TryClaimCaptain(Lobby lobby, ulong playerId)
        {
            if (lobby.CaptainA == playerId || lobby.CaptainB == playerId)
                return true;

            if (lobby.CaptainA == null)
            {
                lobby.CaptainA = playerId;
                return true;
            }

            if (lobby.CaptainB == null)
            {
                lobby.CaptainB = playerId;
                return true;
            }

            return false;
        }

        public bool IsCaptainTurn(Lobby lobby, ulong captainId)
        {
            bool isATurn = _order[lobby.CurrentPickIndex];
            return isATurn ? lobby.CaptainA == captainId : lobby.CaptainB == captainId;
        }

        public bool TryPick(Lobby lobby, ulong captainId, ulong targetId)
        {
            if (!IsCaptainTurn(lobby, captainId))
                return false;

            if (!lobby.Players.Any(p => p.DiscordId == targetId))
                return false;

            if (lobby.TeamAPicks.Contains(targetId) || lobby.TeamBPicks.Contains(targetId))
                return false;

            bool isATurn = _order[lobby.CurrentPickIndex];

            if (isATurn)
                lobby.TeamAPicks.Add(targetId);
            else
                lobby.TeamBPicks.Add(targetId);

            lobby.CurrentPickIndex++;

            return true;
        }

        public bool DraftComplete(Lobby lobby)
        {
            return lobby.TeamAPicks.Count == 8 && lobby.TeamBPicks.Count == 8;
        }
    }
}