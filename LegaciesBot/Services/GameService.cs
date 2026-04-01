using LegaciesBot.Core;
using LegaciesBot.Config;
using LTeam = LegaciesBot.Core.Team;

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

        private const ulong GuildId = 1218338908216229979;

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
                await channel.SendMessageAsync("=== DRAFT COMPLETE ===\nTeams have been drafted.");

            _factionAssignment.AssignFactionsForGame(teamA, teamB, null, null);

            game.TeamA = teamA;
            game.TeamB = teamB;
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

            bool teamAWon = scoreA > scoreB;

            var changes = _eloService.ApplyTeamResult(
                game.TeamA.Players,
                game.TeamB.Players,
                teamAWon
            );

            UpdateFactionStats(game.TeamA, teamAWon, stats, true);
            UpdateFactionStats(game.TeamB, teamAWon, stats, false);

            _matchHistoryService.RecordMatch(game, scoreA, scoreB, changes);

            if (game.Lobby.DraftRoleId.HasValue)
            {
                var draftRoleId = game.Lobby.DraftRoleId.Value;

                var allPlayers = game.TeamA.Players.Concat(game.TeamB.Players).ToList();

                foreach (var player in allPlayers)
                {
                    await _client.RemoveRoleFromMemberAsync(GuildId, player.DiscordId, draftRoleId);
                    await _client.RemoveRoleFromMemberAsync(GuildId, player.DiscordId, RoleConfig.Team1Role);
                    await _client.RemoveRoleFromMemberAsync(GuildId, player.DiscordId, RoleConfig.Team2Role);
                }

                await _client.DeleteRoleAsync(GuildId, draftRoleId);
                game.Lobby.DraftRoleId = null;
            }

            game.Lobby.Players.Clear();
            game.Lobby.DraftStarted = false;
            game.Lobby.IsLocked = false;
            game.Lobby.GameNumber = 0;

            return changes;
        }

        private void UpdateFactionStats(Team team, bool teamAWon, PlayerStatsService statsService, bool isTeamA)
        {
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

                bool won = isTeamA == teamAWon;

                if (won)
                    record.Wins++;
                else
                    record.Losses++;

                statsService.Update(stats);
            }
        }

        public List<Game> GetOngoingGames() =>
            _games.Where(g => !g.Finished).ToList();
        public Game? GetGameById(int id)
        {
            return _games.FirstOrDefault(g => g.Id == id);
        }

    }
}
