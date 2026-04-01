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

        private Lobby? ResolveLobby(int? gameId)
        {
            if (gameId == null)
                return GlobalServices.LobbyService.CurrentLobby;

            var game = GlobalServices.GameService.GetGameById(gameId.Value);
            if (game == null || game.Lobby == null)
                return null;

            if (game.Finished)
                return null;

            return GlobalServices.LobbyService.GetLobbyByGameNumber(gameId.Value);
        }

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

            GlobalServices.GameService.CreatePendingGameIfMissing(lobby);

            await ctx.Message.ReplyAsync("Lobby filled with 16 debug players.");
        }

        [Command("debugcaptains")]
        public async Task DebugCaptains(int? gameId = null)
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = ResolveLobby(gameId);
            if (lobby == null)
            {
                await ctx.Message.ReplyAsync("Invalid or finished game.");
                return;
            }

            if (lobby.Players.Count < 2)
            {
                await ctx.Message.ReplyAsync("Not enough players. Use !debugfill first.");
                return;
            }

            lobby.CaptainA = lobby.Players[0].DiscordId;
            lobby.CaptainB = lobby.Players[1].DiscordId;

            await ctx.Message.ReplyAsync(
                $"Debug captains assigned:\n" +
                $"Captain A: {lobby.Players[0].DisplayName()}\n" +
                $"Captain B: {lobby.Players[1].DisplayName()}"
            );
        }

        [Command("debugdraft")]
        public async Task DebugDraft(int? gameId = null)
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = ResolveLobby(gameId);
            if (lobby == null)
            {
                await ctx.Message.ReplyAsync("Invalid or finished game.");
                return;
            }

            if (lobby.Players.Count < 16)
            {
                await ctx.Message.ReplyAsync($"Lobby has {lobby.Players.Count} players. Use !debugfill first.");
                return;
            }

            if (lobby.TeamAPicks.Count == 0)
                lobby.TeamAPicks = lobby.Players.Take(8).Select(p => p.DiscordId).ToList();

            if (lobby.TeamBPicks.Count == 0)
                lobby.TeamBPicks = lobby.Players.Skip(8).Take(8).Select(p => p.DiscordId).ToList();

            if (lobby.CaptainA == null)
                lobby.CaptainA = lobby.TeamAPicks.First();

            if (lobby.CaptainB == null)
                lobby.CaptainB = lobby.TeamBPicks.First();

            lobby.DraftStarted = true;

            var engine = new DraftEngine(GlobalServices.FactionAssignmentService, new Random(12345));

            try
            {
                var beforeCount = lobby.Players.Count;

                var (teamA, teamB) = engine.RunDraft(lobby);

                var afterCount = lobby.Players.Count;

                lobby.TeamA = teamA;
                lobby.TeamB = teamB;

                await ctx.Message.ReplyAsync(
                    $"Debug draft complete.\n" +
                    $"Lobby players before: {beforeCount}, after: {afterCount}\n\n" +
                    $"Team A: {string.Join(", ", teamA.Players.Select(p => p.DisplayName()))}\n" +
                    $"Team B: {string.Join(", ", teamB.Players.Select(p => p.DisplayName()))}"
                );
            }
            catch (Exception ex)
            {
                await ctx.Message.ReplyAsync(
                    $"Debug draft FAILED: {ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}"
                );
            }
        }

        [Command("debugfactions")]
        public async Task DebugFactions(int? gameId = null)
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = ResolveLobby(gameId);
            if (lobby == null)
            {
                await ctx.Message.ReplyAsync("Invalid or finished game.");
                return;
            }

            if (lobby.TeamA == null || lobby.TeamB == null)
            {
                await ctx.Message.ReplyAsync("Teams not drafted. Use !debugdraft first.");
                return;
            }

            var assign = GlobalServices.FactionAssignmentService;
            assign.AssignFactionsForGame(lobby.TeamA, lobby.TeamB, null, null);

            await ctx.Message.ReplyAsync(
                "Debug factions assigned.\n\n" +
                $"Team A: {string.Join(", ", lobby.TeamA.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}\n" +
                $"Team B: {string.Join(", ", lobby.TeamB.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}"
            );
        }

        [Command("debugstart")]
        public async Task DebugStart(int? gameId = null)
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = ResolveLobby(gameId);
            if (lobby == null)
            {
                await ctx.Message.ReplyAsync("Invalid or finished game.");
                return;
            }

            if (lobby.TeamA == null || lobby.TeamB == null)
            {
                await ctx.Message.ReplyAsync("Teams not drafted. Use !debugdraft first.");
                return;
            }

            var game = GlobalServices.GameService.CreatePendingGameIfMissing(lobby);
            game.TeamA = lobby.TeamA;
            game.TeamB = lobby.TeamB;
            game.IsActive = true;

            await ctx.Message.ReplyAsync(
                $"Debug game started: Game {game.Id}\n\n" +
                $"Team A: {string.Join(", ", game.TeamA.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}\n" +
                $"Team B: {string.Join(", ", game.TeamB.Players.Select(p => $"{p.DisplayName()} ({p.AssignedFaction})"))}"
            );
        }

        [Command("debugstate")]
        public async Task DebugState(int? gameId = null)
        {
            var lobby = ResolveLobby(gameId);
            if (lobby == null)
            {
                await Context.Message.ReplyAsync("Invalid or finished game.");
                return;
            }

            await Context.Message.ReplyAsync(
                $"Players: {lobby.Players.Count}\n" +
                $"TeamA null? {lobby.TeamA == null}\n" +
                $"TeamB null? {lobby.TeamB == null}\n" +
                $"DraftStarted: {lobby.DraftStarted}"
            );
        }

        [Command("debugclear")]
        public async Task DebugClear(int? gameId = null)
        {
            var ctx = this.Context;
            if (!IsAllowed(ctx.User.Id))
            {
                await ctx.Message.ReplyAsync("You do not have permission to use debug commands.");
                return;
            }

            var lobby = ResolveLobby(gameId);
            if (lobby == null)
            {
                await ctx.Message.ReplyAsync("Invalid or finished game.");
                return;
            }

            lobby.Players.Clear();
            lobby.TeamA = null;
            lobby.TeamB = null;
            lobby.CaptainA = null;
            lobby.CaptainB = null;
            lobby.TeamAPicks.Clear();
            lobby.TeamBPicks.Clear();
            lobby.DraftStarted = false;

            await ctx.Message.ReplyAsync("Lobby cleared.");
        }
    }
}
