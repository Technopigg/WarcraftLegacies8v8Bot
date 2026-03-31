using LegaciesBot.Core;

namespace LegaciesBot.Services.CaptainDraft
{
    public interface ICaptainDraftService
    {
        bool TryClaimCaptain(Lobby lobby, ulong userId);
        void BuildDraftOrder(Lobby lobby);
        bool IsCaptainTurn(Lobby lobby, ulong captainId);
        bool TryPick(Lobby lobby, ulong captainId, ulong targetId);
        bool DraftComplete(Lobby lobby);
    }
}