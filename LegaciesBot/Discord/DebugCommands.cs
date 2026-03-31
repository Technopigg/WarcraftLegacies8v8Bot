using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Core;

namespace LegaciesBot.Discord
{
    public class DebugCommands : CommandModule<CommandContext>
    {
        public DebugCommands() { }

        private bool IsAllowed(ulong id) =>
            GlobalServices.PermissionService.IsAdmin(id) ||
            GlobalServices.PermissionService.IsMod(id);

        [Command("debugfill")]
        public async Task DebugFill()
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = GlobalServices.LobbyService.CurrentLobby;
            var registry = GlobalServices.PlayerRegistryService;
            var stats = GlobalServices.PlayerStatsService;

            lobby.Players.Clear();

            for (int i = 1; i <= 16; i++)
            {
                ulong id = (ulong)(100000 + i);

                var player = registry.IsRegistered(id)
                    ? registry.GetPlayer(id)
                    : registry.RegisterPlayer(id, $"Debug{i}");

                stats.GetOrCreate(id);

                lobby.Players.Add(player);
            }

            await ctx.Message.ReplyAsync("Lobby filled with 16 debug players.");
        }

        [Command("debugcaptains")]
        public async Task DebugCaptains()
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = GlobalServices.LobbyService.CurrentLobby;

            lobby.CaptainA = lobby.Players[0].DiscordId;
            lobby.CaptainB = lobby.Players[1].DiscordId;

            await ctx.Message.ReplyAsync(
                $"Debug captains assigned:\nCaptain A: {lobby.Players[0].DisplayName()}\nCaptain B: {lobby.Players[1].DisplayName()}"
            );
        }

        [Command("debugdraft")]
        public async Task DebugDraft()
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = GlobalServices.LobbyService.CurrentLobby;
            var engine = new DraftEngine(GlobalServices.FactionAssignmentService);

            lobby.DraftStarted = true;

            if (lobby.DraftMode != DraftMode.AutoDraft_AutoFaction &&
                lobby.DraftMode != DraftMode.AutoDraft_ManualFaction &&
                lobby.DraftMode != DraftMode.CaptainDraft_AutoFaction &&
                lobby.DraftMode != DraftMode.CaptainDraft_ManualFaction)
            {
                lobby.DraftMode = DraftMode.AutoDraft_AutoFaction;
            }

            var (teamA, teamB) = engine.RunDraft(lobby);

            lobby.TeamA = teamA;
            lobby.TeamB = teamB;

            await ctx.Message.ReplyAsync(
                $"Debug draft complete.\n\n" +
                $"Team A: {string.Join(", ", teamA.Players.Select(p => p.DisplayName()))}\n" +
                $"Team B: {string.Join(", ", teamB.Players.Select(p => p.DisplayName()))}"
            );
        }

        [Command("debugfactions")]
        public async Task DebugFactions()
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = GlobalServices.LobbyService.CurrentLobby;
            var assign = GlobalServices.FactionAssignmentService;

            assign.AssignFactionsForGame(lobby.TeamA, lobby.TeamB, null, null);

            await ctx.Message.ReplyAsync(
                "Debug factions assigned.\n\n" +
                $"Team A: {string.Join(", ", lobby.TeamA.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}\n" +
                $"Team B: {string.Join(", ", lobby.TeamB.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}"
            );
        }

        [Command("debugstart")]
        public async Task DebugStart()
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = GlobalServices.LobbyService.CurrentLobby;

            var game = GlobalServices.GameService.StartGame(lobby, lobby.TeamA, lobby.TeamB);

            await ctx.Message.ReplyAsync(
                $"Debug game started: Game {game.Id}\n\n" +
                $"Team A: {string.Join(", ", game.TeamA.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}\n" +
                $"Team B: {string.Join(", ", game.TeamB.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}"
            );
        }

        [Command("debugclear")]
        public async Task DebugClear()
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = GlobalServices.LobbyService.CurrentLobby;

            lobby.Players.Clear();
            lobby.TeamA = null;
            lobby.TeamB = null;
            lobby.CaptainA = null;
            lobby.CaptainB = null;
            lobby.DraftStarted = false;

            await ctx.Message.ReplyAsync("Lobby cleared.");
        }
    }
}
