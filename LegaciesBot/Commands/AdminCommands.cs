using NetCord.Rest;
using NetCord.Services.Commands;
using LegaciesBot.Services;

namespace LegaciesBot.Commands
{
    public class AdminCommands : CommandModule<CommandContext>
    {
        public AdminCommands()
        {
        }

        private bool IsAdmin(ulong id)
        {
            return GlobalServices.PermissionService.IsAdmin(id);
        }
        
        private ulong? ResolveUserId(string input)
        {
            var ctx = this.Context;
            if (ctx.Message.MentionedUsers.Count > 0)
                return ctx.Message.MentionedUsers[0].Id;
            if (ulong.TryParse(input, out var parsed))
                return parsed;

            return null;
        }
        
        [Command("addadmin")]
        public async Task AddAdmin(string userInput)
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can add new admins.");
                return;
            }

            var userId = ResolveUserId(userInput);
            if (userId == null)
            {
                await ctx.Message.ReplyAsync("Invalid user. Mention them or provide their ID.");
                return;
            }

            GlobalServices.PermissionService.AddAdmin(userId.Value);
            await ctx.Message.ReplyAsync($"User <@{userId}> is now an admin.");
        }

        [Command("addadmin")]
        public async Task AddAdminNoArg()
        {
            await Context.Message.ReplyAsync("Usage: !addadmin <@mention or userId>");
        }

        [Command("addmod")]
        public async Task AddMod(string userInput)
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can add moderators.");
                return;
            }

            var userId = ResolveUserId(userInput);
            if (userId == null)
            {
                await ctx.Message.ReplyAsync("Invalid user. Mention them or provide their ID.");
                return;
            }

            GlobalServices.PermissionService.AddMod(userId.Value);
            await ctx.Message.ReplyAsync($"User <@{userId}> is now a moderator.");
        }

        [Command("addmod")]
        public async Task AddModNoArg()
        {
            await Context.Message.ReplyAsync("Usage: !addmod <@mention or userId>");
        }
        [Command("removeadmin")]
        public async Task RemoveAdmin(string userInput)
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can remove admins.");
                return;
            }

            var userId = ResolveUserId(userInput);
            if (userId == null)
            {
                await ctx.Message.ReplyAsync("Invalid user. Mention them or provide their ID.");
                return;
            }

            GlobalServices.PermissionService.RemoveAdmin(userId.Value);
            await ctx.Message.ReplyAsync($"User <@{userId}> is no longer an admin.");
        }

        [Command("removeadmin")]
        public async Task RemoveAdminNoArg()
        {
            await Context.Message.ReplyAsync("Usage: !removeadmin <@mention or userId>");
        }
        
        [Command("removemod")]
        public async Task RemoveMod(string userInput)
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can remove moderators.");
                return;
            }

            var userId = ResolveUserId(userInput);
            if (userId == null)
            {
                await ctx.Message.ReplyAsync("Invalid user. Mention them or provide their ID.");
                return;
            }

            GlobalServices.PermissionService.RemoveMod(userId.Value);
            await ctx.Message.ReplyAsync($"User <@{userId}> is no longer a moderator.");
        }

        [Command("removemod")]
        public async Task RemoveModNoArg()
        {
            await Context.Message.ReplyAsync("Usage: !removemod <@mention or userId>");
        }
        
        [Command("admins")]
        public async Task ListAdmins()
        {
            var list = GlobalServices.PermissionService.Data.Admins;

            if (list.Count == 0)
            {
                await Context.Message.ReplyAsync("There are no admins.");
                return;
            }

            var names = list.Select(id => $"<@{id}>");
            await Context.Message.ReplyAsync("Admins:\n" + string.Join("\n", names));
        }
        [Command("mods")]
        public async Task ListMods()
        {
            var list = GlobalServices.PermissionService.Data.Mods;

            if (list.Count == 0)
            {
                await Context.Message.ReplyAsync("There are no moderators.");
                return;
            }

            var names = list.Select(id => $"<@{id}>");
            await Context.Message.ReplyAsync("Moderators:\n" + string.Join("\n", names));
        }

        [Command("link")]
        public async Task LinkPlayer(string userInput, [CommandParameter(Remainder = true)] string? battletagArg = null)
        {
            var ctx = this.Context;
            var mentionedUserId = ResolveUserId(userInput);

            ulong targetUserId;
            string? battletag;

            if (mentionedUserId != null)
            {
                if (!GlobalServices.PermissionService.IsModeratorOrAdmin(ctx.User.Id))
                {
                    await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                        EmbedFactory.Error("Forbidden", "Only admins and moderators can link other players.")]));
                    return;
                }

                targetUserId = mentionedUserId.Value;
                battletag = battletagArg;
            }
            else
            {
                targetUserId = ctx.User.Id;
                battletag = userInput;
            }

            if (string.IsNullOrWhiteSpace(battletag))
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Usage", "`!link Name#1234` to link yourself, or `!link @user Name#1234` (mod/admin) to link someone else.")]));
                return;
            }

            var result = await GlobalServices.SiteApiService.LinkDiscordAsync(targetUserId, battletag.Trim());

            if (result.IsUnavailable)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Site unavailable", "Could not reach warcraftlegacies.com — please try again in a minute.")]));
                return;
            }

            if (result.IsNotFound)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("No such player", $"No player `{battletag.Trim()}` on the site yet — have they uploaded a replay?")]));
                return;
            }

            var link = result.Value!;
            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Success("Linked", $"Linked <@{targetUserId}> to **{link.Battletag}** ({link.DisplayName}).")]));
        }

        [Command("unlink")]
        public async Task UnlinkPlayer(string? userInput = null)
        {
            var ctx = this.Context;

            ulong targetUserId;
            if (userInput != null)
            {
                if (!GlobalServices.PermissionService.IsModeratorOrAdmin(ctx.User.Id))
                {
                    await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                        EmbedFactory.Error("Forbidden", "Only admins and moderators can unlink other players.")]));
                    return;
                }

                var resolved = ResolveUserId(userInput);
                if (resolved == null)
                {
                    await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                        EmbedFactory.Warning("Usage", "`!unlink` to unlink yourself, or `!unlink @user` (mod/admin) to unlink someone else.")]));
                    return;
                }

                targetUserId = resolved.Value;
            }
            else
            {
                targetUserId = ctx.User.Id;
            }

            var result = await GlobalServices.SiteApiService.UnlinkDiscordAsync(targetUserId);

            if (result.IsUnavailable)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Site unavailable", "Could not reach warcraftlegacies.com — please try again in a minute.")]));
                return;
            }

            var unlink = result.Value!;
            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                unlink.Unlinked
                    ? EmbedFactory.Success("Unlinked", $"<@{targetUserId}> is no longer linked to a site profile.")
                    : EmbedFactory.Info("Nothing to do", $"<@{targetUserId}> was not linked to any site profile.")]));
        }

        [Command("status")]
        public async Task SiteStatus()
        {
            var ctx = this.Context;

            if (!GlobalServices.PermissionService.IsModeratorOrAdmin(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Forbidden", "Only admins and moderators can check site status.")]));
                return;
            }

            var health = await GlobalServices.SiteApiService.GetHealthAsync();

            if (health == null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Site unreachable", "Could not connect to warcraftlegacies.com.")]));
                return;
            }

            bool ok = health.Status == "ok";
            int ocrPct = health.OcrMonthlyCap > 0
                ? (int)Math.Round(health.OcrProviderCallsThisMonth * 100.0 / health.OcrMonthlyCap)
                : 0;

            string desc =
                $"**DB:** {health.Db}\n" +
                $"**Season:** {health.SeasonKey}\n\n" +
                $"**Discord ranked matches:** {health.DiscordRankedMatches}\n" +
                $"**Public ranked matches:** {health.PublicRankedMatches}\n\n" +
                $"**OCR this month:** {health.OcrProviderCallsThisMonth} / {health.OcrMonthlyCap} ({ocrPct}%)";

            var embed = ok
                ? EmbedFactory.Success("Site status — OK", desc)
                : EmbedFactory.Warning("Site status — degraded", desc);

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([embed]));
        }

        [Command("recompute")]
        public async Task Recompute(string pool = "discord")
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!GlobalServices.PermissionService.IsModeratorOrAdmin(caller))
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Forbidden", "Only admins and moderators can trigger a rating recompute.")]));
                return;
            }

            pool = pool.Trim().ToLowerInvariant();
            if (pool != "discord" && pool != "public")
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Warning("Invalid pool", "Usage: `!recompute [discord|public]`")]));
                return;
            }

            var (result, error) = await GlobalServices.SiteApiService.RecomputeRatingsAsync(pool);

            if (error != null)
            {
                await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                    EmbedFactory.Error("Recompute failed", error)]));
                return;
            }

            string desc =
                $"**Pool:** {result!.RatingPool} — **Season:** {result.SeasonKey}\n" +
                $"Matches replayed: **{result.MatchesReplayed}**\n" +
                $"Events created: **{result.EventsCreated}**\n" +
                $"Player ratings updated: **{result.PlayerRatingsUpdated}**\n" +
                $"_(deleted {result.DeletedRatingEvents} old events, {result.DeletedPlayerRatings} old ratings)_";

            await ctx.Message.ReplyAsync(new ReplyMessageProperties().WithEmbeds([
                EmbedFactory.Success("Rating recompute complete", desc)]));
        }
    }
}
