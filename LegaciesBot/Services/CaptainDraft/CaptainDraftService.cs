using LegaciesBot.Core;

namespace LegaciesBot.Services.CaptainDraft
{
    public class CaptainDraftService : ICaptainDraftService
    {
        public bool TryClaimCaptain(Lobby lobby, ulong userId)
        {
            if (lobby.CaptainA != null && lobby.CaptainB != null)
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

        public bool IsCaptainTurn(Lobby lobby, ulong captainId)
        {
            if (lobby.CaptainA == null || lobby.CaptainB == null)
                return false;
            int aPicks = lobby.TeamAPicks.Count;
            int bPicks = lobby.TeamBPicks.Count;
            if (aPicks == bPicks)
                return captainId == lobby.CaptainA;

            return captainId == lobby.CaptainB;
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

            return true;
        }

        public bool DraftComplete(Lobby lobby)
        {
            return lobby.TeamAPicks.Count == 8 && lobby.TeamBPicks.Count == 8;
        }
    }
}