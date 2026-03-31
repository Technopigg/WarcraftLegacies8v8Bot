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
    }
}
