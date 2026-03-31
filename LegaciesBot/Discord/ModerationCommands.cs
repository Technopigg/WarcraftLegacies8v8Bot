using LegaciesBot.Services;
using LegaciesBot.Moderation;
using NetCord.Services.Commands;

namespace LegaciesBot.Discord
{
    public class ModerationCommands : CommandModule<CommandContext>
    {
        private readonly ModerationService _mod;
        private readonly PermissionService _perm;
        private readonly NicknameService _nick;
        private readonly IMessageResponder _responder;
        private readonly IUserContext _user;

        public ModerationCommands()
        {
            _mod = GlobalServices.ModerationService;
            _perm = GlobalServices.PermissionService;
            _nick = GlobalServices.NicknameService;
            _responder = GlobalServices.MessageResponder;
            _user = GlobalServices.UserContext;
        }

        [Command("warn")]
        public async Task WarnAsync(string user, string reason)
        {
            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await _responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool autoBanned = _mod.AddWarning(userId.Value, _user.UserId, reason);

            await _responder.ReplyAsync($"Warned <@{userId}>: {reason}");

            if (autoBanned)
            {
                await _responder.ReplyAsync(
                    $"<@{userId}> has been automatically banned: Reached warning threshold");
            }
        }

        [Command("removewarn")]
        public async Task RemoveWarnAsync(string user, int index)
        {
            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await _responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool removed = _mod.RemoveWarning(userId.Value, index);
            if (!removed)
            {
                await _responder.ReplyAsync("Invalid warning index.");
                return;
            }

            await _responder.ReplyAsync($"Removed warning {index} from <@{userId}>.");

            if (!_mod.IsBanned(userId.Value))
            {
                await _responder.ReplyAsync($"Unbanned <@{userId}>.");
            }
        }

        [Command("removewarn")]
        public async Task RemoveWarnUsageAsync(string user)
        {
            await _responder.ReplyAsync("Usage: !removewarn <user> <index>");
        }

        [Command("ban")]
        public async Task BanAsync(string user, string reason)
        {
            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await _responder.ReplyAsync("Could not resolve user.");
                return;
            }

            _mod.AddBan(userId.Value, _user.UserId, reason);
            await _responder.ReplyAsync($"Banned <@{userId}>: {reason}");
        }

        [Command("unban")]
        public async Task UnbanAsync(string user)
        {
            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await _responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool removed = _mod.RemoveBan(userId.Value);
            if (!removed)
            {
                await _responder.ReplyAsync("User is not banned.");
                return;
            }

            await _responder.ReplyAsync($"Unbanned <@{userId}>.");
        }

        [Command("warns")]
        public async Task WarningsAsync(string user)
        {
            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await _responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool banned = _mod.IsBanned(userId.Value);
            var warnings = _mod.GetActiveWarnings(userId.Value);

            if (banned)
                await _responder.ReplyAsync($"<@{userId}> is currently **banned**.");
            else
                await _responder.ReplyAsync($"<@{userId}> is **not banned**.");

            if (warnings.Count == 0)
            {
                await _responder.ReplyAsync("No active warnings.");
                return;
            }

            string list = string.Join("\n", warnings.Select((w, i) =>
                $"{i}: {w.Reason} (by <@{w.ModeratorId}>)"));

            await _responder.ReplyAsync($"Warnings for <@{userId}>:\n{list}");
        }
    }
}
