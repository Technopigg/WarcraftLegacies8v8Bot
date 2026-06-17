using LegaciesBot.Core;
using LegaciesBot.Config;
using LTeam = LegaciesBot.Core.Team;
using NetCord.Rest;

namespace LegaciesBot.Services
{
    public class GameService
    {
        private readonly List<Game> _games = new();

        private readonly IGatewayClient _client;
        private readonly IMatchHistoryService _matchHistoryService;
        private readonly IEloService _eloService;
        private readonly IFactionAssignmentService _factionAssignment;
        private readonly IFactionRegistry _factionRegistry;
        private readonly IDefaultPreferences _defaultPreferences;

        private readonly DraftEngine _draftEngine;

        private int _nextGameId = 1;

        private static ulong GuildId => DiscordConfig.GuildId;

        public IGatewayClient Client => _client;

        public GameService(
            IGatewayClient client,
            IMatchHistoryService matchHistoryService,
            IEloService eloService,
            IFactionAssignmentService factionAssignment,
            IFactionRegistry factionRegistry,
            IDefaultPreferences defaultPreferences,
            Random? rng = null)
        {
            _client = client;
            _matchHistoryService = matchHistoryService;
            _eloService = eloService;
            _factionAssignment = factionAssignment;
            _factionRegistry = factionRegistry;
            _defaultPreferences = defaultPreferences;

            _draftEngine = new DraftEngine(factionAssignment, rng);
        }

        public Game CreatePendingGameIfMissing(Lobby lobby)
        {
            var existing = _games.FirstOrDefault(g => g.Lobby == lobby && !g.Finished);
            if (existing != null)
                return existing;

            var game = new Game
            {
                Id = _nextGameId++,
                Lobby = lobby,
                IsActive = false
            };

            lobby.GameNumber = game.Id;
            lobby.IsLocked = true;

            _games.Add(game);
            return game;
        }

        public async Task StartDraft(Lobby lobby, ulong channelId)
        {
            if (lobby.Players.Count != 16)
                throw new ArgumentException("Draft requires exactly 16 players.");

            if (lobby.IsCaptainDraft)
                return;

            var game = CreatePendingGameIfMissing(lobby);

            foreach (var player in lobby.Players)
            {
                if (!player.FactionPreferences.Any())
                    player.FactionPreferences = _defaultPreferences.Factions.ToList();
            }

            var (teamA, teamB) = _draftEngine.RunDraft(lobby);

            lobby.TeamA = teamA;
            lobby.TeamB = teamB;
            lobby.DraftStarted = true;
            lobby.IsLocked = true;

            var channel = await _client.GetTextChannelAsync(channelId);
            if (channel != null)
            {
                var aLines = teamA.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]");
                var bLines = teamB.Players.Select(p => $"• {p.DisplayName()} [{p.AssignedFaction}]");
                string desc = $"**Team A**\n{string.Join("\n", aLines)}\n\n**Team B**\n{string.Join("\n", bLines)}";
                var embed = EmbedFactory.Success($"Draft Complete — Game #{game.Id}", desc);
                await channel.SendMessageAsync(new MessageProperties().WithEmbeds([embed]));
            }

            _factionAssignment.AssignFactionsForGame(teamA, teamB, null, null);

            game.TeamA = teamA;
            game.TeamB = teamB;
            game.StartedAt = DateTime.UtcNow;
            game.IsActive = true;
        }

