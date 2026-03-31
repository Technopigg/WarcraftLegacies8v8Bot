using NetCord.Services.Commands;


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

        [Command("addadmin")]
        public async Task AddAdmin()
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can add new admins.");
                return;
            }

            if (ctx.Message.MentionedUsers.Count == 0)
            {
                await ctx.Message.ReplyAsync("Mention a user to promote.");
                return;
            }

            var target = ctx.Message.MentionedUsers[0].Id;

            GlobalServices.PermissionService.AddAdmin(target);

            await ctx.Message.ReplyAsync($"User <@{target}> is now an admin.");
        }

        [Command("addmod")]
        public async Task AddMod()
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can add moderators.");
                return;
            }

            if (ctx.Message.MentionedUsers.Count == 0)
            {
                await ctx.Message.ReplyAsync("Mention a user to promote.");
                return;
            }

            var target = ctx.Message.MentionedUsers[0].Id;

            GlobalServices.PermissionService.AddMod(target);

            await ctx.Message.ReplyAsync($"User <@{target}> is now a moderator.");
        }

        [Command("removeadmin")]
        public async Task RemoveAdmin()
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can remove admins.");
                return;
            }

            if (ctx.Message.MentionedUsers.Count == 0)
            {
                await ctx.Message.ReplyAsync("Mention a user to demote.");
                return;
            }

            var target = ctx.Message.MentionedUsers[0].Id;

            GlobalServices.PermissionService.RemoveAdmin(target);

            await ctx.Message.ReplyAsync($"User <@{target}> is no longer an admin.");
        }

        [Command("removemod")]
        public async Task RemoveMod()
        {
            var ctx = this.Context;
            var caller = ctx.User.Id;

            if (!IsAdmin(caller))
            {
                await ctx.Message.ReplyAsync("Only admins can remove moderators.");
                return;
            }

            if (ctx.Message.MentionedUsers.Count == 0)
            {
                await ctx.Message.ReplyAsync("Mention a user to demote.");
                return;
            }

            var target = ctx.Message.MentionedUsers[0].Id;

            GlobalServices.PermissionService.RemoveMod(target);

            await ctx.Message.ReplyAsync($"User <@{target}> is no longer a moderator.");
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
    }
}
