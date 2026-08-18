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

    public record PlayerSearchResult(
        string PlayerKey,
        string DisplayName,
        string Battletag,
        int Rating,
        double Sigma,
        int MatchesCount,
        int WinsCount,
        int LossesCount,
        double Winrate,
        string ProfileUrl
    );

    public record LinkResult(
        bool Linked,
        long DiscordUserId,
        string PlayerKey,
        string DisplayName,
        string Battletag
    );

    public record UnlinkResult(
        bool Unlinked,
        long DiscordUserId
    );

    /// <summary>
    /// Outcome of a site call. Distinguishes a genuine "not found" (a real 404 the
    /// site meant) from the site being unreachable or erroring, so callers never
    /// tell a user "not found" when the truth is "we could not reach the site".
    /// </summary>
    public enum SiteCallStatus { Ok, NotFound, Unavailable, Conflict }

    public record SiteResult<T>(SiteCallStatus Status, T? Value)
    {
        public bool IsOk => Status == SiteCallStatus.Ok;
        public bool IsNotFound => Status == SiteCallStatus.NotFound;
        public bool IsUnavailable => Status == SiteCallStatus.Unavailable;
        // The site refused because the target profile is already linked to someone else.
        public bool IsConflict => Status == SiteCallStatus.Conflict;

        public static SiteResult<T> Ok(T value) => new(SiteCallStatus.Ok, value);
        public static SiteResult<T> NotFound() => new(SiteCallStatus.NotFound, default);
        public static SiteResult<T> Unavailable() => new(SiteCallStatus.Unavailable, default);
        public static SiteResult<T> Conflict() => new(SiteCallStatus.Conflict, default);
    }

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

    public record SiteHealthResult(
        string Status,
        string Db,
        string SeasonKey,
        int DiscordRankedMatches,
        int PublicRankedMatches,
        int OcrProviderCallsThisMonth,
        int OcrMonthlyCap
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

        /// <summary>
        /// Fuzzy player search for `!stats &lt;name&gt;` / `!compare`. An empty result
        /// list is a valid Ok outcome ("nobody matched"); a transport/HTTP failure is
        /// Unavailable so the caller can say so instead of "not found".
        /// </summary>
        public async Task<SiteResult<IReadOnlyList<PlayerSearchResult>>> SearchPlayersAsync(
            string query, string pool = "discord")
        {
            var q = Uri.EscapeDataString(query.Trim());
            using var req = new HttpRequestMessage(HttpMethod.Get,
                $"{baseUrl}/api/bot/players/search?q={q}&pool={pool}");
            AddAuth(req);

            try
            {
                using var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                {
                    LogSiteFailure("players/search", resp.StatusCode);
                    return SiteResult<IReadOnlyList<PlayerSearchResult>>.Unavailable();
                }

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<PlayerSearchDto>(body, JsonOpts);
                var results = (dto?.Results ?? new List<PlayerSearchEntryDto>())
                    .Select(e => new PlayerSearchResult(
                        e.PlayerKey, e.DisplayName, e.Battletag, e.Rating, e.Sigma,
                        e.MatchesCount, e.WinsCount, e.LossesCount, e.Winrate, e.ProfileUrl))
                    .ToList();

                return SiteResult<IReadOnlyList<PlayerSearchResult>>.Ok(results);
            }
            catch (Exception ex)
            {
                LogSiteFailure("players/search", ex);
                return SiteResult<IReadOnlyList<PlayerSearchResult>>.Unavailable();
            }
        }

        /// <summary>
        /// "About me" lookup for a bare `!stats`. Returns NotFound only for the site's
        /// own 404 (discord_user_not_linked); any other failure is Unavailable.
        /// </summary>
        public async Task<SiteResult<PlayerRatingResult>> GetPlayerByDiscordAsync(
            ulong discordUserId, string pool = "discord")
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                $"{baseUrl}/api/bot/players/by-discord/{discordUserId}?pool={pool}");
            AddAuth(req);

            try
            {
                using var resp = await http.SendAsync(req);

                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return SiteResult<PlayerRatingResult>.NotFound();

                if (!resp.IsSuccessStatusCode)
                {
                    LogSiteFailure("players/by-discord", resp.StatusCode);
                    return SiteResult<PlayerRatingResult>.Unavailable();
                }

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<PlayerRatingDto>(body, JsonOpts);
                if (dto is null)
                    return SiteResult<PlayerRatingResult>.Unavailable();

                return SiteResult<PlayerRatingResult>.Ok(new PlayerRatingResult(
                    dto.PlayerKey, dto.DisplayName, dto.Rating, dto.Sigma,
                    dto.MatchesCount, dto.WinsCount, dto.LossesCount, dto.Winrate, dto.ProfileUrl));
            }
            catch (Exception ex)
            {
                LogSiteFailure("players/by-discord", ex);
                return SiteResult<PlayerRatingResult>.Unavailable();
            }
        }

        /// <summary>
        /// Link a Discord user to a site battletag. NotFound = battletag_not_found.
        /// </summary>
        public async Task<SiteResult<LinkResult>> LinkDiscordAsync(ulong discordUserId, string battletag, bool requireUnlinked = false)
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, $"{baseUrl}/api/bot/players/link");
            AddAuth(req);
            req.Content = JsonBody(new { discord_user_id = discordUserId, battletag, require_unlinked = requireUnlinked });

            try
            {
                using var resp = await http.SendAsync(req);

                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return SiteResult<LinkResult>.NotFound();

                // Self-service claim of a profile already linked to someone else (site guard).
                if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
                    return SiteResult<LinkResult>.Conflict();

                if (!resp.IsSuccessStatusCode)
                {
                    LogSiteFailure("players/link", resp.StatusCode);
                    return SiteResult<LinkResult>.Unavailable();
                }

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<LinkDto>(body, JsonOpts);
                if (dto is null)
                    return SiteResult<LinkResult>.Unavailable();

                return SiteResult<LinkResult>.Ok(new LinkResult(
                    dto.Linked, dto.DiscordUserId, dto.PlayerKey, dto.DisplayName, dto.Battletag));
            }
            catch (Exception ex)
            {
                LogSiteFailure("players/link", ex);
                return SiteResult<LinkResult>.Unavailable();
            }
        }

        public async Task<SiteResult<UnlinkResult>> UnlinkDiscordAsync(ulong discordUserId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/bot/players/unlink");
            AddAuth(req);
            req.Content = JsonBody(new { discord_user_id = discordUserId });

            try
            {
                using var resp = await http.SendAsync(req);

                if (!resp.IsSuccessStatusCode)
                {
                    LogSiteFailure("players/unlink", resp.StatusCode);
                    return SiteResult<UnlinkResult>.Unavailable();
                }

                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<UnlinkDto>(body, JsonOpts);
                if (dto is null)
                    return SiteResult<UnlinkResult>.Unavailable();

                return SiteResult<UnlinkResult>.Ok(new UnlinkResult(dto.Unlinked, dto.DiscordUserId));
            }
            catch (Exception ex)
            {
                LogSiteFailure("players/unlink", ex);
                return SiteResult<UnlinkResult>.Unavailable();
            }
        }

        private static StringContent JsonBody(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        private static void LogSiteFailure(string endpoint, System.Net.HttpStatusCode code)
            => Console.WriteLine($"[SiteApi] {endpoint} returned {(int)code} {code}");

        private static void LogSiteFailure(string endpoint, Exception ex)
            => Console.WriteLine($"[SiteApi] {endpoint} failed: {ex.Message}");

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

        public async Task<SiteHealthResult?> GetHealthAsync()
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/bot/health");
            AddAuth(req);

            try
            {
                using var resp = await http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<HealthDto>(body, JsonOpts);
                if (dto is null) return null;

                return new SiteHealthResult(
                    dto.Status, dto.Db, dto.SeasonKey,
                    dto.DiscordRankedMatches, dto.PublicRankedMatches,
                    dto.OcrProviderCallsThisMonth, dto.OcrMonthlyCap);
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

        private sealed class PlayerSearchDto
        {
            [JsonPropertyName("results")] public List<PlayerSearchEntryDto> Results { get; set; } = [];
        }

        private sealed class PlayerSearchEntryDto
        {
            [JsonPropertyName("player_key")] public string PlayerKey { get; set; } = "";
            [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
            [JsonPropertyName("battletag")] public string Battletag { get; set; } = "";
            [JsonPropertyName("rating")] public int Rating { get; set; }
            [JsonPropertyName("sigma")] public double Sigma { get; set; }
            [JsonPropertyName("matches_count")] public int MatchesCount { get; set; }
            [JsonPropertyName("wins_count")] public int WinsCount { get; set; }
            [JsonPropertyName("losses_count")] public int LossesCount { get; set; }
            [JsonPropertyName("winrate")] public double Winrate { get; set; }
            [JsonPropertyName("profile_url")] public string ProfileUrl { get; set; } = "";
        }

        private sealed class LinkDto
        {
            [JsonPropertyName("linked")] public bool Linked { get; set; }
            [JsonPropertyName("discord_user_id")] public long DiscordUserId { get; set; }
            [JsonPropertyName("player_key")] public string PlayerKey { get; set; } = "";
            [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
            [JsonPropertyName("battletag")] public string Battletag { get; set; } = "";
        }

        private sealed class UnlinkDto
        {
            [JsonPropertyName("unlinked")] public bool Unlinked { get; set; }
            [JsonPropertyName("discord_user_id")] public long DiscordUserId { get; set; }
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

        private sealed class HealthDto
        {
            [JsonPropertyName("status")] public string Status { get; set; } = "";
            [JsonPropertyName("db")] public string Db { get; set; } = "";
            [JsonPropertyName("season_key")] public string SeasonKey { get; set; } = "";
            [JsonPropertyName("discord_ranked_matches")] public int DiscordRankedMatches { get; set; }
            [JsonPropertyName("public_ranked_matches")] public int PublicRankedMatches { get; set; }
            [JsonPropertyName("ocr_provider_calls_this_month")] public int OcrProviderCallsThisMonth { get; set; }
            [JsonPropertyName("ocr_monthly_cap")] public int OcrMonthlyCap { get; set; }
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