        public async Task StartCaptainDraft(Lobby lobby, ulong channelId)
        {
            if (lobby.Players.Count != 16)
                throw new ArgumentException("Draft requires exactly 16 players.");

            if (!lobby.IsCaptainDraft)
                return;

            var game = CreatePendingGameIfMissing(lobby);

            lobby.GameNumber = game.Id;

            var draftRole = await _client.CreateRoleAsync(GuildId, $"Draft #{lobby.GameNumber} Players");
            lobby.DraftRoleId = draftRole.Id;

            foreach (var player in lobby.Players)
                await _client.AddRoleToMemberAsync(GuildId, player.DiscordId, draftRole.Id);

            lobby.DraftStarted = true;
            lobby.IsLocked = true;

            var channel = await _client.GetTextChannelAsync(channelId);
            if (channel != null)
            {
                string captainA = lobby.CaptainA.HasValue ? $"<@{lobby.CaptainA.Value}>" : "Captain A";
                string captainB = lobby.CaptainB.HasValue ? $"<@{lobby.CaptainB.Value}>" : "Captain B";

                string msg =
                    $"Draft #{lobby.GameNumber} has begun! <@&{draftRole.Id}>\n" +
                    $"Captains: {captainA} and {captainB}\n" +
                    $"{captainA}, it is your pick. Use !draft <player> or !pass.";

                await channel.SendMessageAsync(msg);
            }
        }

        public void TryAutoStartAfterManualFactions(Lobby lobby)
        {
            if (!lobby.TeamAFactionsLocked || !lobby.TeamBFactionsLocked)
                return;

            var game = CreatePendingGameIfMissing(lobby);

            if (lobby.TeamA != null)
                game.TeamA = lobby.TeamA;
            if (lobby.TeamB != null)
                game.TeamB = lobby.TeamB;

            foreach (var p in lobby.Players)
            {
                if (lobby.ManualFactionAssignments.TryGetValue(p.DiscordId, out var faction))
                    p.AssignedFaction = faction;
            }

            game.StartedAt = DateTime.UtcNow;
            game.IsActive = true;
        }

        public async Task<Dictionary<ulong, int>> SubmitScore(
            Game game,
            int scoreA,
            int scoreB,
            PlayerStatsService stats)
        {
            game.ScoreA = scoreA;
            game.ScoreB = scoreB;
            game.Finished = true;
            game.IsActive = false;
            game.FinishedAt = DateTime.UtcNow;

            MatchResult result = scoreA > scoreB ? MatchResult.TeamAWin
                : scoreB > scoreA ? MatchResult.TeamBWin
                : MatchResult.Draw;

            // Season 5: local Elo is disabled — ratings come from the site via replay upload.
            var changes = new Dictionary<ulong, int>();

            UpdateFactionStats(game.TeamA, result, stats, isTeamA: true);
            UpdateFactionStats(game.TeamB, result, stats, isTeamA: false);

            _matchHistoryService.RecordMatch(game, scoreA, scoreB, changes);

            await ReleaseDraftAndTeamRoles(game.Lobby);

            game.Lobby.Players.Clear();
            game.Lobby.DraftStarted = false;
            game.Lobby.IsLocked = false;
            game.Lobby.GameNumber = 0;

            return changes;
        }

        public async Task KillGame(Game game)
        {
            game.Finished = true;
            game.IsActive = false;

            await ReleaseDraftAndTeamRoles(game.Lobby);

            game.Lobby.Players.Clear();
            game.Lobby.DraftStarted = false;
            game.Lobby.IsLocked = false;
            game.Lobby.GameNumber = 0;
        }

        private async Task ReleaseDraftAndTeamRoles(Lobby lobby)
        {
            if (!lobby.DraftRoleId.HasValue)
                return;

            var draftRoleId = lobby.DraftRoleId.Value;

            foreach (var player in lobby.Players)
            {
                await _client.RemoveRoleFromMemberAsync(GuildId, player.DiscordId, draftRoleId);
                await _client.RemoveRoleFromMemberAsync(GuildId, player.DiscordId, DiscordConfig.Team1RoleId);
                await _client.RemoveRoleFromMemberAsync(GuildId, player.DiscordId, DiscordConfig.Team2RoleId);
            }

            await _client.DeleteRoleAsync(GuildId, draftRoleId);
            lobby.DraftRoleId = null;
        }

