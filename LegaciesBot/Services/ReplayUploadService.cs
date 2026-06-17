using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegaciesBot.Services
{
    public class ReplayUploadService
    {
        private readonly HttpClient _http;
        private readonly string _siteBaseUrl;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ReplayUploadService(HttpClient http, string siteBaseUrl)
        {
            _http = http;
            _siteBaseUrl = siteBaseUrl.TrimEnd('/');
        }

        public async Task<ReplayUploadResult> UploadAsync(byte[] data, string filename)
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentLength = data.Length;
            content.Headers.TryAddWithoutValidation("X-Original-Filename", Uri.EscapeDataString(filename));
            content.Headers.TryAddWithoutValidation("X-Rating-Pool", "discord");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync($"{_siteBaseUrl}/api/replays/uploads", content);
            }
            catch (Exception ex)
            {
                return new ReplayUploadResult { NetworkError = ex.Message };
            }

            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                return new ReplayUploadResult { RateLimited = true };

            if ((int)response.StatusCode == 413)
                return new ReplayUploadResult { TooLarge = true };

            try
            {
                var payload = JsonSerializer.Deserialize<SiteUploadPayload>(body, _json);
                return new ReplayUploadResult
                {
                    Status = payload?.Status,
                    Reason = payload?.Reason,
                    MatchPublicId = payload?.Match?.PublicId,
                    MatchStatus = payload?.Match?.Status,
                    RatingPool = payload?.Match?.RatingPool,
                    MapVersion = payload?.Match?.MapVersionLabel,
                    PlayersCount = payload?.Match?.PlayersCount
                };
            }
            catch
            {
                return new ReplayUploadResult { NetworkError = $"Unexpected response ({(int)response.StatusCode}): {body[..Math.Min(200, body.Length)]}" };
            }
        }
    }

    public class ReplayUploadResult
    {
        public string? Status { get; init; }
        public string? Reason { get; init; }
        public string? MatchPublicId { get; init; }
        public string? MatchStatus { get; init; }
        public string? RatingPool { get; init; }
        public string? MapVersion { get; init; }
        public int? PlayersCount { get; init; }
        public bool RateLimited { get; init; }
        public bool TooLarge { get; init; }
        public string? NetworkError { get; init; }

        public bool IsSuccess => Status == "created";
        public bool IsDuplicate => Status == "duplicate";
        public bool IsRejected => Status == "rejected";
        public bool HasError => NetworkError != null || RateLimited || TooLarge;
    }

    // Site response shape (CONTRACT.md §1)
    internal class SiteUploadPayload
    {
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public SiteUploadInfo? Upload { get; set; }
        public SiteMatchInfo? Match { get; set; }
    }

    internal class SiteUploadInfo
    {
        public int Id { get; set; }
        public string? Status { get; set; }

        [JsonPropertyName("map_validation_status")]
        public string? MapValidationStatus { get; set; }
    }

    internal class SiteMatchInfo
    {
        [JsonPropertyName("public_id")]
        public string? PublicId { get; set; }

        public string? Status { get; set; }

        [JsonPropertyName("rating_status")]
        public string? RatingStatus { get; set; }

        [JsonPropertyName("rating_pool")]
        public string? RatingPool { get; set; }

        [JsonPropertyName("result_status")]
        public string? ResultStatus { get; set; }

        [JsonPropertyName("players_count")]
        public int? PlayersCount { get; set; }

        [JsonPropertyName("map_version_label")]
        public string? MapVersionLabel { get; set; }
    }
}
