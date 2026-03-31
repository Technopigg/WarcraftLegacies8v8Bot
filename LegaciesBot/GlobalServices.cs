using LegaciesBot.Services;
using LegaciesBot.Moderation;
using LegaciesBot.Core;

namespace LegaciesBot
{
    public static class GlobalServices
    {
        public static PermissionService PermissionService = new PermissionService();
        public static ModerationService ModerationService = new ModerationService(TimeSpan.FromDays(7), 3);
        public static LobbyService LobbyService;
    }
}