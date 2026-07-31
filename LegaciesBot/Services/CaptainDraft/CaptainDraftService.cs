using LegaciesBot.Core;

namespace LegaciesBot.Services.CaptainDraft
{
    public class CaptainDraftService : ICaptainDraftService
    {
        private readonly SnakeDraftEngine _engine = new();

        public bool TryClaimCaptain(Lobby lobby, ulong userId)
        {
            if (lobby.CaptainA != null && lobby.CaptainB != null)
                return false;

            // Without this the same person could claim twice and captain both
            // sides. !captain guards against it, but it is the only caller and
            // the service should not depend on that staying true.
            if (lobby.CaptainA == userId || lobby.CaptainB == userId)
                return false;

            if (lobby.CaptainA == null)
            {
                lobby.CaptainA = userId;
                return true;
            }

            if (lobby.CaptainB == null)
            {
                lobby.CaptainB = userId;
                return true;
            }

            return false;
        }

        public void BuildDraftOrder(Lobby lobby)
        {
            if (lobby.CaptainA == null || lobby.CaptainB == null)
                return;

            lobby.DraftOrder = _engine.BuildOrder(
                lobby.CaptainA.Value,
                lobby.CaptainB.Value,
                lobby.CaptainAPassed
            );

            lobby.CurrentPickIndex = 0;
        }

        public bool IsCaptainTurn(Lobby lobby, ulong captainId)
        {
            if (lobby.DraftOrder.Count == 0)
                return false;

            if (lobby.CurrentPickIndex >= lobby.DraftOrder.Count)
                return false;

            return lobby.DraftOrder[lobby.CurrentPickIndex] == captainId;
        }

        public bool TryPick(Lobby lobby, ulong captainId, ulong targetId)
        {
            if (!IsCaptainTurn(lobby, captainId))
                return false;

            if (!lobby.Players.Any(p => p.DiscordId == targetId))
                return false;

            if (lobby.TeamAPicks.Contains(targetId) || lobby.TeamBPicks.Contains(targetId))
                return false;

            if (captainId == lobby.CaptainA)
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
