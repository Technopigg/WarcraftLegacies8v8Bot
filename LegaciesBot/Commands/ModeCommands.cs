using NetCord.Services.Commands;
using LegaciesBot.Core;

namespace LegaciesBot.Commands
{
    public class ModeCommands : CommandModule<CommandContext>
    {
        public ModeCommands()
        {
        }

        private bool HasPermission(ulong userId, Lobby lobby)
        {
            if (GlobalServices.PermissionService.IsAdmin(userId) ||
                GlobalServices.PermissionService.IsMod(userId))
                return true;

            return lobby.CaptainA == userId || lobby.CaptainB == userId;
        }

        [Command("mode")]
        public async Task Mode(string input = "show")
        {
            var ctx = this.Context;
            var lobby = GlobalServices.LobbyService.CurrentLobby;
            ulong userId = ctx.User.Id;

            if (input == "show")
            {
                await ctx.Message.ReplyAsync($"Current draft mode: **{lobby.DraftMode}**");
                return;
            }

            if (!HasPermission(userId, lobby))
            {
                await ctx.Message.ReplyAsync("Only captains or admins may change the draft mode.");
                return;
            }

            DraftMode? newMode = input.ToLowerInvariant() switch
            {
                "auto" => DraftMode.AutoDraft_AutoFaction,
                "auto-auto" => DraftMode.AutoDraft_AutoFaction,
                "auto-manual" => DraftMode.AutoDraft_ManualFaction,
                "captain" => DraftMode.CaptainDraft_ManualFaction,
                "captain-manual" => DraftMode.CaptainDraft_ManualFaction,
                "captain-auto" => DraftMode.CaptainDraft_AutoFaction,
                _ => null
            };

            if (newMode == null)
            {
                await ctx.Message.ReplyAsync("Invalid mode. Valid modes: auto, auto-manual, captain, captain-auto, captain-manual.");
                return;
            }

            lobby.DraftMode = newMode.Value;

            await ctx.Message.ReplyAsync($"Draft mode switched to: **{lobby.DraftMode}**");
        }
    }
}
