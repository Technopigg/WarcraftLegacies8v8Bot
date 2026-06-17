using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegaciesBot.Services
{
    public record PlayerRatingResult(
        string PlayerKey,
        string DisplayName,
        int Rating,
        double Sigma,
        int MatchesCount,
        int WinsCount,
        int LossesCount,
        double Winrate,
        string ProfileUrl
    );

    public record LeaderboardEntry(
        int Rank,
        string PlayerKey,
        string DisplayName,
        int Rating,
        int MatchesCount,
        int WinsCount,
        int LossesCount,
        double Winrate,
        string ProfileUrl
    );

    public record LeaderboardResult(
        string Pool,
        string SeasonKey,
        IReadOnlyList<LeaderboardEntry> Entries
    );

    public record RecentMatchParticipant(
        string Battletag,
        string PlayerKey,
        bool IsWinner,
        string? Faction
    );

    public record RecentMatch(
        string PublicId,
        string? PlayedAt,
        string RatingPool,
        IReadOnlyList<RecentMatchParticipant> TeamA,
        IReadOnlyList<RecentMatchParticipant> TeamB,
        string MatchUrl
    );

    public record RecentMatchesResult(
        string SeasonKey,
        IReadOnlyList<RecentMatch> Matches
    );

    public record RecomputeResult(
        string SeasonKey,
        string RatingPool,
        string AlgorithmVersion,
        int DeletedPlayerRatings,
        int DeletedRatingEvents,
        int MatchesReplayed,
        int EventsCreated,
        int PlayerRatingsUpdated
    );

    public class SiteApiService(HttpClient http, string baseUrl, string apiToken)
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        public async Task<PlayerRatingResult?> GetPlayerAsync(string playerKey, string pool = "discord")
        {
            var key = Uri.EscapeDataString(playerKey.Trim().ToLowerInvariant());
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/bot/players/{key}?pool={pool}");
            AddAuth(req);

            try
            {
                using var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<PlayerRatingDto>(body, JsonOpts);
                if (dto is null) return null;

                return new PlayerRatingResult(
                    dto.PlayerKey, dto.DisplayName, dto.Rating, dto.Sigma,
                    dto.MatchesCount, dto.WinsCount, dto.LossesCount, dto.Winrate, dto.ProfileUrl);
            }
            catch
            {
                return null;
            }
        }

        public async Task<LeaderboardResult?> GetLeaderboardAsync(string pool = "discord", int limit = 20)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/bot/ratings?pool={pool}&limit={limit}");
            AddAuth(req);

            try
            {
                using var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<LeaderboardDto>(body, JsonOpts);
                if (dto is null) return null;

                var entries = dto.Entries.Select(e => new LeaderboardEntry(
                    e.Rank, e.PlayerKey, e.DisplayName, e.Rating,
                    e.MatchesCount, e.WinsCount, e.LossesCount, e.Winrate, e.ProfileUrl))
                    .ToList();

                return new LeaderboardResult(dto.Pool, dto.SeasonKey, entries);
            }
            catch
            {
                return null;
            }
        }

        public async Task<RecentMatchesResult?> GetRecentMatchesAsync(string pool = "discord", int limit = 5)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                $"{baseUrl}/api/bot/matches?pool={pool}&limit={limit}");
            AddAuth(req);

            try
            {
                using var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<RecentMatchesDto>(body, JsonOpts);
                if (dto is null) return null;

                var matches = dto.Matches.Select(m => new RecentMatch(
                    m.PublicId,
                    m.PlayedAt,
                    m.RatingPool,
                    m.TeamA.Select(p => new RecentMatchParticipant(p.Battletag, p.PlayerKey, p.IsWinner, p.Faction)).ToList(),
                    m.TeamB.Select(p => new RecentMatchParticipant(p.Battletag, p.PlayerKey, p.IsWinner, p.Faction)).ToList(),
                    m.MatchUrl
                )).ToList();

                return new RecentMatchesResult(dto.SeasonKey, matches);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(RecomputeResult? Result, string? Error)> RecomputeRatingsAsync(string pool = "discord")
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/bot/ratings/recompute?pool={pool}");
            AddAuth(req);
            req.Content = new StringContent("");

            try
            {
                using var resp = await http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var errDto = JsonSerializer.Deserialize<ErrorDto>(body, JsonOpts);
                    return (null, errDto?.Detail ?? resp.ReasonPhrase ?? "Unknown error");
                }

                var dto = JsonSerializer.Deserialize<RecomputeDto>(body, JsonOpts);
                if (dto is null) return (null, "Empty response");

                return (new RecomputeResult(
                    dto.SeasonKey, dto.RatingPool, dto.AlgorithmVersion,
                    dto.DeletedPlayerRatings, dto.DeletedRatingEvents,
                    dto.MatchesReplayed, dto.EventsCreated, dto.PlayerRatingsUpdated), null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        private void AddAuth(HttpRequestMessage req)
        {
            if (!string.IsNullOrEmpty(apiToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }

        private sealed class PlayerRatingDto
        {
            [JsonPropertyName("player_key")] public string PlayerKey { get; set; } = "";
            [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
            [JsonPropertyName("rating")] public int Rating { get; set; }
            [JsonPropertyName("sigma")] public double Sigma { get; set; }
            [JsonPropertyName("matches_count")] public int MatchesCount { get; set; }
            [JsonPropertyName("wins_count")] public int WinsCount { get; set; }
            [JsonPropertyName("losses_count")] public int LossesCount { get; set; }
            [JsonPropertyName("winrate")] public double Winrate { get; set; }
            [JsonPropertyName("profile_url")] public string ProfileUrl { get; set; } = "";
        }

        private sealed class LeaderboardDto
        {
            [JsonPropertyName("pool")] public string Pool { get; set; } = "";
            [JsonPropertyName("season_key")] public string SeasonKey { get; set; } = "";
            [JsonPropertyName("entries")] public List<LeaderboardEntryDto> Entries { get; set; } = [];
        }

        private sealed class LeaderboardEntryDto
        {
            [JsonPropertyName("rank")] public int Rank { get; set; }
            [JsonPropertyName("player_key")] public string PlayerKey { get; set; } = "";
            [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
            [JsonPropertyName("rating")] public int Rating { get; set; }
            [JsonPropertyName("matches_count")] public int MatchesCount { get; set; }
            [JsonPropertyName("wins_count")] public int WinsCount { get; set; }
            [JsonPropertyName("losses_count")] public int LossesCount { get; set; }
            [JsonPropertyName("winrate")] public double Winrate { get; set; }
            [JsonPropertyName("profile_url")] public string ProfileUrl { get; set; } = "";
        }

        private sealed class RecomputeDto
        {
            [JsonPropertyName("season_key")] public string SeasonKey { get; set; } = "";
            [JsonPropertyName("rating_pool")] public string RatingPool { get; set; } = "";
            [JsonPropertyName("algorithm_version")] public string AlgorithmVersion { get; set; } = "";
            [JsonPropertyName("deleted_player_ratings")] public int DeletedPlayerRatings { get; set; }
            [JsonPropertyName("deleted_rating_events")] public int DeletedRatingEvents { get; set; }
            [JsonPropertyName("matches_replayed")] public int MatchesReplayed { get; set; }
            [JsonPropertyName("events_created")] public int EventsCreated { get; set; }
            [JsonPropertyName("player_ratings_updated")] public int PlayerRatingsUpdated { get; set; }
        }

        private sealed class ErrorDto
        {
            [JsonPropertyName("detail")] public string? Detail { get; set; }
        }

        private sealed class RecentMatchesDto
        {
            [JsonPropertyName("season_key")] public string SeasonKey { get; set; } = "";
            [JsonPropertyName("matches")] public List<RecentMatchDto> Matches { get; set; } = [];
        }

        private sealed class RecentMatchDto
        {
            [JsonPropertyName("public_id")] public string PublicId { get; set; } = "";
            [JsonPropertyName("played_at")] public string? PlayedAt { get; set; }
            [JsonPropertyName("rating_pool")] public string RatingPool { get; set; } = "";
            [JsonPropertyName("team_a")] public List<ParticipantDto> TeamA { get; set; } = [];
            [JsonPropertyName("team_b")] public List<ParticipantDto> TeamB { get; set; } = [];
            [JsonPropertyName("match_url")] public string MatchUrl { get; set; } = "";
        }

        private sealed class ParticipantDto
        {
            [JsonPropertyName("battletag")] public string Battletag { get; set; } = "";
            [JsonPropertyName("player_key")] public string PlayerKey { get; set; } = "";
            [JsonPropertyName("is_winner")] public bool IsWinner { get; set; }
            [JsonPropertyName("faction")] public string? Faction { get; set; }
        }
    }
}
