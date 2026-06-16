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
    }
}
