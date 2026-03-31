using NetCord.Services.Commands;
using LegaciesBot.Moderation;
using LegaciesBot.Services;

namespace LegaciesBot.Discord
{
    public class ModerationCommands : CommandModule<CommandContext>
    {
        private readonly ModerationService _moderation;
        private readonly PermissionService _permissions;
        private readonly NicknameService _nickname;

        public ModerationCommands(ModerationService moderation, PermissionService permissions, NicknameService nickname)
        {
            _moderation = moderation;
            _permissions = permissions;
            _nickname = nickname;
        }

        private bool TryResolveUser(string input, out ulong userId)
        {
            userId = 0;

            var nick = _nickname.ResolvePlayerId(input);
            if (nick.HasValue)
            {
                userId = nick.Value;
                return true;
            }

            if (ulong.TryParse(input, out var parsed))
            {
                userId = parsed;
                return true;
            }

            if (input.StartsWith("<@") && input.EndsWith(">"))
            {
                var inner = input.Trim('<', '>', '@', '!');
                if (ulong.TryParse(inner, out parsed))
                {
                    userId = parsed;
                    return true;
                }
            }

            return false;
        }

        [Command("warn")]
        public async Task WarnAsync(string userInput, [CommandParameter(Remainder = true)] string rest)
        {
            if (!_permissions.IsModeratorOrAdmin(Context.User.Id))
            {
                await Context.Message.ReplyAsync("You do not have permission.");
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(rest))
            {
                await Context.Message.ReplyAsync("Usage: !warn <user> <reason>\nOr: !warn <user> <duration> <reason>");
                return;
            }

            if (!TryResolveUser(userInput, out var userId))
            {
                await Context.Message.ReplyAsync("Could not resolve user.");
                return;
            }

            TimeSpan? duration = null;
            string finalReason;

            var firstSpace = rest.IndexOf(' ');
            if (firstSpace > 0)
            {
                var firstToken = rest[..firstSpace];
                var remaining = rest[(firstSpace + 1)..];

                if (TryParseDuration(firstToken, out var parsedDuration))
                {
                    duration = parsedDuration;
                    if (string.IsNullOrWhiteSpace(remaining))
                    {
                        await Context.Message.ReplyAsync("Usage: !warn <user> <duration> <reason>");
                        return;
                    }
                    finalReason = remaining;
                }
                else
                {
                    finalReason = rest;
                }
            }
            else
            {
                if (TryParseDuration(rest, out var parsedDuration))
                {
                    await Context.Message.ReplyAsync("Usage: !warn <user> <duration> <reason>");
                    return;
                }
                finalReason = rest;
            }

            var autoBanned = _moderation.AddWarning(userId, Context.User.Id, finalReason, duration);

            await Context.Message.ReplyAsync($"Warned <@{userId}>: {finalReason}");

            if (autoBanned)
            {
                await Context.Message.ReplyAsync($"<@{userId}> has been automatically banned: Reached warning threshold");
            }
        }

        [Command("warn")]
        public async Task WarnUsageAsync(string userInput)
        {
            await Context.Message.ReplyAsync("Usage: !warn <user> <reason>\nOr: !warn <user> <duration> <reason>");
        }

        [Command("warn")]
        public async Task WarnUsageAsync()
        {
            await Context.Message.ReplyAsync("Usage: !warn <user> <reason>\nOr: !warn <user> <duration> <reason>");
        }

        [Command("removewarn")]
        [Command("unwarn")]
        public async Task RemoveWarnAsync(string userInput, int index)
        {
            if (!_permissions.IsModeratorOrAdmin(Context.User.Id))
            {
                await Context.Message.ReplyAsync("You do not have permission.");
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                await Context.Message.ReplyAsync("Usage: !removewarn <user> <index>");
                return;
            }

            if (!TryResolveUser(userInput, out var userId))
            {
                await Context.Message.ReplyAsync("Could not resolve user.");
                return;
            }

            bool wasBanned = _moderation.IsBanned(userId);

            if (_moderation.RemoveWarning(userId, index))
            {
                await Context.Message.ReplyAsync($"Removed warning {index} from <@{userId}>.");

                bool isNowBanned = _moderation.IsBanned(userId);

                if (wasBanned && !isNowBanned)
                {
                    await Context.Message.ReplyAsync($"Unbanned <@{userId}>.");
                }
            }
            else
            {
                await Context.Message.ReplyAsync("Invalid user or warning index.");
            }
        }

