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
        private IMessageResponder? _responder;
        private IUserContext? _user;

        // These used to be set from GlobalServices, but nothing ever assigned them there,
        // so every moderation command was silently crashing. They need to wrap the live
        // message/user for whichever command is running, not a single global value, so
        // build them here the first time a command actually needs one. Tests still pass
        // their own fakes in through the other constructor, which this never overwrites.
        private IMessageResponder Responder => _responder ??= new DiscordMessageResponder(Context);
        private IUserContext UserCtx => _user ??= new DiscordUserContext(Context);

        public ModerationCommands(
            ModerationService mod,
            PermissionService perm,
            NicknameService nick,
            IMessageResponder responder,
            IUserContext user)
        {
            _mod = mod;
            _perm = perm;
            _nick = nick;
            _responder = responder;
            _user = user;
        }

        public ModerationCommands()
        {
            _mod = GlobalServices.ModerationService;
            _perm = GlobalServices.PermissionService;
            _nick = GlobalServices.NicknameService;
        }

        [Command("warn")]
        public async Task WarnAsync(string user, [CommandParameter(Remainder = true)] string reason)
        {
            if (!_perm.IsModeratorOrAdmin(UserCtx.UserId))
            {
                await Responder.ReplyAsync("You do not have permission to use this command.");
                return;
            }

            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await Responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool autoBanned = _mod.AddWarning(userId.Value, UserCtx.UserId, reason);

            await Responder.ReplyAsync($"Warned <@{userId}>: {reason}");

            if (autoBanned)
            {
                await Responder.ReplyAsync(
                    $"<@{userId}> has been automatically banned: Reached warning threshold");
            }
        }

        [Command("removewarn")]
        public async Task RemoveWarnAsync(string user, int index)
        {
            if (!_perm.IsModeratorOrAdmin(UserCtx.UserId))
            {
                await Responder.ReplyAsync("You do not have permission to use this command.");
                return;
            }

            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await Responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool removed = _mod.RemoveWarning(userId.Value, index);
            if (!removed)
            {
                await Responder.ReplyAsync("Invalid warning index.");
                return;
            }

            await Responder.ReplyAsync($"Removed warning {index} from <@{userId}>.");

            if (!_mod.IsBanned(userId.Value))
            {
                await Responder.ReplyAsync($"Unbanned <@{userId}>.");
            }
        }

        [Command("removewarn")]
        public async Task RemoveWarnUsageAsync(string user)
        {
            await Responder.ReplyAsync("Usage: !removewarn <user> <index>");
        }

        [Command("ban")]
        public async Task BanAsync(string user, [CommandParameter(Remainder = true)] string reason)
        {
            if (!_perm.IsModeratorOrAdmin(UserCtx.UserId))
            {
                await Responder.ReplyAsync("You do not have permission to use this command.");
                return;
            }

            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await Responder.ReplyAsync("Could not resolve user.");
                return;
            }

            _mod.AddBan(userId.Value, UserCtx.UserId, reason);
            await Responder.ReplyAsync($"Banned <@{userId}>: {reason}");
        }

        [Command("unban")]
        public async Task UnbanAsync(string user)
        {
            if (!_perm.IsModeratorOrAdmin(UserCtx.UserId))
            {
                await Responder.ReplyAsync("You do not have permission to use this command.");
                return;
            }

            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await Responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool removed = _mod.RemoveBan(userId.Value);
            if (!removed)
            {
                await Responder.ReplyAsync("User is not banned.");
                return;
            }

            await Responder.ReplyAsync($"Unbanned <@{userId}>.");
        }

        [Command("warns")]
        public async Task WarningsAsync(string user)
        {
            var userId = _nick.ResolvePlayerId(user);
            if (userId == null)
            {
                await Responder.ReplyAsync("Could not resolve user.");
                return;
            }

            bool banned = _mod.IsBanned(userId.Value);
            var warnings = _mod.GetActiveWarnings(userId.Value);

            if (banned)
                await Responder.ReplyAsync($"<@{userId}> is currently **banned**.");
            else
                await Responder.ReplyAsync($"<@{userId}> is **not banned**.");

            if (warnings.Count == 0)
            {
                await Responder.ReplyAsync("No active warnings.");
                return;
            }

            string list = string.Join("\n", warnings.Select((w, i) =>
                $"{i}: {w.Reason} (by <@{w.ModeratorId}>)"));

            await Responder.ReplyAsync($"Warnings for <@{userId}>:\n{list}");
        }
    }
}