        private void UpdateFactionStats(LTeam team, MatchResult result, PlayerStatsService statsService, bool isTeamA)
        {
            var winResult = isTeamA ? MatchResult.TeamAWin : MatchResult.TeamBWin;

            foreach (var player in team.Players)
            {
                if (string.IsNullOrWhiteSpace(player.AssignedFaction))
                    continue;

                var stats = statsService.GetOrCreate(player.DiscordId);

                if (!stats.FactionHistory.TryGetValue(player.AssignedFaction, out var record))
                {
                    record = new FactionRecord();
                    stats.FactionHistory[player.AssignedFaction] = record;
                }

                if (result == MatchResult.Draw)
                    record.Draws++;
                else if (result == winResult)
                    record.Wins++;
                else
                    record.Losses++;

                statsService.Update(stats);
            }
        }

        public async Task<(Team? team, string? faction)> SubstitutePlayer(
            Game game,
            Player outPlayer,
            Player inPlayer)
        {
            var outId = outPlayer.DiscordId;

            // Determine which team the outgoing player belongs to (null if still undrafted).
            Team? team =
                game.TeamA?.Players.Any(p => p.DiscordId == outId) == true ? game.TeamA :
                game.TeamB?.Players.Any(p => p.DiscordId == outId) == true ? game.TeamB :
                null;

            inPlayer.AssignedFaction = outPlayer.AssignedFaction;

            // Replace in the finalized team roster (active game or post-draft manual-faction phase).
            if (team != null)
            {
                int teamIdx = team.Players.FindIndex(p => p.DiscordId == outId);
                if (teamIdx >= 0)
                    team.Players[teamIdx] = inPlayer;
            }

            // Replace in the lobby player list (authoritative during all phases).
            int lobbyIdx = game.Lobby.Players.FindIndex(p => p.DiscordId == outId);
            if (lobbyIdx >= 0)
                game.Lobby.Players[lobbyIdx] = inPlayer;

            // Replace in captain-draft pick lists if the player was already picked.
            int pickAIdx = game.Lobby.TeamAPicks.IndexOf(outId);
            if (pickAIdx >= 0)
                game.Lobby.TeamAPicks[pickAIdx] = inPlayer.DiscordId;

            int pickBIdx = game.Lobby.TeamBPicks.IndexOf(outId);
            if (pickBIdx >= 0)
                game.Lobby.TeamBPicks[pickBIdx] = inPlayer.DiscordId;

            // Discord role handover.
            if (game.IsActive && team != null)
            {
                ulong teamRoleId = team == game.TeamA ? DiscordConfig.Team1RoleId : DiscordConfig.Team2RoleId;
                await _client.RemoveRoleFromMemberAsync(GuildId, outId, teamRoleId);
                await _client.AddRoleToMemberAsync(GuildId, inPlayer.DiscordId, teamRoleId);
            }
            else if (game.Lobby.DraftRoleId.HasValue)
            {
                // Transfer the captain-draft role during the pick phase.
                await _client.RemoveRoleFromMemberAsync(GuildId, outId, game.Lobby.DraftRoleId.Value);
                await _client.AddRoleToMemberAsync(GuildId, inPlayer.DiscordId, game.Lobby.DraftRoleId.Value);
            }

            return (team, inPlayer.AssignedFaction);
        }

        public List<Game> GetOngoingGames() =>
            _games.Where(g => !g.Finished).ToList();

        public Game? GetGameById(int id)
        {
            return _games.FirstOrDefault(g => g.Id == id);
        }

        public Game? FindGameByPlayer(ulong discordId)
        {
            return _games.FirstOrDefault(g =>
                !g.Finished &&
                (g.TeamA?.Players.Any(p => p.DiscordId == discordId) == true ||
                 g.TeamB?.Players.Any(p => p.DiscordId == discordId) == true ||
                 g.Lobby.Players.Any(p => p.DiscordId == discordId)));
        }
    }
}