        [Command("removewarn")]
        [Command("unwarn")]
        public async Task RemoveWarnUsageAsync(string userInput)
        {
            await Context.Message.ReplyAsync("Usage: !removewarn <user> <index>");
        }

        [Command("ban")]
        public async Task BanAsync(string userInput, [CommandParameter(Remainder = true)] string reason)
        {
            if (!_permissions.IsModeratorOrAdmin(Context.User.Id))
            {
                await Context.Message.ReplyAsync("You do not have permission.");
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(reason))
            {
                await Context.Message.ReplyAsync("Usage: !ban <user> <reason>");
                return;
            }

            if (!TryResolveUser(userInput, out var userId))
            {
                await Context.Message.ReplyAsync("Could not resolve user.");
                return;
            }

            _moderation.AddBan(userId, Context.User.Id, reason);
            await Context.Message.ReplyAsync($"Banned <@{userId}>: {reason}");
        }

        [Command("ban")]
        public async Task BanUsageAsync(string userInput)
        {
            await Context.Message.ReplyAsync("Usage: !ban <user> <reason>");
        }

        [Command("unban")]
        public async Task UnbanAsync(string userInput)
        {
            if (!_permissions.IsModeratorOrAdmin(Context.User.Id))
            {
                await Context.Message.ReplyAsync("You do not have permission.");
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                await Context.Message.ReplyAsync("Usage: !unban <user>");
                return;
            }

            if (!TryResolveUser(userInput, out var userId))
            {
                await Context.Message.ReplyAsync("Could not resolve user.");
                return;
            }

            if (_moderation.RemoveBan(userId))
                await Context.Message.ReplyAsync($"Unbanned <@{userId}>.");
            else
                await Context.Message.ReplyAsync("User is not banned.");
        }

        [Command("unban")]
        public async Task UnbanUsageAsync()
        {
            await Context.Message.ReplyAsync("Usage: !unban <user>");
        }

        [Command("warnings")]
        [Command("warns")]
        public async Task WarningsAsync(string? userInput = null)
        {
            ulong userId;

            if (string.IsNullOrWhiteSpace(userInput))
            {
                userId = Context.User.Id;
            }
            else
            {
                if (!TryResolveUser(userInput, out userId))
                {
                    await Context.Message.ReplyAsync("Could not resolve user.");
                    return;
                }
            }

            var warnings = _moderation.GetActiveWarnings(userId);
            var isBanned = _moderation.IsBanned(userId);

            string banStatus = isBanned
                ? $"<@{userId}> is currently **banned**."
                : $"<@{userId}> is **not banned**.";

            if (warnings.Count == 0)
            {
                await Context.Message.ReplyAsync($"{banStatus}\nNo active warnings.");
                return;
            }

            var now = DateTime.UtcNow;

            string FormatExpiry(WarningEntry w)
            {
                if (w.ExpiresAtUtc == null)
                    return "no expiry";

                var remaining = w.ExpiresAtUtc.Value - now;

                if (remaining.TotalDays >= 1)
                    return $"in {(int)remaining.TotalDays}d {(int)remaining.Hours}h";

                if (remaining.TotalHours >= 1)
                    return $"in {(int)remaining.TotalHours}h {(int)remaining.Minutes}m";

                return $"in {(int)remaining.TotalMinutes}m";
            }

            var msg = string.Join("\n", warnings.Select((w, i) =>
                $"{i}: {w.Reason} (by <@{w.ModeratorId}> at {w.IssuedAtUtc:dd-MM-yy}, expires {FormatExpiry(w)})"
            ));

            await Context.Message.ReplyAsync($"{banStatus}\nWarnings for <@{userId}>:\n{msg}");
        }

        private bool TryParseDuration(string input, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;

            if (input.EndsWith("d") && int.TryParse(input[..^1], out var days))
            {
                duration = TimeSpan.FromDays(days);
                return true;
            }

            if (input.EndsWith("h") && int.TryParse(input[..^1], out var hours))
            {
                duration = TimeSpan.FromHours(hours);
                return true;
            }

            if (input.EndsWith("m") && int.TryParse(input[..^1], out var minutes))
            {
                duration = TimeSpan.FromMinutes(minutes);
                return true;
            }

            return false;
        }
    }
}
