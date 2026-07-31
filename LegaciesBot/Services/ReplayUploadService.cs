using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegaciesBot.Services
{
    public class ReplayUploadService
    {
        private readonly HttpClient _http;
        private readonly string _siteBaseUrl;
        private readonly string _apiToken;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ReplayUploadService(HttpClient http, string siteBaseUrl, string apiToken = "")
        {
            _http = http;
            _siteBaseUrl = siteBaseUrl.TrimEnd('/');
            _apiToken = apiToken;
        }

        public async Task<ReplayUploadResult> UploadAsync(byte[] data, string filename, DiscordUploadContext? discord = null)
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentLength = data.Length;
            content.Headers.TryAddWithoutValidation("X-Original-Filename", Uri.EscapeDataString(filename));
            content.Headers.TryAddWithoutValidation("X-Rating-Pool", "discord");

            if (discord != null)
            {
                content.Headers.TryAddWithoutValidation("X-Discord-Guild-Id", discord.GuildId.ToString());
                content.Headers.TryAddWithoutValidation("X-Discord-Channel-Id", discord.ChannelId.ToString());
                content.Headers.TryAddWithoutValidation("X-Discord-Message-Id", discord.MessageId.ToString());
                content.Headers.TryAddWithoutValidation("X-Discord-Attachment-Id", discord.AttachmentId.ToString());
                content.Headers.TryAddWithoutValidation("X-Discord-Uploader-Id", discord.UploaderId.ToString());
                content.Headers.TryAddWithoutValidation("X-Bot-Version", discord.BotVersion);
            }

            // The rating pool override and the six X-Discord-* dedup keys above are
            // privileged: the site accepts them only from a request carrying the bot
            // token, and drops them from anyone else with a warning in its log. Sent
            // without it, every header this method sets was silently discarded.
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_siteBaseUrl}/api/replays/uploads")
            {
                Content = content,
            };
            if (!string.IsNullOrEmpty(_apiToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
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
                    RankedExclusionReason = payload?.Match?.RankedExclusionReason,
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
        public string? RankedExclusionReason { get; init; }
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

    public record DiscordUploadContext(
        ulong GuildId,
        ulong ChannelId,
        ulong MessageId,
        ulong AttachmentId,
        ulong UploaderId,
        string BotVersion = "1.0"
    );

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

        [JsonPropertyName("ranked_exclusion_reason")]
        public string? RankedExclusionReason { get; set; }
    }
}
