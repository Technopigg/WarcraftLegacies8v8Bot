using System.Net;
using LegaciesBot.Services;

namespace LegaciesBot.Tests;

/// <summary>
/// The upload has to carry the bot token, or the site throws away everything
/// the request was trying to say.
///
/// X-Rating-Pool and the six X-Discord-* dedup keys are privileged: the site
/// accepts them only from a bot-authenticated request and drops them from
/// anyone else, logging "privileged headers without a valid bot token". The
/// service sent none, so the pool override and the whole Discord dedup key
/// never reached the site — the pool happened to come out right only because
/// the site also infers it from the game name.
/// </summary>
public class ReplayUploadAuthTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? ContentHeaders { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            ContentHeaders = request.Content is null
                ? string.Empty
                : string.Join(";", request.Content.Headers.Select(h => h.Key));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }

    private static (ReplayUploadService service, CapturingHandler handler) Create(string token)
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler);
        return (new ReplayUploadService(http, "https://site.test", token), handler);
    }

    [Fact]
    public async Task TheUploadCarriesTheBotToken()
    {
        var (service, handler) = Create("secret-token");

        await service.UploadAsync([1, 2, 3], "replay.w3g");

        Assert.NotNull(handler.Request);
        Assert.Equal("Bearer", handler.Request!.Headers.Authorization?.Scheme);
        Assert.Equal("secret-token", handler.Request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ThePrivilegedHeadersStillTravelWithIt()
    {
        var (service, handler) = Create("secret-token");
        var discord = new DiscordUploadContext(1, 2, 3, 4, 5);

        await service.UploadAsync([1, 2, 3], "replay.w3g", discord);

        Assert.Contains("X-Rating-Pool", handler.ContentHeaders);
        Assert.Contains("X-Discord-Message-Id", handler.ContentHeaders);
        Assert.Contains("X-Discord-Attachment-Id", handler.ContentHeaders);
        // Sending these without the Authorization header above is what made them
        // meaningless; the two belong together.
        Assert.NotNull(handler.Request!.Headers.Authorization);
    }

    [Fact]
    public async Task NoTokenMeansNoAuthorizationHeaderRatherThanAnEmptyOne()
    {
        var (service, handler) = Create(string.Empty);

        await service.UploadAsync([1, 2, 3], "replay.w3g");

        Assert.Null(handler.Request!.Headers.Authorization);
    }

    [Fact]
    public async Task TheRequestStillGoesToTheUploadEndpoint()
    {
        var (service, handler) = Create("t");

        await service.UploadAsync([1, 2, 3], "replay.w3g");

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://site.test/api/replays/uploads", handler.Request.RequestUri?.ToString());
    }
}
