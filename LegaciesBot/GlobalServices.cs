using LegaciesBot.Services;
using LegaciesBot.Moderation;
using LegaciesBot.Core;
using LegaciesBot.Seasons;
using LegaciesBot.Services.CaptainDraft;

namespace LegaciesBot
{
    public static class GlobalServices
    {
        public static PermissionService PermissionService = new PermissionService();
        public static ModerationService ModerationService = new ModerationService(TimeSpan.FromDays(7), 3);
        public static LobbyService LobbyService;
        public static GameService GameService;
        public static PlayerDataService PlayerDataService;
        public static PlayerStatsService PlayerStatsService;
        public static PlayerRegistryService PlayerRegistryService;
        public static MatchHistoryService MatchHistoryService;
        public static NicknameService NicknameService;
        public static FactionManualAssignmentService FactionManualAssignmentService;
        public static CaptainDraftService CaptainDraftService;
        public static SeasonService SeasonService;
        public static IMessageResponder MessageResponder;
        public static IUserContext UserContext;
    }
}